using NovaPointLibrary.Solutions.Report;
using NovaPointLibrary.Solutions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class SiteReportForm : Page, ISolutionForm
    {
        public SiteReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = SiteReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(SiteReport);
            SolutionHeader.SolutionDocs = SiteReport.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            SiteReportParameters parameters = new(AdminF.Parameters, SiteF.Parameters, SiteDetails.IncludeHubInfo, SiteDetails.IncludeClassification, SiteDetails.IncludeSharingLinks);

            await SiteReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
