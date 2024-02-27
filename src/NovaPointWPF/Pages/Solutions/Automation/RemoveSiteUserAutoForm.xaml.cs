using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RemoveSiteUserAutoForm.xaml
    /// </summary>
    public partial class RemoveSiteUserAutoForm : Page, ISolutionForm
    {
        public bool AllUsers { get; set; }
        public string TargetUserUPN { get; set; }
        public bool IncludeExternalUsers { get; set; }
        public bool IncludeEveryone { get; set; }
        public bool IncludeEveryoneExceptExternal { get; set; }

        public bool RemoveAdmin { get; set; }

        public bool IncludePersonalSite { get; set; }
        public bool IncludeShareSite { get; set; }
        public bool OnlyGroupIdDefined { get; set; }
        public string SiteUrl { get; set; }

        public RemoveSiteUserAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RemoveSiteUserAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RemoveSiteUserAuto);
            SolutionHeader.SolutionDocs = RemoveSiteUserAuto.s_SolutionDocs;

            this.AllUsers = true;
            this.TargetUserUPN = string.Empty;
            this.IncludeExternalUsers = false;
            this.IncludeEveryone = false;
            this.IncludeEveryoneExceptExternal = false;

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

            RemoveUserAutoParameters parameters = new()
            {
                //AllUsers = this.AllUsers,
                //TargetUserUPN = this.TargetUserUPN,
                //IncludeExternalUsers = this.IncludeExternalUsers,
                //IncludeEveryone = this.IncludeEveryone,
                //IncludeEveryoneExceptExternal = this.IncludeEveryoneExceptExternal,
                UserParameters = this.UserForm.Parameters,

                SiteParameters = siteParameters,

                //RemoveAdmin = this.RemoveAdmin,

                //IncludePersonalSite = this.IncludePersonalSite,
                //IncludeShareSite = this.IncludeShareSite,
                //OnlyGroupIdDefined = this.OnlyGroupIdDefined,
                //SiteUrl = this.SiteUrl,
            };
            //uiLog(LogInfo.ErrorNotification($"AllUsers: {UserForm.Parameters.AllUsers}"));
            //uiLog(LogInfo.ErrorNotification($"TargetUserUPN: {UserForm.Parameters.IncludeUserUPN}"));
            //uiLog(LogInfo.ErrorNotification($"IncludeExternalUsers: {UserForm.Parameters.IncludeExternalUsers}"));
            //uiLog(LogInfo.ErrorNotification($"IncludeEveryone: {UserForm.Parameters.IncludeEveryone}"));
            //uiLog(LogInfo.ErrorNotification($"IncludeEveryoneExceptExternal: {UserForm.Parameters.IncludeEveryoneExceptExternal}"));
            await new RemoveSiteUserAuto(parameters, uiLog, cancelTokenSource).RunAsync();

        }
    }
}
