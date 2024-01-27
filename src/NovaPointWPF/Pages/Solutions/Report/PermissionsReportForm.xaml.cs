using NovaPointLibrary.Solutions.Report;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for PermissionsReportForm.xaml
    /// </summary>
    public partial class PermissionsReportForm : Page, ISolutionForm
    {

        public bool UserListOnly { get; set; }

        public bool IncludeAdmins { get; set; }
        public bool IncludeSiteAccess { get; set; }
        private bool _includeUniquePermissions = true;
        public bool IncludeUniquePermissions
        {
            get { return _includeUniquePermissions; }
            set
            {
                _includeUniquePermissions = value;
                if (value)
                {
                    ListPanel.Visibility = Visibility.Visible;
                    ItemPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    ListPanel.Visibility = Visibility.Collapsed;
                    ItemPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        public string TargetUPN { get; set; }
        public bool TargetEveryone { get; set; }

        public bool RemoveAdmin { get; set; }

        public bool IncludePersonalSite { get; set; }
        public bool IncludeShareSite { get; set; }
        public bool OnlyGroupIdDefined { get; set; }
        public string SiteUrl { get; set; }
        public bool IncludeSubsites { get; set; }

        public bool ListAll { get; set; }
        public bool IncludeHiddenLists { get; set; }
        public bool IncludeSystemLists { get; set; }
        public string ListTitle { get; set; }


        public bool ItemsAll { get; set; } = false;
        public string FolderRelativeUrl { get; set; } = String.Empty;


        public PermissionsReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = PermissionsReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(PermissionsReport);
            SolutionHeader.SolutionDocs = PermissionsReport.s_SolutionDocs;

            this.UserListOnly = false;
            this.TargetUPN = "StartingText";
            this.TargetEveryone = false;

            this.IncludeAdmins = true;
            this.IncludeSiteAccess = true;
            this.IncludeUniquePermissions = true;

            this.RemoveAdmin = true;

            this.IncludePersonalSite = false;
            this.IncludeShareSite = true;
            this.OnlyGroupIdDefined = false;
            this.SiteUrl = String.Empty;
            this.IncludeSubsites = false;

            this.ListAll = true;
            this.IncludeHiddenLists = false;
            this.IncludeSystemLists = false;
            this.ListTitle = String.Empty;

            this.ItemsAll = true;
            this.FolderRelativeUrl = String.Empty;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {

            PermissionsReportParameters parameters = new()
            {
                UserListOnly = this.UserListOnly,
                
                IncludeAdmins = this.IncludeAdmins,
                IncludeSiteAccess = this.IncludeSiteAccess,
                IncludeUniquePermissions = this.IncludeUniquePermissions,

                TargetUPN = this.TargetUPN,
                TargetEveryone = this.TargetEveryone,

                RemoveAdmin = this.RemoveAdmin,

                IncludePersonalSite = this.IncludePersonalSite,
                IncludeShareSite = this.IncludeShareSite,
                OnlyGroupIdDefined = this.OnlyGroupIdDefined,
                SiteUrl = this.SiteUrl,
                IncludeSubsites = this.IncludeSubsites,

                ListAll = this.ListAll,
                IncludeHiddenLists = this.IncludeHiddenLists,
                IncludeSystemLists = this.IncludeSystemLists,
                ListTitle = this.ListTitle,

                FolderRelativeUrl = this.FolderRelativeUrl,
            };

            await new PermissionsReport(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
