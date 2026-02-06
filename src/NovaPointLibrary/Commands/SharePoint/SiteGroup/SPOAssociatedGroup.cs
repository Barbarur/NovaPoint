using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;


namespace NovaPointLibrary.Commands.SharePoint.SiteGroup
{
    internal class SPOAssociatedGroup
    {
        private readonly LoggerSolution _logger;
        private readonly IAppClient _appInfo;

        internal SPOAssociatedGroup(LoggerSolution logger, IAppClient appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        private readonly Expression<Func<Microsoft.SharePoint.Client.User, object>>[] _userRetrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
        {
            u => u.AadObjectId,
            u => u.Email,
            u => u.Id,
            u => u.IsSiteAdmin,
            u => u.LoginName,
            u => u.PrincipalType,
            u => u.Title,
            u => u.UserId,
            u => u.UserPrincipalName,
        };

        internal async Task<IEnumerable<Group>> GetAsync(string siteUrl)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting all SharePoint Group from site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            var groups = clientContext.LoadQuery(clientContext.Web.SiteGroups.IncludeWithDefaultProperties(g => g.Users, g => g.Title, g => g.OwnerTitle, g => g.Owner.LoginName, g => g.LoginName));
            clientContext.ExecuteQueryRetry();

            return groups;
        }

        internal async Task<Group> GetSiteOwnersAsync(string siteUrl)
        {
            _appInfo.IsCancelled();

            _logger.Debug(GetType().Name, $"Getting Site Owners from '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            clientContext.Load(clientContext.Web.AssociatedOwnerGroup);
            clientContext.Load(clientContext.Web.AssociatedOwnerGroup.Users, u => u.Include(_userRetrievalExpressions));
            clientContext.ExecuteQueryRetry();

            return clientContext.Web.AssociatedOwnerGroup;
        }

        internal async Task<Group> GetSiteMembersAsync(string siteUrl)
        {
            _appInfo.IsCancelled();

            _logger.Debug(GetType().Name, $"Getting Site Members from '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            clientContext.Load(clientContext.Web.AssociatedMemberGroup);
            clientContext.Load(clientContext.Web.AssociatedMemberGroup.Users, u => u.Include(_userRetrievalExpressions));
            clientContext.ExecuteQueryRetry();

            return clientContext.Web.AssociatedMemberGroup;
        }

        internal async Task<Group> GetSiteVisitorsAsync(string siteUrl)
        {
            _appInfo.IsCancelled();

            _logger.Debug(GetType().Name, $"Getting Site Visitors from '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            clientContext.Load(clientContext.Web.AssociatedVisitorGroup);
            clientContext.Load(clientContext.Web.AssociatedVisitorGroup.Users, u => u.Include(_userRetrievalExpressions));
            clientContext.ExecuteQueryRetry();

            return clientContext.Web.AssociatedVisitorGroup;
        }
    }
}
