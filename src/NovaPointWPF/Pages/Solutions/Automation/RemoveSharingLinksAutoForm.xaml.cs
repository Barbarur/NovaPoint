using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
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
            SPOTenantSiteUrlsWithAccessParameters siteAccParam = new()
            {
                AdminAccess = AdminF.Parameters,
                SiteParam = SiteF.Parameters,
            };

            RemoveSharingLinksAutoParameters parameters = new(ModeF.ReportMode, siteAccParam);

            await RemoveSharingLinksAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
