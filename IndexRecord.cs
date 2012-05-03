using System;
using System.Collections.Generic;

namespace CC_CEDICT.WindowsPhone
{
    public class IndexRecord : ILine
    {
        string data;
        
        string _key = null;
        public string Key
        {
            get
            {
                if (_key == null)
                    _key = data.Substring(0, data.IndexOf(','));
                return _key;
            }
        }

        Dictionary<int, int> _values = new Dictionary<int, int>();
        public Dictionary<int, int> Values
        {
            get
            {
                if (_values.Count == 0)
                {
                    char[] comma = { ',' };
                    char[] pipe = { '|' };
                    foreach (string tuple in data.Substring(data.IndexOf(',') + 1).Split(comma))
                    {
                        string[] fields = tuple.Split(pipe);
                        int index, relevance;
                        Int32.TryParse(fields[0], out index);
                        Int32.TryParse(fields[1], out relevance);
                        _values.Add(index, relevance);
                    }
                }
                return _values;
            }
        }

        public override void Initialize(ref byte[] data)
        {
            this.data = System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
        }
    }
}
