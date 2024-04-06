using Microsoft.Graph;
using NovaPointLibrary.Commands.SharePoint.PreservationHoldLibrary;
using PnP.Core.QueryModel;
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for PHLForm.xaml
    /// </summary>
    public partial class PHLForm : UserControl, INotifyPropertyChanged
    {

        public SPOPreservationHoldLibraryParameters Parameters { get; set; } = new();


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

                    RetainedByDate = false;
                    ModifiedByEmail = string.Empty;
                    OriginalLocation = string.Empty;
                    AboveFileSizeMb = 0;

                }
                    OnPropertyChanged();
            }
        }

        public bool _retainedByDate = false;
        public bool RetainedByDate
        { 
            get { return _retainedByDate; }
            set
            {
                if (value)
                {
                    DateFilter.Visibility = Visibility.Visible;
                }
                else
                {
                    DateFilter.Visibility = Visibility.Collapsed;

                    CBAfterYear.SelectedIndex = 0;
                    CBAfterMonth.SelectedIndex = 0;
                    CBAfterDay.SelectedIndex = 0;
                    CBAfterHour.SelectedIndex = 0;

                    CBBeforeYear.SelectedIndex = listYears.Count - 1;
                    CBBeforeMonth.SelectedIndex = DateTime.Today.Month - 1;
                    CBBeforeDay.SelectedIndex = DateTime.Today.Day - 1;
                    CBBeforeHour.SelectedIndex = 47;
                }
                _retainedByDate = value;
                Parameters.RetainedByDate = value;
                OnPropertyChanged();
            }
        }

        private DateTime _retainedAfter;
        public DateTime RetainedAfter
        {
            get { return _retainedAfter; }
            set
            {
                _retainedAfter = value;
                Parameters.RetainedAfterDate = value;
                OnPropertyChanged();
            }
        }


        private DateTime _retainedBefore;
        public DateTime RetainedBefore
        {
            get { return _retainedBefore; }
            set
            {
                _retainedBefore = value;
                Parameters.RetainedBeforeDate = value;
                OnPropertyChanged();
            }
        }

        private string _itemName = string.Empty;
        public string ItemName
        {
            get { return _itemName; }
            set
            {
                _itemName = value;
                Parameters.ItemName = value;
                OnPropertyChanged();
            }
        }

        private string _originalLocation = string.Empty;
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

        private string _modifiedByEmail = string.Empty;
        public string ModifiedByEmail
        {
            get { return _modifiedByEmail; }
            set
            {
                _modifiedByEmail = value;
                Parameters.ModifiedByEmail = value;
                OnPropertyChanged();
            }
        }


        private int _aboveFileSizeMb = 0;
        public int AboveFileSizeMb
        {
            get { return _aboveFileSizeMb; }
            set
            {
                _aboveFileSizeMb = value;
                Parameters.AboveFileSizeMb = value;
                OnPropertyChanged();
            }
        }


        public List<string> listDates = new();
        public List<string> listYears = new();
        public List<string> listMonths = new()
        {
            "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"
        };
        public List<string> listDays = new();
        public List<string> listHours = new();


        public PHLForm()
        {
            InitializeComponent();

            CBAfterYear.ItemsSource = listYears;
            CBAfterMonth.ItemsSource = listMonths;
            CBAfterDay.ItemsSource = listDays;
            CBAfterHour.ItemsSource = listHours;

            CBBeforeYear.ItemsSource = listYears;
            CBBeforeMonth.ItemsSource = listMonths;
            CBBeforeDay.ItemsSource = listDays;
            CBBeforeHour.ItemsSource = listHours;

            AddDatesHours();

            CBAfterYear.SelectedIndex = 0;
            CBAfterMonth.SelectedIndex = 0;
            CBAfterDay.SelectedIndex = 0;
            CBAfterHour.SelectedIndex = 0;

            CBBeforeYear.SelectedIndex = listYears.Count -1;
            CBBeforeMonth.SelectedIndex = DateTime.Today.Month - 1;
            CBBeforeDay.SelectedIndex = DateTime.Today.Day - 1;
            CBBeforeHour.SelectedIndex = 47;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _startYear = 2010;

        private void AddDatesHours()
        {
            int todayYear = DateTime.Today.Year;
            for (int year = _startYear; year <= todayYear; year++)
            {
                listYears.Add(year.ToString());
            }
            
            for (int day = 1; day <= 31;  day++)
            {
                listDays.Add(day.ToString());
            }

            List<string> hours = new() { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" };
            foreach (var h in hours)
            {
                listHours.Add($"{h}:00");
                listHours.Add($"{h}:30");

            }
        }

        private void DateTimeAfterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? year = CBAfterYear.SelectedItem as string;
            string? month = CBAfterMonth.SelectedItem as string;
            string? day = CBAfterDay.SelectedItem as string;
            string? hour = CBAfterHour.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(year) && !string.IsNullOrWhiteSpace(month) && !string.IsNullOrWhiteSpace(day) && !string.IsNullOrWhiteSpace(hour))
            {
                string value = year + " " + month + " " + day + " " + hour;

                DateTime dateTime;
                try
                {
                    dateTime = DateTime.ParseExact(value, "yyyy MMMM d HH:mm",
                                        System.Globalization.CultureInfo.InvariantCulture);

                    RetainedAfter = dateTime;
                }
                catch
                {
                    int y = _startYear + listYears.IndexOf(year);
                    int m = listMonths.IndexOf(month) + 1;
                    var daysMonth = DateTime.DaysInMonth(y, m);
                    CBAfterDay.SelectedIndex = daysMonth - 1;
                }
            }
        }

        private void DateTimeBeforeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? year = CBBeforeYear.SelectedItem as string;
            string? month = CBBeforeMonth.SelectedItem as string;
            string? day = CBBeforeDay.SelectedItem as string;
            string? hour = CBBeforeHour.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(year) && !string.IsNullOrWhiteSpace(month) && !string.IsNullOrWhiteSpace(day) && !string.IsNullOrWhiteSpace(hour))
            {
                string value = year + " " + month + " " + day + " " + hour;

                DateTime dateTime;
                try
                {
                    dateTime = DateTime.ParseExact(value, "yyyy MMMM d HH:mm",
                                        System.Globalization.CultureInfo.InvariantCulture);

                    RetainedBefore = dateTime;
                }
                catch
                {
                    int y = _startYear + listYears.IndexOf(year);
                    int m = listMonths.IndexOf(month) + 1;
                    var daysMonth = DateTime.DaysInMonth(y, m);
                    CBBeforeDay.SelectedIndex = daysMonth - 1;
                }
            }
        }
    }
}
