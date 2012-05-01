using System;
using System.Collections.Generic;
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
        public enum Type { Unknown, English, Pinyin, Hanzi };
        Dictionary dictionary;
        Dictionary<Type, Index> index = new Dictionary<Type, Index>();
        Dictionary<string, bool> pinyin = new Dictionary<string, bool>();

        public Searcher(Dictionary d, Index english, Index pinyin, Index hanzi)
        {
            dictionary = d;
            index[Type.English] = english;
            index[Type.Pinyin] = pinyin;
            index[Type.Hanzi] = hanzi;
            foreach (IndexRecord r in index[Type.Pinyin])
                this.pinyin[r.Key] = true; // pre-cache all pinyin syllables
        }

        class SearchTerm
        {
            public Type type = Type.Unknown;
            public string text;
            public Pinyin pinyin;
        }

        void Intersect(ref List<int> target, List<int> items)
        {
            for (int i = target.Count - 1; i > 0; i--)
                if (!items.Contains(target[i]))
                    target.RemoveAt(i);
        }

        public List<DictionaryRecord> Search(string query)
        {
            List<DictionaryRecord> results = new List<DictionaryRecord>();
            foreach (int i in AbstractSearch(query))
                results.Add(dictionary[i]);
            return results;
        }

        struct ResultNode
        {
            public string Term = null;
            public Type Type = Type.Unknown;
            public List<int> Results = new List<int>();
        }

        List<int> AbstractSearch(string query)
        {
            //TODO: fix this so just one set of results (combination) of all results for each term
            List<List<ResultNode>> results = new List<List<ResultNode>>();
            query = query.Trim();
            if (query.Length == 0)
                return null;

            // break query into individual terms
            char[] delim = { ' ' };
            int i = 0;
            foreach (string term in query.ToLower().Split(delim, StringSplitOptions.RemoveEmptyEntries))
            {
                List<int> items;
                bool matchedEnglish = false;
                if ((items = index[Type.English][term]) != null) // English
                {
                    matchedEnglish = true;
                    results[i].Add(new ResultNode { Term = term, Type = Type.English, Results = items });
                }

                bool matchedPinyin = false;
                if ((items = index[Type.Pinyin][term]) != null) // Pinyin
                {
                    matchedPinyin = true;
                    results[i].Add(new ResultNode { Term = term, Type = Type.Pinyin, Results = items });
                }

                bool matchedHanzi = false;
                if ((items = index[Type.Hanzi][term]) != null) // Hanzi
                {
                    matchedHanzi = true;
                    results[i].Add(new ResultNode { Term = term, Type = Type.Hanzi, Results = items });
                }

                if (!matchedEnglish && !matchedPinyin && !matchedHanzi) // might be a compound
                {
                    MatchCollection matches = hanziRegex.Matches(term);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            string hanzi = match.Groups[1].Value;
                            if (isHanzi(hanzi))
                                terms.Add(new SearchTerm { text = hanzi, type = SearchTerm.Types.Hanzi });
                        }
                        continue;
                    }
                }
                
                i++;
            }

            return null;
        }

        static Regex pinyinRegex = new Regex("^[a-z]+\\d");
        static Regex hanziRegex = new Regex("([\\u2600-\\uffff])");

    }
}
