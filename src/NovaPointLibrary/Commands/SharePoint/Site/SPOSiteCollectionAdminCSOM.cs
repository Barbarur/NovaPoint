using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOSiteCollectionAdminCSOM
    {
        private Main _main;

        internal SPOSiteCollectionAdminCSOM(Main main)
        {
            _main = main;
        }

        internal async Task Set(string siteUrl, string userAdmin)
        {
            await Process(siteUrl, userAdmin, true);
        }


        internal async Task Remove(string siteUrl, string userAdmin)
        {
            await Process(siteUrl, userAdmin, false);
        }

        internal async Task Process(string siteUrl, string userAdmin, bool isSiteAdmin)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.Set";
            _main.AddLogToTxt(methodName, $"Start processing '{siteUrl}' setting '{userAdmin}' IsSiteAdmin '{isSiteAdmin}'");


            try
            {
                _main.AddLogToTxt(methodName, "Using Tenant context");
                var tenantContext = new Tenant(await _main.GetContext(_main._adminUrl));
                tenantContext.SetSiteAdmin(siteUrl, userAdmin, isSiteAdmin);
                tenantContext.Context.ExecuteQueryRetry();
            }
            catch (Exception ex)
            {
                _main.AddLogToTxt(methodName, "Using Tenant context failed");
                _main.AddLogToTxt(methodName, ex.Message);
                _main.AddLogToTxt(methodName, "Using Site context");

                var siteContext = await _main.GetContext(siteUrl);
                var user = siteContext.Web.EnsureUser(userAdmin);
                user.IsSiteAdmin = isSiteAdmin;
                user.Update();
                siteContext.Load(user);
                siteContext.ExecuteQueryRetry();
            }
            _main.AddLogToTxt(methodName, $"Finish processing '{siteUrl}' setting '{userAdmin}' IsSiteAdmin '{isSiteAdmin}'");
        }
    }
}
