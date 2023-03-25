using NovaPointLibrary.Solutions.Reports;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.User;

namespace NovaPointLibrary
{
    public class GeneralTester
    {
        // Baic parameters required for all reports
        private LogHelper _LogHelper;
        private readonly Commands.Authentication.AppInfo _appInfo;

        public GeneralTester(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo)
        {
            // Baic parameters required for all reports
            _LogHelper = new(uiAddLog, "Reports", GetType().Name);
            _appInfo = appInfo;
        }

        public async Task GetUser(string siteUrl, string UserUPN)
        {
            _LogHelper.ScriptStartNotice();

            string rootUrl = siteUrl.Substring(0, siteUrl.IndexOf(".com") + 4);
            string rootSiteAccessToken = await new GetAccessToken(_LogHelper, _appInfo).SpoInteractiveNoTenatIdAsync(rootUrl);

            User? user = new GetUser(_LogHelper, rootSiteAccessToken).CsomSingle(siteUrl, UserUPN);

            if (user != null)
            {
                _LogHelper.AddLogToTxt("USER FOUND!!!");
                _LogHelper.AddLogToTxt($"{user.LoginName}");
            }
            else
            {
                _LogHelper.AddLogToTxt("USER NO FOUND");
            }

        }
    }
}
