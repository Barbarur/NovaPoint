using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using PnP.Core.Model.SharePoint;
using PnP.Framework;
using PnP.Framework.Extensions;
using PnP.Framework.Provisioning.Model;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveSiteAuto
    {
        public static readonly string s_SolutionName = "Delete Site Collections and Subsites";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveSiteAuto";

        private SPOTenantSiteUrlsWithAccessParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private RemoveSiteAuto(NPLogger logger, Commands.Authentication.AppInfo appInfo, SPOTenantSiteUrlsWithAccessParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        private readonly Expression<Func<Web, object>>[] _siteExpressions = new Expression<Func<Web, object>>[]
        {
            w => w.Id,
            w => w.Title,
            w => w.Url,
        };

        public static async Task RunAsync(RemoveSiteAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            SPOTenantSiteUrlsWithAccessParameters param = new();
            param.SiteParam.SiteUrl = String.Empty;
            param.SiteParam.AllSiteCollections = false;
            param.SiteParam.ListOfSitesPath = parameters.ListOfSitesPath;
            param.AdminAccess.AddAdmin = true;
            param.AdminAccess.RemoveAdmin = false;

            NPLogger logger = new(uiAddLog, "RemoveSiteAuto", param);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new RemoveSiteAuto(logger, appInfo, param).RunScriptAsync();

                logger.ScriptFinish();

            }
            catch (Exception ex)
            {
                logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();


            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param).GetAsync())
            {
                _appInfo.IsCancelled();

                if ( siteResults.Ex != null )
                {
                    AddRecord(siteResults.SiteUrl, remarks: siteResults.Ex.Message);
                    continue;
                }

                try
                {
                    await ProcessSite(siteResults.SiteUrl);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(GetType().Name, "Site", siteResults.SiteUrl, ex);

                    AddRecord(siteResults.SiteUrl, ex.Message);
                }
            }
        }

        private async Task ProcessSite(string siteUrl)
        {
            _appInfo.IsCancelled();

            await RemoveSubsites(siteUrl);

            Web web = await new SPOWebCSOM(_logger, _appInfo).GetAsync(siteUrl, _siteExpressions);

            if (web.IsSubSite())
            {
                web.DeleteObject();
                web.Context.ExecuteQueryRetry();
                AddRecord(siteUrl, "Deleted Subsite");
            }
            else
            {
                var expressions = new Expression<Func<Microsoft.SharePoint.Client.Site, object>>[]
                {
                    s => s.GroupId,
                    s => s.IsHubSite,
                    s => s.HubSiteId,
                };
                var oSite = await new SPOSiteCSOM(_logger, _appInfo).GetAsync(siteUrl, expressions);

                if (oSite.IsHubSite)
                {
                    var tenant = new Tenant(await _appInfo.GetContext(_appInfo.AdminUrl));
                    tenant.UnregisterHubSiteById(oSite.HubSiteId);
                    tenant.Context.ExecuteQueryRetry();
                    AddRecord(siteUrl, "Unresgitered Site Colleciton as Hub");
                }
                
                if (oSite.GroupId.ToString() != "00000000-0000-0000-0000-000000000000")
                {
                    await new AADGroup(_logger, _appInfo).RemoveGroupAsync(oSite.GroupId.ToString());
                    AddRecord(siteUrl, "Deleted Microsoft365 group. Site Collection will be deleted by the system automatically");
                }
                else
                {
                    Tenant tenant = new(await _appInfo.GetContext(_appInfo.AdminUrl));
                    tenant.DeleteSiteCollection(siteUrl, true, TimeoutFunction);
                    AddRecord(siteUrl, "Deleted Site Collection");
                }
            }
        }

        private async Task RemoveSubsites(string siteUrl)
        {
            _appInfo.IsCancelled();

            List<Web> collSubsites = await new SPOSubsiteCSOM(_logger, _appInfo).GetAsync(siteUrl);
            collSubsites = collSubsites.OrderByDescending( w => w.Url).ToList();
            foreach (var oSubsite in collSubsites)
            {
                oSubsite.DeleteObject();
                oSubsite.Context.ExecuteQueryRetry();
            }

        }

        private bool TimeoutFunction(TenantOperationMessage message)
        {
            switch (message)
            {
                case TenantOperationMessage.DeletingSiteCollection:
                    return _appInfo.CancelToken.IsCancellationRequested;

                case TenantOperationMessage.RemovingDeletedSiteCollectionFromRecycleBin:
                    return _appInfo.CancelToken.IsCancellationRequested;
                default:
                    break;
            }
            return false;
        }

        private void AddRecord(string siteUrl,
                               string remarks = "")
        {
            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;

            recordItem.Remarks = remarks;

            _logger.DynamicCSV(recordItem);
        }
    }

    public class RemoveSiteAutoParameters : ISolutionParameters
    {
        public string ListOfSitesPath { get; set; }

        public RemoveSiteAutoParameters(string listOfSitesPath)
        {
            ListOfSitesPath = listOfSitesPath;
        }
    }
}
