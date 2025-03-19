using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Controls.UserControls
{
    public partial class DateTimeSelector : UserControl
    {
        public DateTime DateTimeSelected
        {
            get => (DateTime)GetValue(DateTimeSelectedProperty);
            set => SetValue(DateTimeSelectedProperty, value);
        }

        public static readonly DependencyProperty DateTimeSelectedProperty = DependencyProperty.Register(
            nameof(DateTimeSelected),
            typeof(DateTime),
            typeof(DateTimeSelector),
            new FrameworkPropertyMetadata(
                defaultValue: DateTime.UtcNow,
                flags: FrameworkPropertyMetadataOptions.AffectsMeasure));


        public bool IsAfter
        {
            get => (bool)GetValue(IsAfterProperty);
            set => SetValue(IsAfterProperty, value);
        }

        public static readonly DependencyProperty IsAfterProperty = DependencyProperty.Register(
            nameof(IsAfter),
            typeof(bool),
            typeof(DateTimeSelector),
            new FrameworkPropertyMetadata(
                defaultValue: false,
                propertyChangedCallback: OnYearChanged,
                flags: FrameworkPropertyMetadataOptions.AffectsMeasure));

        public int FirstYear
        {
            get => (int)GetValue(FirstYearProperty);
            set => SetValue(FirstYearProperty, value);
        }

        public static readonly DependencyProperty FirstYearProperty = DependencyProperty.Register(
            nameof(FirstYear),
            typeof(int),
            typeof(DateTimeSelector),
            new FrameworkPropertyMetadata(
                defaultValue: 1999,
                propertyChangedCallback: OnYearChanged,
                flags: FrameworkPropertyMetadataOptions.AffectsMeasure));

        public int SelectedYear
        {
            get => (int)GetValue(SelectedYearProperty);
            set => SetValue(SelectedYearProperty, value);
        }

        public static readonly DependencyProperty SelectedYearProperty = DependencyProperty.Register(
            nameof(SelectedYear),
            typeof(int),
            typeof(DateTimeSelector),
            new FrameworkPropertyMetadata(
                defaultValue: 2010,
                propertyChangedCallback: OnYearChanged,
                flags: FrameworkPropertyMetadataOptions.AffectsMeasure));

        public List<string> ListYears = [];

        public List<string> ListMonths =
        [
            "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"
        ];
        public List<string> ListDays = [];
        public List<string> ListHours = [];

        public DateTimeSelector()
        {
            InitializeComponent();

            Reset();
        }

        public void Reset()
        {
            AddYears();
            AddMonths();
            AddDays();
            AddHours();
            SetDateTimeSelected();
        }

        private static void OnYearChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DateTimeSelector)d;
            control.Reset();
        }

        private void AddYears()
        {
            int todayYear = DateTime.Today.Year;
            ListYears = [];
            for (int year = FirstYear; year <= todayYear; year++)
            {
                ListYears.Add(year.ToString());
            }
            ComboBoxYear.ItemsSource = ListYears;
            if (IsAfter)
            {
                int y = ListYears.IndexOf(SelectedYear.ToString());
                if (y > -1) { ComboBoxYear.SelectedIndex = y; }
                else { ComboBoxYear.SelectedIndex = 0; }
            }
            else { ComboBoxYear.SelectedIndex = ListYears.Count - 1; }
        }

        private void AddMonths()
        {
            ComboBoxMonth.ItemsSource = ListMonths;
            if (IsAfter) { ComboBoxMonth.SelectedIndex = 0; }
            else { ComboBoxMonth.SelectedIndex = ListMonths.Count - 1; }
        }
        private void AddDays()
        {
            ListDays = [];
            for (int day = 1; day <= 31; day++)
            {
                ListDays.Add(day.ToString());
            }
            ComboBoxDay.ItemsSource = ListDays;
            if (IsAfter) { ComboBoxDay.SelectedIndex = 0; }
            else { ComboBoxDay.SelectedIndex = ListDays.Count - 1; }
        }

        private void AddHours()
        {
            ListHours = [];
            List<string> hours = new() { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23" };
            foreach (var h in hours)
            {
                ListHours.Add($"{h}:00");
                ListHours.Add($"{h}:30");
            }
            ComboBoxHour.ItemsSource = ListHours;
            if (IsAfter) { ComboBoxHour.SelectedIndex = 0; }
            else { ComboBoxHour.SelectedIndex = ListHours.Count - 1; }
        }

        private void DateTimeAfterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetDateTimeSelected();
        }

        private void SetDateTimeSelected()
        {
            string? year = ComboBoxYear.SelectedItem as string;
            string? month = ComboBoxMonth.SelectedItem as string;
            string? day = ComboBoxDay.SelectedItem as string;
            string? hour = ComboBoxHour.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(year) && !string.IsNullOrWhiteSpace(month) && !string.IsNullOrWhiteSpace(day) && !string.IsNullOrWhiteSpace(hour))
            {
                string value = year + " " + month + " " + day + " " + hour;

                DateTime dateTime;
                try
                {
                    dateTime = DateTime.ParseExact(value, "yyyy MMMM d HH:mm",
                                        System.Globalization.CultureInfo.InvariantCulture);

                    DateTimeSelected = dateTime;
                }
                catch
                {
                    int y = FirstYear + ListYears.IndexOf(year);
                    int m = ListMonths.IndexOf(month) + 1;
                    var daysMonth = DateTime.DaysInMonth(y, m);
                    ComboBoxDay.SelectedIndex = daysMonth - 1;
                }
            }
        }
    }
}
