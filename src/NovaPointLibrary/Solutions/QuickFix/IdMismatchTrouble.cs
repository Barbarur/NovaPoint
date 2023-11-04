using Microsoft.Graph;
using Microsoft.IdentityModel.Logging;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Commands.Site;
using NovaPointLibrary.Commands.User;
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
        public static string _solutionName = "Resolve user ID Mismatch";
        public static string _solutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-QuickFix-IdMismatchTrouble";

        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;
        
        private readonly string _userUpn;
        private readonly string _siteUrl;
        private readonly string _adminUpn;
        
        private readonly bool _preventAllSites;
        private readonly bool _removeAdmin;
        public readonly bool _reportMode;


        public IdMismatchTrouble(Action<LogInfo> uiAddLog,
                                 Commands.Authentication.AppInfo appInfo,
                                 IdMismatchTroubleParameters parameters)
        {
            _logger = new(uiAddLog, "QuickFix", GetType().Name);
            _appInfo = appInfo;
            
            _userUpn = parameters.UserUpn;
            _siteUrl = parameters.SiteUrl;
            _adminUpn = parameters.AdminUpn;

            _preventAllSites = parameters.PreventAllSites;
            _removeAdmin = parameters.RemoveAdmin;
            _reportMode = parameters.ReportMode;
        }


        public async Task RunAsync()
        {
            try
            {
                if ( string.IsNullOrWhiteSpace(_userUpn) || string.IsNullOrWhiteSpace(_siteUrl) || string.IsNullOrWhiteSpace(_adminUpn) )
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
            _logger.ScriptStartNotice();

            string spoAdminAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(_appInfo.AdminUrl);
            string rootUrl = _siteUrl.Substring(0, _siteUrl.IndexOf(".com") + 4);
            string rootSiteAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(rootUrl);

            new SetSPOSiteCollectionAdmin(_logger, _appInfo, spoAdminAccessToken).CSOM(_adminUpn, _siteUrl);
            if (!_reportMode)
            {
                SingleSiteAsync(spoAdminAccessToken, _siteUrl, rootSiteAccessToken, "abcdefghijk");
            }


            if (_preventAllSites) 
            {
                new RegisterSPOSiteUser(_logger, _appInfo, rootSiteAccessToken).CSOM(_siteUrl, _userUpn);

                User? user = new GetUser(_logger, rootSiteAccessToken).CsomSingle(_siteUrl, _userUpn);
                if (user == null) { throw new Exception("User couldn't be found to obtain correct user ID"); }

                UserIdInfo userIdInfo = user.UserId;
                string userCorrectId = userIdInfo.NameId;

                await AllSitesAsync(spoAdminAccessToken, userCorrectId); 
            }

            if (!_reportMode && _removeAdmin)
            {
                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(_siteUrl, _adminUpn);
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
                User? user = new GetUser(_logger, siteAccessToken).CsomSingle(siteUrl, _userUpn);

                if (user == null) { return; }

                string siteUserID = ((UserIdInfo)user.UserId).NameId;
                if (siteUserID != correctUserID)
                {
                    if (!_reportMode)
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

                    _logger.AddLogToTxt(remarks);
                }

                string urlOwnerODBCheckUp = _userUpn.Replace("@", "_").Replace(".", "_");
                if (siteUrl.Contains(urlOwnerODBCheckUp, StringComparison.OrdinalIgnoreCase) && siteUrl.Contains("-my.sharepoint.com") && !_reportMode)
                {
                    _logger.LogUI(methodName, $"Adding user '{user.UserPrincipalName}' as OneDrive Admin for site {siteUrl}");
                    new SetSPOSiteCollectionAdmin(_logger, _appInfo, spoAdminAccessToken).CSOM(_userUpn, siteUrl);
                    new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(_siteUrl, _adminUpn);
                }
            }
            catch(Exception ex)
            {
                string remarks = $"Error: {ex.Message}";

                dynamic recordError = new ExpandoObject();
                recordError.SiteUrl = siteUrl;
                recordError.Remarks = remarks;
                _logger.RecordCSV(recordError);

                _logger.AddLogToUI(remarks);
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

                progress.MainReportProgress($"Processing Site '{oSiteCollection.Title}'");
                
                string currentSiteAccessToken = oSiteCollection.Url.Contains("-my.sharepoint.com") ? rootPersonalSiteAccessToken : rootShareSiteAccessToken;
                
                try
                {
                    new SetSPOSiteCollectionAdmin(_logger, _appInfo, spoAdminAccessToken).CSOM(_adminUpn, oSiteCollection.Url);

                    SingleSiteAsync(spoAdminAccessToken, oSiteCollection.Url, currentSiteAccessToken, correctUserID);

                    var collSubsites = new GetSubsite(_logger, _appInfo, currentSiteAccessToken).CsomAllSubsitesWithRoles(oSiteCollection.Url);
                    progress.SubTaskProgressReset(collSubsites.Count);
                    foreach (var oSubsite in collSubsites)
                    {
                        progress.SubTaskReportProgress($"Processing SubSite '{oSubsite.Title}'");

                        if (oSubsite.HasUniqueRoleAssignments)
                        {
                            try
                            {
                                SingleSiteAsync(spoAdminAccessToken, oSubsite.Url, currentSiteAccessToken, correctUserID);
                            }
                            catch (Exception ex)
                            {
                                _logger.AddLogToUI($"Error processing Site Collection '{oSubsite.Url}'");
                                _logger.AddLogToTxt($"Exception: {ex.Message}");
                                _logger.AddLogToTxt($"Trace: {ex.StackTrace}");

                                AddRecordToCSV(oSubsite.Url, ex.Message);
                            }
                        }

                        progress.SubTaskCounterIncrement();
                    }


                    if (_removeAdmin)
                    {
                        if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
                        new RemoveSiteCollectionAdmin(_logger, spoAdminAccessToken, _appInfo.Domain).Csom(oSiteCollection.Url, _adminUpn);
                    }
                }
                catch (Exception ex)
                {
                    _logger.AddLogToUI($"Error processing Site Collection '{oSiteCollection.Url}'");
                    _logger.AddLogToTxt($"Exception: {ex.Message}");
                    _logger.AddLogToTxt($"Trace: {ex.StackTrace}");

                    AddRecordToCSV(oSiteCollection.Url, ex.Message);
                }

                progress.MainCounterIncrement();
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

    public class IdMismatchTroubleParameters
    {
        internal readonly string UserUpn;
        internal readonly string SiteUrl;
        internal readonly string AdminUpn;
        public bool RemoveAdmin { get; set; } = false;
        public bool PreventAllSites { get; set; } = false;
        public bool ReportMode { get; set; } = false;

        public IdMismatchTroubleParameters(string userUpn, string siteUrl, string adminUpn)
        {
            UserUpn = userUpn;
            SiteUrl = siteUrl;
            AdminUpn = adminUpn;
        }
    }
}
