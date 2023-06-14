using Microsoft.Online.SharePoint.TenantAdministration;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.User
{
    internal class GetSPOGroup
    {
        private LogHelper _LogHelper;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;
        internal GetSPOGroup(LogHelper logHelper, AppInfo appInfo, string accessToken)
        {
            _LogHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }
    }
}
