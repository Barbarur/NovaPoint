using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class MembershipReportForm : Page, ISolutionForm
    {
        public MembershipReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = MembershipReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(MembershipReport);
            SolutionHeader.SolutionDocs = MembershipReport.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            MembershipReportParameters parameters = new(MembershipF.Parameters, AdminF.Parameters, SiteF.Parameters);

            await MembershipReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
