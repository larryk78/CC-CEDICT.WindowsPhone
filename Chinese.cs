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

        bool pinyinMarkupDone = false;
        string _pinyin;
        public string Pinyin
        {
            get
            {
                if (!pinyinMarkupDone)
                {
                    foreach (Character c in Characters)
                        _pinyin = _pinyin.Replace(c.Pinyin.Original, c.Pinyin.MarkedUp);
                    pinyinMarkupDone = true;
                }
                return _pinyin;
            }
        }

        public Chinese(string traditional, string simplified, string pinyin)
        {
            Traditional = traditional;
            Simplified = simplified;
            _pinyin = pinyin;
            ProcessChinese();
        }

        void ProcessChinese()
        {
            char[] pDelim = { ' ', ',', '·' }; // ignore: space, comma, middle-dot

            List<char> t = Split(Traditional);
            List<char> s = Split(Simplified);
            List<string> p = new List<string>(_pinyin.Split(pDelim, StringSplitOptions.RemoveEmptyEntries));

            if (t.Count != s.Count || s.Count != p.Count)
                throw new FormatException(String.Format("Non-matching Hanzi-Pinyin: T[{0}], S[{1}], P[{2}]", t, s, p));

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
