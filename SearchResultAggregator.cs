using System;
using System.Collections.Generic;
using System.Net;
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
    public class SearchResultAggregator
    {
        private Dictionary<int, IndexRecord.Reference> bucket = new Dictionary<int, IndexRecord.Reference>();
        private Dictionary<int, int> hitCounter = new Dictionary<int, int>();
        private int total = 0;

        public void Add(List<IndexRecord.Reference> results)
        {
            foreach (IndexRecord.Reference r in results)
            {
                if (!bucket.ContainsKey(r.Index))
                {
                    bucket[r.Index] = r;
                    hitCounter[r.Index] = 1;
                }
                else
                {
                    bucket[r.Index].Relevance += r.Relevance;
                    hitCounter[r.Index]++;
                }
            }

            total++; // counts the number of calls to Add(...)
        }

        public int Count;
        public List<IndexRecord.Reference> Results(int minRelevance=0)
        {
            List<IndexRecord.Reference> results = new List<IndexRecord.Reference>();

            Count = 0;
            foreach (IndexRecord.Reference r in bucket.Values)
            {
                // adjust relevance based on ratio of matched words
                r.Relevance *= hitCounter[r.Index] / total;

                // keep count of the full-matches
                if (r.Relevance > 0)
                    Count++;

                // dump those that don't make the cut
                if (r.Relevance >= minRelevance)
                    results.Add(r);
            }

            results.Sort();
            return results;
        }
    }
}
