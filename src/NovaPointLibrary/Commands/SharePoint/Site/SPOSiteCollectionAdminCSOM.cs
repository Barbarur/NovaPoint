using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOSiteCollectionAdminCSOM
    {
        //private Main _main;
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        //internal SPOSiteCollectionAdminCSOM(Main main)
        //{
        //    _main = main;
        //}

        internal SPOSiteCollectionAdminCSOM(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        //internal async Task SetDEPRECATED(string siteUrl, string userAdmin)
        //{
        //    await ProcessDEPRECATED(siteUrl, userAdmin, true);
        //}


        //internal async Task RemoveDEPRECATED(string siteUrl, string userAdmin)
        //{
        //    await ProcessDEPRECATED(siteUrl, userAdmin, false);
        //}

        //internal async Task ProcessDEPRECATED(string siteUrl, string userAdmin, bool isSiteAdmin)
        //{
        //    _main.IsCancelled();
        //    string methodName = $"{GetType().Name}.Set";
        //    _main.AddLogToTxt(methodName, $"Start processing '{siteUrl}' setting '{userAdmin}' IsSiteAdmin '{isSiteAdmin}'");


        //    try
        //    {
        //        _main.AddLogToTxt(methodName, "Using Tenant context");
        //        var tenantContext = new Tenant(await _main.GetContext(_main._adminUrl));
        //        tenantContext.SetSiteAdmin(siteUrl, userAdmin, isSiteAdmin);
        //        tenantContext.Context.ExecuteQueryRetry();
        //    }
        //    catch (Exception ex)
        //    {
        //        _main.AddLogToTxt(methodName, "Using Tenant context failed");
        //        _main.AddLogToTxt(methodName, ex.Message);
        //        _main.AddLogToTxt(methodName, "Using Site context");

        //        var siteContext = await _main.GetContext(siteUrl);
        //        var user = siteContext.Web.EnsureUser(userAdmin);
        //        user.IsSiteAdmin = isSiteAdmin;
        //        user.Update();
        //        siteContext.Load(user);
        //        siteContext.ExecuteQueryRetry();
        //    }
        //    _main.AddLogToTxt(methodName, $"Finish processing '{siteUrl}' setting '{userAdmin}' IsSiteAdmin '{isSiteAdmin}'");
        //}


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
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.Set";
            _logger.LogTxt(methodName, $"Start processing '{siteUrl}' setting '{userAdmin}' IsSiteAdmin '{isSiteAdmin}'");


            try
            {
                _logger.LogTxt(methodName, "Using Tenant context");
                var tenantContext = new Tenant(await _appInfo.GetContext(_appInfo.AdminUrl));
                tenantContext.SetSiteAdmin(siteUrl, userAdmin, isSiteAdmin);
                tenantContext.Context.ExecuteQueryRetry();
            }
            catch (Exception ex)
            {
                _logger.LogTxt(methodName, "Using Tenant context failed");
                _logger.LogTxt(methodName, ex.Message);
                _logger.LogTxt(methodName, "Using Site context");

                var siteContext = await _appInfo.GetContext(siteUrl);
                var user = siteContext.Web.EnsureUser(userAdmin);
                user.IsSiteAdmin = isSiteAdmin;
                user.Update();
                siteContext.Load(user);
                siteContext.ExecuteQueryRetry();
            }
            _logger.LogTxt(methodName, $"Finish processing '{siteUrl}' setting '{userAdmin}' IsSiteAdmin '{isSiteAdmin}'");
        }
    }
}
