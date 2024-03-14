using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
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

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RestoreRecycleBinAutoForm.xaml
    /// </summary>
    public partial class RestoreRecycleBinAutoForm : Page, ISolutionForm
    {
        public bool RenameFile { get; set; }

        public RestoreRecycleBinAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RestoreRecycleBinAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RestoreRecycleBinAuto);
            SolutionHeader.SolutionDocs = RestoreRecycleBinAuto.s_SolutionDocs;

        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            var siteAccParam = AdminF.Parameters;
            var siteParam = SiteF.Parameters;
            siteAccParam.SiteParam = siteParam;

            RestoreRecycleBinAutoParameters parameters = new(RecycleF.Parameters, siteAccParam)
            {
                RenameFile = this.RenameFile
            };

            //await new RestoreRecycleBinAuto(parameters, uiLog, cancelTokenSource).RunAsync();

            await RestoreRecycleBinAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
