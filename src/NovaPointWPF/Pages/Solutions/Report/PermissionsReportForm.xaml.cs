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
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointWPF.UserControls;

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for PermissionsReportForm.xaml
    /// </summary>
    public partial class PermissionsReportForm : Page, ISolutionForm
    {

        public bool UserListOnly { get; set; }

        public bool IncludeAdmins { get; set; }
        public bool IncludeSiteAccess { get; set; }
        private bool _includeUniquePermissions = true;
        public bool IncludeUniquePermissions
        {
            get { return _includeUniquePermissions; }
            set
            {
                _includeUniquePermissions = value;
                if (value)
                {
                    ListForm.Visibility = Visibility.Visible;
                    ItemForm.Visibility = Visibility.Visible;
                }
                else
                {
                    ListForm.Visibility = Visibility.Collapsed;
                    ItemForm.Visibility = Visibility.Collapsed;
                }
            }
        }

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


        public PermissionsReportForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = PermissionsReport.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(PermissionsReport);
            SolutionHeader.SolutionDocs = PermissionsReport.s_SolutionDocs;

            this.UserListOnly = false;

            this.IncludeAdmins = true;
            this.IncludeSiteAccess = true;
            this.IncludeUniquePermissions = true;

            //this.RemoveAdmin = true;

            //this.IncludePersonalSite = false;
            //this.IncludeShareSite = true;
            //this.OnlyGroupIdDefined = false;
            //this.SiteUrl = String.Empty;

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
            //SPOSitePermissionsCSOMParameters permissionsParameters = new()
            //{
            //    IncludeAdmins = this.IncludeAdmins,
            //    IncludeSiteAccess = this.IncludeSiteAccess,
            //    IncludeUniquePermissions = this.IncludeUniquePermissions,

            //    RemoveAdmin = this.RemoveAdmin,

            //    IncludePersonalSite = this.IncludePersonalSite,
            //    IncludeShareSite = this.IncludeShareSite,
            //    OnlyGroupIdDefined = this.OnlyGroupIdDefined,
            //    SiteUrl = this.SiteUrl,

            //    IncludeLists = this.IncludeLists,
            //    IncludeLibraries = this.IncludeLibraries,
            //    IncludeHiddenLists = this.IncludeHiddenLists,
            //    IncludeSystemLists = this.IncludeSystemLists,
            //    ListTitle = this.ListTitle,

            //    FolderRelativeUrl = this.FolderRelativeUrl,
            //};

            //PermissionsReportParameters parameters = new()
            //{
            //    UserParameters = this.UserForm.Parameters,

            //    OnlyUserList = this.UserListOnly,

            //    PermissionsParameters = permissionsParameters,

            //    //IncludeAdmins = this.IncludeAdmins,
            //    //IncludeSiteAccess = this.IncludeSiteAccess,
            //    //IncludeUniquePermissions = this.IncludeUniquePermissions,

            //    //RemoveAdmin = this.RemoveAdmin,

            //    //IncludePersonalSite = this.IncludePersonalSite,
            //    //IncludeShareSite = this.IncludeShareSite,
            //    //OnlyGroupIdDefined = this.OnlyGroupIdDefined,
            //    //SiteUrl = this.SiteUrl,

            //    //IncludeLists = this.IncludeLists,
            //    //IncludeLibraries = this.IncludeLibraries,
            //    //IncludeHiddenLists = this.IncludeHiddenLists,
            //    //IncludeSystemLists = this.IncludeSystemLists,
            //    //ListTitle = this.ListTitle,

            //    //FolderRelativeUrl = this.FolderRelativeUrl,
            //};





            //var userParameters = this.UserForm.Parameters;


            //SPOTenantSiteUrlsParameters tSiteParam = new()
            //{
            //    RemoveAdmin = this.RemoveAdmin,

            //    IncludePersonalSite = this.IncludePersonalSite,
            //    IncludeShareSite = this.IncludeShareSite,
            //    OnlyGroupIdDefined = this.OnlyGroupIdDefined,
            //    SiteUrl = this.SiteUrl,
            //    IncludeSubsites = this.IncludeSubsites,
            //};

            //SPOSitePermissionsCSOMParameters permissionsParameters = new(ListForm.Parameters, ItemForm.Parameters)
            //{
            //    IncludeAdmins = this.IncludeAdmins,
            //    IncludeSiteAccess = this.IncludeSiteAccess,
            //    IncludeUniquePermissions = this.IncludeUniquePermissions,
            //};

            //PermissionsReportParameters parameters = new(userParameters, tSiteParam, permissionsParameters)
            //{
            //    OnlyUserList = this.UserListOnly,
            //};

            //await new PermissionsReport(parameters, uiLog, cancelTokenSource).RunAsync();




            var siteAccParam = AdminF.Parameters;
            var siteParam = SiteF.Parameters;
            siteAccParam.SiteParam = siteParam;

            SPOSitePermissionsCSOMParameters permissionsParameters = new(ListForm.Parameters, ItemForm.Parameters)
            {
                IncludeAdmins = this.IncludeAdmins,
                IncludeSiteAccess = this.IncludeSiteAccess,
                IncludeUniquePermissions = this.IncludeUniquePermissions,
            };

            PermissionsReportParameters parameters = new(UserForm.Parameters, siteAccParam, permissionsParameters)
            {
                OnlyUserList = this.UserListOnly,
            };

            await new PermissionsReport(parameters, uiLog, cancelTokenSource).RunAsync();
        }
    }
}
