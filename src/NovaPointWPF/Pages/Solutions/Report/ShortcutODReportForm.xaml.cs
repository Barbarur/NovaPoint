using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using NovaPointWPF.UserControls;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for ShortcutODReportForm.xaml
    /// </summary>
    public partial class ShortcutODReportForm : Page, ISolutionForm
    {

        public ShortcutODReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = ShortcutODReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(ShortcutODReport);
            SolutionHeader.SolutionDocs = ShortcutODReport.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            SPOTenantSiteUrlsWithAccessParameters siteAccParam = new()
            {
                AdminAccess = AdminF.Parameters,
                SiteParam = SiteF.Parameters,
            };

            ShortcutODReportParameters parameters = new(siteAccParam, new(), ItemForm.Parameters);

            await ShortcutODReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
