using NovaPointLibrary;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public partial class RecycleBinForm : UserControl, INotifyPropertyChanged
    {
        public SPORecycleBinItemParameters Parameters { get; set; } = new();


        public List<string> listDates = new();
        public List<string> lisHours = new();


        private bool _allItems = true;
        public bool AllItems
        {
            get { return _allItems; }
            set
            {
                _allItems = value;
                Parameters.AllItems = value;
                OnPropertyChanged();
            }
        }

        private bool _filterItems = false;
        public bool FilterItems
        {
            get { return _filterItems; }
            set
            {
                _filterItems = value;
                if (value) { FilterPanel.Visibility = Visibility.Visible; }
                else
                {
                    FilterPanel.Visibility = Visibility.Collapsed;

                    FirstStage = true;
                    SecondStage = true;

                    CBAfterDates.SelectedIndex = 0;
                    CBAfterHour.SelectedIndex = 0;
                    CBBeforeDates.SelectedIndex = 95;
                    CBBeforeHour.SelectedIndex = 47;

                    DeletedByEmail = string.Empty;
                    OriginalLocation = string.Empty;
                    FileSizeMb = 0;
                    FileSizeAbove = true;

                    OnPropertyChanged();
                }
            }
        }

        private bool _firstStage = true;
        public bool FirstStage
        {
            get { return _firstStage; }
            set
            {
                _firstStage = value;
                Parameters.FirstStage = value;
                OnPropertyChanged();
            }
        }

        private bool _secondStage = true;
        public bool SecondStage
        {
            get { return _secondStage; }
            set
            {
                _secondStage = value;
                Parameters.SecondStage = value;
                OnPropertyChanged();
            }
        }

        private DateTime _deletedAfter;
        public DateTime DeletedAfter
        {
            get { return _deletedAfter; }
            set
            {
                _deletedAfter = value;
                Parameters.DeletedAfter = value;
                OnPropertyChanged();
            }
        }


        private DateTime _deletedBefore;
        public DateTime DeletedBefore
        {
            get { return _deletedBefore; }
            set
            {
                _deletedBefore = value;
                Parameters.DeletedBefore = value;
                OnPropertyChanged();
            }
        }

        private string _deletedByEmail;
        public string DeletedByEmail
        {
            get { return _deletedByEmail; }
            set
            {
                _deletedByEmail = value;
                Parameters.DeletedByEmail = value;
                OnPropertyChanged();
            }
        }

        private string _originalLocation;
        public string OriginalLocation
        {
            get { return _originalLocation; }
            set
            {
                _originalLocation = value;
                Parameters.OriginalLocation = value;
                OnPropertyChanged();
            }
        }

        private int _fileSizeMb;
        public int FileSizeMb
        {
            get { return _fileSizeMb; }
            set
            {
                _fileSizeMb = value;
                Parameters.FileSizeMb = value;
                OnPropertyChanged();
            }
        }

        private bool _fileSizeAbove = true;
        public bool FileSizeAbove
        {
            get { return _fileSizeAbove; }
            set
            {
                _fileSizeAbove = value;
                Parameters.FileSizeAbove = value;
                OnPropertyChanged();
            }
        }



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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            string? date = CBAfterDates.SelectedItem as string;
            string? hour = CBAfterHour.SelectedItem as string;

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
            string? date = CBBeforeDates.SelectedItem as string;
            string? hour = CBBeforeHour.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(date) && !string.IsNullOrWhiteSpace(hour))
            {
                string value = date + " " + hour;

                DateTime dateTime = DateTime.ParseExact(value, "MMM dd, yyyy HH:mm",
                                    System.Globalization.CultureInfo.InvariantCulture);

                DeletedBefore = dateTime;
            }
        }

    }
}
