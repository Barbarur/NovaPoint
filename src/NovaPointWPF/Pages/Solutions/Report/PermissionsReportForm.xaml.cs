using NovaPointLibrary.Commands.SharePoint.Permission;
using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Directory;
using NovaPointLibrary.Solutions.Report;
using NovaPointWPF.UserControls;
using System;
using System.Windows;
using System.Windows.Controls;

namespace NovaPointWPF.Pages.Solutions.Report
{
    public partial class PermissionsReportForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

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

            SolutionName = PermissionsReport.s_SolutionName;
            SolutionCode = nameof(PermissionsReport);
            SolutionDocs = PermissionsReport.s_SolutionDocs;

            SolutionCreate = PermissionsReport.Create;

            this.UserListOnly = false;

            this.IncludeAdmins = true;
            this.IncludeSiteAccess = true;
            this.IncludeUniquePermissions = true;
        }

        public ISolutionParameters GetParameters()
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
            return parameters;
        }

    }
}
