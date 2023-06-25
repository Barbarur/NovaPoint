using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SetSPOSiteCollectionAdmin
    {
        private readonly LogHelper _logHelper;
        private readonly Authentication.AppInfo _appInfo;
        private readonly string AccessToken;

        internal SetSPOSiteCollectionAdmin(LogHelper logHelper, Authentication.AppInfo appInfo, string accessToken)
        {
            _logHelper = logHelper;
            _appInfo = appInfo;
            AccessToken = accessToken;
        }

        internal void CSOM(string userAdmin, string siteUrl)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.CSOM";
            _logHelper.AddLogToTxt(methodName, $"Start setting '{userAdmin}' as Site Admin for '{siteUrl}'");

            //string adminUrl = "https://" + Domain + "-admin.sharepoint.com";
            using var clientContext = new ClientContext(_appInfo._adminUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };
            var tenant = new Tenant(clientContext);


            if (string.IsNullOrWhiteSpace(userAdmin))
            {
                throw new Exception("Admin UPN cannot be empty");
            }
            else
            {
                try
                {
                    _appInfo.IsCancelled();
                    _logHelper.AddLogToTxt(methodName, $"Using Tenant context");
                    tenant.SetSiteAdmin(siteUrl, userAdmin, true);
                    tenant.Context.ExecuteQueryRetry();
                }
                catch (Exception)
                {
                    _appInfo.IsCancelled();
                    _logHelper.AddLogToTxt(methodName, "Using Tenant context failed");
                    _logHelper.AddLogToTxt(methodName, "Using Site context");

                    using var site = tenant.Context.Clone(siteUrl);
                    var user = site.Web.EnsureUser(userAdmin);
                    user.IsSiteAdmin = true;
                    user.Update();
                    site.Load(user);
                    site.ExecuteQueryRetry();
                }
                _logHelper.AddLogToTxt(methodName, $"Finish setting '{userAdmin}' as Site Admin for '{siteUrl}'");
            }
        }
    }
}
