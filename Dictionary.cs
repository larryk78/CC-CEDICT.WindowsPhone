using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;

namespace CC_CEDICT.WindowsPhone
{
    public class Dictionary : StreamLineArray<DictionaryRecord>
    {
        public Dictionary<string, string> Header = new Dictionary<string, string>();
        
        public Dictionary(string path)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                Read(new IsolatedStorageFileStream(path, FileMode.Open, store));
        }

        protected override bool ProcessHeader(ref byte[] data, int offset, int length)
        {
            if (data[offset] != (byte)'#')
                return false;

            if (data[offset + 1] == (byte)'!' && data[offset + 2] == (byte)' ')
            {
                string header = System.Text.Encoding.UTF8.GetString(data, offset + 3, length - 3);
                int i = header.IndexOf("=");
                string key = header.Substring(0, i);
                string value = header.Substring(i + 1);
                switch (key)
                {
                    //case ...
                    default:
                        Header[key] = value;
                        break;
                }
            }

            return true;
        }
    }
}
