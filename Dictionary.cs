using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;

namespace CC_CEDICT.WindowsPhone
{
    public class Dictionary : IEnumerable<Record>
    {
        Stream file;
        enum LineEndingLength { Unknown = 0, Unix = 1, DOS = 2 };
        LineEndingLength lineEndingLength = LineEndingLength.Unknown;
        Dictionary<string, string> headers = new Dictionary<string, string>();
        List<long> offsets = new List<long>();
        
        public Dictionary(string path)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                Read(new IsolatedStorageFileStream(path, FileMode.Open, store));
        }

        void Read(Stream file)
        {
            this.file = file;
            headers.Clear();
            offsets.Clear();

            long done = 0;
            int size = 32768;
            byte[] data = new byte[size];

            while ((size = file.Read(data, 0, data.Length)) > 0)
            {
                int i = 0;
                int j = System.Array.IndexOf<byte>(data, (byte)'\n') + 1;

                if (lineEndingLength == LineEndingLength.Unknown)
                    lineEndingLength = data[j-2].Equals((byte)'\r') ? LineEndingLength.DOS : LineEndingLength.Unix;
                
                while (j > 0 && j <= size)
                {
                    if (data[i] == (byte)'#') // comments and headers both begin with #
                    {
                        if (data[i + 1] == (byte)'!' && data[i + 2] == (byte)' ') // header format: "#! key=value"
                            processHeader(System.Text.Encoding.UTF8.GetString(data, i + 3, j - i - 3 - (int)lineEndingLength));
                    }
                    else // dictionary content (i.e. entries/definitions)
                    {
                        if (offsets.Count == 0)
                            offsets.Add(i); // add the initial line offset as a special case

                        offsets.Add(done + j); // general case is to add the end of line as an offset
                    }

                    i = j; // move forward in chunk of data
                    j = System.Array.IndexOf<byte>(data, (byte)'\n', i) + 1;
                }
                
                done += size;
            }

            if (offsets[offsets.Count - 1] != (done - (int)lineEndingLength)) // *sigh* file didn't end in a newline
                offsets.Add(done + (int)lineEndingLength); // let's pretend that it did :)
        }

        #region dictionary headers

        void processHeader(string data)
        {
            int i = data.IndexOf("=");
            string key = data.Substring(0, i);
            string value = data.Substring(i + 1);
            switch (key)
            {
                //case ...
                default:
                    headers[key] = value;
                    break;
            }
        }

        public string[] Headers
        {
            get
            {
                string[] keys = new string[headers.Count];
                headers.Keys.CopyTo(keys, 0);
                return keys;
            }
        }

        public string Header(string key)
        {
            return headers.ContainsKey(key) ? headers[key] : "";
        }

        #endregion

        public Record this[int index]
        {
            get
            {
                if (index < 0 || index > this.Count - 1)
                    throw new IndexOutOfRangeException();

                long offset = offsets[index];
                long length = offsets[index + 1] - offset - (int)lineEndingLength;

                file.Seek(offset, SeekOrigin.Begin);
                byte[] data = new byte[length];
                file.Read(data, 0, data.Length);

                return new Record(System.Text.Encoding.UTF8.GetString(data, 0, data.Length));
            }
        }

        public int Count
        {
            get
            {
                return offsets.Count - 1;
            }
        }

        #region IEnumerable implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator<Record>)GetEnumerator();
        }

        public IEnumerator<Record> GetEnumerator()
        {
            return new DictionaryEnumerator(this);
        }

        public class DictionaryEnumerator : IEnumerator<Record>
        {
            private Dictionary dictionary;
            private int index = -1;

            public DictionaryEnumerator(Dictionary dictionary)
            {
                this.dictionary = dictionary;
            }

            public bool MoveNext()
            {
                return (++index < dictionary.Count);
            }

            public void Reset()
            {
                index = -1;
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public Record Current
            {
                get
                {
                    try
                    {
                        return dictionary[index];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            public void Dispose()
            {
                // TODO: should there be some implementation here?
            }
        }

        #endregion
    }
}
