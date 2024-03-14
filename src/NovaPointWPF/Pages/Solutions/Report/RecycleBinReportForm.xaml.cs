using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
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

namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class RecycleBinReportForm : Page, ISolutionForm
    {
        public RecycleBinReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RecycleBinReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RecycleBinReport);
            SolutionHeader.SolutionDocs = RecycleBinReport.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            var siteAccParam = AdminF.Parameters;
            var siteParam = SiteF.Parameters;
            siteAccParam.SiteParam = siteParam;

            RecycleBinReportParameters parameters = new(RecycleF.Parameters, siteAccParam);

            //await new RecycleBinReport(parameters, uiLog, cancelTokenSource).RunAsync();

            await RecycleBinReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
