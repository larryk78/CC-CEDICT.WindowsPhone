using System;
using System.ComponentModel;

namespace CC_CEDICT.WindowsPhone
{
    public class Indexer : BackgroundWorker
    {
        Dictionary dictionary;

        public Indexer(Dictionary d)
        {
            WorkerReportsProgress = true;
            WorkerSupportsCancellation = false;
            DoWork += new DoWorkEventHandler(Indexer_DoWork);
            dictionary = d;
        }

        public void IndexAsync()
        {
            RunWorkerAsync();
        }

        void Indexer_DoWork(object sender, DoWorkEventArgs e)
        {
            Indexer indexer = (Indexer)sender;
            int total = dictionary.Count;
            int done = 0;

            foreach (Record r in dictionary)
            {
                if (r.Chinese == null) // invalid (i.e. malformed record)
                    continue;

                // TODO: implement indexing

                ReportProgress((int)(100 * ++done / total));
            }
        }
    }
}
