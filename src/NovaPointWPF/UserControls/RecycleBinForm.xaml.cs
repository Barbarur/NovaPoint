using NovaPointLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    /// Interaction logic for RecycleBinForm.xaml
    /// </summary>
    public partial class RecycleBinForm : UserControl
    {
        public List<string> listDates = new();
        public List<string> lisHours = new();

        public RecycleBinForm()
        {
            InitializeComponent();

            CBAfterDates.ItemsSource = listDates;
            CBAfterHour.ItemsSource = lisHours;
            CBBeforeDates.ItemsSource = listDates;
            CBBeforeHour.ItemsSource = lisHours;

            AddDatesHours();
            
            CBAfterDates.SelectedIndex = 0;
            CBAfterHour.SelectedIndex = 0;
            CBBeforeDates.SelectedIndex = 95;
            CBBeforeHour.SelectedIndex = 47;
        }


        private void AddDatesHours()
        {
            DateTime startDate = DateTime.UtcNow.AddDays(-94);
            DateTime endDate = DateTime.UtcNow.AddDays(1);

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                listDates.Add(date.ToString("MMM dd, yyyy"));
            }

            List<string> hours = new() { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" };
            foreach (var h in hours)
            {
                lisHours.Add($"{h}:00");
                lisHours.Add($"{h}:30");

            }
        }

        private void DateTimeAfterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string date = CBAfterDates.SelectedItem as string;
            string hour = CBAfterHour.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(date) && !string.IsNullOrWhiteSpace(hour))
            {
                string value = date + " " + hour;

                DateTime dateTime = DateTime.ParseExact(value, "MMM dd, yyyy HH:mm",
                                    System.Globalization.CultureInfo.InvariantCulture);

                DeletedAfter = dateTime;
            }
        }

        private void DateTimeBeforeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string date = CBBeforeDates.SelectedItem as string;
            string hour = CBBeforeHour.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(date) && !string.IsNullOrWhiteSpace(hour))
            {
                string value = date + " " + hour;

                DateTime dateTime = DateTime.ParseExact(value, "MMM dd, yyyy HH:mm",
                                    System.Globalization.CultureInfo.InvariantCulture);

                DeletedBefore = dateTime;
            }
        }

        public bool FirstStage
        {
            get { return (bool)GetValue(FirstStageProperty); }
            set { SetValue(FirstStageProperty, value); }
        }
        public static readonly DependencyProperty FirstStageProperty =
            DependencyProperty.Register("FirstStage", typeof(bool), typeof(RecycleBinForm), new FrameworkPropertyMetadata(defaultValue: true));

        public bool SecondStage
        {
            get { return (bool)GetValue(SecondStageProperty); }
            set { SetValue(SecondStageProperty, value); }
        }
        public static readonly DependencyProperty SecondStageProperty =
            DependencyProperty.Register("SecondStage", typeof(bool), typeof(RecycleBinForm), new FrameworkPropertyMetadata(defaultValue: false));



        public DateTime DeletedAfter
        {
            get { return (DateTime)GetValue(DeletedAfterProperty); }
            set { SetValue(DeletedAfterProperty, value); }
        }
        public static readonly DependencyProperty DeletedAfterProperty =
            DependencyProperty.Register("DeletedAfter", typeof(DateTime), typeof(RecycleBinForm), new FrameworkPropertyMetadata(defaultValue: DateTime.UtcNow));

        public DateTime DeletedBefore
        {
            get { return (DateTime)GetValue(DeletedBeforeProperty); }
            set { SetValue(DeletedBeforeProperty, value); }
        }
        public static readonly DependencyProperty DeletedBeforeProperty =
            DependencyProperty.Register("DeletedBefore", typeof(DateTime), typeof(RecycleBinForm), new FrameworkPropertyMetadata(defaultValue: DateTime.UtcNow));



        public string DeletedByEmail
        {
            get { return (string)GetValue(DeletedByEmailProperty); }
            set { SetValue(DeletedByEmailProperty, value); }
        }
        public static readonly DependencyProperty DeletedByEmailProperty =
            DependencyProperty.Register("DeletedByEmail", typeof(string), typeof(RecycleBinForm), new PropertyMetadata(string.Empty));

        public string OriginalLocation
        {
            get { return (string)GetValue(OriginalLocationProperty); }
            set { SetValue(OriginalLocationProperty, value); }
        }
        public static readonly DependencyProperty OriginalLocationProperty =
            DependencyProperty.Register("OriginalLocation", typeof(string), typeof(RecycleBinForm), new PropertyMetadata(string.Empty));

        public int FileSizeMb
        {
            get { return (int)GetValue(FileSizeMbProperty); }
            set { SetValue(FileSizeMbProperty, value); }
        }
        public static readonly DependencyProperty FileSizeMbProperty =
            DependencyProperty.Register("FileSizeMb", typeof(int), typeof(RecycleBinForm), new PropertyMetadata(0));

        public bool FileSizeAbove
        {
            get { return (bool)GetValue(FileSizeAboveProperty); }
            set { SetValue(FileSizeAboveProperty, value); }
        }
        public static readonly DependencyProperty FileSizeAboveProperty =
            DependencyProperty.Register("FileSizeAbove", typeof(bool), typeof(RecycleBinForm), new FrameworkPropertyMetadata(defaultValue: true));

    }
}
