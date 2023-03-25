using Microsoft.Identity.Client;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security;
using NovaPointLibrary.Solutions;

namespace NovaPoint.Commands.Site
{
    internal class SetSiteCollectionAdmin
    {
        private LogHelper _logHelper;
        private readonly string AccessToken;
        private readonly string Domain;

        internal SetSiteCollectionAdmin(LogHelper logHelper, string accessToken, string domain)
        {
            _logHelper = logHelper;
            AccessToken = accessToken;
            Domain = domain;
        }

        internal void Add(string userAdmin, string siteUrl)
        {
            Run(userAdmin, siteUrl, true);
            return;
        }

        internal void Remove(string userAdmin, string siteUrl)
        {
            Run(userAdmin, siteUrl, false);
            return;
        }

        private void Run(string userAdmin, string siteUrl, bool isSiteAdmin)
        {
            _logHelper = new(_logHelper, $"{GetType().Name}.Run");

            _logHelper.AddLogToTxt($"Setting '{userAdmin}' IsSiteAdmin '{isSiteAdmin}' for '{siteUrl}'");

            string adminUrl = "https://" + Domain + "-admin.sharepoint.com";
            using var clientContext = new ClientContext(adminUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };
            var tenant = new Tenant(clientContext);


            if (string.IsNullOrEmpty(userAdmin))
            {
                throw new ArgumentNullException("Admin UPN cannot be empty");
            }
            else
            {
                try
                {
                    _logHelper.AddLogToTxt("Using Tenant context");
                    tenant.SetSiteAdmin(siteUrl, userAdmin, isSiteAdmin);
                    tenant.Context.ExecuteQueryRetry();
                }
                catch (Exception)
                {
                    _logHelper.AddLogToTxt("Using Tenant context failed");
                    _logHelper.AddLogToTxt("Using Site context");

                    using var site = tenant.Context.Clone(siteUrl);
                    var user = site.Web.EnsureUser(userAdmin);
                    user.IsSiteAdmin = isSiteAdmin;
                    user.Update();
                    site.Load(user);
                    site.ExecuteQueryRetry();
                }
                _logHelper.AddLogToTxt($"Setting '{userAdmin}' IsSiteAdmin '{isSiteAdmin}' for '{siteUrl}' COMPLETED");
            }
        }
    }
}
