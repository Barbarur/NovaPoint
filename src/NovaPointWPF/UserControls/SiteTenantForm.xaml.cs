using Microsoft.Graph;
using Microsoft.Win32;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
    /// Interaction logic for SiteTenantForm.xaml
    /// </summary>
    public partial class SiteTenantForm : UserControl, INotifyPropertyChanged
    {
        public SPOTenantSiteUrlsParameters Parameters { get; set; } = new();

        private bool _activeSites;
        public bool ActiveSites
        {
            get {  return _activeSites; }
            set
            {
                _activeSites = value;
                Parameters.ActiveSites = value;
                OnPropertyChanged();
                if (value)
                {
                    AllSitesFilter.Visibility = Visibility.Visible;
                    IncludeSites = true;
                }
                else
                {
                    AllSitesFilter.Visibility = Visibility.Collapsed;
                    IncludePersonalSite = false;
                    ChangeCheckedSites(false);
                }
            }
        }

        private bool _includePersonalSite = false;
        public bool IncludePersonalSite
        {
            get { return _includePersonalSite; }
            set
            {
                _includePersonalSite = value;
                Parameters.IncludePersonalSite = value;
                OnPropertyChanged();
            }
        }

        private bool _includeSites = false;
        public bool IncludeSites
        {
            get { return _includeSites; }
            set
            {
                if (value && !IsAllSitesIncluded())
                {
                    ChangeCheckedSites(true);
                }
                else if (!value && IsAllSitesIncluded())
                {
                    ChangeCheckedSites(false);
                }

                _includeSites = value;
                OnPropertyChanged();
            }
        }

        private bool _includeTeamSite = false;
        public bool IncludeTeamSite
        {
            get { return _includeTeamSite; }
            set
            {
                _includeTeamSite = value;
                Parameters.IncludeTeamSite = value;
                OnPropertyChanged();

                if (!value) { IncludeSites = false; }
                else { CheckControlAllSites(); }
            }
        }

        private bool _includeTeamSiteWithTeams = false;
        public bool IncludeTeamSiteWithTeams
        {
            get { return _includeTeamSiteWithTeams; }
            set
            {
                _includeTeamSiteWithTeams = value;
                Parameters.IncludeTeamSiteWithTeams = value;
                OnPropertyChanged();

                if (!value) { IncludeSites = false; }
                else { CheckControlAllSites(); }
            }
        }

        private bool _includeTeamSiteWithNoGroup = false;
        public bool IncludeTeamSiteWithNoGroup
        {
            get { return _includeTeamSiteWithNoGroup; }
            set
            {
                _includeTeamSiteWithNoGroup = value;
                Parameters.IncludeTeamSiteWithNoGroup = value;
                OnPropertyChanged();
                
                if (!value) { IncludeSites = false; }
                else { CheckControlAllSites(); }
            }
        }

        private bool _includeCommunication = false;
        public bool IncludeCommunication
        {
            get { return _includeCommunication; }
            set
            {
                _includeCommunication = value;
                Parameters.IncludeCommunication = value;
                OnPropertyChanged();

                if (!value) { IncludeSites = false; }
                else { CheckControlAllSites(); }
            }
        }

        private bool _includeChannels = false;
        public bool IncludeChannels
        {
            get { return _includeChannels; }
            set
            {
                _includeChannels = value;
                Parameters.IncludeChannels = value;
                OnPropertyChanged();

                if (!value) { IncludeSites = false; }
                else { CheckControlAllSites(); }
            }
        }

        private bool _includeClassic = false;
        public bool IncludeClassic
        {
            get { return _includeClassic; }
            set
            {
                _includeClassic = value;
                Parameters.IncludeClassic = value;
                OnPropertyChanged();
                
                if (!value) { IncludeSites = false; }
                else { CheckControlAllSites(); }
            }
        }

        private bool _singleSite = true;
        public bool SingleSite
        {
            get { return _singleSite; }
            set
            {
                _singleSite = value;
                OnPropertyChanged();
                if (value)
                {
                    SingleSiteForm.Visibility = Visibility.Visible;
                }
                else
                {
                    SingleSiteForm.Visibility = Visibility.Collapsed;
                    SiteUrl = string.Empty;
                }
            }
        }

        private string _siteUrl = string.Empty;
        public string SiteUrl
        {
            get { return _siteUrl; }
            set
            { 
                _siteUrl = value;
                Parameters.SiteUrl = value;
                OnPropertyChanged();
            }
        }

        private bool _listOfSites = false;
        public bool ListOfSites
        {
            get { return _listOfSites; }
            set
            {
                _listOfSites = value;
                OnPropertyChanged();
                if (value)
                {
                    ListOfSitesForm.Visibility = Visibility.Visible;
                }
                else
                {
                    ListOfSitesForm.Visibility = Visibility.Collapsed;
                    ListOfSitesPath = string.Empty;
                }
            }
        }

        private string _listOfSitesPath = string.Empty;
        public string ListOfSitesPath
        {
            get { return _listOfSitesPath; }
            set
            {
                _listOfSitesPath = value;
                Parameters.ListOfSitesPath = value;
                OnPropertyChanged();
            }
        }

        public bool _listOfSitesVisibility = true;
        public bool ListOfSitesVisibility
        {
            get { return _listOfSitesVisibility; }
            set
            {
                _listOfSitesVisibility = value;
                if (value)
                {
                    ListOfSitesRadioButton.Visibility = Visibility.Visible;
                }
                else
                {
                    ListOfSitesRadioButton.Visibility = Visibility.Collapsed;
                    ListOfSitesPath = string.Empty;
                }
            }
        }


        private bool _includeSubsites = false;
        public bool IncludeSubsites
        {
            get { return _includeSubsites ; }
            set
            {
                _includeSubsites = value;
                Parameters.IncludeSubsites = value;
            }
        }

        public bool _subsitesVisibility = true;
        public bool SubsitesVisibility
        {
            get { return _subsitesVisibility; }
            set
            {
                _subsitesVisibility = value;
                if (value)
                {
                    SubsiteToggleButton.Visibility = Visibility.Visible;
                }
                else
                {
                    SubsiteToggleButton.Visibility = Visibility.Collapsed;
                    IncludeSubsites = false;
                }
            }
        }


        public SiteTenantForm()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                ListOfSitesPath = openFileDialog.FileName;
        }

        private void CheckControlAllSites()
        {
            if (IsAllSitesIncluded())
            {
                IncludeSites = true;
            }
        }

        private bool IsAllSitesIncluded()
        {
            if (IncludeTeamSite && IncludeTeamSiteWithTeams && IncludeTeamSiteWithNoGroup && IncludeCommunication && IncludeChannels && IncludeClassic)
            {
                return true;
            }
            else { return false; }
        }


        private void ChangeCheckedSites (bool IsChecked)
        {
            IncludeTeamSite = IsChecked;
            IncludeTeamSiteWithTeams = IsChecked;
            IncludeTeamSiteWithNoGroup = IsChecked;
            IncludeCommunication = IsChecked;
            IncludeChannels = IsChecked;
            IncludeClassic = IsChecked;
        }

    }
}
