using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Resources;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using CC_CEDICT.WindowsPhone;
using SevenZip.Compression.LZMA.WindowsPhone;

namespace Test
{
    public partial class MainPage : PhoneApplicationPage
    {
        Dictionary dictionary;
        const string file = "cedict_ts.db";
        DateTime start;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            IndexButton.IsEnabled = false;
            ReadIndexButton.IsEnabled = false;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Resource2IsolatedStorageDecoder d = new Resource2IsolatedStorageDecoder();
            d.ProgressChanged += new ProgressChangedEventHandler(d_ProgressChanged);
            d.RunWorkerCompleted += new RunWorkerCompletedEventHandler(d_RunWorkerCompleted);
            d.DecodeAsync(file + ".lzma", file);
        }

        void d_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressText.Text = "Decompressing " + file + "... " + e.ProgressPercentage + "%";
            Progress.Value = e.ProgressPercentage;
        }

        void d_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dictionary = new Dictionary(file);

            foreach (string key in dictionary.Header.Keys)
            {
                InfoList.Items.Add(String.Format("{0}={1}", key, dictionary.Header[key]));
            }

            foreach (string i in new List<string> { "pinyin", "hanzi", "english" })
            {
                string indexFileName = String.Format("{0}-{1}.csv", i, dictionary.Header["time"]);
                string compressedName = indexFileName + ".lzma";
                Resource2IsolatedStorageDecoder d = new Resource2IsolatedStorageDecoder();
                d.ProgressChanged += new ProgressChangedEventHandler(d_ProgressChanged);
                d.RunWorkerCompleted += new RunWorkerCompletedEventHandler(d_RunWorkerCompleted2);
                d.DecodeAsync(compressedName, indexFileName);
            }

            IndexButton.IsEnabled = true;
        }

        void d_RunWorkerCompleted2(object sender, RunWorkerCompletedEventArgs e)
        {
            ReadIndexButton.IsEnabled = true;
        }

        private void IndexButton_Click(object sender, RoutedEventArgs e)
        {
            if (dictionary == null)
                return;

            Indexer i = new Indexer(dictionary);
            i.ProgressChanged += new ProgressChangedEventHandler(i_ProgressChanged);
            i.RunWorkerCompleted += new RunWorkerCompletedEventHandler(i_RunWorkerCompleted);

            start = DateTime.Now;
            i.IndexAsync();
        }

        void i_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressText.Text = "Indexing " + file + "... " + e.ProgressPercentage + "%";
            Progress.Value = e.ProgressPercentage;
        }

        void i_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show(String.Format("Indexing took {0} seconds.", ((TimeSpan)(DateTime.Now - start)).TotalSeconds));
        }

        private void ReadIndexButton_Click(object sender, RoutedEventArgs e)
        {
            Index pi = new Index("pinyin", dictionary);
            Index hi = new Index("hanzi", dictionary);
            Index ei = new Index("english", dictionary);
            InfoList.Items.Clear();
            List<DictionaryRecord> results = new List<DictionaryRecord>();
            foreach (int id in ei["terrorist"])
            {
                results.Add(dictionary[id]);
            }
            DictionaryRecord.Comparer comp = new DictionaryRecord.Comparer(new List<string> { "terrorist" });
            results.Sort(comp);
            foreach (DictionaryRecord r in results)
            {
                InfoList.Items.Add(r);
            }
        }
    }
}