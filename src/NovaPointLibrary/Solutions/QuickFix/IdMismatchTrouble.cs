using Microsoft.Graph;
using Microsoft.IdentityModel.Logging;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Commands.Site;
using NovaPointLibrary.Commands.User;
using NovaPointLibrary.Solutions.Report;
using PnP.Framework.Modernization.Telemetry;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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
        
        //private readonly string _userUpn;
        //private readonly string _siteUrl;
        //private readonly string _adminUpn;
        
        //private readonly bool _preventAllSites;
        //private readonly bool _removeAdmin;
        //public readonly bool _reportMode;

        public IdMismatchTrouble(IdMismatchTroubleParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            Parameters = parameters;
            _logger = new(uiAddLog, this.GetType().Name, parameters);
            _appInfo = new(_logger, cancelTokenSource);
        }
        //public IdMismatchTrouble(Action<LogInfo> uiAddLog,
        //                         Commands.Authentication.AppInfo appInfo,
        //                         IdMismatchTroubleParameters parameters)
        //{
        //    _logger = new(uiAddLog, "QuickFix", GetType().Name);
        //    _appInfo = appInfo;
            
        //    _userUpn = parameters.UserUpn;
        //    _siteUrl = parameters.SiteUrl;
        //    _adminUpn = parameters.AdminUpn;

        //    _preventAllSites = parameters.PreventAllSites;
        //    _removeAdmin = parameters.RemoveAdmin;
        //    _reportMode = parameters.ReportMode;
        //}

        public async Task RunAsync()
        {
            try
            {
                if ( string.IsNullOrWhiteSpace(_param.UserUpn) || string.IsNullOrWhiteSpace(_param.SiteUrl) || string.IsNullOrWhiteSpace(_param.AdminUpn) )
                {
                    string message = $"FORM INCOMPLETED: Please fill up the form";
                    Exception ex = new(message);
                    throw ex;
                }
                else
                {
                    await RunScriptAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            string spoAdminAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(_appInfo.AdminUrl);
            string rootUrl = _param.SiteUrl.Substring(0, _param.SiteUrl.IndexOf(".com") + 4);
            string rootSiteAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(rootUrl);

            new SetSPOSiteCollectionAdmin(_logger, _appInfo, spoAdminAccessToken).CSOM(_param.AdminUpn, _param.SiteUrl);
            if (!_param.ReportMode)
            {
                SingleSiteAsync(spoAdminAccessToken, _param.SiteUrl, rootSiteAccessToken, "abcdefghijk");
            }


            if (_param.PreventAllSites) 
            {
                new RegisterSPOSiteUser(_logger, _appInfo, rootSiteAccessToken).CSOM(_param.SiteUrl, _param.UserUpn);

                User? user = new GetUser(_logger, rootSiteAccessToken).CsomSingle(_param.SiteUrl, _param.UserUpn);
                if (user == null) { throw new Exception("User couldn't be found to obtain correct user ID"); }

                UserIdInfo userIdInfo = user.UserId;
                string userCorrectId = userIdInfo.NameId;

                await AllSitesAsync(spoAdminAccessToken, userCorrectId); 
            }

            if (!_param.ReportMode && _param.RemoveAdmin)
            {
                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(_param.SiteUrl, _param.AdminUpn);
            }

            _logger.ScriptFinish();
        }

        private void SingleSiteAsync(string spoAdminAccessToken, string siteUrl, string siteAccessToken, string correctUserID)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.SingleSiteAsync";
            _logger.LogTxt(methodName, $"Start processing Site '{siteUrl}'");

            try
            {
                User? user = new GetUser(_logger, siteAccessToken).CsomSingle(siteUrl, _param.UserUpn);

                if (user == null) { return; }

                string siteUserID = ((UserIdInfo)user.UserId).NameId;
                if (siteUserID != correctUserID)
                {
                    if (!_param.ReportMode)
                    {
                        if (user.IsSiteAdmin)
                        {
                            if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                            new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(siteUrl, user.UserPrincipalName);
                        }

                        if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                        new RemoveUser(_logger, siteAccessToken).Csom(siteUrl, user.UserPrincipalName);
                    }

                    string remarks = "User with incorrect ID found on Site and Removed";

                    AddRecordToCSV(siteUrl, remarks);

                    _logger.LogTxt(GetType().Name, remarks);
                }

                string urlOwnerODBCheckUp = _param.UserUpn.Replace("@", "_").Replace(".", "_");
                if (siteUrl.Contains(urlOwnerODBCheckUp, StringComparison.OrdinalIgnoreCase) && siteUrl.Contains("-my.sharepoint.com") && !_param.ReportMode)
                {
                    _logger.LogUI(methodName, $"Adding user '{user.UserPrincipalName}' as OneDrive Admin for site {siteUrl}");
                    new SetSPOSiteCollectionAdmin(_logger, _appInfo, spoAdminAccessToken).CSOM(_param.UserUpn, siteUrl);
                    new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(_param.SiteUrl, _param.AdminUpn);
                }
            }
            catch(Exception ex)
            {
                string remarks = $"Error: {ex.Message}";

                dynamic recordError = new ExpandoObject();
                recordError.SiteUrl = siteUrl;
                recordError.Remarks = remarks;
                _logger.RecordCSV(recordError);

                _logger.LogUI(methodName, remarks);
            }
        }


        private async Task AllSitesAsync(string spoAdminAccessToken, string correctUserID)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.AllSitesAsync";
            _logger.LogTxt(methodName, $"Start fixing ID Mismatch for all Sites");

            string rootPersonalSiteAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(_appInfo.RootPersonalUrl);
            string rootShareSiteAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(_appInfo.RootSharedUrl);

            List<SiteProperties> collSiteCollections = new GetSPOSiteCollection(_logger, _appInfo, spoAdminAccessToken).CSOM_AdminAll(_appInfo.AdminUrl, true);
            ProgressTracker progress = new(_logger, collSiteCollections.Count);
            foreach (SiteProperties oSiteCollection in collSiteCollections)
            {
                _appInfo.IsCancelled();
                _logger.LogTxt(methodName, $"Processing Site '{oSiteCollection.Title}'");
                
                string currentSiteAccessToken = oSiteCollection.Url.Contains("-my.sharepoint.com") ? rootPersonalSiteAccessToken : rootShareSiteAccessToken;
                
                try
                {
                    new SetSPOSiteCollectionAdmin(_logger, _appInfo, spoAdminAccessToken).CSOM(_param.AdminUpn, oSiteCollection.Url);

                    SingleSiteAsync(spoAdminAccessToken, oSiteCollection.Url, currentSiteAccessToken, correctUserID);

                    if (_param.RemoveAdmin)
                    {
                        if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                        new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(oSiteCollection.Url, _param.AdminUpn);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogUI(GetType().Name, $"Error processing Site Collection '{oSiteCollection.Url}'");
                    _logger.LogTxt(GetType().Name, $"Exception: {ex.Message}");
                    _logger.LogTxt(GetType().Name, $"Trace: {ex.StackTrace}");

                    AddRecordToCSV(oSiteCollection.Url, ex.Message);
                }

                progress.ProgressUpdateReport();
            }
        }

        private void AddRecordToCSV(string siteUrl, string remarks)
        {
            dynamic recordSite = new ExpandoObject();
            recordSite.SiteUrl = siteUrl;
            recordSite.Remarks = remarks;
            _logger.RecordCSV(recordSite);
        }
    }

    public class IdMismatchTroubleParameters : ISolutionParameters
    {
        public string UserUpn { get; set; } = String.Empty;
        public string SiteUrl { get; set; } = String.Empty;
        public string AdminUpn { get; set; } = String.Empty;
        public bool RemoveAdmin { get; set; } = false;
        public bool PreventAllSites { get; set; } = false;
        public bool ReportMode { get; set; } = false;
    }
}
