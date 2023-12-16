using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NovaPointWPF.UserControls
{
    /// <summary>
    /// Interaction logic for ComboBoxTesting.xaml
    /// </summary>
    public partial class ComboBoxTesting : UserControl
    {
        //public List<string> listDataA = new() { "aaa", "bbb" };
        //public List<string> listDataB = new() { "lll", "mmm" };
        public List<string> listDates = new();
        public List<string> lisHours = new();
        public ComboBoxTesting()
        {
            InitializeComponent();

            DataContext = this;

            //listDataA.Add("ccc");
            //listDataA.Add("ddd");
            //listDataA.Add("eee");
            //CMB.ItemsSource = listDataB;

            //listDataB.Add("rrr");
            //listDataB.Add("sss");

            CBAfterDates.ItemsSource = listDates;
            CBAfterHour.ItemsSource = lisHours;
            CBBefore.ItemsSource = listDates;
            AddDatesHours();
            CBAfterDates.SelectedIndex = 0;
            CBAfterHour.SelectedIndex = 20;
            CBBefore.SelectedIndex = 95;
        }

        //private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        //{
        //    var comboBox = sender as ComboBox;
        //    comboBox.ItemsSource = listDataA;

        //    listDataB.Add("nnn");
        //    listDataB.Add("ooo");
        //    listDataB.Add("ppp");

        //}

        private void AddDatesHours()
        {
            //DateTime startDate = new DateTime(2023, 3, 1);
            //DateTime endDate = new DateTime(2023, 3, 10);
            DateTime startDate = DateTime.UtcNow.AddDays(-94);
            DateTime endDate = DateTime.UtcNow.AddDays(1);

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                listDates.Add(date.ToString("MMM dd, yyyy"));
            }

            List<string> hours = new() { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" };
            foreach(var h in hours)
            {
                lisHours.Add($"{h}:00");
                lisHours.Add($"{h}:30");

            }
            //for (int hour = 0; hour <= 23; hour++)
            //{
            //}
        }



        private void CBAfter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            //CBALabel.Content = comboBox.SelectedItem as string;

            //string value = comboBox.SelectedItem as string;
            string date = CBAfterDates.SelectedItem as string;
            string hour = CBAfterHour.SelectedItem as string;

            if(!string.IsNullOrWhiteSpace(date) && !string.IsNullOrWhiteSpace(hour))
            {
                string value = date + " " + hour;

                DateTime dateTime = DateTime.ParseExact(value, "MMM dd, yyyy HH:mm",
                                    System.Globalization.CultureInfo.InvariantCulture);

                CBALabel.Content = dateTime.ToString("yyyyMMddHHmm");

                CBALabelFullTime.Content = dateTime.DayOfWeek;
            }

            //CBALabel.Content = value;

        }

        private void CBBefore_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        //https://learn.microsoft.com/en-us/dotnet/api/system.datetime?view=net-8.0#parse-a-string-that-represents-a-datetime

    }
}
