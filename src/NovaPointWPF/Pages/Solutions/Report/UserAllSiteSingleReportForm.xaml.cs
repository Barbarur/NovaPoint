using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions.Reports;
using NovaPointLibrary.Solutions;
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
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions.Report;

namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class UserAllSiteSingleReportForm : Page, ISolutionForm
    {
        public string SiteUrl { get; set; }

        public UserAllSiteSingleReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = UserAllSiteSingleReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(UserAllSiteSingleReport);
            SolutionHeader.SolutionDocs = UserAllSiteSingleReport.s_SolutionDocs;

            SiteUrl = string.Empty;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {

            UserAllSiteSingleReportParameters parameters = new(SiteUrl);
            await new UserAllSiteSingleReport(uiLog, appInfo, parameters).RunAsync();

        }
    }
}
