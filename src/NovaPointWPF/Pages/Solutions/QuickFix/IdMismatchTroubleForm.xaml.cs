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

namespace NovaPointWPF.Pages.Solutions.QuickFix
{
    /// <summary>
    /// Interaction logic for IdMismatchTroubleForm.xaml
    /// </summary>
    public partial class IdMismatchTroubleForm : Page, ISolutionForm
    {
        // Optional parameters for the current report to filter sites
        public string UserUpn { get; set; } = string.Empty;
        public string SiteUrl { get; set; } = string.Empty;
        public string AdminUpn { get; set; } = string.Empty;
        public bool RemoveAdmin { get; set; } = true;
        public bool PreventAllSites { get; set; } = false;


        public IdMismatchTroubleForm()
        {
            InitializeComponent();

            DataContext = this;
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
