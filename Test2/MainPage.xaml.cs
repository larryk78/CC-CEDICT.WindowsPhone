using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using SevenZip.Compression.LZMA.WindowsPhone;
using CC_CEDICT.WindowsPhone;

namespace Test2
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            SearchButton.IsEnabled = false;

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        // Handle selection changed on ListBox
        private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainListBox.SelectedIndex == -1) return;
            MessageBox.Show(d[((ItemViewModel)(e.AddedItems[0])).Index].ToString());
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string file in new List<string> { "cedict_ts.u8", "english.csv", "pinyin.csv", "hanzi.csv" })
            {
                Resource2IsolatedStorageDecoder decoder = new Resource2IsolatedStorageDecoder();
                decoder.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(decoder_ProgressChanged);
                decoder.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(decoder_RunWorkerCompleted);
                decoder.DecodeAsync(file + ".lzma", file);
            }
        }

        void decoder_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            Status.Text = "Decompressing... " + e.ProgressPercentage + "%";
            Progress.Value = e.ProgressPercentage;
        }

        int x = 0;
        Dictionary d;
        Searcher s;
        void decoder_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (++x < 4) return;
            SearchButton.IsEnabled = true;
            d = new Dictionary("cedict_ts.u8");
            s = new Searcher(d, new Index("english.csv"), new Index("pinyin.csv"), new Index("hanzi.csv"));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DateTime start = DateTime.Now;
            List<DictionaryRecord> results = s.Search(Query.Text);
            TimeSpan elapsed = DateTime.Now - start;
            Status.Text = String.Format("Search: '{0}' took {1:f2}s. ({2} results)", s.LastQuery, elapsed.TotalSeconds, results.Count);
            App.ViewModel.LoadData(results);
        }
    }
}