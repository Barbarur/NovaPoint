using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;

namespace NovaPointLibrary.Commands.SharePoint.SiteGroup
{
    internal class SPOSiteGroupCSOM
    {
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        internal SPOSiteGroupCSOM(NPLogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<IEnumerable<Group>> GetAsync(string siteUrl)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting all SharePoint Group from site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            var groups = clientContext.LoadQuery(clientContext.Web.SiteGroups.IncludeWithDefaultProperties(g => g.Users, g => g.Title, g => g.OwnerTitle, g => g.Owner.LoginName, g => g.LoginName));
            clientContext.ExecuteQueryRetry();

            return groups;
        }

        internal async Task<Group> GetAsync(string siteUrl, int groupId)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting SharePoint Group with ID '{groupId}' from site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            var group = clientContext.Web.SiteGroups.GetById(groupId);
            clientContext.Load(group);
            clientContext.ExecuteQueryRetry();

            return group;

        }

        internal async Task RemoveAsync(string siteUrl, Group group)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Removing SharePoint Group '{group.Title}' from site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            clientContext.Web.SiteGroups.RemoveById(group.Id);
            clientContext.ExecuteQueryRetry();
        }
    }
}
