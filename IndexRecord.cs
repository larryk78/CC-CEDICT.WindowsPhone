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

        public class Reference : IComparable, IEquatable<Reference>
        {
            public string Key;
            public int Index;
            public int Relevance;

            #region IComparable interface

            int IComparable.CompareTo(object obj)
            {
                Reference other = (Reference)obj;
                if (this.Relevance > other.Relevance)
                    return -1;
                else if (this.Relevance == other.Relevance)
                    return this.Key.CompareTo(other.Key);
                else
                    return 1;
            }

            #endregion

            #region IEquatable<Reference> interface

            public bool Equals(Reference other)
            {
                return Index == other.Index;
            }

            #endregion

            public override bool Equals(object obj)
            {
                Reference other = (Reference)obj;
                return other != null && Equals(other);
            }

            public override int GetHashCode()
            {
                return Index.GetHashCode();
            }
        }

        static char[] comma = { ',' }; // index lines are comma-separated (CSV)
        static char[] pipe  = { '|' }; // CSV elements are pipe-separated

        List<Reference> _refs = new List<Reference>();
        public List<Reference> References
        {
            get
            {
                if (_refs.Count == 0) // lazy loading
                {
                    foreach (string tuple in data.Substring(Key.Length + 1).Split(comma))
                    {
                        string[] fields = tuple.Split(pipe);
                        int index, relevance;
                        Int32.TryParse(fields[0], out index);
                        Int32.TryParse(fields[1], out relevance);
                        _refs.Add(new Reference { Key = this.Key, Index = index, Relevance = relevance });
                    }
                }
                return _refs;
            }
        }
    }
}
