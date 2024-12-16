using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NovaPointWPF.Pages.Solutions.Report
{

    public partial class OrphanSiteReportForm : Page, ISolutionForm
    {
        public OrphanSiteReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = OrphanSiteReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(OrphanSiteReport);
            SolutionHeader.SolutionDocs = OrphanSiteReport.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            OrphanSiteReportParameters parameters = new(SiteF.Parameters);

            await OrphanSiteReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
