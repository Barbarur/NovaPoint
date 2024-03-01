using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Report;
using NovaPointWPF.UserControls;
using PnP.Framework.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for ItemReportForm.xaml
    /// </summary>
    public partial class ItemReportForm : Page, ISolutionForm
    {
        //public bool RemoveAdmin { get; set; }

        //public bool IncludePersonalSite { get; set; }
        //public bool IncludeShareSite { get; set; }
        //public bool OnlyGroupIdDefined { get; set; }
        //public string SiteUrl { get; set; }
        //public bool IncludeSubsites { get; set; }

        //public bool IncludeLists { get; set; }
        //public bool IncludeLibraries { get; set; }
        //public bool IncludeHiddenLists { get; set; }
        //public bool IncludeSystemLists { get; set; }
        //public string ListTitle { get; set; }


        //public bool ItemsAll { get; set; } = false;
        //public string FolderRelativeUrl { get; set; } = String.Empty;

        public ItemReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = ItemReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(ItemReport);
            SolutionHeader.SolutionDocs = ItemReport.s_SolutionDocs;

            //this.RemoveAdmin = true;

            //this.IncludePersonalSite = false;
            //this.IncludeShareSite = true;
            //this.OnlyGroupIdDefined = false;
            //this.SiteUrl = String.Empty;
            //this.IncludeSubsites = false;

            //this.IncludeLists = true;
            //this.IncludeLibraries = true;
            //this.IncludeHiddenLists = false;
            //this.IncludeSystemLists = false;
            //this.ListTitle = String.Empty;

            //this.ItemsAll = true;
            //this.FolderRelativeUrl = String.Empty;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {

            //ItemReportParameters parameters = new()
            //{
            //    RemoveAdmin = this.RemoveAdmin,

            //    IncludePersonalSite = this.IncludePersonalSite,
            //    IncludeShareSite = this.IncludeShareSite,
            //    OnlyGroupIdDefined = this.OnlyGroupIdDefined,
            //    SiteUrl = this.SiteUrl,
            //    IncludeSubsites = this.IncludeSubsites,

            //    IncludeLists = this.IncludeLists,
            //    IncludeLibraries = this.IncludeLibraries,
            //    IncludeHiddenLists = this.IncludeHiddenLists,
            //    IncludeSystemLists = this.IncludeSystemLists,
            //    ListTitle = this.ListTitle,

            //    FolderRelativeUrl = this.FolderRelativeUrl,
            //};

            //SPOTenantSiteUrlsParameters tSiteParam = new()
            //{
            //    IncludePersonalSite = this.IncludePersonalSite,
            //    IncludeShareSite = this.IncludeShareSite,
            //    OnlyGroupIdDefined = this.OnlyGroupIdDefined,
            //    SiteUrl = this.SiteUrl,
            //    IncludeSubsites = this.IncludeSubsites,
            //};

            //var listParameters = ListForm.Parameters;

            //SPOTenantListsParameters tListParam = new(tSiteParam, listParameters);

            //var itemParameters = ItemForm.Parameters;

            //ItemReportParameters parameters = new(tListParam, itemParameters);

            //await new ItemReport(parameters, uiLog, cancelTokenSource).RunAsync();



            var siteAccParam = AdminF.Parameters;
            var siteParam = SiteF.Parameters;
            siteAccParam.SiteParam = siteParam;

            var listParameters = ListForm.Parameters;

            SPOTenantListsParameters tListParam = new(siteAccParam, listParameters);

            var itemParameters = ItemForm.Parameters;

            ItemReportParameters parameters = new(tListParam, itemParameters);

            await new ItemReport(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
