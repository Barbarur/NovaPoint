using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD.Groups;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Logging;
using PnP.Framework;
using System.Dynamic;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveSiteAuto
    {
        public static readonly string s_SolutionName = "Delete Site Collections and Subsites";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveSiteAuto";

        private RemoveSiteAutoParameters _param;
        private readonly LoggerSolution _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private RemoveSiteAuto(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, RemoveSiteAutoParameters parameters)
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
            LoggerSolution logger = new(uiAddLog, "RemoveSiteAuto", parameters);

            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new RemoveSiteAuto(logger, appInfo, parameters).RunScriptAsync();

                logger.SolutionFinish();

            }
            catch (Exception ex)
            {
                logger.SolutionFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();


            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
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
                    _logger.Error(GetType().Name, "Site", siteResults.SiteUrl, ex);

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
        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }

        public RemoveSiteAutoParameters(string listOfSitesPath)
        {
            AdminAccess = new()
            {
                AddAdmin = true,
                RemoveAdmin = false,
            };

            SiteParam = new()
            {
                ListOfSitesPath = listOfSitesPath,
            };
        }
    }
}
