using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using NovaPointWPF.UserControls;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
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
    /// <summary>
    /// Interaction logic for ListReportForm.xaml
    /// </summary>
    public partial class ListReportForm : Page, ISolutionForm
    {

        public ListReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = ListReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(ListReport);
            SolutionHeader.SolutionDocs = ListReport.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            var siteAccParam = AdminF.Parameters;
            var siteParam = SiteF.Parameters;
            siteAccParam.SiteParam = siteParam;

            var listParameters = ListForm.Parameters;

            SPOTenantListsParameters tListParam = new(siteAccParam, listParameters);

            ListReportParameters parameters = new(tListParam);

            //await new ListReport(parameters, uiLog, cancelTokenSource).RunAsync();

            await ListReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
