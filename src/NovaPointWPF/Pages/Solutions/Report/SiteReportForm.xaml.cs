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
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointWPF.UserControls;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Item;

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for SiteReportForm.xaml
    /// </summary>
    public partial class SiteReportForm : Page, ISolutionForm
    {
        public bool Detailed { get; set; }

        public bool IncludeAdmins {  get; set; }


        //public bool RemoveAdmin { get; set; }

        //public bool IncludePersonalSite { get; set; }
        //public bool IncludeShareSite { get; set; }
        //public bool OnlyGroupIdDefined { get; set; }
        //public string SiteUrl { get; set; }
        //private bool _includeSubsites;
        //public bool IncludeSubsites
        //{
        //    get { return _includeSubsites; }
        //    set
        //    {
        //        _includeSubsites = value;
        //        NeedAddAdmin();
        //    }
        //}

        public SiteReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = SiteReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(SiteReport);
            SolutionHeader.SolutionDocs = SiteReport.s_SolutionDocs;

            this.Detailed = true;
            this.IncludeAdmins = false;

            //this.RemoveAdmin = true;

            //this.IncludePersonalSite = false;
            //this.IncludeShareSite = true;
            //this.OnlyGroupIdDefined = false;
            //this.SiteUrl = String.Empty;
            //this.IncludeSubsites = false;
        }

        //private void NeedAddAdmin()
        //{
        //    if(Detailed || IncludeAdmins || IncludeSubsites)
        //    {
        //        AdminF.Visibility = Visibility.Visible;
        //    }
        //    else
        //    {
        //        AdminF.Visibility = Visibility.Collapsed;
        //    }
        //}

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            //SPOTenantSiteUrlsParameters tSiteParam = new()
            //{
            //    RemoveAdmin = this.RemoveAdmin,

            //    IncludePersonalSite = this.IncludePersonalSite,
            //    IncludeShareSite = this.IncludeShareSite,
            //    OnlyGroupIdDefined = this.OnlyGroupIdDefined,
            //    SiteUrl = this.SiteUrl,
            //    IncludeSubsites = this.IncludeSubsites,
            //};

            //SPOListsParameters l = new();
            //SPOItemsParameters i = new();
            //SPOSitePermissionsCSOMParameters permissionsParameters = new(l, i)
            //{
            //    IncludeAdmins = this.IncludeAdmins,
            //};

            //SiteReportParameters parameters = new(tSiteParam, permissionsParameters)
            //{
            //    Detailed = this.Detailed,
            //};

            //await new SiteReport(parameters, uiLog, cancelTokenSource).RunAsync();








            SPOListsParameters l = new();
            SPOItemsParameters i = new();
            SPOSitePermissionsCSOMParameters permissionsParameters = new(l, i)
            {
                IncludeAdmins = this.IncludeAdmins,
            };

            var siteAccParam = AdminF.Parameters;
            var siteParam = SiteF.Parameters;
            siteAccParam.SiteParam = siteParam;

            SiteReportParameters parameters = new(siteAccParam, permissionsParameters)
            {
                Detailed = this.Detailed,
            };
            await new SiteReport(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
