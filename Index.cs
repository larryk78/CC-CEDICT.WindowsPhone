using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;

namespace CC_CEDICT.WindowsPhone
{
    public class Index : StreamLineArray<IndexRecord>
    {
        string indexFilePath;
        bool loaded = false;

        public Index(string name, Dictionary dict)
        {
            indexFilePath = String.Format("{0}-{1}.csv", name, dict.Header["time"]);
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists(indexFilePath))
                {
                    Debug.WriteLine(String.Format("Loading index file: {0}", indexFilePath));
                    Read(new IsolatedStorageFileStream(indexFilePath, FileMode.Open, store));
                    loaded = true;
                }
            }
        }

        Dictionary<string, int> lookup = new Dictionary<string, int>();
        
        public List<int> this[string key]
        {
            get
            {
                if (!loaded)
                    return null;
                key = key.ToLower();
                if (!lookup.ContainsKey(key))
                    lookup[key] = BinarySearch(key);
                return lookup[key] < 0 ? null : this[lookup[key]].Values;
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

        #region Index creation

        Dictionary<string, List<int>> index = new Dictionary<string, List<int>>();
        
        public void Insert(string key, int value)
        {
            key = key.ToLower();
            if (!this.index.ContainsKey(key))
                this.index[key] = new List<int> { value };
            else if (!this.index[key].Contains(value))
                this.index[key].Add(value);
        }

        public void Save()
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
                    record += "\n";
                    byte[] data = Encoding.UTF8.GetBytes(record);
                    file.Write(data, 0, data.Length);
                }
                file.Close();
                Debug.WriteLine(String.Format("Created index file: {0}", indexFilePath));
            }
        }
        
        #endregion
    }
}
