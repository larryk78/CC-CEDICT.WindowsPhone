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
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            StreamResourceInfo input = Application.GetResourceStream(new Uri("/Test;component/cedict_ts.lzma", UriKind.Relative));
            IsolatedStorageDecoder d = new IsolatedStorageDecoder();
            d.ProgressChanged += new ProgressChangedEventHandler(d_ProgressChanged);
            d.RunWorkerCompleted += new RunWorkerCompletedEventHandler(d_RunWorkerCompleted);
            d.DecodeAsync(input.Stream, file);
        }

        void d_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressText.Text = "Decompressing " + file + "... " + e.ProgressPercentage + "%";
            Progress.Value = e.ProgressPercentage;
        }

        void d_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dictionary = new Dictionary(file);

            foreach (string key in dictionary.Headers)
            {
                InfoList.Items.Add(String.Format("{0}={1}", key, dictionary.Header(key)));
            }

            InfoList.Items.Add(dictionary.Count + " items in dictionary");
            IndexButton.IsEnabled = true;
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
    }
}