using NovaPointLibrary.Commands.SharePoint.SharingLinks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.UserControls
{
    /// <summary>
    /// Interaction logic for SharingLinksFilterForm.xaml
    /// </summary>
    public partial class SharingLinksFilterForm : UserControl , INotifyPropertyChanged
    {
        public SpoSharingLinksFilter Parameters { get; set; } = new();

        private bool _allTypes = false;
        public bool AllTypes
        {
            get { return _allTypes; }
            set
            {
                if (value && !IsAllTypesIncluded())
                {
                    ChangeIncludedTypes(true);
                }
                else if (!value && IsAllTypesIncluded())
                {
                    ChangeIncludedTypes(false);
                }

                _allTypes = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeAnyone
        {
            get { return Parameters.IncludeAnyone; }
            set
            {
                Parameters.IncludeAnyone = value;
                OnPropertyChanged();

                if (!value) { AllTypes = false; }
                else { CheckControlTypes(); }
            }
        }

        public bool IncludeOrganization
        {
            get { return Parameters.IncludeOrganization; }
            set
            {
                Parameters.IncludeOrganization = value;
                OnPropertyChanged();

                if (!value) { AllTypes = false; }
                else { CheckControlTypes(); }
            }
        }

        public bool IncludeSpecific
        {
            get { return Parameters.IncludeSpecific; }
            set
            {
                Parameters.IncludeSpecific = value;
                OnPropertyChanged();

                if (!value) { AllTypes = false; }
                else { CheckControlTypes(); }
            }
        }


        private bool _allPermissions = false;
        public bool AllPermissions
        {
            get { return _allPermissions; }
            set
            {
                if (value && !IsAllPermissionsIncluded())
                {
                    ChangeIncludedPermissions(true);
                }
                else if (!value && IsAllPermissionsIncluded())
                {
                    ChangeIncludedPermissions(false);
                }


                _allPermissions = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeCanEdit
        {
            get { return Parameters.IncludeCanEdit; }
            set
            {
                Parameters.IncludeCanEdit = value;
                OnPropertyChanged();

                if (!value) { AllPermissions = false; }
                else { CheckControlPermissions(); }
            }
        }

        public bool IncludeCanReview
        {
            get { return Parameters.IncludeCanReview; }
            set
            {
                Parameters.IncludeCanReview = value;
                OnPropertyChanged();

                if (!value) { AllPermissions = false; }
                else { CheckControlPermissions(); }
            }
        }

        public bool IncludeCanNotDownload
        {
            get { return Parameters.IncludeCanNotDownload; }
            set
            {
                Parameters.IncludeCanNotDownload = value;
                OnPropertyChanged();

                if (!value) { AllPermissions = false; }
                else { CheckControlPermissions(); }
            }
        }

        public bool IncludeCanView
        {
            get { return Parameters.IncludeCanView; }
            set
            {
                Parameters.IncludeCanView = value;
                OnPropertyChanged();

                if (!value) { AllPermissions = false; }
                else { CheckControlPermissions(); }
            }
        }

        public string FilterCreatedBy
        {
            get { return Parameters.FilterCreatedBy; }
            set
            {
                Parameters.FilterCreatedBy = value;
                OnPropertyChanged();
            }
        }

        public int DaysOld
        {
            get { return Parameters.DaysOld; }
            set
            {
                Parameters.DaysOld = value;
                OnPropertyChanged();
            }
        }

        public SharingLinksFilterForm()
        {
            InitializeComponent();

            DataContext = this;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CheckControlTypes()
        {
            if (IsAllTypesIncluded())
            {
                AllTypes = true;
            }
        }

        private bool IsAllTypesIncluded()
        {
            if (IncludeAnyone && IncludeOrganization && IncludeSpecific)
            {
                return true;
            }
            else { return false; }
        }

        private void ChangeIncludedTypes(bool IsChecked)
        {
            IncludeAnyone = IsChecked;
            IncludeOrganization = IsChecked;
            IncludeSpecific = IsChecked;
        }


        private void CheckControlPermissions()
        {
            if (IsAllPermissionsIncluded())
            {
                AllPermissions = true;
            }
        }

        private bool IsAllPermissionsIncluded()
        {
            if (IncludeCanEdit && IncludeCanReview && IncludeCanNotDownload && IncludeCanView)
            {
                return true;
            }
            else { return false; }
        }

        private void ChangeIncludedPermissions(bool IsChecked)
        {
            IncludeCanEdit = IsChecked;
            IncludeCanReview = IsChecked;
            IncludeCanNotDownload = IsChecked;
            IncludeCanView = IsChecked;
        }
    }
}
