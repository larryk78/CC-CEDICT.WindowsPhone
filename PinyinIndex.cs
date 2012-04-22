using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Text;

namespace CC_CEDICT.WindowsPhone
{
    public class PinyinIndex
    {
        Dictionary<string, List<int>> index = new Dictionary<string, List<int>>();
        string indexFilePath = "pinyin.csv";

        public PinyinIndex()
        {
        }

        public void Insert(string pinyin, int value)
        {
            string key = pinyin.ToLower();
            if (!this.index.ContainsKey(key))
                this.index[key] = new List<int> { value };
            else if (!this.index[key].Contains(value))
                this.index[key].Add(value);
        }

        static byte[] comma = Encoding.UTF8.GetBytes(",");
        static byte[] newline = Encoding.UTF8.GetBytes("\n");
        public void Serialize()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                IsolatedStorageFileStream file = new IsolatedStorageFileStream(indexFilePath, FileMode.Create, store);
                List<string> keys = new List<string>(index.Keys);
                keys.Sort();
                foreach (string key in keys)
                {
                    byte[] data = Encoding.UTF8.GetBytes(key);
                    file.Write(data, 0, data.Length);
                    foreach (int value in index[key])
                    {
                        file.Write(comma, 0, comma.Length);
                        byte[] n = Encoding.UTF8.GetBytes(value.ToString()); // for binary, use: BitConverter.GetBytes(value);
                        file.Write(n, 0, n.Length);
                    }
                    file.Write(newline, 0, newline.Length);
                }
                file.Close();
            }
        }
    }
}
