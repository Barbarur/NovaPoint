using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RemoveSharingLinksAutoForm.xaml
    /// </summary>
    public partial class RemoveSharingLinksAutoForm : Page, ISolutionForm
    {
        public RemoveSharingLinksAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RemoveSharingLinksAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RemoveSharingLinksAuto);
            SolutionHeader.SolutionDocs = RemoveSharingLinksAuto.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            RemoveSharingLinksAutoParameters parameters = new(LinkF.Parameters, AdminF.Parameters, SiteF.Parameters);

            await RemoveSharingLinksAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
