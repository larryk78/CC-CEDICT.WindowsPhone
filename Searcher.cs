﻿using System;
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

        List<SearchResult> _unused = new List<SearchResult>();
        public string Ignored
        {
            get
            {
                string terms = "";
                foreach (SearchResult term in _unused)
                {
                    if (terms.Length > 0)
                        terms += " ";
                    terms += term.Type == Type.Pinyin ? term.Pinyin.Original : term.Term;
                }
                return terms;
            }
        }

        public List<DictionaryRecord> Search(string query, int minRelevance=1)
        {
            DateTime start = DateTime.Now;
            Debug.WriteLine(String.Format("Searching for: '{0}'", query));

            List<DictionaryRecord> results = new List<DictionaryRecord>();
            foreach (IndexReference reference in AbstractSearch(query, minRelevance))
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

        public bool SmartSearch;
        public int Total;
        List<IndexReference> AbstractSearch(string query, int minRelevance)
        {
            query = query.Trim();
            if (query.Length == 0)
                return new List<IndexReference>(); // empty

            SearchResultAggregator aggregator = new SearchResultAggregator();
            List<SearchResult> terms = Tokenize(query, minRelevance);

            SmartSearch = false;
            _used.Clear();
            _unused.Clear();
            
            List<IndexReference> tracker = new List<IndexReference>();
            foreach (SearchResult term in terms)
            {
                if (term.Type == Type.Unknown)
                {
                    _unused.Add(term);
                    SmartSearch = true;
                    continue;
                }

                if (tracker.Count == 0)
                    tracker.AddRange(term.Results);
                else
                {
                    List<IndexReference> temp = new List<IndexReference>(tracker);
                    this.Intersect(ref temp, term.Results);
                    if (temp.Count == 0) // empty set (i.e. destroys results)
                    {
                        _unused.Add(term);
                        SmartSearch = true;
                        continue;
                    }
                    tracker = temp;
                }

                aggregator.Add(term.Results);
                _used.Add(term);
            }

            List<IndexReference> results = aggregator.Results(minRelevance);
            Total = aggregator.Count;
            Debug.WriteLine("Total results (pre-aggregation): " + Total);
            return results;
        }

        List<IndexReference> Aggregate(List<SearchResult> results, int minRelevance=0)
        {
            SearchResultAggregator a = new SearchResultAggregator();
            foreach (SearchResult r in results)
                a.Add(r.Results);
            return a.Results(minRelevance);
        }

        class SearchResult
        {
            public string Term;
            public Pinyin Pinyin;
            public Type Type;
            
            Dictionary<int, IndexReference> _results = new Dictionary<int, IndexReference>();
            public List<IndexReference> Results
            {
                get
                {
                    List<IndexReference> list = new List<IndexReference>();
                    foreach (int key in _results.Keys)
                        list.Add(_results[key]);
                    return list;
                }
            }

            public void Add(List<IndexReference> references)
            {
                foreach (IndexReference r in references)
                {
                    if (!_results.ContainsKey(r.Index))
                        _results.Add(r.Index, r);
                    else if (r.Relevance > _results[r.Index].Relevance)
                        _results[r.Index] = r;
                }
            }       
        }

        enum SearchFlags { None = 0, IgnoreEnglish = 1, AlwaysSplitHanzi = 2 };
        List<SearchResult> Tokenize(string query, int minRelevance=0, SearchFlags flags = SearchFlags.None)
        {
            List<SearchResult> results = new List<SearchResult>();
            List<IndexReference> items = new List<IndexReference>();

            // break query into individual terms
            char[] delim = { ' ' };
            foreach (string token in query.ToLower().Split(delim, StringSplitOptions.RemoveEmptyEntries))
            {
                SearchResult result = new SearchResult { Term = token, Type = Type.Unknown };

                if (ContainsHanzi(token))
                {
                    if (OnlyHanzi(token)) // full Hanzi token
                    {
                        IndexRecord hanziLookup = index[Type.Hanzi][token];
                        if ((token.Length == 1 || (flags & SearchFlags.AlwaysSplitHanzi) > 0) && hanziLookup != null) // found an exact match (TODO: too strict?)
                        {
                            result.Type = Type.Hanzi;
                            result.Term = token;
                            result.Add(hanziLookup.References);
                            results.Add(result);
                        }
                        else if (token.Length > 1) // not found, so split and search
                        {
                            // TODO: this is where the intelligent Chinese wordsearch combinatorics come in :)
                            results.AddRange(Tokenize(String.Join(" ", token.ToCharArray()), minRelevance));
                        }
                        continue; // completely handled this token
                    }
                    else // some mixture of Hanzi and something else
                    {
                        results.AddRange(Tokenize(String.Join(" ", SegregateHanziNonHanzi(token)), minRelevance));
                    }
                }

                IndexRecord pinyinLookup = index[Type.Pinyin][token];
                if (pinyin.ContainsKey(token)) // Pinyin with no tone (check all the tones)
                {
                    result.Type = Type.Pinyin;
                    result.Pinyin = new Pinyin(token);

                    if (pinyinLookup != null) // try with no tone
                        result.Add(pinyinLookup.References);

                    for (int i = 1; i <= 5; i++) // then go through all the tones
                    {
                        IndexRecord pinyinWithToneLookup = index[Type.Pinyin][token + i.ToString()];
                        if (pinyinWithToneLookup != null)
                            result.Add(pinyinWithToneLookup.References);
                    }
                }
                else if (pinyinLookup != null) // Pinyin (with tone)?
                {
                    result.Type = Type.Pinyin;
                    result.Pinyin = new Pinyin(token);
                    result.Add(pinyinLookup.References);
                }
                
                IndexRecord englishLookup = index[Type.English][token];
                if ((flags & SearchFlags.IgnoreEnglish) == 0 && englishLookup != null) // English?
                {
                    result.Type = (result.Type == Type.Pinyin) ? Type.Ambiguous : Type.English;
                    result.Add(englishLookup.References);
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
                        temp.AddRange(Tokenize(capture.Value, minRelevance, SearchFlags.IgnoreEnglish));
                    if (temp.Count == 0) // compound was a red-herring, e.g. secure -> se cu re
                        continue;
                    if (result.Type == Type.Unknown)
                    {
                        results.RemoveAt(results.Count - 1); // delete the pre-split compound result (that didn't match anything)
                        results.AddRange(temp); // add the post-split results instead
                    }
                    else // matched both pre-split and post-split, e.g. Beijing matches English and Bei jing Pinyin
                    {
                        items = Aggregate(temp, minRelevance);
                        if (items.Count > 0)
                            result.Add(items); // append aggregated post-split results to existing result
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
            chunks.Add(hanziChunk.Length > 0 ? hanziChunk : otherChunk);
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
    }
}
