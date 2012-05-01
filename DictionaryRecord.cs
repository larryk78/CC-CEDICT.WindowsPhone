using System;
using System.Collections.Generic;

namespace CC_CEDICT.WindowsPhone
{
    public class DictionaryRecord : ILine, IComparable
    {
        public Chinese Chinese = null;
        public List<string> English = new List<string>();

        public override void Initialize(ref byte[] data)
        {
            string line = System.Text.Encoding.UTF8.GetString(data, 0, data.Length);

            int i = 0;
            int j = line.IndexOf(" ", i);
            if (j == -1)
                return;
            string traditional = line.Substring(i, j - i);

            i = j + 1;
            j = line.IndexOf(" [", i);
            if (j == -1)
                return;
            string simplified = line.Substring(i, j - i);

            i = j + 2;
            j = line.IndexOf("] /", i);
            if (j == -1)
                return;
            string pinyin = line.Substring(i, j - i);

            try
            {
                Chinese = new Chinese(traditional, simplified, pinyin);
            }
            catch (Exception)
            {
                return;
            }

            i = j + 3;
            j = line.IndexOf("/", i);
            if (j == -1)
                return;
            English.Add(line.Substring(i, j - i));

            while (line.Length > j + 1)
            {
                i = j + 1;
                j = line.IndexOf("/", i);
                if (j == -1)
                    break;
                English.Add(line.Substring(i, j - i));
            }
        }

        public override string ToString()
        {
            return String.Format("{0} {1} [{2}] /{3}/",
                Chinese.Traditional,
                Chinese.Simplified,
                Chinese.Pinyin,
                String.Join("/", English));
        }

        int IComparable.CompareTo(object obj)
        {
            DictionaryRecord r = (DictionaryRecord)obj;
            return this.Chinese.Pinyin.CompareTo(r.Chinese.Pinyin);
        }

        public class Comparer : IComparer<DictionaryRecord>
        {
            List<string> context;
            Dictionary<int, int> cache = new Dictionary<int,int>();

            public Comparer(List<string> context)
            {
                this.context = context;
            }

            int IComparer<DictionaryRecord>.Compare(DictionaryRecord a, DictionaryRecord b)
            {
                int aRelevance = Relevance(a);
                int bRelevance = Relevance(b);

                if (aRelevance > bRelevance)
                    return -1;
                else if (aRelevance < bRelevance)
                    return 1;
                else
                    return ((IComparable)a).CompareTo(b);
            }

            int Relevance(DictionaryRecord r)
            {
                if (!cache.ContainsKey(r.LineNumber))
                {
                    int relevance = 0;
                    foreach (string s in context)
                    {
                        string word = s.ToLower();
                        string text = r.ToString().ToLower();
                        relevance += 100 - (100 * text.IndexOf(word) / text.Length);
                    }
                    cache[r.LineNumber] = relevance;
                }
                return cache[r.LineNumber];
            }
        }
    }
}
