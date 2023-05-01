using NovaPointLibrary.Solutions.Reports;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions.Report;

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for SiteAllReportForm.xaml
    /// </summary>
    public partial class SiteAllReportForm : Page, ISolutionForm
    {

        // Optional parameters for the current report to filter sites
        public bool IncludeAdmins { get; set; } = false;
        public string SPOAdminUPN { get; set; } = String.Empty;

        public bool RemoveAdmin { get; set; } = false;

        public bool IncludePersonalSite { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool GroupIdDefined { get; set; } = false;

        public bool IncludeSubsites { get; set; } = false;

        public SiteAllReportForm()
        {
            InitializeComponent();

            DataContext = this;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {
            SiteAllReportParameters parameters = new()
            {
                IncludeAdmins = IncludeAdmins,
                AdminUPN = SPOAdminUPN,
                RemoveAdmin = RemoveAdmin,

                IncludePersonalSite = IncludePersonalSite,
                IncludeShareSite = IncludeShareSite,
                GroupIdDefined = GroupIdDefined,

                IncludeSubsites = IncludeSubsites

            };
            await new SiteAllReport(uiLog, appInfo, parameters).RunAsync();
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

        private void GoToDocumentation(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://github.com/Barbarur/NovaPoint/wiki/Solution:-Report#all-site-collections";
            var url = NavigateUri.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
}
