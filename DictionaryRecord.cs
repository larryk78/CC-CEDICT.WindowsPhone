using System;
using System.Collections.Generic;

namespace CC_CEDICT.WindowsPhone
{
    public class DictionaryRecord : ILine, IComparable
    {
        public Chinese Chinese = null;
        public List<string> English = new List<string>();
        public int Relevance = 100;

        #region ILine initialization

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

        #endregion

        public override string ToString()
        {
            return String.Format("{0} {1} [{2}] /{3}/",
                Chinese.Traditional,
                Chinese.Simplified,
                Chinese.Pinyin,
                String.Join("/", English));
        }

        #region IComparable interface

        // TODO-NTH: sort chinese by stroke count and popularity and in-order chinese (and deprioritise "variant of...")

        int IComparable.CompareTo(object obj)
        {
            DictionaryRecord other = (DictionaryRecord)obj;
            // compare first by relevance
            if (this.Relevance > other.Relevance)
                return -1;
            if (this.Relevance < other.Relevance)
                return 1;
            // then by Pinyin
            for (int i = 0; i < this.Chinese.Characters.Count; i++)
            {
                if (i >= other.Chinese.Characters.Count) // other is shorter than this
                    return 1;
                int cmp = this.Chinese.Characters[i].Pinyin.CompareTo(other.Chinese.Characters[i].Pinyin);
                if (cmp != 0)
                    return cmp;
            }
            if (this.Chinese.Characters.Count < other.Chinese.Characters.Count) // this is shorter than other
                return -1;
            // finally by English
            return String.Join(";", this.English).CompareTo(String.Join(";", other.English));
        }

        #endregion
    }
}
