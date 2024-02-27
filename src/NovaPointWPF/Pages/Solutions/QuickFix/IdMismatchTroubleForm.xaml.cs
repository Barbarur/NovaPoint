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

namespace NovaPointWPF.Pages.Solutions.QuickFix
{
    /// <summary>
    /// Interaction logic for IdMismatchTroubleForm.xaml
    /// </summary>
    public partial class IdMismatchTroubleForm : Page, ISolutionForm
    {
        public bool ReportMode { get; set; }

        public string UserUpn { get; set; }
        
        public bool RemoveAdmin { get; set; }

        public bool IncludePersonalSite { get; set; }
        public bool IncludeShareSite { get; set; }
        public bool OnlyGroupIdDefined { get; set; }
        public string SiteUrl { get; set; }

        public IdMismatchTroubleForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = IdMismatchTrouble._solutionName;
            SolutionHeader.SolutionCode = nameof(IdMismatchTrouble);
            SolutionHeader.SolutionDocs = IdMismatchTrouble._solutionDocs;

            this.ReportMode = true;

            this.UserUpn = string.Empty;
            
            this.RemoveAdmin = true;

            this.IncludePersonalSite = false;
            this.IncludeShareSite = true;
            this.OnlyGroupIdDefined = false;
            this.SiteUrl = String.Empty;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            SPOTenantSiteUrlsParameters siteParameters = new()
            {
                RemoveAdmin = this.RemoveAdmin,

                IncludePersonalSite = this.IncludePersonalSite,
                IncludeShareSite = this.IncludeShareSite,
                OnlyGroupIdDefined = this.OnlyGroupIdDefined,
                SiteUrl = this.SiteUrl,
            };

            IdMismatchTroubleParameters parameters = new()
            {
                ReportMode = this.ReportMode,
                UserUpn = this.UserUpn,
                SiteParameters = siteParameters,
            };
            await new IdMismatchTrouble(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
