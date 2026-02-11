using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Directory;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using PnP.Framework;
using System.Dynamic;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveSiteAuto : ISolution
    {
        public static readonly string s_SolutionName = "Delete Site Collections and Subsites";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveSiteAuto";

        private ContextSolution _ctx;
        private RemoveSiteAutoParameters _param;

        private readonly Expression<Func<Web, object>>[] _siteExpressions = new Expression<Func<Web, object>>[]
        {
            w => w.Id,
            w => w.Title,
            w => w.Url,
        };

        private RemoveSiteAuto(ContextSolution context, RemoveSiteAutoParameters parameters)
        {
            _ctx = context;
            _param = parameters;
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new RemoveSiteAuto(context, (RemoveSiteAutoParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();


            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_ctx.Logger, _ctx.AppClient, _param.SiteAccParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();

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
                    _ctx.Logger.Error(GetType().Name, "Site", siteResults.SiteUrl, ex);

                    AddRecord(siteResults.SiteUrl, ex.Message);
                }
            }
        }

        private async Task ProcessSite(string siteUrl)
        {
            _ctx.AppClient.IsCancelled();

            await RemoveSubsites(siteUrl);

            Web web = await new SPOWebCSOM(_ctx.Logger, _ctx.AppClient).GetAsync(siteUrl, _siteExpressions);

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
                var oSite = await new SPOSiteCSOM(_ctx.Logger, _ctx.AppClient).GetAsync(siteUrl, expressions);

                if (oSite.IsHubSite)
                {
                    var tenant = new Tenant(await _ctx.AppClient.GetContext(_ctx.AppClient.AdminUrl));
                    tenant.UnregisterHubSiteById(oSite.HubSiteId);
                    tenant.Context.ExecuteQueryRetry();
                    AddRecord(siteUrl, "Unresgitered Site Colleciton as Hub");
                }
                
                if (oSite.GroupId.ToString() != "00000000-0000-0000-0000-000000000000")
                {
                    await new DirectoryGroup(_ctx.Logger, _ctx.AppClient).RemoveGroupAsync(oSite.GroupId.ToString());
                    AddRecord(siteUrl, "Deleted Microsoft365 group. Site Collection will be deleted by the system automatically");
                }
                else
                {
                    Tenant tenant = new(await _ctx.AppClient.GetContext(_ctx.AppClient.AdminUrl));
                    tenant.DeleteSiteCollection(siteUrl, true, TimeoutFunction);
                    AddRecord(siteUrl, "Deleted Site Collection");
                }
            }
        }

        private async Task RemoveSubsites(string siteUrl)
        {
            _ctx.AppClient.IsCancelled();

            List<Web> collSubsites = await new SPOSubsiteCSOM(_ctx.Logger, _ctx.AppClient).GetAsync(siteUrl);
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
                    return _ctx.AppClient.CancelToken.IsCancellationRequested;

                case TenantOperationMessage.RemovingDeletedSiteCollectionFromRecycleBin:
                    return _ctx.AppClient.CancelToken.IsCancellationRequested;
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

            _ctx.Logger.DynamicCSV(recordItem);
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
