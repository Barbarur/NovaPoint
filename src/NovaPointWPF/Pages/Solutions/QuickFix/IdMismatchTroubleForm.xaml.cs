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
        public string UserUpn { get; set; }

        public IdMismatchTroubleForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = IdMismatchTrouble._solutionName;
            SolutionHeader.SolutionCode = nameof(IdMismatchTrouble);
            SolutionHeader.SolutionDocs = IdMismatchTrouble._solutionDocs;

            this.UserUpn = string.Empty;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            IdMismatchTroubleParameters parameters = new(AdminF.Parameters, SiteF.Parameters)
            {
                ReportMode = Mode.ReportMode,
                UserUpn = this.UserUpn,
            };

            await IdMismatchTrouble.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
