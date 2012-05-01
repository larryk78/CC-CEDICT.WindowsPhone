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
        bool loaded = false;
        Dictionary<string, int> lookup = new Dictionary<string, int>();

        public Index(string path)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists(path))
                {
                    Debug.WriteLine(String.Format("Loading index file: {0}", path));
                    Read(new IsolatedStorageFileStream(path, FileMode.Open, store));
                    loaded = true;
                }
            }
        }
        
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
    }
}
