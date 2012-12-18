using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CC_CEDICT.WindowsPhone
{
    public class Chinese
    {
        public List<Character> Characters = new List<Character>();
        public class Character
        {
            public char Traditional;
            public char Simplified;
            public Pinyin Pinyin;
        }
        
        public string Traditional;
        public string Simplified;
        public string PinyinNoMarkup;

        bool pinyinMarkupDone = false;
        string _Pinyin;
        public string Pinyin
        {
            get
            {
                if (!pinyinMarkupDone)
                {
                    foreach (Character c in Characters)
                        _Pinyin = _Pinyin.Replace(c.Pinyin.Original, c.Pinyin.MarkedUp);
                    pinyinMarkupDone = true;
                }
                return _Pinyin;
            }
            set
            {
                _Pinyin = value;
                pinyinMarkupDone = false;
            }
        }

        public Chinese(string traditional, string simplified, string pinyin)
        {
            // store the original values (contains punctuation)
            Traditional = traditional;
            Simplified = simplified;
            PinyinNoMarkup = pinyin;
            Pinyin = pinyin.Replace("u:", "v");

            // split to individual characters
            List<char> t = Split(traditional);
            List<char> s = Split(simplified);

            char[] pDelim = { ' ', ',', '·' }; // ignore: space, comma, middle-dot
            List<string> p = new List<string>(pinyin.Split(pDelim, StringSplitOptions.RemoveEmptyEntries));

            if (t.Count != s.Count || s.Count != p.Count)
                throw new FormatException(String.Format("Non-matching Hanzi-Pinyin: T[{0}], S[{1}], P[{2}]", t, s, p));

            // populate Characters list
            for (int i = 0; i < t.Count; i++)
                Characters.Add(new Character { Traditional = t[i], Simplified = s[i], Pinyin = new Pinyin(p[i]) });
        }

        List<char> Split(string hanzi)
        {
            List<char> chars = new List<char>();
            foreach (char c in hanzi.ToCharArray())
                if (c != ' ' && c != '\uff0c' && c != '\u30fb') // ignore: space, full-width comma, full-width middle-dot
                    chars.Add(c);
            return chars;
        }
    }
}
