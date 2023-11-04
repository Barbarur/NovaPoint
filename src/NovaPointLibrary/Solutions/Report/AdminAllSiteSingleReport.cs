using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Site;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Reports
{
    // TO BE DEPRECATED ONCE SiteAllReport IS IN PRODUCTION
    public class AdminAllSiteSingleReport
    {
        internal LogHelper _logHelper;
        internal AppInfo AppInfo;
        internal string SiteUrl;
        public AdminAllSiteSingleReport(Action<LogInfo> uiAddLog, AppInfo appInfo, AdminAllSiteSingleReportParameters parameters)
        {
            _logHelper = new(uiAddLog, "Reports", GetType().Name);
            AppInfo = appInfo;
            SiteUrl = parameters.SiteUrl;
        }
        public async Task RunAsync()
        {
            try
            {
                if (String.IsNullOrEmpty(SiteUrl) || String.IsNullOrWhiteSpace(SiteUrl))
                {
                    string message = "FORM INCOMPLETED: Site URL cannot be empty if you need to obtain the Site Collection Administrators";
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
                _logHelper.ScriptFinishErrorNotice(ex);
            }
        }
        private async Task RunScriptAsync()
        {
            _logHelper.ScriptStartNotice();

            string rootUrl = SiteUrl.Substring(0, SiteUrl.IndexOf(".com") + 4);
            string rootAccessToken = await new GetAccessToken(_logHelper, AppInfo).SpoInteractiveAsync(rootUrl);

            if (this.AppInfo.CancelToken.IsCancellationRequested) { this.AppInfo.CancelToken.ThrowIfCancellationRequested(); };
            var collSiteCollAdmins = new GetSiteCollectionAdmin(_logHelper, rootAccessToken).Csom(SiteUrl);

            foreach (User oAdmin in collSiteCollAdmins)
            {
                if (this.AppInfo.CancelToken.IsCancellationRequested) { this.AppInfo.CancelToken.ThrowIfCancellationRequested(); };

                dynamic recordAdmin = new ExpandoObject();
                recordAdmin.SiteUrl = SiteUrl;
                recordAdmin.Title = oAdmin.Title;
                recordAdmin.Email = oAdmin.Email;
                recordAdmin.PrincipalType = oAdmin.PrincipalType;

                string userId = oAdmin.UserId != null ? oAdmin.UserId.NameId : "";
                recordAdmin.SPOUserID = userId;

                _logHelper.AddRecordToCSV(recordAdmin);

            }

            _logHelper.ScriptFinishSuccessfulNotice();

        }

    }
    public class AdminAllSiteSingleReportParameters
    {
        internal string SiteUrl;
        public AdminAllSiteSingleReportParameters(string siteUrl)
        {
            SiteUrl = siteUrl;
        }
    }
}
