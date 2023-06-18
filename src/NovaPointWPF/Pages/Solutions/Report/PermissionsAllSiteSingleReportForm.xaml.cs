using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
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
    /// Interaction logic for PermissionsAllSiteSingleReportForm.xaml
    /// </summary>
    public partial class PermissionsAllSiteSingleReportForm : Page, ISolutionForm
    {
        public string SiteUrl { get; set; }

        public bool IncludeAdmins { get; set; }
        public bool IncludeSiteAccess { get; set; }
        public bool IncludeUniquePermissions { get; set; }
        public bool IncludeSubsites { get; set; }

        private bool IncludeSystemLists { get; set; }
        private bool IncludeResourceLists { get; set; }


        public PermissionsAllSiteSingleReportForm()
        {
            InitializeComponent();

            DataContext = this;


            SiteUrl = string.Empty;

            IncludeAdmins = true;
            IncludeSiteAccess = true;
            IncludeUniquePermissions = true;
            IncludeSubsites = true;

            IncludeSystemLists = false;
            IncludeResourceLists = false;


            SolutionHeader.SolutionTitle = PermissionsAllSiteSingleReport._solutionName;
            SolutionHeader.SolutionCode = nameof(PermissionsAllSiteSingleReport);
            SolutionHeader.SolutionDocs = PermissionsAllSiteSingleReport._solutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {
            PermissionsAllSiteSingleParameters parameters = new(SiteUrl)
            {
                IncludeAdmins = IncludeAdmins,
                IncludeSiteAccess = IncludeSiteAccess,
                IncludeUniquePermissions = IncludeUniquePermissions,
                IncludeSubsites = IncludeSubsites,

                IncludeSystemLists = IncludeSystemLists,
                IncludeResourceLists = IncludeResourceLists,
            };
            await new PermissionsAllSiteSingleReport(uiLog, appInfo, parameters).RunAsync();
        }
    }
}
