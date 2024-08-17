using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions.Automation;
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
using NovaPointLibrary.Solutions.Report;

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for SharingLinksReportForm.xaml
    /// </summary>
    public partial class SharingLinksReportForm : Page, ISolutionForm
    {
        public SharingLinksReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = SharingLinksReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(SharingLinksReport);
            SolutionHeader.SolutionDocs = SharingLinksReport.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            SPOTenantSiteUrlsWithAccessParameters siteAccParam = new()
            {
                AdminAccess = AdminF.Parameters,
                SiteParam = SiteF.Parameters,
            };

            SharingLinksReportParameters parameters = new(siteAccParam);

            await SharingLinksReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
