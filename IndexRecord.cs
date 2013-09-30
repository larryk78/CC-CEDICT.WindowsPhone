using System;
using System.Collections.Generic;

namespace CC_CEDICT.WindowsPhone
{
    public class IndexRecord : ILine
    {
        #region ILine initialization

        string data;
        public override void Initialize(ref byte[] data)
        {
            this.data = System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
        }

        #endregion

        string _key = null;
        public string Key
        {
            get
            {
                if (_key == null) // lazy loading
                    _key = data.Substring(0, data.IndexOf(','));
                return _key;
            }
        }

        static char[] comma = { ',' }; // index lines are comma-separated (CSV)
        static char[] pipe  = { '|' }; // CSV elements are pipe-separated

        List<IndexReference> _refs = new List<IndexReference>();
        public List<IndexReference> References
        {
            get
            {
                if (_refs.Count == 0) // lazy loading
                {
                    foreach (string tuple in data.Substring(Key.Length + 1).Split(comma))
                    {
                        string[] fields = tuple.Split(pipe);
                        int index, relevance;
                        // handle malformed records
                        if (fields.Length < 2) continue;
                        if (!Int32.TryParse(fields[0], out index)) continue;
                        if (!Int32.TryParse(fields[1], out relevance)) continue;
                        _refs.Add(new IndexReference { Key = this.Key, Index = index, Relevance = relevance });
                    }
                }
                return _refs;
            }
        }
    }
}
