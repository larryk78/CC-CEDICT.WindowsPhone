using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;

namespace CC_CEDICT.WindowsPhone
{
    public class PinyinIndex
    {
        string path = "pinyin.xml";

        [DataContract]
        public class IndexEntry
        {
            [DataMember]
            public Dictionary<char,IndexEntry> children = new Dictionary<char,IndexEntry>();
            //[DataMember]
            public List<int> records = new List<int>();
        }

        IndexEntry root;

        public PinyinIndex()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.FileExists(path))
                {
                    root = new IndexEntry();
                    return;
                }

                IsolatedStorageFileStream file = new IsolatedStorageFileStream(path, FileMode.Open, store);
                DataContractSerializer ser = new DataContractSerializer(typeof(IndexEntry));
                byte[] array = new byte[file.Length];
                file.Read(array, 0, array.Length);
                root = (IndexEntry)ser.ReadObject(new MemoryStream(array));
                file.Close();
            }
        }

        public void Insert(string word, int index)
        {
            IndexEntry entry = root;
            foreach (char c in word.ToLower().ToCharArray())
            {
                if (!entry.children.ContainsKey(c))
                    entry.children.Add(c, new IndexEntry());
                entry.children.TryGetValue(c, out entry);
            }
            if (!entry.records.Contains(index))
                entry.records.Add(index);
        }

        public void Serialize()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                IsolatedStorageFileStream file = new IsolatedStorageFileStream(path, FileMode.Create, store);
                MemoryStream ms = new MemoryStream();
                
                DataContractSerializer ser = new DataContractSerializer(typeof(IndexEntry));
                ser.WriteObject(ms, root);
                byte[] array = ms.ToArray();
                ms.Close();
                file.Write(array, 0, array.Length);
                file.Close();
            }
        }
    }
}
