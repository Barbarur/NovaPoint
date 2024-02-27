using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.User
{
    internal class SPOSiteUserCSOM
    {
        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        private readonly Expression<Func<Microsoft.SharePoint.Client.User, object>>[] _retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
        {
            u => u.Id,
            u => u.Title,
            u => u.LoginName,
            u => u.UserPrincipalName,
            u => u.Email,
            u => u.UserId,
        };

        internal SPOSiteUserCSOM(NPLogger logger, AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<Microsoft.SharePoint.Client.User?> GetAsync(string siteUrl, string userUPN)
        {
            _appInfo.IsCancelled();

            string userLoginName = "i:0#.f|membership|" + userUPN;

            _logger.LogTxt(GetType().Name, $"Getting '{userUPN}', LoginName '{userLoginName}' from Site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            try
            {
                Microsoft.SharePoint.Client.User user = clientContext.Web.SiteUsers.GetByLoginName(userLoginName);

                clientContext.Load(user, _retrievalExpressions);
                clientContext.ExecuteQueryRetry();

                return user;
            }
            catch
            {
                _logger.LogTxt(GetType().Name, $"User '{userUPN}' no found in Site '{siteUrl}'");
                return null;
            }
        }

        internal async Task<Microsoft.SharePoint.Client.User?> GetByEmailAsync(string siteUrl,
                                                                               string userEmail,
                                                                               Expression<Func<Microsoft.SharePoint.Client.User, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();

            _logger.LogTxt(GetType().Name, $"Getting user with email '{userEmail}' from Site '{siteUrl}'");

            var expressions = _retrievalExpressions.Union(retrievalExpressions).ToArray();

            var clientContext = await _appInfo.GetContext(siteUrl);

            try
            {
                Microsoft.SharePoint.Client.User user = clientContext.Web.SiteUsers.GetByEmail(userEmail);

                clientContext.Load(user, expressions);
                clientContext.ExecuteQueryRetry();

                return user;
            }
            catch
            {
                _logger.LogTxt(GetType().Name, $"User with email '{userEmail}' no found in Site '{siteUrl}'");
                return null;
            }
        }

        internal async Task<UserCollection?> GetAsync(string siteUrl,
                                                      Expression<Func<Microsoft.SharePoint.Client.User, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();

            _logger.LogTxt(GetType().Name, $"Getting all users from Site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            var expressions = _retrievalExpressions.Union(retrievalExpressions).ToArray();

            try
            {
                UserCollection collUsers = clientContext.Web.SiteUsers;

                clientContext.Load(collUsers, u => u.Include(retrievalExpressions));
                clientContext.ExecuteQueryRetry();

                return collUsers;
            }
            catch
            {
                _logger.LogTxt(GetType().Name, $"No users found in this Site");
                return null;
            }
        }

        internal async Task<List<Microsoft.SharePoint.Client.User>?> GetEXTAsync(string siteUrl,
                                                                                 Expression<Func<Microsoft.SharePoint.Client.User, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();

            _logger.LogTxt(GetType().Name, $"Getting all EXT users from Site '{siteUrl}'");

            var collUsers = await GetAsync(siteUrl, retrievalExpressions);

            List<Microsoft.SharePoint.Client.User> collExtUsers = new() { };
            if ( collUsers != null)
            {
                foreach (var oUser in collUsers)
                {
                    if (oUser.LoginName.Contains("#ext#", StringComparison.OrdinalIgnoreCase) || oUser.LoginName.Contains("urn:spo:guest", StringComparison.OrdinalIgnoreCase) ) { collExtUsers.Add(oUser); }
                }
                return collExtUsers;
            }
            else
            {
                return null;
            }
        }

        internal async Task<Microsoft.SharePoint.Client.User?> GetEveryoneAsync(string siteUrl,
                                                                                Expression<Func<Microsoft.SharePoint.Client.User, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting 'Everyone' group from Site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            var expressions = _retrievalExpressions.Union(retrievalExpressions).ToArray();

            try
            {
                Microsoft.SharePoint.Client.User user = clientContext.Web.SiteUsers.GetByLoginName("c:0(.s|true");

                clientContext.Load(user, expressions);
                clientContext.ExecuteQueryRetry();

                return user;
            }
            catch
            {
                _logger.LogTxt(GetType().Name, $"'Everyone' group no found in Site '{siteUrl}'");
                return null;
            }
        }

        internal async Task<Microsoft.SharePoint.Client.User?> GetEveryoneExceptExternalUsersAsync(string siteUrl,
                                                                                                   Expression<Func<Microsoft.SharePoint.Client.User, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Getting 'Everyone except external users' group from Site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            var expressions = _retrievalExpressions.Union(retrievalExpressions).ToArray();

            try
            {
                Microsoft.SharePoint.Client.User user = clientContext.Web.SiteUsers.GetByLoginName($"c:0-.f|rolemanager|spo-grid-all-users/{_appInfo._tenantId}");

                clientContext.Load(user, expressions);
                clientContext.ExecuteQueryRetry();

                return user;
            }
            catch
            {
                _logger.LogTxt(GetType().Name, $"'Everyone except external users' group no found in Site '{siteUrl}'");
                return null;
            }
        }


        internal async IAsyncEnumerable<Microsoft.SharePoint.Client.User> GetAsync(string siteUrl,
                                                                                   SPOSiteUserParameters parameters,
                                                                                   Expression<Func<Microsoft.SharePoint.Client.User, object>>[] retrievalExpressions)
        {
            _appInfo.IsCancelled();

            var expressions = _retrievalExpressions.Union(retrievalExpressions).ToArray();


            if (parameters.AllUsers)
            {
                var collUsers = await GetAsync(siteUrl, expressions);

                if (collUsers != null)
                {
                    foreach (Microsoft.SharePoint.Client.User oUser in collUsers)
                    {
                        yield return oUser;
                    }
                }

                yield break;
            }

            if (!string.IsNullOrWhiteSpace(parameters.IncludeUserUPN))
            {
                var oUser = await GetByEmailAsync(siteUrl, parameters.IncludeUserUPN, expressions);

                if (oUser != null) { yield return oUser; }
            }
            if (parameters.IncludeExternalUsers)
            {
                var collExtUsers = await GetEXTAsync(siteUrl, expressions);

                if (collExtUsers != null)
                {
                    foreach (Microsoft.SharePoint.Client.User oUser in collExtUsers)
                    {
                        yield return oUser;
                    }
                }
            }
            if (parameters.IncludeEveryone)
            {
                var oUser = await GetEveryoneAsync(siteUrl, expressions);

                if (oUser != null) { yield return oUser; }
            }
            if (parameters.IncludeEveryoneExceptExternal)
            {
                var oUser = await GetEveryoneExceptExternalUsersAsync(siteUrl, expressions);

                if (oUser != null) { yield return oUser; }
            }
        }

        internal async Task Register(string siteUrl, string userUPN)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start registering '{userUPN}' from Site '{siteUrl}'");

            var clientContext = await _appInfo.GetContext(siteUrl);

            Microsoft.SharePoint.Client.User user = clientContext.Web.EnsureUser(userUPN);
            user.Update();
            clientContext.Load(user);
            clientContext.ExecuteQueryRetry();
        }

        internal async Task RemoveAsync(string siteUrl, Microsoft.SharePoint.Client.User oUser)
        {
            _appInfo.IsCancelled();

            _logger.LogTxt(GetType().Name, $"Removing user with LoginName '{oUser.LoginName}' from Site '{siteUrl}'");

            var siteContext = await _appInfo.GetContext(siteUrl);

            siteContext.Web.SiteUsers.RemoveByLoginName(oUser.LoginName);
            siteContext.ExecuteQueryRetry();
        }
    }
}
