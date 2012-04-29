using System;
using System.Collections;
using System.ComponentModel;

namespace CC_CEDICT.WindowsPhone
{
    public class Indexer : BackgroundWorker
    {
        Dictionary dictionary;
        Index pinyinIndex, hanziIndex;

        public Indexer(Dictionary d)
        {
            WorkerReportsProgress = true;
            WorkerSupportsCancellation = false;
            DoWork += new DoWorkEventHandler(Indexer_DoWork);
            dictionary = d;
            pinyinIndex = new Index("pinyin-" + d.Header["time"], d);
            hanziIndex = new Index("hanzi-" + d.Header["time"], d);
        }

        public void IndexAsync()
        {
            RunWorkerAsync();
        }

        void Indexer_DoWork(object sender, DoWorkEventArgs e)
        {
            Indexer indexer = (Indexer)sender;
            int total = dictionary.Count;
            int index = 0;

            foreach (Record r in dictionary)
            {
                if (r.Chinese == null) // invalid (i.e. malformed record)
                    continue;

                foreach (Chinese.Character c in r.Chinese.Characters)
                {
                    pinyinIndex.Insert(c.Pinyin.Original, index);
                    hanziIndex.Insert(c.Simplified.ToString(), index);
                    hanziIndex.Insert(c.Traditional.ToString(), index);
                }

                ReportProgress((int)(100 * ++index / total));
            }

            pinyinIndex.Save();
            hanziIndex.Save();
        }
    }
}
