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
        public bool IncludePersonalSite { get; set; }
        public bool IncludeShareSite { get; set; }
        public bool GroupIdDefined { get; set; }

        public string AdminUPN { get; set; }
        public bool RemoveAdmin { get; set; }

        public bool IncludeAdmins { get; set; }
        public bool IncludeSiteAccess { get; set; }
        public bool IncludeSubsites { get; set; }

        //private readonly string SolutionDocs;

        public SiteAllReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = SiteAllReport._solutionName;
            SolutionHeader.SolutionCode = nameof(SiteAllReport);
            SolutionHeader.SolutionDocs = SiteAllReport._solutionDocs;

            this.IncludePersonalSite = false;
            this.IncludeShareSite = true;
            this.GroupIdDefined = false;

            this.AdminUPN = String.Empty;
            this.RemoveAdmin = false;

            this.IncludeAdmins = false;
            this.IncludeSiteAccess = false;
            this.IncludeSubsites = false;

        }


        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {
            SiteAllReportParameters parameters = new()
            {
                AdminUPN = this.AdminUPN,
                RemoveAdmin = this.RemoveAdmin,

                IncludePersonalSite = this.IncludePersonalSite,
                IncludeShareSite = this.IncludeShareSite,
                GroupIdDefined = this.GroupIdDefined,

                IncludeAdmins = this.IncludeAdmins,
                IncludeSiteAccess = this.IncludeSiteAccess,
                IncludeSubsites = this.IncludeSubsites

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

    }
}
