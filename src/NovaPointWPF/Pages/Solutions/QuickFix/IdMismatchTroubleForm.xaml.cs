using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.QuickFix;
using System.Threading;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointWPF.UserControls;

namespace NovaPointWPF.Pages.Solutions.QuickFix
{
    /// <summary>
    /// Interaction logic for IdMismatchTroubleForm.xaml
    /// </summary>
    public partial class IdMismatchTroubleForm : Page, ISolutionForm
    {
        public bool ReportMode { get; set; }

        public string UserUpn { get; set; }

        public IdMismatchTroubleForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = IdMismatchTrouble._solutionName;
            SolutionHeader.SolutionCode = nameof(IdMismatchTrouble);
            SolutionHeader.SolutionDocs = IdMismatchTrouble._solutionDocs;

            this.ReportMode = true;

            this.UserUpn = string.Empty;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            var siteAccParam = AdminF.Parameters;
            var siteParam = SiteF.Parameters;
            siteAccParam.SiteParam = siteParam;

            IdMismatchTroubleParameters parameters = new(siteAccParam)
            {
                ReportMode = this.ReportMode,
                UserUpn = this.UserUpn,
            };
            //await new IdMismatchTrouble(parameters, uiLog, cancelTokenSource).RunAsync();

            await IdMismatchTrouble.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
