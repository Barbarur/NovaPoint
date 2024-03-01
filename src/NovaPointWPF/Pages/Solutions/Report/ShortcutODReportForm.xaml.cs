using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using NovaPointWPF.UserControls;
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
        //public bool RemoveAdmin { get; set; }

        private bool _allSiteCollections = false;
        public bool AllSiteCollections
        {
            get { return _allSiteCollections; }
            set
            {
                _allSiteCollections = value;
                if (value)
                {
                    SingleSiteUrl.Visibility = Visibility.Collapsed;
                    SiteUrl = string.Empty;
                    SiteUrlTextBox.Text = String.Empty;
                }
                else
                {
                    SingleSiteUrl.Visibility = Visibility.Visible;
                }
            }
        }

        public string SiteUrl { get; set; }

        //public string FolderRelativeUrl { get; set; } = String.Empty;


        public ShortcutODReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = ShortcutODReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(ShortcutODReport);
            SolutionHeader.SolutionDocs = ShortcutODReport.s_SolutionDocs;

            //this.RemoveAdmin = true;

            //this.SiteUrl = String.Empty;

            //this.FolderRelativeUrl = String.Empty;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {

            //ShortcutODReportParameters parameters = new()
            //{
            //    RemoveAdmin = this.RemoveAdmin,

            //    SiteUrl = this.SiteUrl,

            //    FolderRelativeUrl = this.FolderRelativeUrl,
            //};

            //SPOTenantSiteUrlsParameters tSiteParam = new()
            //{
            //    RemoveAdmin = this.RemoveAdmin,
            //    SiteUrl = this.SiteUrl,
            //};

            //SPOListsParameters listParameters = new();

            //SPOTenantListsParameters tListParam = new(tSiteParam, listParameters);

            //var itemParameters = ItemForm.Parameters;

            //ShortcutODReportParameters parameters = new(tListParam, itemParameters);

            //await new ShortcutODReport(parameters, uiLog, cancelTokenSource).RunAsync();






            var siteAccParam = AdminF.Parameters;
            siteAccParam.SiteParam.AllSiteCollections = this.AllSiteCollections;
            siteAccParam.SiteParam.SiteUrl = this.SiteUrl;

            SPOListsParameters listParameters = new();

            SPOTenantListsParameters tListParam = new(siteAccParam, listParameters);


            ShortcutODReportParameters parameters = new(tListParam, ItemForm.Parameters);

            await new ShortcutODReport(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
