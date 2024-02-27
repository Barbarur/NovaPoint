//using Microsoft.SharePoint.Client;
//using NovaPointLibrary.Solutions;
//using PnP.Framework.Provisioning.Providers.Xml.V202103;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using System.Threading.Tasks;

//namespace NovaPointLibrary.Commands.User
//{
//    internal class GetUser
//    {
//        private readonly NPLogger _logger;
//        private readonly string AccessToken;
//        internal GetUser(NPLogger logger, string accessToken)
//        {
//            _logger = logger;
//            AccessToken = accessToken;
//        }

//        // Reference:
//        // https://github.com/pnp/powershell/blob/dev/src/Commands/Base/PipeBinds/UserPipeBind.cs
//        internal Microsoft.SharePoint.Client.User? CsomSingle(string siteUrl, string userUPN)
//        {
//            _logger.AddLogToTxt($"Start obtaining User '{userUPN}' from Site '{siteUrl}'");

//            using var clientContext = new ClientContext(siteUrl);
//            clientContext.ExecutingWebRequest += (sender, e) =>
//            {
//                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
//            };

//            var retrievalExpressions = new Expression<Func<Microsoft.SharePoint.Client.User, object>>[]
//            {
//                u => u.Id,
//                u => u.Title,
//                u => u.LoginName,
//                u => u.UserPrincipalName,
//                u => u.Email,
//                u => u.IsShareByEmailGuestUser,
//                u => u.IsSiteAdmin,
//                u => u.UserId,
//                u => u.IsHiddenInUI,
//                u => u.PrincipalType,
//                u => u.Alerts.Include(
//                    a => a.Title,
//                    a => a.Status),
//                u => u.Groups.Include(
//                    g => g.Id,
//                    g => g.Title,
//                    g => g.LoginName)
//            };

//            string userLoginName = "i:0#.f|membership|" + userUPN;
//            _logger.AddLogToTxt($"User LoginName '{userLoginName}'");

//            try
//            {

//                Microsoft.SharePoint.Client.User user = clientContext.Web.SiteUsers.GetByLoginName(userLoginName);

//                clientContext.Load(user, retrievalExpressions);
//                clientContext.ExecuteQueryRetry();

//                _logger.AddLogToTxt($"User '{userUPN}' found in Site '{siteUrl}'");
//                return user;
            
//            }
//            catch
//            {
//                _logger.AddLogToTxt($"User '{userUPN}' no found in Site '{siteUrl}'");
//                return null;
//            }

//        }
//    }
//}
