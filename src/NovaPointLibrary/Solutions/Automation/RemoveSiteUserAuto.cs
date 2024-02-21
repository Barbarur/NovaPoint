using AngleSharp.Css.Dom;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Commands.Site;
using NovaPointLibrary.Commands.User;
using NovaPointLibrary.Solutions.Report;
using PnP.Core.Model.SharePoint;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveSiteUserAuto
    {
        public static readonly string s_SolutionName = "Remove user from Site";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveSiteUserAuto";

        private RemoveUserAutoParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (RemoveUserAutoParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        private Expression<Func<User, object>>[] _userRetrievalExpressions = new Expression<Func<User, object>>[]
        {
            u => u.Email,
            u => u.IsSiteAdmin,
            u => u.LoginName,
            u => u.UserPrincipalName,
        };

        public RemoveSiteUserAuto(RemoveUserAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            Parameters = parameters;
            _param.IncludeSubsites = false;

            _logger = new(uiAddLog, this.GetType().Name, parameters);
            _appInfo = new(_logger, cancelTokenSource);
        }

        public async Task RunAsync()
        {
            try
            {
                await RunScriptAsync();

                _logger.ScriptFinish();
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param).GetAsyncNEW())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(siteResults.ErrorMessage))
                {
                    AddRecord(siteResults.SiteUrl, remarks: siteResults.ErrorMessage);
                    continue;
                }

                try
                {
                    await RemoveSiteUserAsync(siteResults.SiteUrl, siteResults.Progress);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", siteResults.SiteUrl, ex);
                    AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                }
            }
        }

        private async Task RemoveSiteUserAsync(string siteUrl, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();

            ProgressTracker progress = new(parentProgress, 0);

            if (_param.AllUsers)
            {
                var collUsers = await new SPOSiteUserCSOM(_logger, _appInfo).GetAsync(siteUrl, _userRetrievalExpressions);

                if (collUsers != null)
                {
                    AddRecord(siteUrl, $"Deletion of all users triggered.");

                    progress.IncreaseTotalCount(collUsers.Count);
                    foreach (User oUser in collUsers)
                    {
                        await RemoveSiteUserAsync(siteUrl, oUser);

                        progress.ProgressUpdateReport();
                    }
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(_param.TargetUserUPN))
            {
                User? oUser = await new SPOSiteUserCSOM(_logger, _appInfo).GetByEmailAsync(siteUrl, _param.TargetUserUPN, _userRetrievalExpressions);

                if (oUser != null)
                {
                    progress.IncreaseTotalCount(1);

                    AddRecord(siteUrl, $"User {_param.TargetUserUPN} found. Deletion triggered.");
                    
                    await RemoveSiteUserAsync(siteUrl, oUser);

                    progress.ProgressUpdateReport();
                }
            }
            if (_param.IncludeExternalUsers)
            {
                var collExtUsers = await new SPOSiteUserCSOM(_logger, _appInfo).GetEXTAsync(siteUrl, _userRetrievalExpressions);

                if (collExtUsers == null) { return; }

                AddRecord(siteUrl, $"External users found. Deletion triggered.");

                progress.IncreaseTotalCount(collExtUsers.Count);
                foreach (var oUser in collExtUsers)
                {
                    await RemoveSiteUserAsync(siteUrl, oUser);
                    
                    progress.ProgressUpdateReport();
                }
            }
            if (_param.IncludeEveryone)
            {
                
                User? oUser = await new SPOSiteUserCSOM(_logger, _appInfo).GetEveryoneAsync(siteUrl, _userRetrievalExpressions);

                if (oUser != null)
                {
                    progress.IncreaseTotalCount(1);

                    AddRecord(siteUrl, $"'Everyone' group found. Deletion triggered.");

                    await RemoveSiteUserAsync(siteUrl, oUser);

                    progress.ProgressUpdateReport();
                }
            }
            if (_param.IncludeEveryoneExceptExternal)
            {
                User? oUser = await new SPOSiteUserCSOM(_logger, _appInfo).GetEveryoneExceptExternalUsersAsync(siteUrl, _userRetrievalExpressions);

                if (oUser != null)
                {
                    progress.IncreaseTotalCount(1);

                    AddRecord(siteUrl, $"'Everyone except external users' group found. Deletion triggered.");

                    await RemoveSiteUserAsync(siteUrl, oUser);

                    progress.ProgressUpdateReport();
                }
            }
        }

        private async Task RemoveSiteUserAsync(string siteUrl, User oUser)
        {
            _appInfo.IsCancelled();

            try
            {
                if (oUser.IsSiteAdmin) { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(siteUrl, oUser.UserPrincipalName); }
                await new SPOSiteUserCSOM(_logger, _appInfo).RemoveAsync(siteUrl, oUser);
            }
            catch (Exception ex)
            {
                _logger.ReportError("Site", siteUrl, ex);
                AddRecord(siteUrl, $"Error while removing user {oUser.Email}: {ex.Message}");
            }
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

    public class RemoveUserAutoParameters : SPOTenantSiteUrlsParameters, ISolutionParameters
    {
        public bool AllUsers { get; set; } = true;
        public string TargetUserUPN { get; set; } = string.Empty;
        public bool IncludeExternalUsers { get; set; } = false;
        public bool IncludeEveryone { get; set; } = false;
        public bool IncludeEveryoneExceptExternal { get; set; } = false;
    }
}
