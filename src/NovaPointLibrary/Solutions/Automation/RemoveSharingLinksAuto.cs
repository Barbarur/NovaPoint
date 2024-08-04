using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Sharing;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Permision.Utilities;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.SiteGroup;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Solutions.Report;
using PnP.Core.Model.SharePoint;
using PnP.Framework.Provisioning.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveSharingLinksAuto
    {
        public static readonly string s_SolutionName = "Remove Sharing Links";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveSharingLinksAuto";

        private RemoveSharingLinksAutoParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private readonly Expression<Func<Web, object>>[] _siteExpressions = new Expression<Func<Web, object>>[]
        {
            w => w.HasUniqueRoleAssignments,
            w => w.Id,
            w => w.RoleAssignments.Include(
                ra => ra.RoleDefinitionBindings,
                ra => ra.Member),
            w => w.Title,
            w => w.Url,
        };

        private RemoveSharingLinksAuto(NPLogger logger, Commands.Authentication.AppInfo appInfo, RemoveSharingLinksAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(RemoveSharingLinksAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            NPLogger logger = new(uiAddLog, "RemoveSharingLinksAuto", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new RemoveSharingLinksAuto(logger, appInfo, parameters).RunScriptAsync();

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

            await foreach (var siteRecord in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (siteRecord.Ex != null)
                {
                    RemoveSharingLinksAutoRecord record = new(siteRecord);
                    _logger.RecordCSV(record);
                    continue;
                }

                try
                {
                    await ProcessSite(siteRecord);
                }
                catch (Exception ex)
                {
                    RemoveSharingLinksAutoRecord record = new(siteRecord, ex.Message);
                    _logger.RecordCSV(record);
                    _logger.ReportError(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                }

            }
        }

        private async Task ProcessSite(SPOTenantSiteUrlsRecord siteRecord)
        {
            _appInfo.IsCancelled();

            var collGroups = await new SPOSiteGroupCSOM(_logger, _appInfo).GetAsync(siteRecord.SiteUrl);

            ProgressTracker progress = new(siteRecord.Progress, collGroups.Count());
            foreach (var group in collGroups)
            {
                if (group.Title.Contains("SharingLinks"))
                {
                    try
                    {
                        if (!_param.ReportMode)
                        {
                            await new SPOSiteGroupCSOM(_logger, _appInfo).RemoveAsync(siteRecord.SiteUrl, group);
                        }

                        RemoveSharingLinksAutoRecord record = new(siteRecord, "Removed");
                        record.AddDetails(group);
                        _logger.RecordCSV(record);
                    }
                    catch (Exception ex)
                    {
                        RemoveSharingLinksAutoRecord record = new(siteRecord, ex.Message);
                        _logger.RecordCSV(record);
                        _logger.ReportError(GetType().Name, "Sharing Link", $"{group.Id}", ex);
                    }
                }
                progress.ProgressUpdateReport();
            }

        }
    }

    public class RemoveSharingLinksAutoRecord : ISolutionRecord
    {
        internal string SiteUrl { get; set; } = String.Empty;

        internal string GroupID { get; set; } = String.Empty;
        internal string GroupTitle { get; set; } = String.Empty;
        internal string GroupDescription { get; set; } = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

        internal RemoveSharingLinksAutoRecord(SPOTenantSiteUrlsRecord siteRecord,
                                             string remarks = "")
        {
            SiteUrl = siteRecord.SiteUrl;
            if ( siteRecord.Ex != null) { Remarks = siteRecord.Ex.Message; }
            else { Remarks = remarks; }
        }

        internal void AddDetails(Group groupSharedLink)
        {
            GroupID = groupSharedLink.Id.ToString();
            GroupTitle = groupSharedLink.Title;
            GroupDescription = groupSharedLink.Description;
        }

    }

    public class RemoveSharingLinksAutoParameters : ISolutionParameters
    {
        public bool ReportMode { get; set; } = true;

        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam { get; set; }
        public RemoveSharingLinksAutoParameters(bool reportMode,
                                               SPOTenantSiteUrlsWithAccessParameters siteParam)
        {
            ReportMode = reportMode;
            SiteAccParam = siteParam;
        }
    }
}
