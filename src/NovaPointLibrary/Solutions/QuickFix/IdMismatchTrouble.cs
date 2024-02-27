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

        private IdMismatchTroubleParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (IdMismatchTroubleParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private Expression<Func<User, object>>[] _userRetrievalExpressions = new Expression<Func<User, object>>[]
        {
            u => u.Email,
            u => u.IsSiteAdmin,
            u => u.LoginName,
            u => u.UserPrincipalName,
        };

        public IdMismatchTrouble(IdMismatchTroubleParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            Parameters = parameters;
            _logger = new(uiAddLog, this.GetType().Name, parameters);
            _appInfo = new(_logger, cancelTokenSource);
        }

        public async Task RunAsync()
        {
            try
            {
                await RunScriptAsync();

                _logger.ScriptFinish();
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
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


            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteParameters).GetAsyncNEW())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(siteResults.ErrorMessage))
                {
                    AddRecord(siteResults.SiteUrl, remarks: siteResults.ErrorMessage);
                    continue;
                }

                try
                {
                    await FixIDMismatchAsync(siteResults.SiteUrl, userSID);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", siteResults.SiteUrl, ex);
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
                    _logger.LogUI(GetType().Name, $"Adding user '{oUser.UserPrincipalName}' as Site Collection Admin for OneDrive {siteUrl}");
                    if (!_param.ReportMode) { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).SetAsync(siteUrl, oUser.UserPrincipalName); }
                }
            }
            catch (Exception ex)
            {
                _logger.ReportError("Site", siteUrl, ex);
                AddRecord(siteUrl, $"Error while processing the site: {ex.Message}");
            }
        }

        //private async Task RunScriptAsyncOLD()
        //{
        //    string spoAdminAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(_appInfo.AdminUrl);
        //    string rootUrl = _param.SiteUrl.Substring(0, _param.SiteUrl.IndexOf(".com") + 4);
        //    string rootSiteAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(rootUrl);

        //    new SetSPOSiteCollectionAdmin(_logger, _appInfo, spoAdminAccessToken).CSOM(_param.AdminUpn, _param.SiteUrl);
        //    if (!_param.ReportMode)
        //    {
        //        SingleSiteAsync(spoAdminAccessToken, _param.SiteUrl, rootSiteAccessToken, "abcdefghijk");
        //    }


        //    if (_param.PreventAllSites) 
        //    {
        //        new RegisterSPOSiteUser(_logger, _appInfo, rootSiteAccessToken).CSOM(_param.SiteUrl, _param.UserUpn);

        //        User? user = new GetUser(_logger, rootSiteAccessToken).CsomSingle(_param.SiteUrl, _param.UserUpn);
        //        if (user == null) { throw new Exception("User couldn't be found to obtain correct user ID"); }

        //        UserIdInfo userIdInfo = user.UserId;
        //        string userCorrectId = userIdInfo.NameId;

        //        await AllSitesAsync(spoAdminAccessToken, userCorrectId); 
        //    }

        //    if (!_param.ReportMode && _param.RemoveAdmin)
        //    {
        //        if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
        //        new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(_param.SiteUrl, _param.AdminUpn);
        //    }

        //    _logger.ScriptFinish();
        //}

        //private void SingleSiteAsync(string spoAdminAccessToken, string siteUrl, string siteAccessToken, string correctUserID)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.SingleSiteAsync";
        //    _logger.LogTxt(methodName, $"Start processing Site '{siteUrl}'");

        //    try
        //    {
        //        User? user = new GetUser(_logger, siteAccessToken).CsomSingle(siteUrl, _param.UserUpn);

        //        if (user == null) { return; }

        //        string siteUserID = ((UserIdInfo)user.UserId).NameId;
        //        if (siteUserID != correctUserID)
        //        {
        //            if (!_param.ReportMode)
        //            {
        //                if (user.IsSiteAdmin)
        //                {
        //                    if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
        //                    new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(siteUrl, user.UserPrincipalName);
        //                }

        //                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
        //                new RemoveUser(_logger, siteAccessToken).Csom(siteUrl, user.UserPrincipalName);
        //            }

        //            string remarks = "User with incorrect ID found on Site and Removed";

        //            AddRecordToCSV(siteUrl, remarks);

        //            _logger.LogTxt(GetType().Name, remarks);
        //        }

        //        string urlOwnerODBCheckUp = _param.UserUpn.Replace("@", "_").Replace(".", "_");
        //        if (siteUrl.Contains(urlOwnerODBCheckUp, StringComparison.OrdinalIgnoreCase) && siteUrl.Contains("-my.sharepoint.com") && !_param.ReportMode)
        //        {
        //            _logger.LogUI(methodName, $"Adding user '{user.UserPrincipalName}' as OneDrive Admin for site {siteUrl}");
        //            new SetSPOSiteCollectionAdmin(_logger, _appInfo, spoAdminAccessToken).CSOM(_param.UserUpn, siteUrl);
        //            new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(_param.SiteUrl, _param.AdminUpn);
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        string remarks = $"Error: {ex.Message}";

        //        dynamic recordError = new ExpandoObject();
        //        recordError.SiteUrl = siteUrl;
        //        recordError.Remarks = remarks;
        //        _logger.DynamicCSV(recordError);

        //        _logger.LogUI(methodName, remarks);
        //    }
        //}


        //private async Task AllSitesAsync(string spoAdminAccessToken, string correctUserID)
        //{
        //    _appInfo.IsCancelled();
        //    string methodName = $"{GetType().Name}.AllSitesAsync";
        //    _logger.LogTxt(methodName, $"Start fixing ID Mismatch for all Sites");

        //    string rootPersonalSiteAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(_appInfo.RootPersonalUrl);
        //    string rootShareSiteAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(_appInfo.RootSharedUrl);

        //    List<SiteProperties> collSiteCollections = new GetSPOSiteCollection(_logger, _appInfo, spoAdminAccessToken).CSOM_AdminAll(_appInfo.AdminUrl, true);
        //    ProgressTracker progress = new(_logger, collSiteCollections.Count);
        //    foreach (SiteProperties oSiteCollection in collSiteCollections)
        //    {
        //        _appInfo.IsCancelled();
        //        _logger.LogTxt(methodName, $"Processing Site '{oSiteCollection.Title}'");

        //        string currentSiteAccessToken = oSiteCollection.Url.Contains("-my.sharepoint.com") ? rootPersonalSiteAccessToken : rootShareSiteAccessToken;

        //        try
        //        {
        //            new SetSPOSiteCollectionAdmin(_logger, _appInfo, spoAdminAccessToken).CSOM(_param.AdminUpn, oSiteCollection.Url);

        //            SingleSiteAsync(spoAdminAccessToken, oSiteCollection.Url, currentSiteAccessToken, correctUserID);

        //            if (_param.RemoveAdmin)
        //            {
        //                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
        //                new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(oSiteCollection.Url, _param.AdminUpn);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogUI(GetType().Name, $"Error processing Site Collection '{oSiteCollection.Url}'");
        //            _logger.LogTxt(GetType().Name, $"Exception: {ex.Message}");
        //            _logger.LogTxt(GetType().Name, $"Trace: {ex.StackTrace}");

        //            AddRecordToCSV(oSiteCollection.Url, ex.Message);
        //        }

        //        progress.ProgressUpdateReport();
        //    }
        //}

        //private void AddRecordToCSV(string siteUrl, string remarks)
        //{
        //    dynamic recordSite = new ExpandoObject();
        //    recordSite.SiteUrl = siteUrl;
        //    recordSite.Remarks = remarks;
        //    _logger.DynamicCSV(recordSite);
        //}

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

        public SPOTenantSiteUrlsParameters SiteParameters { get; set; } = new();

        //private string _siteUrl = string.Empty;
        //public string SiteUrl
        //{
        //    get { return _siteUrl; }
        //    set { _siteUrl = value.Trim(); }
        //}

        //private string _adminUpn = string.Empty;
        //public string AdminUpn
        //{
        //    get { return _adminUpn; }
        //    set { _adminUpn = value.Trim(); }
        //}
        //public bool RemoveAdmin { get; set; } = false;
        //public bool PreventAllSites { get; set; } = false;
    }
}
