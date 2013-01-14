using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CC_CEDICT.WindowsPhone
{
    public class IndexReference : IComparable, IEquatable<IndexReference>
    {
        public string Key;
        public int Index;
        public int Relevance;

        #region IComparable interface

        int IComparable.CompareTo(object obj)
        {
            IndexReference other = (IndexReference)obj;
            if (this.Relevance > other.Relevance)
                return -1;
            else if (this.Relevance == other.Relevance)
                return this.Key.CompareTo(other.Key);
            else
                return 1;
        }

        #endregion

        #region IEquatable interface

        public bool Equals(IndexReference other)
        {
            return Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            IndexReference other = (IndexReference)obj;
            return other != null && Equals(other);
        }

        #endregion

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }
    }
}
