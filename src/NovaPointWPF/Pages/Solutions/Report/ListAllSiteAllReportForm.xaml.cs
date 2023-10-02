using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Reports;
using PnP.Framework.Diagnostics;
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

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for ListAllSiteAllReportForm.xaml
    /// </summary>
    public partial class ListAllSiteAllReportForm : Page, ISolutionForm
    {
        // Required parameters for the current report
        public string SiteAdminUPN { get; set; }
        // Optional parameters for the current report to filter sites
        public bool RemoveAdmin { get; set; }
        public bool IncludePersonalSite { get; set; }
        public bool IncludeShareSite { get; set; }
        public bool GroupIdDefined { get; set; }
        // Optional parameters for the current report to filter lists
        public bool IncludeSystemLists { get; set; }
        public bool IncludeResourceLists { get; set; }

        public ListAllSiteAllReportForm()
        {
            InitializeComponent();

            DataContext = this;
            
            SiteAdminUPN = string.Empty;
            
            RemoveAdmin = false;
            IncludePersonalSite = false;
            IncludeShareSite = true;
            GroupIdDefined = false;

            IncludeSystemLists = false;
            IncludeResourceLists = false;
        }
        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {

            //ListAllSiteAllReportParameters parameters = new(SiteAdminUPN)
            //{
            //    RemoveAdmin = RemoveAdmin,
            //    IncludePersonalSite = IncludePersonalSite,
            //    IncludeShareSite = IncludeShareSite,
            //    GroupIdDefined = GroupIdDefined,

            //    IncludeSystemLists = IncludeSystemLists,
            //    IncludeResourceLists = IncludeResourceLists
            //};
            //await new ListAllSiteAllReport(uiLog, appInfo, parameters).RunAsync();

        }

        private void CheckBoxIncludePersonalSites_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxIncludeGroupIdDefined.IsChecked = false;
        }

        private void CheckBoxIncludeShareSites_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBoxIncludeGroupIdDefined.IsChecked = false;
        }

        private void CheckBoxIncludeGroupIdDefined_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxIncludePersonalSites.IsChecked = false;
            CheckBoxIncludeShareSites.IsChecked = true;
        }
    }
}
