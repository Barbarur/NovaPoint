using NovaPointLibrary.Commands.Authentication;
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
    /// Interaction logic for ClearRecycleBinAutoForm.xaml
    /// </summary>
    public partial class ClearRecycleBinAutoForm : Page, ISolutionForm
    {
        public ClearRecycleBinAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = ClearRecycleBinAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(ClearRecycleBinAuto);
            SolutionHeader.SolutionDocs = ClearRecycleBinAuto.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            var siteAccParam = AdminF.Parameters;
            var siteParam = SiteF.Parameters;
            siteAccParam.SiteParam = siteParam;

            ClearRecycleBinAutoParameters parameters = new(RecycleF.Parameters, siteAccParam);

            await new ClearRecycleBinAuto(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
