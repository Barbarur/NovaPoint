using NovaPointLibrary.Commands.SharePoint.User;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.UserControls
{
    public partial class UserSiteForm : UserControl, INotifyPropertyChanged
    {
        public SPOSiteUserParameters Parameters { get; set; } = new();
        
        public bool AllUsers
        {
            get { return Parameters.AllUsers; }
            set
            {
                Parameters.AllUsers = value;
                OnPropertyChanged();
                if (value && IsAnyUserSelected())
                {
                    SingleUser = false;
                    IncludeExternalUsers = false;
                    IncludeSystemGroups = false;
                }
            }
        }

        private bool _singleUser = true;
        public bool SingleUser
        {
            get { return _singleUser; }
            set
            {
                _singleUser = value;
                CheckDetailed();
                OnPropertyChanged();

                if (value)
                { 
                    AllUsers = false;
                    SingleUserPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    CheckUsers();
                    IncludeUserUPN = string.Empty;
                    SingleUserPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        public string IncludeUserUPN
        {
            get { return Parameters.IncludeUserUPN; }
            set
            { 
                Parameters.IncludeUserUPN = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeExternalUsers
        {
            get { return Parameters.IncludeExternalUsers; }
            set
            { 
                Parameters.IncludeExternalUsers = value;
                CheckDetailed();
                OnPropertyChanged();
                
                if (value)
                {
                    AllUsers = false;
                    ExternalLabel.Visibility = Visibility.Visible; 
                }
                else
                {
                    CheckUsers();
                    ExternalLabel.Visibility = Visibility.Collapsed;
                }
            }
        }

        public bool Detailed
        {
            get { return Parameters.Detailed; }
            set
            {
                Parameters.Detailed = value;
                OnPropertyChanged();
            }
        }

        private bool _includeSystemGroups = false;
        public bool IncludeSystemGroups
        {
            get { return _includeSystemGroups; }
            set
            {
                _includeSystemGroups = value;
                OnPropertyChanged();
                if (value)
                {
                    AllUsers = false;
                    SystemGroupPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    CheckUsers();
                    IncludeEveryone = false;
                    IncludeEveryoneExceptExternal = false;
                    SystemGroupPanel.Visibility = Visibility.Collapsed;
                }

            }
        }

        public bool IncludeEveryone
        {
            get { return Parameters.IncludeEveryone; }
            set
            {
                Parameters.IncludeEveryone = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeEveryoneExceptExternal
        {
            get { return Parameters.IncludeEveryoneExceptExternal; }
            set
            {
                Parameters.IncludeEveryoneExceptExternal = value;
                OnPropertyChanged();
            }
        }
        

        public event PropertyChangedEventHandler? PropertyChanged;

        public UserSiteForm()
        {
            InitializeComponent();
        }

        private void OnPropertyChanged( [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CheckUsers()
        {
            if (!IsAnyUserSelected())
            {
                AllUsers = true;
            }
        }

        private bool IsAnyUserSelected()
        {
            if (SingleUser || IncludeExternalUsers || IncludeSystemGroups)
            {
                return true;
            }
            else { return false; }
        }

        private void CheckDetailed()
        {
            if (SingleUser || IncludeExternalUsers)
            {
                DetailedButton.Visibility = Visibility.Visible;
            }
            else
            {
                DetailedButton.Visibility = Visibility.Collapsed;
                Detailed = false;
            }
        }
    }
}
