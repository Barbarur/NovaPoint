using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RemoveFileVersionAutoForm.xaml
    /// </summary>
    public partial class RemoveFileVersionAutoForm : Page, ISolutionForm
    {
        public RemoveFileVersionAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RemoveFileVersionAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RemoveFileVersionAuto);
            SolutionHeader.SolutionDocs = RemoveFileVersionAuto.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            SPOTenantSiteUrlsWithAccessParameters siteAccParam = new()
            {
                AdminAccess = AdminF.Parameters,
                SiteParam = SiteF.Parameters,
            };

            RemoveFileVersionAutoParameters parameters = new(Mode.ReportMode, VersionF.Parameters, siteAccParam, ListForm.Parameters, ItemForm.Parameters);

            await RemoveFileVersionAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
