using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.UserProfiles;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Core.Context;
using System.Dynamic;
using System.Linq.Expressions;
using User = Microsoft.SharePoint.Client.User;

namespace NovaPointLibrary.Solutions.QuickFix
{
    public class IdMismatchTrouble : ISolution
    {
        public readonly static string s_SolutionName = "Resolve user ID Mismatch";
        public readonly static string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-QuickFix-IdMismatchTrouble";

        private ContextSolution _ctx;
        private IdMismatchTroubleParameters _param;

        private static Expression<Func<User, object>>[] _userRetrievalExpressions = new Expression<Func<User, object>>[]
        {
            u => u.Email,
            u => u.IsSiteAdmin,
            u => u.LoginName,
            u => u.UserPrincipalName,
        };

        private IdMismatchTrouble(ContextSolution context, IdMismatchTroubleParameters parameters)
        {
            _ctx = context;
            _param = parameters;
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new IdMismatchTrouble(context, (IdMismatchTroubleParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            var tenant = new Tenant(await _ctx.AppClient.GetContext(_ctx.AppClient.AdminUrl));
            var result = tenant.EncodeClaim(_param.UserUpn);
            tenant.Context.ExecuteQueryRetry();
            var accountName = result.Value;
            _ctx.Logger.UI(GetType().Name, $"Affected user account name: {accountName}");

            var peopleManager = new PeopleManager(tenant.Context);
            var personProperties = peopleManager.GetPropertiesFor(accountName);
            tenant.Context.Load(personProperties);
            tenant.Context.ExecuteQueryRetry();

            string? userSID = null;
            foreach (var property in personProperties.UserProfileProperties)
            {
                if (property.Key == "SID") { userSID = property.Value; }
            }
            if (userSID == null)
            {
                throw new Exception("Unable to obtain users SID");
            }
            _ctx.Logger.Info(GetType().Name, $"Affected user account SID: {userSID}");
            userSID = userSID.Substring(userSID.IndexOf("i:0h.f|membership|") + 18);
            userSID = userSID[..(userSID.IndexOf("@live.com"))];
            _ctx.Logger.UI(GetType().Name, $"Affected user account SID: {userSID}");


            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_ctx.Logger, _ctx.AppClient, _param.SiteAccParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();

                if (siteResults.Ex != null)
                {
                    AddRecord(siteResults.SiteUrl, remarks: siteResults.Ex.Message);
                    continue;
                }

                try
                {
                    await FixIDMismatchAsync(siteResults.SiteUrl, userSID);
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Site", siteResults.SiteUrl, ex);
                    AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                }
            }
        }

        private async Task FixIDMismatchAsync(string siteUrl, string correctUserID)
        {
            _ctx.AppClient.IsCancelled();

            try
            {
                User? oUser = await new SPOSiteUserCSOM(_ctx.Logger, _ctx.AppClient).GetByEmailAsync(siteUrl, _param.UserUpn, _userRetrievalExpressions);

                if (oUser == null) { return; }

                string siteUserID = ((UserIdInfo)oUser.UserId).NameId;
                _ctx.Logger.Info(GetType().Name, $"User found on site with ID '{siteUserID}', correct ID is {correctUserID}");
                if (siteUserID != correctUserID)
                {
                    if (oUser.IsSiteAdmin)
                    {
                        if (!_param.ReportMode) { await new SPOSiteCollectionAdminCSOM(_ctx.Logger, _ctx.AppClient).RemoveForceAsync(siteUrl, oUser.LoginName); }
                        AddRecord(siteUrl, "User removed as Site Collection Admin");
                    }

                    if (!_param.ReportMode) { await new SPOSiteUserCSOM(_ctx.Logger, _ctx.AppClient).RemoveAsync(siteUrl, oUser); }
                    AddRecord(siteUrl, "User removed from site");
                }

                string upnCoded = oUser.UserPrincipalName.Trim().Replace("@", "_").Replace(".", "_");
                if (siteUrl.Contains(upnCoded, StringComparison.OrdinalIgnoreCase) && siteUrl.Contains("-my.sharepoint.com", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_param.ReportMode) { await new SPOSiteCollectionAdminCSOM(_ctx.Logger, _ctx.AppClient).AddPrimarySiteCollectionAdminAsync(siteUrl, oUser.UserPrincipalName); }
                    AddRecord(siteUrl, "Added user as Primary Site Collection Admin");
                }
            }
            catch (Exception ex)
            {
                _ctx.Logger.Error(GetType().Name, "Site", siteUrl, ex);
                AddRecord(siteUrl, $"Error while processing the site: {ex.Message}");
            }
        }

        private void AddRecord(string siteUrl,
                               string remarks = "")
        {
            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;
            recordItem.Remarks = remarks;

            _ctx.Logger.DynamicCSV(recordItem);
        }

    }

    public class IdMismatchTroubleParameters : ISolutionParameters
    {
        public bool ReportMode { get; set; } = true;

        private string _userUpn = string.Empty;
        public string UserUpn
        {
            get { return _userUpn; }
            set { _userUpn = value.Trim(); }
        }

        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }

        public IdMismatchTroubleParameters(SPOAdminAccessParameters adminAccess, SPOTenantSiteUrlsParameters siteParam)
        {
            AdminAccess = adminAccess;
            SiteParam = siteParam;
        }
    }
}
