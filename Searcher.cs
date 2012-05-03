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

        public bool SmartSearch;
        List<SearchTerm> _used = new List<SearchTerm>();
        public string LastQuery
        {
            get
            {
                string query = "";
                foreach (SearchTerm term in _used)
                {
                    if (query.Length > 0)
                        query += " ";
                    query += term.Type == Type.Pinyin ? term.Pinyin.Original : term.Term;
                }
                return query;
            }
        }

        public List<DictionaryRecord> Search(string query, int minRelevance=0)
        {
            DateTime start = DateTime.Now;
            Debug.WriteLine(String.Format("Searching for: '{0}'", query));

            List<DictionaryRecord> results = new List<DictionaryRecord>();
            SmartSearch = false;
            foreach (int i in AbstractSearch(query, minRelevance))
                results.Add(dictionary[i]);

            Debug.WriteLine(String.Format("Actual query: '{0}' took {1}ms. and produced {2} results",
                LastQuery,
                ((TimeSpan)(DateTime.Now - start)).TotalMilliseconds,
                results.Count));

            return results;
        }

        List<int> AbstractSearch(string query, int minRelevance) // TODO: exact matches, in-order matches (esp. compound Pinyin)
        {
            DateTime start = DateTime.Now;
            List<int> results = new List<int>();
            query = query.Trim();
            if (query.Length == 0)
                return results; // empty

            List<SearchTerm> terms = Tokenize(query);
            _used.Clear();
            foreach (SearchTerm term in terms)
            {
                if (term.Type == Type.Unknown || term.Results == null || term.Results.Count == 0)
                    continue;

                if (results.Count == 0) // first time through the loop
                {
                    results.AddRange(term.Results);
                }
                else
                {
                    List<int> temp = new List<int>(results);
                    this.Intersect(ref temp, term.Results);
                    if (temp.Count == 0) // this search term obliterates all results
                        continue; // ignore
                    results = new List<int>(temp);
                }
                _used.Add(term);
            }

            if (_used.Count != terms.Count) // not all terms were used (duh!)
                SmartSearch = true;

            Debug.WriteLine(String.Format("AbstractSearch took {0}ms", ((TimeSpan)(DateTime.Now - start)).TotalMilliseconds));
            return results;
        }

        struct SearchTerm
        {
            public string Term;
            public Pinyin Pinyin;
            public Type Type;
            public Dictionary<int, int> Results;
        }

        List<SearchTerm> Tokenize(string query, bool ignoreEnglish=false)
        {
            List<SearchTerm> terms = new List<SearchTerm>();

            // break query into individual terms
            char[] delim = { ' ' };
            foreach (string token in query.ToLower().Split(delim, StringSplitOptions.RemoveEmptyEntries))
            {
                SearchTerm term = new SearchTerm { Term = token, Type = Type.Unknown };
                Dictionary<int, int> items = new Dictionary<int, int>();

                if (ContainsHanzi(token))
                {
                    if (OnlyHanzi(token)) // full Hanzi token
                    {
                        if ((items = index[Type.Hanzi][token]) != null) // found an exact match
                        {
                            term.Type = Type.Hanzi;
                            term.Term = token;
                            term.Results = items;
                            terms.Add(term);
                        }
                        else if (token.Length > 1) // not found, so split and search
                        {
                            // TODO: this is where the intelligent Chinese wordsearch combinatorics come in :)
                            terms.AddRange(Tokenize(String.Join(" ", token.ToCharArray())));
                        }
                    }
                    else // some mixture of Hanzi and something else
                    {
                        terms.AddRange(Tokenize(String.Join(" ", SegregateHanziNonHanzi(token))));
                    }
                    continue; // completely handled this token
                }

                if (pinyin.ContainsKey(token)) // Pinyin with no tone
                {
                    List<int> temp;
                    if ((temp = index[Type.Pinyin][token]) != null) // try with no tone
                        items.AddRange(temp);

                    for (int i=1; i<=5; i++) // then go through all the tones
                        if ((temp = index[Type.Pinyin][token + i.ToString()]) != null)
                            items.AddRange(temp);
                    
                    term.Type = Type.Pinyin;
                    term.Pinyin = new Pinyin(token);
                    term.Results = items;
                }
                else if ((items = index[Type.Pinyin][token]) != null) // Pinyin (with tone)
                {
                    term.Type = Type.Pinyin;
                    term.Pinyin = new Pinyin(token);
                    term.Results = items;
                }
                else // could be a compound Pinyin term?
                {
                    Match match = compoundPinyin.Match(token);
                    if (match.Groups.Count > 1) // yes, it was :)
                    {
                        foreach (Capture capture in match.Groups[1].Captures)
                            terms.AddRange(Tokenize(capture.Value, true));
                        continue;
                    }
                }

                if (!ignoreEnglish && (items = index[Type.English][token]) != null) // English
                {
                    if (term.Type == Type.Pinyin) // already matched Pinyin
                    {
                        term.Type = Type.Ambiguous;
                        term.Results.AddRange(items);
                    }
                    else // plain English
                    {
                        term.Type = Type.English;
                        term.Results = items;
                    }
                }

                terms.Add(term);

                Debug.WriteLine(String.Format("Query term: '{0}' is {1} with {2} associated results.",
                    term.Term,
                    term.Type,
                    term.Results == null ? 0 : term.Results.Count));
            }

            return terms;
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

        void Intersect(ref List<int> target, List<int> items)
        {
            if (items == null)
                return;
            for (int i = target.Count - 1; i >= 0; i--)
                if (!items.Contains(target[i]))
                    target.RemoveAt(i);
        }
    }
}
