//using Microsoft.Online.SharePoint.TenantAdministration;
//using Microsoft.SharePoint.Client;
//using NovaPointLibrary.Commands.Authentication;
//using NovaPointLibrary.Solutions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.NetworkInformation;
//using System.Text;
//using System.Threading.Tasks;

//namespace NovaPointLibrary.Commands.SharePoint.Site
//{
//    internal class SetSPOSiteCollectionAdmin
//    {
//        private readonly NPLogger _logger;
//        private readonly Authentication.AppInfo _appInfo;
//        private readonly string AccessToken;

//        internal SetSPOSiteCollectionAdmin(NPLogger logger, Authentication.AppInfo appInfo, string accessToken)
//        {
//            _logger = logger;
//            _appInfo = appInfo;
//            AccessToken = accessToken;
//        }

//        internal void CSOM(string userAdmin, string siteUrl)
//        {
//            _appInfo.IsCancelled();
//            string methodName = $"{GetType().Name}.CSOM";
//            _logger.LogTxt(methodName, $"Start setting '{userAdmin}' as Site Admin for '{siteUrl}'");

//            using var clientContext = new ClientContext(_appInfo.AdminUrl);
//            clientContext.ExecutingWebRequest += (sender, e) =>
//            {
//                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
//            };
//            var tenant = new Tenant(clientContext);


//            if (string.IsNullOrWhiteSpace(userAdmin))
//            {
//                throw new Exception("Admin UPN cannot be empty");
//            }
//            else
//            {
//                try
//                {
//                    _appInfo.IsCancelled();
//                    _logger.LogTxt(methodName, $"Using Tenant context");
//                    tenant.SetSiteAdmin(siteUrl, userAdmin, true);
//                    tenant.Context.ExecuteQueryRetry();
//                }
//                catch (Exception)
//                {
//                    _appInfo.IsCancelled();
//                    _logger.LogTxt(methodName, "Using Tenant context failed");
//                    _logger.LogTxt(methodName, "Using Site context");

//                    using var site = tenant.Context.Clone(siteUrl);
//                    var user = site.Web.EnsureUser(userAdmin);
//                    user.IsSiteAdmin = true;
//                    user.Update();
//                    site.Load(user);
//                    site.ExecuteQueryRetry();
//                }
//                _logger.LogTxt(methodName, $"Finish setting '{userAdmin}' as Site Admin for '{siteUrl}'");
//            }
//        }
//    }
//}
