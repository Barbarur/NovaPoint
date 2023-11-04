using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using PnP.Framework.Provisioning.Providers.Xml.V202103;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.User
{
    internal class GetUser
    {
        private readonly NPLogger _logger;
        private readonly string AccessToken;
        internal GetUser(NPLogger logger, string accessToken)
        {
            _logger = logger;
            AccessToken = accessToken;
        }

        // Reference:
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Base/PipeBinds/UserPipeBind.cs
        internal Microsoft.SharePoint.Client.User? CsomSingle(string siteUrl, string userUPN)
        {
            _logger.AddLogToTxt($"Start obtaining User '{userUPN}' from Site '{siteUrl}'");

            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
            {
                u => u.Id,
                u => u.Title,
                u => u.LoginName,
                u => u.UserPrincipalName,
                u => u.Email,
                u => u.IsShareByEmailGuestUser,
                u => u.IsSiteAdmin,
                u => u.UserId,
                u => u.IsHiddenInUI,
                u => u.PrincipalType,
                u => u.Alerts.Include(
                    a => a.Title,
                    a => a.Status),
                u => u.Groups.Include(
                    g => g.Id,
                    g => g.Title,
                    g => g.LoginName)
            };

            string userLoginName = "i:0#.f|membership|" + userUPN;
            _logger.AddLogToTxt($"User LoginName '{userLoginName}'");

            try
            {

                Microsoft.SharePoint.Client.User user = clientContext.Web.SiteUsers.GetByLoginName(userLoginName);

                clientContext.Load(user, retrievalExpressions);
                clientContext.ExecuteQueryRetry();

                _logger.AddLogToTxt($"User '{userUPN}' found in Site '{siteUrl}'");
                return user;
            
            }
            catch
            {
                _logger.AddLogToTxt($"User '{userUPN}' no found in Site '{siteUrl}'");
                return null;
            }

        }


        // Reference:
        // https://pnp.github.io/powershell/cmdlets/Get-PnPUser.html
        // https://github.com/pnp/powershell/blob/dev/src/Commands/Principals/GetUser.cs
        // https://www.sharepointdiary.com/2017/02/sharepoint-online-get-all-users-using-powershell.html
        internal List<Microsoft.SharePoint.Client.User> CsomAll(string siteUrl, bool WithRightsAssigned = false, bool WithRightsAssignedDetailed = false)
        {
            //WriteWarning("Using the -WithRightsAssignedDetailed parameter will cause the script to take longer than normal because of the all enumerations that take place");
            _logger.AddLogToTxt($"Start obtaining Users for '{siteUrl}'");
            using var clientContext = new ClientContext(siteUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
            {
                u => u.Id,
                u => u.Title,
                u => u.LoginName,
                u => u.UserPrincipalName,
                u => u.Email,
                u => u.IsShareByEmailGuestUser,
                u => u.IsSiteAdmin,
                u => u.UserId,
                u => u.IsHiddenInUI,
                u => u.PrincipalType,
                u => u.Alerts.Include(
                    a => a.Title,
                    a => a.Status),
                u => u.Groups.Include(
                    g => g.Id,
                    g => g.Title,
                    g => g.LoginName)
            };

            UserCollection collUsers = clientContext.Web.SiteUsers;
            clientContext.Load(collUsers, u => u.Include(retrievalExpressions));

            List<DetailedUser> users = new();
            List<Microsoft.SharePoint.Client.User> listUsersReturned = new();

            // To be reviewed
            if (WithRightsAssigned || WithRightsAssignedDetailed)
            {
                // Get all the role assignments and role definition bindings to be able to see which users have been given rights directly on the site level
                clientContext.Load(clientContext.Web.RoleAssignments, ac => ac.Include(a => a.RoleDefinitionBindings, a => a.Member));
                var usersWithDirectPermissions = clientContext.Web.SiteUsers.Where(u => clientContext.Web.RoleAssignments.Any(ra => ra.Member.LoginName == u.LoginName));

                // Get all the users contained in SharePoint Groups
                clientContext.Load(clientContext.Web.SiteGroups, sg => sg.Include(u => u.Users.Include(retrievalExpressions), u => u.LoginName));
                clientContext.ExecuteQueryRetry();

                // Get all SharePoint groups that have been assigned access
                var usersWithGroupPermissions = new List<Microsoft.SharePoint.Client.User>();
                foreach (var group in clientContext.Web.SiteGroups.Where(g => clientContext.Web.RoleAssignments.Any(ra => ra.Member.LoginName == g.LoginName)))
                {
                    usersWithGroupPermissions.AddRange(group.Users);
                }

                // Merge the users with rights directly on the site level and those assigned rights through SharePoint Groups
                List<Microsoft.SharePoint.Client.User> allUsersWithPermissions = new(usersWithDirectPermissions.Count() + usersWithGroupPermissions.Count());
                allUsersWithPermissions.AddRange(usersWithDirectPermissions);
                allUsersWithPermissions.AddRange(usersWithGroupPermissions);

                // Add the found users and add them to the custom object
                if (WithRightsAssignedDetailed)
                {
                    clientContext.Load(clientContext.Web, s => s.ServerRelativeUrl);
                    clientContext.ExecuteQueryRetry();

                    users.AddRange(GetPermissions(clientContext.Web.RoleAssignments, clientContext.Web.ServerRelativeUrl));
                    foreach (var user in allUsersWithPermissions)
                    {
                        users.Add(new DetailedUser()
                        {
                            Groups = user.Groups,
                            User = user,
                            Url = clientContext.Web.ServerRelativeUrl
                        });
                    }
                    _logger.AddLogToTxt($"Successfully obtained Users for '{siteUrl}'");
                    _logger.AddLogToTxt($"Currently 'WithRightsAssignedDetailed' is not supported");

                    // Section to be changed once 'WithRightsAssignedDetailed' is included
                    // Filter out the users that have been given rights at both places so they will only be returned once
                    listUsersReturned.AddRange(allUsersWithPermissions.GroupBy(u => u.Id).Select(u => u.First()));

                    listUsersReturned.RemoveAll(u => u.Title == "System Account" || u.Title == "SharePoint App" || u.Title == "NT Service\\spsearch");
                    return listUsersReturned;
                }
                else
                {
                    _logger.AddLogToTxt($"Successfully obtained Users for '{siteUrl}'");
                    
                    // Filter out the users that have been given rights at both places so they will only be returned once
                    listUsersReturned.AddRange(allUsersWithPermissions.GroupBy(u => u.Id).Select(u => u.First()));

                    listUsersReturned.RemoveAll(u => u.Title == "System Account" || u.Title == "SharePoint App" || u.Title == "NT Service\\spsearch");
                    return listUsersReturned;
                }


            }
            else
            {
                
                clientContext.ExecuteQuery();

                _logger.AddLogToTxt($"Successfully obtained Users for '{siteUrl}'");
                listUsersReturned.AddRange(clientContext.Web.SiteUsers);
                listUsersReturned.RemoveAll(u => u.Title == "System Account" || u.Title == "SharePoint App" || u.Title == "NT Service\\spsearch");
                return listUsersReturned;
            
            }
        }

        public class DetailedUser
        {
            public Microsoft.SharePoint.Client.User? User { get; set; }
            public string? Url { get; set; }
            public List<string> Permissions { get; set; }
            public GroupCollection Groups { get; set; }
        }

        private static List<DetailedUser> GetPermissions(RoleAssignmentCollection roleAssignments, string url)
        {
            List<DetailedUser> users = new List<DetailedUser>();
            foreach (var roleAssignment in roleAssignments)
            {
                if (roleAssignment.Member.PrincipalType == Microsoft.SharePoint.Client.Utilities.PrincipalType.User)
                {
                    var detailedUser = new DetailedUser();
                    detailedUser.Url = url;
                    detailedUser.User = roleAssignment.Member as Microsoft.SharePoint.Client.User;
                    detailedUser.Permissions = new List<string>();

                    foreach (var roleDefinition in roleAssignment.RoleDefinitionBindings)
                    {
                        if (roleDefinition.Name == "Limited Access")
                            continue;

                        detailedUser.Permissions.Add(roleDefinition.Name);
                    }

                    // if no permissions are recorded (hence, limited access, skip the adding of the permissions)
                    if (detailedUser.Permissions.Count == 0)
                        continue;

                    users.Add(detailedUser);
                }
            }
            return users;
        }
    }
}
