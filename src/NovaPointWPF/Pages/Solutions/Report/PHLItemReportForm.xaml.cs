using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using NovaPointWPF.UserControls;
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
    /// <summary>
    /// Interaction logic for PHLItemReportForm.xaml
    /// </summary>
    public partial class PHLItemReportForm : Page, ISolutionForm
    {
        public PHLItemReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = PHLItemReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(PHLItemReport);
            SolutionHeader.SolutionDocs = PHLItemReport.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            SPOTenantSiteUrlsWithAccessParameters siteAccParam = new()
            {
                AdminAccess = AdminF.Parameters,
                SiteParam = SiteF.Parameters,
            };

            PHLItemReportParameters parameters = new(PHLForm.Parameters, siteAccParam, new(), new());

            await PHLItemReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
