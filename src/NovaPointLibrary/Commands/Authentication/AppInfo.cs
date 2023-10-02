
namespace NovaPointLibrary.Commands.Authentication
{
    public class AppInfo
    {
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
        internal string _domain { get; set; } = string.Empty;
        internal string _adminUrl { get; set; } = string.Empty;
        internal string _rootPersonalUrl { get; set; } = string.Empty;
        internal string _rootSharedUrl { get; set; } = string.Empty;
        

        internal string _tenantId = string.Empty;
        // Added PnPManagementShellClientId as default ID
        internal string _clientId = "31359c7f-bd7e-475c-86db-fdb8c937548e";
        internal bool _cachingToken = false;

        public CancellationTokenSource CancelTokenSource { get; init; }
        public CancellationToken CancelToken { get; init; }

        public AppInfo(string domain, string tenantId, string clientId, bool cachingToken)
        {
            Domain = domain;
            _tenantId = tenantId;
            _clientId = clientId;
            _cachingToken = cachingToken;

            this.CancelTokenSource = new();
            this.CancelToken = CancelTokenSource.Token;
        }
        public void IsCancelled()
        {
            if ( CancelToken.IsCancellationRequested ) { CancelToken.ThrowIfCancellationRequested(); }
        }
        public static void RemoveTokenCache()
        {
            TokenCacheHelper.RemoveCache();
        }

    }
}
