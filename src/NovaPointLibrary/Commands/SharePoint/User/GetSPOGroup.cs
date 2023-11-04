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
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;
        private readonly string AccessToken;
        internal GetSPOGroup(NPLogger logger, AppInfo appInfo, string accessToken)
        {
            _logger = logger;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }
    }
}
