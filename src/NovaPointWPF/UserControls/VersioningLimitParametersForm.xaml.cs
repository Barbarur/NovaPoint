using NovaPointLibrary.Solutions.Automation;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.UserControls
{
    /// <summary>
    /// Interaction logic for VersioningLimitParametersForm.xaml
    /// </summary>
    public partial class VersioningLimitParametersForm : UserControl, INotifyPropertyChanged
    {
        public VersioningLimitParameters Parameters { get; set; } = new();

        public bool LibrarySetVersioningSettings
        {
            get { return Parameters.LibrarySetVersioningSettings; }
            set
            {
                if (value)
                {
                    LibraryVersionForm.Visibility = Visibility.Visible;
                    LibraryNewLibraries = true;
                    LibraryExistingLibraries = true;
                    LibraryCustomLimits = true;
                    LibraryManualVersionLimit = true;
                }
                else
                {
                    LibraryVersionForm.Visibility = Visibility.Collapsed;
                    LibraryNewLibraries = false;
                    LibraryExistingLibraries = false;
                }

                Parameters.LibrarySetVersioningSettings = value;
                OnPropertyChanged();
            }
        }
        public bool LibraryNewLibraries
        {
            get { return Parameters.LibraryNewLibraries; }
            set
            {
                Parameters.LibraryNewLibraries = value;
                OnPropertyChanged();
            }
        }

        public bool LibraryExistingLibraries
        {
            get { return Parameters.LibraryExistingLibraries; }
            set
            {
                Parameters.LibraryExistingLibraries = value;
                OnPropertyChanged();
                if (value)
                {
                    ExistingLibrariesForm.Visibility = Visibility.Visible;
                    LibraryApplyToAllExistingLibraries = true;
                }
                else
                {
                    ExistingLibrariesForm.Visibility = Visibility.Collapsed;
                    LibraryApplyToAllExistingLibraries = false;
                    LibrarySingle = false;
                }
            }
        }
        public bool LibraryApplyToAllExistingLibraries
        {
            get { return Parameters.LibraryApplyToAllExistingLibraries; }
            set
            {
                Parameters.LibraryApplyToAllExistingLibraries = value;
                OnPropertyChanged();
            }
        }

        internal bool _librarySingle;
        public bool LibrarySingle
        {
            get { return _librarySingle; }
            set
            {
                _librarySingle = value;
                OnPropertyChanged();
                if (value)
                {
                    SingleLibraryForm.Visibility = Visibility.Visible;
                }
                else
                {
                    LibraryApplyToSingleLibraryTitle = string.Empty;
                    SingleLibraryForm.Visibility = Visibility.Collapsed;
                }
            }
        }

        public string LibraryApplyToSingleLibraryTitle
        {
            get { return Parameters.LibraryApplyToSingleLibraryTitle; }
            set
            {
                Parameters.LibraryApplyToSingleLibraryTitle = value;
                OnPropertyChanged();
            }
        }

        public bool LibraryInheritTenantVersionSettings
        {
            get { return Parameters.LibraryInheritTenantVersionSettings; }
            set
            {
                Parameters.LibraryInheritTenantVersionSettings = value;
                OnPropertyChanged();
            }
        }

        private bool _libraryCustomLimits;
        public bool LibraryCustomLimits
        {
            get { return _libraryCustomLimits; }
            set
            {
                _libraryCustomLimits = value;
                OnPropertyChanged();
                if(value)
                {
                    CustomLimitForm.Visibility = Visibility.Visible;
                }
                else
                {
                    CustomLimitForm.Visibility = Visibility.Collapsed;
                }
            }
        }

        public bool LibraryEnableVersioning
        {
            get { return !Parameters.LibraryEnableVersioning; }
            set
            {
                Parameters.LibraryEnableVersioning = !value;
                OnPropertyChanged();
            }
        }

        public bool LibraryAutomaticVersionLimit
        {
            get { return Parameters.LibraryAutomaticVersionLimit; }
            set
            {
                Parameters.LibraryAutomaticVersionLimit = value;
                OnPropertyChanged();
            }
        }

        internal bool _libraryManualVersionLimit;
        public bool LibraryManualVersionLimit
        {
            get { return _libraryManualVersionLimit; }
            set
            {
                _libraryManualVersionLimit = value;
                OnPropertyChanged();
                if (value)
                {
                    ManualVersionForm.Visibility = Visibility.Visible;
                }
                else
                {
                    ManualVersionForm.Visibility = Visibility.Collapsed;
                    LibraryMajorVersionLimit = 500;
                    LibraryExpirationDays = 0;
                    LibraryMinorVersionLimit = 0;
                }
            }
        }

        public int LibraryMajorVersionLimit
        {
            get { return Parameters.LibraryMajorVersionLimit; }
            set
            {
                Parameters.LibraryMajorVersionLimit = value;
                OnPropertyChanged();
            }
        }

        public int LibraryExpirationDays
        {
            get { return Parameters.LibraryExpirationDays; }
            set
            {
                Parameters.LibraryExpirationDays = value;
                OnPropertyChanged();
            }
        }

        public int LibraryMinorVersionLimit
        {
            get { return Parameters.LibraryMinorVersionLimit; }
            set
            {
                Parameters.LibraryMinorVersionLimit = value;
                OnPropertyChanged();
            }
        }


        public bool ListSetVersioningSettings
        {
            get { return Parameters.ListSetVersioningSettings; }
            set
            {
                Parameters.ListSetVersioningSettings = value;
                OnPropertyChanged();
                if (value)
                {
                    ListVersionForm.Visibility = Visibility.Visible;
                    ListApplyToAllExistingLists = true;
                    ListManualVersionLimit = true;
                }
                else
                {
                    ListVersionForm.Visibility = Visibility.Collapsed;
                    ListApplyToAllExistingLists = false;
                    ListSingle = false;
                    ListManualVersionLimit = false;
                }
            }
        }

        public bool ListApplyToAllExistingLists
        {
            get { return Parameters.ListApplyToAllExistingLists; }
            set
            {
                Parameters.ListApplyToAllExistingLists = value;
                OnPropertyChanged();
            }
        }

        internal bool _listSingle;
        public bool ListSingle
        {
            get { return _listSingle; }
            set
            {
                _listSingle = value;
                OnPropertyChanged();
                if (value)
                {
                    SingleListForm.Visibility = Visibility.Visible;
                }
                else
                {
                    SingleListForm.Visibility = Visibility.Collapsed;
                    ListApplySingleListTitle = string.Empty;
                }
            }
        }

        public string ListApplySingleListTitle
        {
            get { return Parameters.ListApplySingleListTitle; }
            set
            {
                Parameters.ListApplySingleListTitle = value;
                OnPropertyChanged();
            }
        }

        public bool ListEnableVersioning
        {
            get { return !Parameters.ListEnableVersioning; }
            set
            {
                Parameters.ListEnableVersioning = !value;
                OnPropertyChanged();
            }
        }

        internal bool _listManualVersionLimit;
        public bool ListManualVersionLimit
        {
            get { return _listManualVersionLimit; }
            set
            {
                _listManualVersionLimit = value;
                OnPropertyChanged();
                if (value)
                {
                    ManualVersionListForm.Visibility = Visibility.Visible;
                }
                else
                {
                    ManualVersionListForm.Visibility = Visibility.Collapsed;
                    ListMajorVersionLimit = 500;
                }
            }
        }

        public int ListMajorVersionLimit
        {
            get { return Parameters.ListMajorVersionLimit; }
            set
            {
                Parameters.ListMajorVersionLimit = value;
                OnPropertyChanged();
            }
        }



        public VersioningLimitParametersForm()
        {
            InitializeComponent();

            LibrarySetVersioningSettings = true;

            ListSetVersioningSettings = true;
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
