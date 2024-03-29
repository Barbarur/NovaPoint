﻿using Microsoft.Graph;
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

        private bool _allSiteCollections;
        public bool AllSiteCollections
        {
            get {  return _allSiteCollections; }
            set
            {
                _allSiteCollections = value;
                Parameters.AllSiteCollections = value;
                OnPropertyChanged();
                if (value)
                {
                    AllSitesFilter.Visibility = Visibility.Visible;
                }
                else
                {
                    AllSitesFilter.Visibility = Visibility.Collapsed;
                    IncludePersonalSite = false;
                    IncludeShareSite = false;
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
                if (value)
                {
                    OnlyGroupIdDefined = false;
                }
            }
        }


        private bool _includeShareSite = false;
        public bool IncludeShareSite
        {
            get { return _includeShareSite; }
            set
            {
                _includeShareSite = value;
                Parameters.IncludeShareSite = value;
                OnPropertyChanged();
                if (!value)
                {
                    OnlyGroupIdDefined = false;
                }
            }
        }

        
        private bool _onlyGroupIdDefined = false;
        public bool OnlyGroupIdDefined
        {
            get { return _onlyGroupIdDefined; }
            set
            {
                _onlyGroupIdDefined = value;
                Parameters.OnlyGroupIdDefined = value;
                OnPropertyChanged();
                if (value)
                {
                    IncludeShareSite = true;
                    IncludePersonalSite = false;
                }
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
    }
}
