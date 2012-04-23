using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Text;

namespace CC_CEDICT.WindowsPhone
{
    public class PinyinIndex : StreamLineArray<IndexRecord>
    {
        Dictionary<string, List<int>> index = new Dictionary<string, List<int>>();
        string indexFilePath = "pinyin.csv";

        public PinyinIndex()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                if (store.FileExists(indexFilePath))
                    Read(new IsolatedStorageFileStream(indexFilePath, FileMode.Open, store));
        }

        public void Insert(string key, int value)
        {
            key = key.ToLower();
            if (!this.index.ContainsKey(key))
                this.index[key] = new List<int> { value };
            else if (!this.index[key].Contains(value))
                this.index[key].Add(value);
        }

        Dictionary<string, int> _lookup = new Dictionary<string, int>();
        public List<int> this[string key]
        {
            get
            {
                key = key.ToLower();
                if (!_lookup.ContainsKey(key))
                    _lookup[key] = BinarySearch(key);
                return _lookup[key] < 0 ? null : this[_lookup[key]].Values;
            }
        }

        int BinarySearch(string key)
        {
            int min = 0, pos, max = this.Count - 1;
            while (min <= max)
            {
                pos = (min + max) / 2;
                switch (key.CompareTo(this[pos].Key))
                {
                    case -1:
                        max = pos - 1;
                        break;
                    case 0:
                        return pos;
                    case 1:
                        min = pos + 1;
                        break;
                }
            }
            return -1;
        }

        public void Serialize()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                IsolatedStorageFileStream file = new IsolatedStorageFileStream(indexFilePath, FileMode.Create, store);
                List<string> keys = new List<string>(index.Keys);
                keys.Sort();
                foreach (string key in keys)
                {
                    string record = key;
                    foreach (int value in index[key])
                        record += "," + value.ToString();
                    Debug.WriteLine(record);
                    record += "\n";
                    byte[] data = Encoding.UTF8.GetBytes(record);
                    file.Write(data, 0, data.Length);
                }
                file.Close();
            }
        }
    }
}
