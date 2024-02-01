using NovaPointLibrary.Solutions.Report;
using NovaPointLibrary.Solutions;
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

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for SiteReportForm.xaml
    /// </summary>
    public partial class SiteReportForm : Page, ISolutionForm
    {
        private bool _detailed;
        public bool Detailed
        {
            get { return _detailed; }
            set
            {
                _detailed = value;
                NeedAddAdmin();
            }
        }

        private bool _includeAdmins;
        public bool IncludeAdmins
        {
            get { return _includeAdmins; }
            set
            {
                _includeAdmins = value;
                NeedAddAdmin();
            }
        }

        public bool RemoveAdmin { get; set; }

        public bool IncludePersonalSite { get; set; }
        public bool IncludeShareSite { get; set; }
        public bool OnlyGroupIdDefined { get; set; }
        public string SiteUrl { get; set; }
        private bool _includeSubsites;
        public bool IncludeSubsites
        {
            get { return _includeSubsites; }
            set
            {
                _includeSubsites = value;
                NeedAddAdmin();
            }
        }

        public SiteReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = SiteReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(SiteReport);
            SolutionHeader.SolutionDocs = SiteReport.s_SolutionDocs;

            this.Detailed = true;
            this.IncludeAdmins = false;

            this.RemoveAdmin = true;

            this.IncludePersonalSite = false;
            this.IncludeShareSite = true;
            this.OnlyGroupIdDefined = false;
            this.SiteUrl = String.Empty;
            this.IncludeSubsites = false;
        }

        private void NeedAddAdmin()
        {
            if(Detailed || IncludeAdmins || IncludeSubsites)
            {
                AdminPanel.Visibility = Visibility.Visible;
            }
            else
            {
                AdminPanel.Visibility = Visibility.Collapsed;
            }
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            SiteReportParameters parameters = new()
            {
                Detailed = this.Detailed,
                IncludeAdmins = this.IncludeAdmins,

                IncludePersonalSite = this.IncludePersonalSite,
                IncludeShareSite = this.IncludeShareSite,
                OnlyGroupIdDefined = this.OnlyGroupIdDefined,
                SiteUrl = this.SiteUrl,
                IncludeSubsites = this.IncludeSubsites,
            };

            await new SiteReport(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
