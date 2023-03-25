﻿using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.User;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Site
{
    internal class RemoveSiteCollectionAdmin
    {
        private LogHelper _logHelper;
        private readonly string AccessToken;
        private readonly string Domain;

        internal RemoveSiteCollectionAdmin(LogHelper logHelper, string accessToken, string domain)
        {
            _logHelper = logHelper;
            AccessToken = accessToken;
            Domain = domain;
        }

        internal void Csom(string siteUrl, string targetAdminUPN)
        {
            _logHelper = new(_logHelper, $"{GetType().Name}.Csom");

            _logHelper.AddLogToTxt($"Removing '{targetAdminUPN}' as Site Collection Admin for '{siteUrl}'");

            string adminUrl = "https://" + Domain + "-admin.sharepoint.com";
            using var clientContext = new ClientContext(adminUrl);
            clientContext.ExecutingWebRequest += (sender, e) =>
            {
                e.WebRequestExecutor.RequestHeaders["Authorization"] = "Bearer " + AccessToken;
            };

            var tenant = new Tenant(clientContext);

            try
            {
                try
                {
                    _logHelper.AddLogToTxt("Using Tenant context");
                    tenant.SetSiteAdmin(siteUrl, targetAdminUPN, false);
                    tenant.Context.ExecuteQueryRetry();
                }
                catch
                {
                    _logHelper.AddLogToTxt("Using Tenant context failed");
                    _logHelper.AddLogToTxt("Using Site context");

                    using var site = tenant.Context.Clone(siteUrl);
                    var user = site.Web.EnsureUser(targetAdminUPN);
                    user.IsSiteAdmin = false;
                    user.Update();
                    site.Load(user);
                    site.ExecuteQueryRetry();
                }
            }
            catch
            {

                _logHelper.AddLogToTxt($"You cannot remove '{targetAdminUPN}' from the site collection administrators of Site collection '{siteUrl}'");
                    
                string message = $"You cannot remove '{targetAdminUPN}' from the site collection administrators list.";
                Exception exception = new(message);
                throw exception;
                
            }
        }
    }
}
