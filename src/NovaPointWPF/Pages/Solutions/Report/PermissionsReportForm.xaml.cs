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
using NovaPointLibrary.Commands.SharePoint.Permission;
using NovaPointWPF.UserControls;

namespace NovaPointWPF.Pages.Solutions.Report
{
    /// <summary>
    /// Interaction logic for PermissionsReportForm.xaml
    /// </summary>
    public partial class PermissionsReportForm : Page, ISolutionForm
    {
        public bool _userListOnly = false;
        public bool UserListOnly
        {
            get { return _userListOnly; }
            set
            {
                _userListOnly = value;
                if (value)
                {
                    UserForm.AllUsers = true;
                    UserForm.Visibility = Visibility.Collapsed;
                }
                else
                {
                    UserForm.Visibility = Visibility.Visible;
                    UserForm.SingleUser = true;
                }
            }
        }

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
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            SPOSitePermissionsCSOMParameters permissionsParameters = new(ListForm.Parameters, ItemForm.Parameters)
            {
                IncludeAdmins = this.IncludeAdmins,
                IncludeSiteAccess = this.IncludeSiteAccess,
                IncludeUniquePermissions = this.IncludeUniquePermissions,
            };

            PermissionsReportParameters parameters = new(UserForm.Parameters, AdminF.Parameters, SiteF.Parameters, permissionsParameters)
            {
                OnlyUserList = this.UserListOnly,
            };
            await PermissionsReport.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
