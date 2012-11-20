using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Threading;

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
                    Stream stream = new IsolatedStorageFileStream(path, FileMode.Open, store);
                    if (!stream.CanRead)
                        return;
                    loaded = Read(stream);
                }
            }
        }
        
        public List<IndexRecord.Reference> this[string key]
        {
            get
            {
                if (!loaded)
                    return null;
                key = key.ToLower();
                if (!lookup.ContainsKey(key))
                    lookup[key] = BinarySearch(key);
                return lookup[key] < 0 ? null : this[lookup[key]].References;
            }
        }

        static CultureInfo en_US = new CultureInfo("en-US");
        int BinarySearch(string key)
        {
            CultureInfo old = null;
            if (Thread.CurrentThread.CurrentCulture.Name != en_US.Name) // Hanzi might not sort properly
            {
                old = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = en_US;
                Debug.WriteLine("Culture: {0} changed to {1}", old.Name, en_US.Name);
            }
            try
            {
                int min = 0, pos, max = this.Count - 1;
                while (min <= max)
                {
                    pos = (min + max) / 2;
                    switch (key.CompareTo(this[pos].Key))
                    {
                        case -1:
                            max = pos - 1;
                            Debug.WriteLine("BinarySearch: compare {0} to {1} (down)", key, this[pos].Key);
                            break;
                        case 0:
                            Debug.WriteLine("BinarySearch: compare {0} to {1} (MATCH)", key, this[pos].Key);
                            return pos;
                        case 1:
                            min = pos + 1;
                            Debug.WriteLine("BinarySearch: compare {0} to {1} (up)", key, this[pos].Key);
                            break;
                    }
                }
                Debug.WriteLine("BinarySearch: not found.");
                return -1;
            }
            finally // restore CultureInfo if changed earlier
            {
                if (old != null)
                {
                    Thread.CurrentThread.CurrentCulture = old;
                    Debug.WriteLine("Culture: reset to {0}", old.Name);
                }
            }
        }
    }
}
