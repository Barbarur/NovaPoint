﻿using NovaPointLibrary.Solutions.Reports;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.User;
using NovaPointLibrary.Commands.Site;
using System.Diagnostics.Metrics;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Solutions.Report;
using NovaPointLibrary.Solutions.QuickFix;
using NovaPointLibrary.Commands.SharePoint.Site;

namespace NovaPointLibrary
{
    public class GeneralTester
    {
        // Baic parameters required for all reports
        private LogHelper _LogHelper;
        private readonly AppInfo _appInfo;

        public GeneralTester(Action<LogInfo> uiAddLog, AppInfo appInfo)
        {
            // Baic parameters required for all reports
            _LogHelper = new(uiAddLog, "Test", GetType().Name);
            _appInfo = appInfo;
        }

        public async Task AddAdmin(string userUpn, string siteUrl, string adminUpn)
        {

            string spoAdminAccessToken = await new GetAccessToken(_LogHelper, _appInfo).SpoInteractiveAsync(_appInfo._adminUrl);

            new SetSPOSiteCollectionAdmin(_LogHelper, _appInfo, spoAdminAccessToken).CSOM(adminUpn, siteUrl);

            new SetSPOSiteCollectionAdmin(_LogHelper, _appInfo, spoAdminAccessToken).CSOM(adminUpn, siteUrl);

            new SetSPOSiteCollectionAdmin(_LogHelper, _appInfo, spoAdminAccessToken).CSOM(userUpn, siteUrl);
        }
    }
}
