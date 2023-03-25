using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Authentication
{
    public class AppInfo
    {
        internal string _domain = string.Empty;
        internal string Domain
        {
            init
            {
                _domain = value;
                _adminUrl = "https://" + value + "-admin.sharepoint.com";
                _rootPersonalUrl = "https://" + value + "-my.sharepoint.com";
                _rootSharedUrl = "https://" + value + ".sharepoint.com";
            }
        }
        internal string _adminUrl = string.Empty;
        internal string _rootPersonalUrl = string.Empty;
        internal string _rootSharedUrl = string.Empty;

        internal string _tenantId = string.Empty;
        // Added PnPManagementShellClientId as default ID
        internal string _clientId = "31359c7f-bd7e-475c-86db-fdb8c937548e";
        internal bool _cachingToken = false;

        public AppInfo(string domain, string tenantId, string clientId, bool cachingToken)
        {
            Domain = domain;
            _tenantId = tenantId;
            _clientId = clientId;
            _cachingToken = cachingToken;
        }

        public static void RemoveTokenCache()
        {
            TokenCacheHelper.RemoveCache();
        }
        
    }
}
