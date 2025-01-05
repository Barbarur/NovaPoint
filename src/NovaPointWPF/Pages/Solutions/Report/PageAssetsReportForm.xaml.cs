using NovaPointLibrary.Solutions.Report;
using NovaPointLibrary.Solutions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for PageAssetsReportForm.xaml
    /// </summary>
    public partial class PageAssetsReportForm : Page, ISolutionForm
    {
        public PageAssetsReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = PageAssetsReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(PageAssetsReport);
            SolutionHeader.SolutionDocs = PageAssetsReport.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            PageAssetsReportParameters parameters = new(AdminF.Parameters, SiteF.Parameters);

            await PageAssetsReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }

}
