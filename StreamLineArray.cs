using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace CC_CEDICT.WindowsPhone
{
    public class StreamLineArray<T> : IEnumerable<T> where T : ILine, new()
    {
        Stream stream;
        enum LineEndingLength { Unknown = 0, Unix = 1, DOS = 2 };
        LineEndingLength lineEndingLength = LineEndingLength.Unknown;
        List<long> offsets = new List<long>();
        
        public StreamLineArray()
        {
        }

        protected void Read(Stream stream)
        {
            this.stream = stream;
            offsets.Clear();

            long done = 0;
            int size = 32768;
            byte[] data = new byte[size];

            while ((size = stream.Read(data, 0, data.Length)) > 0)
            {
                int i = 0;
                int j = System.Array.IndexOf<byte>(data, (byte)'\n') + 1;

                if (lineEndingLength == LineEndingLength.Unknown)
                    lineEndingLength = data[j-2].Equals((byte)'\r') ? LineEndingLength.DOS : LineEndingLength.Unix;
                
                while (j > 0 && j <= size)
                {
                    if (!ProcessHeader(ref data, i, j - i - (int)lineEndingLength))
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

            if (offsets[offsets.Count - 1] != done) // newline at EOF is missing
                offsets.Add(done + (int)lineEndingLength);
        }

        protected virtual bool ProcessHeader(ref byte[] data, int offset, int length)
        {
            return false;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index > this.Count - 1)
                    throw new IndexOutOfRangeException();

                long offset = offsets[index];
                long length = offsets[index + 1] - offset - (int)lineEndingLength;
                byte[] data = new byte[length];

                stream.Seek(offset, SeekOrigin.Begin);
                stream.Read(data, 0, data.Length);

                T line = new T();
                line.Index = index;
                line.Initialize(ref data);
                return line;
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
            return (IEnumerator<byte[]>)GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new LineEnumerator(this);
        }

        public class LineEnumerator : IEnumerator<T>
        {
            private StreamLineArray<T> reader;
            private int index = -1;

            public LineEnumerator(StreamLineArray<T> reader)
            {
                this.reader = reader;
            }

            public bool MoveNext()
            {
                return (++index < reader.Count);
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

            public T Current
            {
                get
                {
                    try
                    {
                        return reader[index];
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
