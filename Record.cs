using System;
using System.Collections.Generic;

namespace CC_CEDICT.WindowsPhone
{
    public class Record
    {
        public Chinese Chinese = null;
        public List<string> English = new List<string>();

        public Record(string data)
        {
            int i = 0;
            int j = data.IndexOf(" ", i);
            if (j == -1)
                return;
            string traditional = data.Substring(i, j - i);

            i = j + 1;
            j = data.IndexOf(" [", i);
            if (j == -1)
                return;
            string simplified = data.Substring(i, j - i);

            i = j + 2;
            j = data.IndexOf("] /", i);
            if (j == -1)
                return;
            string pinyin = data.Substring(i, j - i);

            try
            {
                Chinese = new Chinese(traditional, simplified, pinyin);
            }
            catch (Exception)
            {
                return;
            }

            i = j + 3;
            j = data.IndexOf("/", i);
            if (j == -1)
                return;
            English.Add(data.Substring(i, j - i));

            while (data.Length > j + 1)
            {
                i = j + 1;
                j = data.IndexOf("/", i);
                if (j == -1)
                    break;
                English.Add(data.Substring(i, j - i));
            }
        }
    }
}
