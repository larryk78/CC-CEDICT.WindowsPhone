﻿using System;
using System.Collections.Generic;

namespace CC_CEDICT.WindowsPhone
{
    public class Record : ILine
    {
        public Chinese Chinese = null;
        public List<string> English = new List<string>();

        public void Initialize(ref byte[] data)
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
    }
}
