﻿using AngleSharp.Css.Dom;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.User;
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

        private RemoveUserAutoParameters _param;
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
            _param.SiteAccParam.SiteParam.IncludeSubsites = false;

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

            await foreach (var siteResults in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsyncNEW())
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

            StringBuilder sb = new();

            await foreach (var oUser in new SPOSiteUserCSOM(_logger, _appInfo).GetAsync(siteUrl, _param.UserParam, _userRetrievalExpressions))
            {
                _appInfo.IsCancelled();

                try
                {
                    if (oUser.IsSiteAdmin) { await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(siteUrl, oUser.UserPrincipalName); }
                    await new SPOSiteUserCSOM(_logger, _appInfo).RemoveAsync(siteUrl, oUser);
                    sb.Append($"{oUser.Title}: {oUser.UserPrincipalName} ");
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Site", siteUrl, ex);
                    AddRecord(siteUrl, $"Error while removing user {oUser.Email}: {ex.Message}");
                }

            }

            AddRecord(siteUrl, $"Deleted users: {sb}");

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

    public class RemoveUserAutoParameters : ISolutionParameters
    {
        public SPOSiteUserParameters UserParam {  get; set; }
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam {  get; set; }

        public RemoveUserAutoParameters(SPOSiteUserParameters userParam,
                                        SPOTenantSiteUrlsWithAccessParameters siteParam)
        {
            UserParam = userParam;
            SiteAccParam = siteParam;
        }
    }
}
