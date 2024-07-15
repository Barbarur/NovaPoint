using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
    /// Interaction logic for RemoveSiteUserAutoForm.xaml
    /// </summary>
    public partial class RemoveSiteUserAutoForm : Page, ISolutionForm
    {

        public RemoveSiteUserAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RemoveSiteUserAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RemoveSiteUserAuto);
            SolutionHeader.SolutionDocs = RemoveSiteUserAuto.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            SPOTenantSiteUrlsWithAccessParameters siteAccParam = new()
            {
                AdminAccess = AdminF.Parameters,
                SiteParam = SiteF.Parameters,
            };

            RemoveUserAutoParameters parameters = new(UserF.Parameters, siteAccParam);

            //await new RemoveSiteUserAuto(parameters, uiLog, cancelTokenSource).RunAsync();

            await RemoveSiteUserAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
