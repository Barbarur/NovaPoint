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
        private bool _reportMode;
        public bool ReportMode
        { 
            get { return _reportMode; }
            set
            {
                _reportMode = value;
                if (value == true)
                {
                    SiteUrlAffectedTextBox.Visibility = Visibility.Collapsed;
                    SiteUrlCorrectCheckBox.Visibility = Visibility.Visible;
                    PreventAllSitesCheckBox.IsChecked = true;
                }
                else if (value == false)
                {
                    SiteUrlAffectedTextBox.Visibility = Visibility.Visible;
                    SiteUrlCorrectCheckBox.Visibility = Visibility.Collapsed;
                }

            }
        }

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

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {


            IdMismatchTroubleParameters parameters = new()
            {
                UserUpn = this.UserUpn,
                SiteUrl = this.SiteUrl,
                AdminUpn = this.AdminUpn,
                RemoveAdmin = this.RemoveAdmin,
                PreventAllSites = this.PreventAllSites,
                ReportMode = this.ReportMode,

            };
            await new IdMismatchTrouble(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
