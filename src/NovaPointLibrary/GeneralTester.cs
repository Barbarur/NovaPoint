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
using NovaPointLibrary.Commands.Site;
using System.Diagnostics.Metrics;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Solutions.Report;

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
            _LogHelper = new(uiAddLog, "Reports", GetType().Name);
            _appInfo = appInfo;
        }
    }
}
