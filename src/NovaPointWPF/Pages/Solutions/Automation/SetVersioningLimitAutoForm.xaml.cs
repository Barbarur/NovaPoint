using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
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

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for SetVersioningLimitAutoForm.xaml
    /// </summary>
    public partial class SetVersioningLimitAutoForm : Page, ISolutionForm
    {
        public int LibraryMajorVersionLimit { get; set; } = 500;
        public int LibraryMinorVersionLimit { get; set; } = 0;
        public int ListMajorVersionLimit { get; set; } = 500;


        public SetVersioningLimitAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = SetVersioningLimitAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(SetVersioningLimitAuto);
            SolutionHeader.SolutionDocs = SetVersioningLimitAuto.s_SolutionDocs;

            this.LibraryMajorVersionLimit = 500;
            this.LibraryMinorVersionLimit = 0;
            this.ListMajorVersionLimit = 500;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            var siteAccParam = AdminF.Parameters;
            var siteParam = SiteF.Parameters;
            siteAccParam.SiteParam = siteParam;

            SPOTenantListsParameters tListParam = new(siteAccParam, ListForm.Parameters);

            SetVersioningLimitAutoParameters parameters = new(tListParam)
            {
                LibraryMajorVersionLimit = this.LibraryMajorVersionLimit,
                LibraryMinorVersionLimit = this.LibraryMinorVersionLimit,
                ListMajorVersionLimit = this.ListMajorVersionLimit,
            };

            //await new SetVersioningLimitAuto(parameters, uiLog, cancelTokenSource).RunAsync();

            await SetVersioningLimitAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
