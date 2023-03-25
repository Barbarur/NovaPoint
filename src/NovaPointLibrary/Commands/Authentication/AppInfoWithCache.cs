using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Authentication
{
    // Not in use yet. Unders testing
    internal class AppInfoWithCache
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
        internal string _clientId = string.Empty;
        internal bool _cachingToken = false;

        public AppInfoWithCache(string domain, string tenantId, string clientId, bool cachingToken)
        {
            Domain = domain;
            _tenantId = tenantId;
            _clientId = clientId;
            _cachingToken = cachingToken;
        }
    }
}
