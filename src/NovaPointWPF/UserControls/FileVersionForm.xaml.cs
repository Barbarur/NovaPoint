using NovaPointLibrary.Commands.SharePoint.PreservationHoldLibrary;
using NovaPointLibrary.Solutions.Automation;
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
    /// Interaction logic for FileVersionForm.xaml
    /// </summary>
    public partial class FileVersionForm : UserControl, INotifyPropertyChanged
    {
        public SPOFileVersionParameters Parameters { get; set; } = new();


        private bool _deleteAll = false;
        public bool DeleteAll
        {
            get { return _deleteAll; }
            set
            {
                _deleteAll = value;
                Parameters.DeleteAll = value;
                OnPropertyChanged();
            }
        }

        private bool _keepVersions = true;
        public bool KeepVersions
        {
            get { return _keepVersions; }
            set
            {
                _keepVersions = value;

                if (value) { KeepVersionsForm.Visibility = Visibility.Visible; }
                else
                {
                    KeepVersionsForm.Visibility = Visibility.Collapsed;

                    Recycle = true;

                    VersionsToKeep = 500;
                    //CreatedBefore = DateTime.MinValue;
                    DateTimeSelectionChanged(null, null);
                    CreatedBeforeYear.SelectedIndex = 0;
                    CreatedBeforeMonth.SelectedIndex = 0;
                    CreatedBeforeDay.SelectedIndex = 0;
                    CreatedBeforeHour.SelectedIndex = 0;
                }
            }
        }

        private bool _recycle = true;
        public bool Recycle
        {
            get { return _recycle; }
            set
            {
                _recycle = value;
                Parameters.Recycle = value;
                OnPropertyChanged();
            }
        }

        private int _versionsToKeep = 500;
        public int VersionsToKeep
        {
            get { return _versionsToKeep; }
            set
            {
                _versionsToKeep = value;
                Parameters.VersionsToKeep = value;
                OnPropertyChanged();
            }
        }

        private DateTime _createdBefore;
        public DateTime CreatedBefore
        {
            get { return _createdBefore; }
            set
            {
                _createdBefore = value;
                Parameters.CreatedBefore = value;
            }
        }




        public List<string> listYears = new();
        public List<string> listMonths = new()
        {
            "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"
        };
        public List<string> listDays = new();
        public List<string> listHours = new();


        public FileVersionForm()
        {
            InitializeComponent();

            CreatedBeforeYear.ItemsSource = listYears;
            CreatedBeforeMonth.ItemsSource = listMonths;
            CreatedBeforeDay.ItemsSource = listDays;
            CreatedBeforeHour.ItemsSource = listHours;

            AddDatesHours();

            CreatedBeforeYear.SelectedIndex = 0;
            CreatedBeforeMonth.SelectedIndex = 0;
            CreatedBeforeDay.SelectedIndex = 0;
            CreatedBeforeHour.SelectedIndex = 0;

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

            for (int day = 1; day <= 31; day++)
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

        private void DateTimeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? year = CreatedBeforeYear.SelectedItem as string;
            string? month = CreatedBeforeMonth.SelectedItem as string;
            string? day = CreatedBeforeDay.SelectedItem as string;
            string? hour = CreatedBeforeHour.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(year) && !string.IsNullOrWhiteSpace(month) && !string.IsNullOrWhiteSpace(day) && !string.IsNullOrWhiteSpace(hour))
            {
                string value = year + " " + month + " " + day + " " + hour;

                DateTime dateTime;
                try
                {
                    dateTime = DateTime.ParseExact(value, "yyyy MMMM d HH:mm",
                                        System.Globalization.CultureInfo.InvariantCulture);

                    CreatedBefore = dateTime;
                }
                catch
                {
                    int y = _startYear + listYears.IndexOf(year);
                    int m = listMonths.IndexOf(month) + 1;
                    var daysMonth = DateTime.DaysInMonth(y, m);
                    CreatedBeforeDay.SelectedIndex = daysMonth - 1;
                }
            }
        }

    }
}
