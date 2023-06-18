using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Reports;
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

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// TO BE DEPRECATED
    /// </summary>
    public partial class SiteCollAllReportForm : Page, ISolutionForm
    {
        // Optional parameters for the current report to filter sites
        public bool IncludeAdmins { get; set; } = false;
        public string SPOAdminUPN { get; set; } = String.Empty;

        public bool RemoveAdmin { get; set; } = false;
        
        public bool IncludePersonalSite { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool GroupIdDefined { get; set; } = false;

        public SiteCollAllReportForm()
        {
            InitializeComponent();

            DataContext = this;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {
            SiteCollAllReportParameters parameters = new()
            {
                IncludeAdmins = IncludeAdmins,
                AdminUPN = SPOAdminUPN,
                RemoveAdmin = RemoveAdmin,
                IncludePersonalSite = IncludePersonalSite,
                IncludeShareSite = IncludeShareSite,
                GroupIdDefined = GroupIdDefined

            };
            await new SiteCollAllReport(uiLog, appInfo, parameters).RunAsync();
        }

        private void IncludeAdmins_Checked(object sender, RoutedEventArgs e)
        {
            TextBoxIncludeAdmins.Visibility = Visibility.Visible;
        }

        private void IncludeAdmins_UnChecked(object sender, RoutedEventArgs e)
        {
            TextBoxIncludeAdmins.Visibility = Visibility.Collapsed;
            SPOAdminUPN = String.Empty;
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

        private void Hyperlink_LearnMore(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void GoToDocumentation(object sender, RoutedEventArgs e)
        {
            string NavigateUri = "https://github.com/Barbarur/NovaPoint/wiki/Solution:-Report#all-site-collections";
            var url = NavigateUri.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
}
