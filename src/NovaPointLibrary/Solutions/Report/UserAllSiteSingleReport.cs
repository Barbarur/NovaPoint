using Microsoft.Graph;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.List;
using NovaPointLibrary.Commands.Site;
using NovaPointLibrary.Commands.User;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Reports
{
    public class UserAllSiteSingleReport
    {
        public static readonly string s_SolutionName = "Users in a single Site report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-UserAllSiteSingleReport";

        // Baic parameters required for all reports
        private readonly LogHelper _logHelper;
        private readonly Commands.Authentication.AppInfo AppInfo;
        // Required parameters for the current report
        private readonly string SiteUrl;

        public UserAllSiteSingleReport(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo, UserAllSiteSingleReportParameters parameters)
        {
            _logHelper = new(uiAddLog, "Reports", GetType().Name); ;
            AppInfo = appInfo;
            SiteUrl = parameters.SiteUrl;
        }

        public async Task RunAsync()
        {
            try
            {
                if (String.IsNullOrEmpty(SiteUrl) || String.IsNullOrWhiteSpace(SiteUrl))
                {
                    string message = "FORM INCOMPLETED: Site URL cannot be empty if you need to obtain the Site Users";
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
            string rootSiteAccessToken = await new GetAccessToken(_logHelper, AppInfo).SpoInteractiveAsync(rootUrl);
            
            if (AppInfo.CancelToken.IsCancellationRequested) { AppInfo.CancelToken.ThrowIfCancellationRequested(); };
            List<Microsoft.SharePoint.Client.User> collUsers = new GetUser(_logHelper, rootSiteAccessToken).CsomAll(SiteUrl, false);
            foreach (Microsoft.SharePoint.Client.User oUser in collUsers)
            {
                if (AppInfo.CancelToken.IsCancellationRequested) { AppInfo.CancelToken.ThrowIfCancellationRequested(); };

                dynamic recordUser = new ExpandoObject();
                recordUser.SiteUrl = SiteUrl;
                recordUser.Title = oUser.Title;
                recordUser.UserPrincipalName = oUser.UserPrincipalName;
                recordUser.Email = oUser.Email;
                recordUser.IsSiteAdmin = oUser.IsSiteAdmin;
                recordUser.UserSPOID = GetUserId(oUser.UserId);

                _logHelper.AddRecordToCSV(recordUser);

            }

            _logHelper.ScriptFinishSuccessfulNotice();
        }
        private static string GetUserId(UserIdInfo userIdInfo)
        {
            if (userIdInfo == null) { return ""; }
            else { return userIdInfo.NameId.ToString(); }
        }
    }

    public class UserAllSiteSingleReportParameters
    {
        
        // Required parameters for the current report
        internal string SiteUrl;

        public UserAllSiteSingleReportParameters(string siteUrl)
        {
            SiteUrl = siteUrl;
        }
    }
}
