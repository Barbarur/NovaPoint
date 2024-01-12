using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
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

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for ShortcutODReportForm.xaml
    /// </summary>
    public partial class ShortcutODReportForm : Page, ISolutionForm
    {
        public string AdminUPN { get; set; }
        public bool RemoveAdmin { get; set; }

        private bool _siteAll;
        public bool SiteAll
        {
            get { return _siteAll; }
            set
            {
                _siteAll = value;
                if (value)
                {
                    SingleSiteUrl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SingleSiteUrl.Visibility = Visibility.Visible;
                    SiteUrl = string.Empty;
                    SiteUrlTextBox.Text = String.Empty;
                }
            }
        }
        public string SiteUrl { get; set; }

        public string FolderRelativeUrl { get; set; } = String.Empty;


        public ShortcutODReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = ShortcutODReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(ShortcutODReport);
            SolutionHeader.SolutionDocs = ShortcutODReport.s_SolutionDocs;

            this.AdminUPN = String.Empty;
            this.RemoveAdmin = true;

            this.SiteAll = true;
            this.SiteUrl = String.Empty;

            this.FolderRelativeUrl = String.Empty;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {

            ShortcutODReportParameters parameters = new()
            {
                AdminUPN = this.AdminUPN,
                RemoveAdmin = this.RemoveAdmin,

                SiteAll = this.SiteAll,
                SiteUrl = this.SiteUrl,

                FolderRelativeUrl = this.FolderRelativeUrl,
            };

            await new ShortcutODReport(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
