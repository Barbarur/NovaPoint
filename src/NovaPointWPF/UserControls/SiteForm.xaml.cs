using CamlBuilder;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using NovaPointWPF.Pages.DesignMaterial;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for SiteForm.xaml
    /// </summary>
    public partial class SiteForm : UserControl
    {
        public SiteForm()
        {
            InitializeComponent();
        }

        //public string AdminUPN
        //{
        //    get { return (string)GetValue(AdminUPNProperty); }
        //    set { SetValue(AdminUPNProperty, value); }
        //}
        //public static readonly DependencyProperty AdminUPNProperty =
        //    DependencyProperty.Register("AdminUPN", typeof(string), typeof(SiteForm), new PropertyMetadata(string.Empty));


        //public bool RemoveAdmin
        //{
        //    get { return (bool)GetValue(RemoveAdminProperty); }
        //    set { SetValue(RemoveAdminProperty, value); }
        //}
        //public static readonly DependencyProperty RemoveAdminProperty =
        //    DependencyProperty.Register("RemoveAdmin", typeof(bool), typeof(SiteForm), new FrameworkPropertyMetadata(defaultValue: false));



        public bool SiteAll
        {
            get { return (bool)GetValue(SiteAllProperty); }
            set { SetValue(SiteAllProperty, value); }
        }
        public static readonly DependencyProperty SiteAllProperty =
            DependencyProperty.Register("SiteAll", typeof(bool), typeof(SiteForm), new FrameworkPropertyMetadata(defaultValue: false));



        public bool IncludePersonalSite
        {
            get { return (bool)GetValue(IncludePersonalSiteProperty); }
            set { SetValue(IncludePersonalSiteProperty, value); }
        }
        public static readonly DependencyProperty IncludePersonalSiteProperty =
            DependencyProperty.Register("IncludePersonalSite", typeof(bool), typeof(SiteForm), new FrameworkPropertyMetadata(defaultValue: false));


        public bool IncludeShareSite
        {
            get { return (bool)GetValue(IncludeShareSiteProperty); }
            set { SetValue(IncludeShareSiteProperty, value); }
        }
        public static readonly DependencyProperty IncludeShareSiteProperty =
            DependencyProperty.Register("IncludeShareSite", typeof(bool), typeof(SiteForm), new FrameworkPropertyMetadata(defaultValue: false));



        public bool OnlyGroupIdDefined
        {
            get { return (bool)GetValue(OnlyGroupIdDefinedProperty); }
            set { SetValue(OnlyGroupIdDefinedProperty, value); }
        }
        public static readonly DependencyProperty OnlyGroupIdDefinedProperty =
            DependencyProperty.Register("OnlyGroupIdDefined", typeof(bool), typeof(SiteForm), new FrameworkPropertyMetadata(defaultValue: false));



        private bool _singleSite;
        public bool SingleSite
        {
            get { return _singleSite; }
            set
            {
                _singleSite = value;
                if(value)
                {
                    AllSitesFilter.Visibility = Visibility.Collapsed; 
                    
                    SingleSiteUrl.Visibility = Visibility.Visible;
                }
                else
                {
                    AllSitesFilter.Visibility = Visibility.Visible;

                    SingleSiteUrl.Visibility = Visibility.Collapsed;
                    SiteUrl = "";
                    SetValue(SiteUrlProperty, string.Empty);
                    SiteUrlTextBox.Text = String.Empty;
                }
            }
        }


        public string SiteUrl
        {
            get { return (string)GetValue(SiteUrlProperty); }
            set { SetValue(SiteUrlProperty, value); }
        }
        public static readonly DependencyProperty SiteUrlProperty =
            DependencyProperty.Register("SiteUrl", typeof(string), typeof(SiteForm), new PropertyMetadata(string.Empty));



        public bool IncludeSubsites
        {
            get { return (bool)GetValue(IncludeSubsitesProperty); }
            set { SetValue(IncludeSubsitesProperty, value); }
        }
        public static readonly DependencyProperty IncludeSubsitesProperty =
            DependencyProperty.Register("IncludeSubsites", typeof(bool), typeof(SiteForm), new FrameworkPropertyMetadata(defaultValue: false));

    }
}
