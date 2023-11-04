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
    // TO BE DEPRECATED WHEN SPOSiteCollectionAdminCSOM IS STABLE
    internal class SetSiteCollectionAdmin
    {
        private readonly NPLogger _logger;
        private readonly string AccessToken;
        private readonly string Domain;

        internal SetSiteCollectionAdmin(NPLogger logger, string accessToken, string domain)
        {
            _logger = logger;
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

            _logger.AddLogToTxt($"Setting '{userAdmin}' IsSiteAdmin '{isSiteAdmin}' for '{siteUrl}'");

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
                    _logger.AddLogToTxt("Using Tenant context");
                    tenant.SetSiteAdmin(siteUrl, userAdmin, isSiteAdmin);
                    tenant.Context.ExecuteQueryRetry();
                }
                catch (Exception)
                {
                    _logger.AddLogToTxt("Using Tenant context failed");
                    _logger.AddLogToTxt("Using Site context");

                    using var site = tenant.Context.Clone(siteUrl);
                    var user = site.Web.EnsureUser(userAdmin);
                    user.IsSiteAdmin = isSiteAdmin;
                    user.Update();
                    site.Load(user);
                    site.ExecuteQueryRetry();
                }
                _logger.AddLogToTxt($"Setting '{userAdmin}' IsSiteAdmin '{isSiteAdmin}' for '{siteUrl}' COMPLETED");
            }
        }
    }
}
