using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Site;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class SetSiteCollectionAdminAllAuto
    {
        private readonly LogHelper _logHelper;
        private readonly Commands.Authentication.AppInfo _appInfo;
        
        private readonly string _targetUserUPN;
        private readonly bool _isSiteAdmin;

        private readonly bool _includePersonalSite;
        private readonly bool _includeShareSite;
        private readonly bool _groupIdDefined;

        public SetSiteCollectionAdminAllAuto(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo, SetSiteCollectionAdminAllAutoParameters parameters)
        {
            _logHelper = new(uiAddLog, "Reports", GetType().Name);
            _appInfo = appInfo;
            
            _targetUserUPN = parameters.TargetUserUPN;
            _isSiteAdmin= parameters.IsSiteAdmin;
            
            _includePersonalSite = parameters.IncludePersonalSite;
            _includeShareSite = parameters.IncludeShareSite;
            _groupIdDefined = parameters.GroupIdDefined;
        }

        public async Task RunAsync()
        {
            try
            {
                await RunScriptAsync();
            }
            catch (Exception ex)
            {
                _logHelper.ScriptFinishErrorNotice(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _logHelper.ScriptStartNotice();

            string accessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(_appInfo._adminUrl);

            if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };
            var collSiteCollections = new GetSiteCollection(_logHelper, accessToken).CSOM_AdminAll(_appInfo._adminUrl, _includePersonalSite, _groupIdDefined);
            collSiteCollections.RemoveAll(s => s.Title == "" || s.Template.Contains("Redirect"));
            if (!_includePersonalSite) { collSiteCollections.RemoveAll(s => s.Template.Contains("SPSPERS")); }
            if (!_includeShareSite) { collSiteCollections.RemoveAll(s => !s.Template.Contains("SPSPERS")); }

            string urlOwnerODBCheckUp = _targetUserUPN
                .Replace("@", "_")
                .Replace(".", "_");
            collSiteCollections.RemoveAll(s => s.Title == "" || s.Url.Contains(urlOwnerODBCheckUp) && s.Template.Contains("SPSPERS"));

            double counter = 0;
            foreach (SiteProperties oSiteCollection in collSiteCollections)
            {
                if (this._appInfo.CancelToken.IsCancellationRequested) { this._appInfo.CancelToken.ThrowIfCancellationRequested(); };

                double progress = Math.Round(counter * 100 / collSiteCollections.Count, 2);
                counter++;
                _logHelper.AddProgressToUI(progress);
                _logHelper.AddLogToUI($"Processing Site Collection '{oSiteCollection.Title}'");

                try
                {
                    if (_isSiteAdmin)
                    {
                        new SetSiteCollectionAdmin(_logHelper, accessToken, _appInfo._domain).Add(_targetUserUPN, oSiteCollection.Url);
                    }
                    else
                    {
                        new RemoveSiteCollectionAdmin(_logHelper, accessToken, _appInfo._domain).Csom(_targetUserUPN, oSiteCollection.Url);
                    }
                    AddSiteRecordToCSV(oSiteCollection, $"Correctly chanced Site Collection Admin property");
                }
                catch (Exception ex)
                {
                    AddSiteRecordToCSV(oSiteCollection, $"Error processing Site Collection: {ex.Message}");
                    _logHelper.AddLogToUI($"Error processing Site Collection: {ex.Message}");
                    _logHelper.AddLogToTxt($"Exception Message: {ex.Message}");
                    _logHelper.AddLogToTxt($"Exception Trace: {ex.StackTrace}");
                }
            }
            _logHelper.ScriptFinishSuccessfulNotice();
        }

        private void AddSiteRecordToCSV(SiteProperties site, string remarks)
        {
            dynamic recordList = new ExpandoObject();
            recordList.Title = site.Title;
            recordList.SiteUrl = site.Url;
            recordList.ID = site.GroupId;

            recordList.Remarks = remarks;

            _logHelper.AddRecordToCSV(recordList);
        }
    }


    public class SetSiteCollectionAdminAllAutoParameters
    {
        internal readonly string TargetUserUPN;
        internal readonly bool IsSiteAdmin;
        public bool IncludeShareSite { get; set; } = true;
        public bool IncludePersonalSite { get; set; } = false;
        public bool GroupIdDefined { get; set; } = false;

        public SetSiteCollectionAdminAllAutoParameters(string targetUserUPN, bool isSiteAdmin)
        {
            TargetUserUPN = targetUserUPN;
            IsSiteAdmin = isSiteAdmin;
        }
    }
}
