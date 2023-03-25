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

namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class UserAllSiteSingleReportForm : Page, ISolutionForm
    {
        // Required parameters for the current report
        public string SiteUrl { get; set; }

        public UserAllSiteSingleReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SiteUrl = string.Empty;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {

            UserAllSiteSingleReportParameters parameters = new(SiteUrl);
            await new UserAllSiteSingleReport(uiLog, appInfo, parameters).RunAsync();

        }
    }
}
