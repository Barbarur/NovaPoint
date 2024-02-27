using NovaPointLibrary.Commands.SharePoint.User;
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
    public partial class UserSiteForm : UserControl, INotifyPropertyChanged
    {
        public SPOSiteUserParameters Parameters { get; set; } = new();
        
        
        private bool _allUsers = false;
        public bool AllUsers
        {
            get { return _allUsers; }
            set
            {
                _allUsers = value;
                Parameters.AllUsers = value;
                OnPropertyChanged();
                if (value)
                {
                    SingleUser = false;
                    IncludeExternalUsers = false;
                    IncludeSystemGroups = false;
                }
                SwapButtons();
            }
        }

        private bool _singleUser = true;
        public bool SingleUser
        {
            get { return _singleUser; }
            set
            {
                _singleUser = value;
                OnPropertyChanged();
                if (value) { AllUsers = false; }
                SwapButtons();
            }
        }

        private string _includeUserUPN = string.Empty;
        public string IncludeUserUPN
        {
            get { return _includeUserUPN; }
            set
            { 
                _includeUserUPN = value;
                Parameters.IncludeUserUPN = value;
                OnPropertyChanged();
            }
        }

        private bool _includeExternalUsers = false;
        public bool IncludeExternalUsers
        {
            get { return _includeExternalUsers; }
            set
            { 
                _includeExternalUsers = value;
                Parameters.IncludeExternalUsers = value;
                OnPropertyChanged();
                if (value) { AllUsers = false; }
                SwapButtons();
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
                if (value) { AllUsers = false; }
                SwapButtons();
            }
        }

        private bool _includeEveryone = false;
        public bool IncludeEveryone
        {
            get { return _includeEveryone; }
            set
            {
                _includeEveryone = value;
                Parameters.IncludeEveryone = value;
                OnPropertyChanged();
            }
        }

        private bool _includeEveryoneExceptExternal = false;
        public bool IncludeEveryoneExceptExternal
        {
            get { return _includeEveryoneExceptExternal; }
            set
            {
                _includeEveryoneExceptExternal = value;
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

        private void SwapButtons()
        {
            if (AllUsers) { AllUsersLabel.Visibility = Visibility.Visible; }
            else { AllUsersLabel.Visibility = Visibility.Collapsed; }

            if (SingleUser) { SingleUserPanel.Visibility = Visibility.Visible; }
            else
            {
                IncludeUserUPN = string.Empty;
                SingleUserPanel.Visibility = Visibility.Collapsed;
            }

            if (IncludeExternalUsers) { ExternalLabel.Visibility = Visibility.Visible; }
            else { ExternalLabel.Visibility = Visibility.Collapsed; }

            if (IncludeSystemGroups) { SystemGroupPanel.Visibility = Visibility.Visible; }
            else
            {
                IncludeEveryone = false;
                IncludeEveryoneExceptExternal = false;
                SystemGroupPanel.Visibility = Visibility.Collapsed;
            }
        }

    }
}
