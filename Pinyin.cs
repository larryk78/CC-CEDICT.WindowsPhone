using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CC_CEDICT.WindowsPhone
{
    public class Pinyin
    {
        public string Original;
        public string Syllable;
        public enum Tones { Unknown = 0, Flat = 1, Rising = 2, FallingRising = 3, Falling = 4, Neutral = 5 };
        public Tones Tone = Tones.Unknown;

        public Pinyin(string input)
        {
            Original = input;
            input = input.Replace("u:", "v").Replace("U:", "V");

            Regex pattern = new Regex("^([a-zA-Z]+)([1-5])$");
            Match match = pattern.Match(input);
            if (match.Success)
            {
                Syllable = match.Groups[1].Value;
                Tone = (Tones)int.Parse(match.Groups[2].Value);
                return;
            }

            pattern = new Regex("^([a-zA-Z]+)$"); // TODO: this could be more discerning
            match = pattern.Match(input);
            if (match.Success)
            {
                Syllable = match.Groups[1].Value;
                return;
            }

            throw new FormatException(String.Format("Invalid Pinyin: '{0}'", Original));
        }

        #region Pinyin markup

        static Dictionary<char, char[]> markupTable = new Dictionary<char, char[]>
        {
            { 'a', new char[5]{ 'a', '\u0101', '\u00e1', '\u01ce', '\u00e0' } },
            { 'A', new char[5]{ 'A', '\u0100', '\u00c1', '\u01cd', '\u00c0' } },
            { 'e', new char[5]{ 'e', '\u0113', '\u00e9', '\u011b', '\u00e8' } },
            { 'E', new char[5]{ 'E', '\u0112', '\u00c9', '\u011a', '\u00c8' } },
            { 'i', new char[5]{ 'i', '\u012b', '\u00ed', '\u01d0', '\u00ec' } },
            { 'I', new char[5]{ 'I', '\u012a', '\u00cd', '\u01cf', '\u00cc' } },
            { 'o', new char[5]{ 'o', '\u014d', '\u00f3', '\u01d2', '\u00f2' } },
            { 'O', new char[5]{ 'O', '\u014c', '\u00d3', '\u01d1', '\u00d2' } },
            { 'u', new char[5]{ 'u', '\u016b', '\u00fa', '\u01d4', '\u00f9' } },
            { 'U', new char[5]{ 'U', '\u016a', '\u00da', '\u01d3', '\u00d9' } },
            { 'v', new char[5]{ 'v', '\u01d6', '\u01d8', '\u01da', '\u01dc' } },
            { 'V', new char[5]{ 'V', '\u01d5', '\u01d7', '\u01d9', '\u01db' } }
        };

        static string initial = "(?:[bcdfghjklmnpqrstwxyz]|[csz]h)";
        static string vowel = "[aeiouv]";
        static string priorityVowel = "[aeo]";
        static string secondaryVowel = "[iu]";

        static Regex singleVowel = new Regex("^(" + initial + ")?(" + vowel + ")(?!" + vowel + ")", RegexOptions.IgnoreCase);
        static Regex multiVowelPriority = new Regex("^(" + initial + ")?(" + priorityVowel + ")(?=" + vowel + ")", RegexOptions.IgnoreCase);
        static Regex multiVowelNormal = new Regex("^(" + initial + "?" + secondaryVowel + ")(" + vowel + ")", RegexOptions.IgnoreCase);
        
        string _markedup = null;
        public string MarkedUp
        {
            get
            {
                if (_markedup == null)
                {
                    if (Tone == Tones.Unknown)
                    {
                        _markedup = Syllable;
                    }
                    else
                    {
                        string temp;
                        MatchEvaluator eval = new MatchEvaluator(this.MarkupEvaluator);
                        if ((temp = singleVowel.Replace(Syllable, eval)) != Syllable)
                            _markedup = temp;
                        else if ((temp = multiVowelPriority.Replace(Syllable, eval)) != Syllable)
                            _markedup = temp;
                        else if ((temp = multiVowelNormal.Replace(Syllable, eval)) != Syllable)
                            _markedup = temp;
                        else
                            _markedup = Syllable;
                    }
                }
                return _markedup;
            }
        }

        public string MarkupEvaluator(Match match)
        {
            char vowel;
            switch (match.Groups.Count)
            {
                case 2:
                    vowel = match.Groups[1].Value.ToCharArray()[0];
                    return markupTable[vowel][(int)Tone % 5].ToString();
                case 3:
                    vowel = match.Groups[2].Value.ToCharArray()[0];
                    return match.Groups[1].Value + markupTable[vowel][(int)Tone % 5].ToString();
                default:
                    throw new FormatException("confused.com");
            }
        }

        #endregion
    }
}
