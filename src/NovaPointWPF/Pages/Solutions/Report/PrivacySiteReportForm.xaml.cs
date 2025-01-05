using NovaPointLibrary.Solutions.Report;
using NovaPointLibrary.Solutions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for PrivacySiteReportForm.xaml
    /// </summary>
    public partial class PrivacySiteReportForm : Page, ISolutionForm
    {
        public PrivacySiteReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = PrivacySiteReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(PrivacySiteReport);
            SolutionHeader.SolutionDocs = PrivacySiteReport.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            PrivacySiteReportParameters parameters = new();

            await PrivacySiteReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
