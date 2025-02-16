using NovaPointLibrary.Solutions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using NovaPointLibrary.Solutions.Report;

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for SharingLinksReportForm.xaml
    /// </summary>
    public partial class SharingLinksReportForm : Page, ISolutionForm
    {
        public bool BreakdownInvitations { get; set; } = true;

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
            SharingLinksReportParameters parameters = new(BreakdownInvitations, LinkF.Parameters, SiteF.Parameters, AdminF.Parameters);

            await SharingLinksReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
