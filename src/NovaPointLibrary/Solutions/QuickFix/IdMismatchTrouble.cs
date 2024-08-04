using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.UserProfiles;
using NovaPointLibrary.Commands;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using User = Microsoft.SharePoint.Client.User;

namespace NovaPointLibrary.Solutions.QuickFix
{
    public class IdMismatchTrouble
    {
        public readonly static string _solutionName = "Resolve user ID Mismatch";
        public readonly static string _solutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-QuickFix-IdMismatchTrouble";

        private IdMismatchTroubleParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static Expression<Func<User, object>>[] _userRetrievalExpressions = new Expression<Func<User, object>>[]
        {
            u => u.Email,
            u => u.IsSiteAdmin,
            u => u.LoginName,
            u => u.UserPrincipalName,
        };

        private IdMismatchTrouble(NPLogger logger, Commands.Authentication.AppInfo appInfo, IdMismatchTroubleParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(IdMismatchTroubleParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            NPLogger logger = new(uiAddLog, "IdMismatchTrouble", parameters);
            try
            {
                var appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new IdMismatchTrouble(logger, appInfo, parameters).RunScriptAsync();

                logger.ScriptFinish();

            }
            catch (Exception ex)
            {
                logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            var tenant = new Tenant(await _appInfo.GetContext(_appInfo.AdminUrl));
            var result = tenant.EncodeClaim(_param.UserUpn);
            tenant.Context.ExecuteQueryRetry();
            var accountName = result.Value;
            _logger.LogUI(GetType().Name, $"Affected user account name: {accountName}");

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
            _logger.LogTxt(GetType().Name, $"Affected user account SID: {userSID}");
            userSID = userSID.Substring(userSID.IndexOf("i:0h.f|membership|") + 18);
            userSID = userSID[..(userSID.IndexOf("@live.com"))];
            _logger.LogUI(GetType().Name, $"Affected user account SID: {userSID}");


            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();

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
                    _logger.ReportError(GetType().Name, "Site", siteResults.SiteUrl, ex);
                    AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                }
            }
        }

        private async Task FixIDMismatchAsync(string siteUrl, string correctUserID)
        {
            _appInfo.IsCancelled();

            try
            {
                User? oUser = await new SPOSiteUserCSOM(_logger, _appInfo).GetByEmailAsync(siteUrl, _param.UserUpn, _userRetrievalExpressions);

                if (oUser == null) { return; }

                string siteUserID = ((UserIdInfo)oUser.UserId).NameId;
                _logger.LogTxt(GetType().Name, $"User found on site with ID '{siteUserID}', correct ID is {correctUserID}");
                if (siteUserID != correctUserID)
                {
                    if (oUser.IsSiteAdmin)
                    {
                        if (!_param.ReportMode) { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveForceAsync(siteUrl, oUser.LoginName); }
                        AddRecord(siteUrl, "User removed as Site Collection Admin");
                    }

                    if (!_param.ReportMode) { await new SPOSiteUserCSOM(_logger, _appInfo).RemoveAsync(siteUrl, oUser); }
                    AddRecord(siteUrl, "User removed from site");
                }

                string upnCoded = oUser.UserPrincipalName.Trim().Replace("@", "_").Replace(".", "_");
                if (siteUrl.Contains(upnCoded, StringComparison.OrdinalIgnoreCase) && siteUrl.Contains("-my.sharepoint.com", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_param.ReportMode) { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).AddPrimarySiteCollectionAdminAsync(siteUrl, oUser.UserPrincipalName); }
                    AddRecord(siteUrl, "Added user as Primary Site Collection Admin");
                }
            }
            catch (Exception ex)
            {
                _logger.ReportError(GetType().Name, "Site", siteUrl, ex);
                AddRecord(siteUrl, $"Error while processing the site: {ex.Message}");
            }
        }

        private void AddRecord(string siteUrl,
                               string remarks = "")
        {
            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;
            recordItem.Remarks = remarks;

            _logger.DynamicCSV(recordItem);
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

        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam { get; set; }

        public IdMismatchTroubleParameters(SPOTenantSiteUrlsWithAccessParameters siteParam)
        {
            SiteAccParam = siteParam;
        }
    }
}
