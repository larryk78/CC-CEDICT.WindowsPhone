using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace CC_CEDICT.WindowsPhone
{
    public class Searcher
    {
        public enum Type { Unknown = 0, English = 1, Pinyin = 2, Hanzi = 4, Ambiguous = English|Pinyin };
        Dictionary dictionary;
        Dictionary<Type, Index> index = new Dictionary<Type, Index>();
        Dictionary<string, bool> pinyin = new Dictionary<string, bool>();
        Regex compoundPinyin;

        public Searcher(Dictionary d, Index english, Index pinyin, Index hanzi)
        {
            DateTime start = DateTime.Now;

            dictionary = d;
            index[Type.English] = english;
            index[Type.Pinyin] = pinyin;
            index[Type.Hanzi] = hanzi;

            // pre-cache all pinyin syllables for lookup
            List<string> syllables = new List<string>();
            foreach (IndexRecord r in index[Type.Pinyin])
            {
                Pinyin p = new Pinyin(r.Key);
                this.pinyin[p.Syllable] = true;
                if (p.Syllable.Length > 1 || p.Syllable == "a" || p.Syllable == "e") // skip random individual characters
                    syllables.Add(p.Syllable);
            }
            
            // create regex for matching compound Pinyin terms, e.g. nihao, pengyou, etc.
            syllables.Sort((a, b) => b.Length.CompareTo(a.Length));
            compoundPinyin = new Regex("^(" + String.Join("[1-5]?|", syllables) + "[1-5]?)+$");

            Debug.WriteLine(String.Format("Searcher initialisation took {0}ms", ((TimeSpan)(DateTime.Now - start)).TotalMilliseconds));
        }

        List<SearchResult> _used = new List<SearchResult>();
        public string LastQuery
        {
            get
            {
                string query = "";
                foreach (SearchResult term in _used)
                {
                    if (query.Length > 0)
                        query += " ";
                    query += term.Type == Type.Pinyin ? term.Pinyin.Original : term.Term;
                }
                return query;
            }
        }

        public bool SmartSearch;
        public List<DictionaryRecord> Search(string query, int minRelevance=1)
        {
            DateTime start = DateTime.Now;
            Debug.WriteLine(String.Format("Searching for: '{0}'", query));

            List<DictionaryRecord> results = new List<DictionaryRecord>();
            SmartSearch = false;
            foreach (IndexRecord.Reference reference in AbstractSearch(query, minRelevance))
            {
                DictionaryRecord result = dictionary[reference.Index];
                result.Relevance = reference.Relevance;
                results.Add(result);
            }

            Debug.WriteLine(String.Format("Actual query: '{0}' took {1}ms. and produced {2} results",
                LastQuery,
                ((TimeSpan)(DateTime.Now - start)).TotalMilliseconds,
                results.Count));

            results.Sort();
            return results;
        }

        public int Total;
        List<IndexRecord.Reference> AbstractSearch(string query, int minRelevance)
        {
            query = query.Trim();
            if (query.Length == 0)
                return new List<IndexRecord.Reference>(); // empty

            SearchResultAggregator aggregator = new SearchResultAggregator();
            List<SearchResult> terms = Tokenize(query, minRelevance);

            _used.Clear();
            List<IndexRecord.Reference> tracker = new List<IndexRecord.Reference>();
            foreach (SearchResult term in terms)
            {
                if (term.Type == Type.Unknown)
                    continue;

                if (tracker.Count == 0)
                    tracker.AddRange(term.Results);
                else
                {
                    List<IndexRecord.Reference> temp = new List<IndexRecord.Reference>(tracker);
                    this.Intersect(ref temp, term.Results);
                    if (temp.Count == 0) // empty set (i.e. destroys results)
                        continue;
                    tracker = temp;
                }

                aggregator.Add(term.Results);
                _used.Add(term);
            }

            if (_used.Count != terms.Count) // not all terms were used (duh!)
                SmartSearch = true;

            List<IndexRecord.Reference> results = aggregator.Results(minRelevance);
            Total = aggregator.Count;
            Debug.WriteLine("Total results (pre-aggregation): " + Total);
            return results;
        }

        List<IndexRecord.Reference> Aggregate(List<SearchResult> results, int minRelevance=0)
        {
            SearchResultAggregator a = new SearchResultAggregator();
            foreach (SearchResult r in results)
                a.Add(r.Results);
            return a.Results(minRelevance);
        }

        struct SearchResult
        {
            public string Term;
            public Pinyin Pinyin;
            public Type Type;
            public List<IndexRecord.Reference> Results;
        }

        List<SearchResult> Tokenize(string query, int minRelevance=0, bool ignoreEnglish=false)
        {
            List<SearchResult> results = new List<SearchResult>();

            // break query into individual terms
            char[] delim = { ' ' };
            foreach (string token in query.ToLower().Split(delim, StringSplitOptions.RemoveEmptyEntries))
            {
                SearchResult result = new SearchResult { Term = token, Type = Type.Unknown };
                List<IndexRecord.Reference> items = new List<IndexRecord.Reference>();

                if (ContainsHanzi(token))
                {
                    if (OnlyHanzi(token)) // full Hanzi token
                    {
                        if ((items = index[Type.Hanzi][token]) != null) // found an exact match
                        {
                            result.Type = Type.Hanzi;
                            result.Term = token;
                            result.Results = items;
                            results.Add(result);
                        }
                        else if (token.Length > 1) // not found, so split and search
                        {
                            // TODO: this is where the intelligent Chinese wordsearch combinatorics come in :)
                            results.AddRange(Tokenize(String.Join(" ", token.ToCharArray()), minRelevance));
                        }
                    }
                    else // some mixture of Hanzi and something else
                    {
                        results.AddRange(Tokenize(String.Join(" ", SegregateHanziNonHanzi(token)), minRelevance));
                    }
                    continue; // completely handled this token
                }

                if (pinyin.ContainsKey(token)) // Pinyin with no tone (check all the tones)
                {
                    List<IndexRecord.Reference> temp;
                    if ((temp = index[Type.Pinyin][token]) != null) // try with no tone
                        items.AddRange(temp);

                    for (int i=1; i<=5; i++) // then go through all the tones
                        if ((temp = index[Type.Pinyin][token + i.ToString()]) != null)
                            this.Unify(ref items, temp);
                    
                    result.Type = Type.Pinyin;
                    result.Pinyin = new Pinyin(token);
                    result.Results = items;
                }
                else if ((items = index[Type.Pinyin][token]) != null) // Pinyin (with tone)?
                {
                    result.Type = Type.Pinyin;
                    result.Pinyin = new Pinyin(token);
                    result.Results = items;
                }
                
                if (!ignoreEnglish && (items = index[Type.English][token]) != null) // English?
                {
                    if (result.Type == Type.Pinyin) // already matched Pinyin
                    {
                        result.Type = Type.Ambiguous;
                        this.Unify(ref result.Results, items);
                    }
                    else // plain English
                    {
                        result.Type = Type.English;
                        result.Results = items;
                    }
                }

                results.Add(result);
                
                Debug.WriteLine(String.Format("Query term: '{0}' is {1} with {2} associated results.",
                    result.Term,
                    result.Type,
                    result.Results == null ? 0 : result.Results.Count));

                // might be compound Pinyin?
                Match match = compoundPinyin.Match(token);
                if (match.Groups.Count > 1 && match.Groups[1].Captures.Count > 1) // yes, it was :)
                {
                    List<SearchResult> temp = new List<SearchResult>();
                    foreach (Capture capture in match.Groups[1].Captures)
                        temp.AddRange(Tokenize(capture.Value, minRelevance, true));
                    if (temp.Count == 0) // compound was a red-herring, e.g. secure -> se cu re
                        continue;
                    else if (result.Type == Type.Unknown)
                        results.AddRange(temp);
                    else
                    {
                        List<IndexRecord.Reference> list = Aggregate(temp, minRelevance);
                        if (list.Count > 0)
                        {
                            //terms.AddRange(temp);
                            this.Unify(ref result.Results, list); // include sub-matches
                        }
                    }
                }
            }

            return results;
        }

        static Regex containsHanziRegex = new Regex("([\\u2600-\\uffff])");
        bool ContainsHanzi(string term) // strictly speaking, implements ContainsHighValueUnicode
        {
            Match match = containsHanziRegex.Match(term);
            return match.Success;
        }

        static Regex onlyHanziRegex = new Regex("^([\\u2600-\\uffff])+$");
        bool OnlyHanzi(string term) // ditto
        {
            Match match = onlyHanziRegex.Match(term);
            return match.Success;
        }

        List<string> SegregateHanziNonHanzi(string term)
        {
            List<string> chunks = new List<string>();
            string hanziChunk = "";
            string otherChunk = "";
            bool lastWasHanzi = false;
            foreach (char c in term.ToCharArray())
            {
                if (OnlyHanzi(c.ToString()))
                {
                    if (!lastWasHanzi && otherChunk.Length > 0)
                    {
                        chunks.Add(otherChunk);
                        otherChunk = "";
                    }
                    hanziChunk += c;
                    lastWasHanzi = true;
                }
                else
                {
                    if (lastWasHanzi && hanziChunk.Length > 0)
                    {
                        chunks.Add(hanziChunk);
                        hanziChunk = "";
                    }
                    otherChunk += c;
                    lastWasHanzi = false;
                }
            }
            return chunks;
        }

        void Intersect<T>(ref List<T> target, List<T> items)
        {
            if (items == null)
                return;
            for (int i = target.Count - 1; i >= 0; i--)
                if (!items.Contains(target[i]))
                    target.RemoveAt(i);
        }

        void Unify(ref List<IndexRecord.Reference> target, List<IndexRecord.Reference> items) // i.e. create a union
        {
            if (items == null)
                return;
            DateTime start = DateTime.Now;
            for (int i = 0; i <= items.Count - 1; i++)
            {
                if (!target.Contains(items[i]))
                {
                    target.Add(items[i]);
                }
                else // it's there but we need to update if the relevance is higher
                {
                    int j = target.IndexOf(items[i]);
                    if (target[j].Relevance < items[i].Relevance)
                        target[j] = items[i];
                }
            }
            Debug.WriteLine(String.Format("(total time spent in Unify: {0}ms.)", ((TimeSpan)(DateTime.Now - start)).TotalMilliseconds));
        }
    }
}
