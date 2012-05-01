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

        List<int> _values = new List<int>();
        public List<int> Values
        {
            get
            {
                if (_values.Count == 0)
                {
                    char[] comma = { ',' };
                    foreach (string number in data.Substring(data.IndexOf(',') + 1).Split(comma))
                    {
                        int n;
                        Int32.TryParse(number, out n);
                        _values.Add(n);
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
