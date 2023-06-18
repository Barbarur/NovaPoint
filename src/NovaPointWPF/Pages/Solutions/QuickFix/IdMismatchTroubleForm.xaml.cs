using NovaPointLibrary.Solutions.Reports;
using NovaPointLibrary.Solutions;
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
using NovaPointLibrary.Solutions.QuickFix;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions.Report;

namespace NovaPointWPF.Pages.Solutions.QuickFix
{
    /// <summary>
    /// Interaction logic for IdMismatchTroubleForm.xaml
    /// </summary>
    public partial class IdMismatchTroubleForm : Page, ISolutionForm
    {
        public string UserUpn { get; set; }
        public string SiteUrl { get; set; }
        public string AdminUpn { get; set; }
        public bool RemoveAdmin { get; set; }
        public bool PreventAllSites { get; set; }


        public IdMismatchTroubleForm()
        {
            InitializeComponent();

            DataContext = this;

            UserUpn = string.Empty;
            SiteUrl = string.Empty;
            AdminUpn = string.Empty;
            RemoveAdmin = true;
            PreventAllSites = false;


            SolutionHeader.SolutionTitle = IdMismatchTrouble._solutionName;
            SolutionHeader.SolutionCode = nameof(IdMismatchTrouble);
            SolutionHeader.SolutionDocs = IdMismatchTrouble._solutionDocs;

        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {


            IdMismatchTroubleParameters parameters = new(UserUpn, SiteUrl, AdminUpn)
            {
                RemoveAdmin = RemoveAdmin,
                PreventAllSites = PreventAllSites

            };
            await new IdMismatchTrouble(uiLog, appInfo, parameters).RunAsync();
        }
    }
}
