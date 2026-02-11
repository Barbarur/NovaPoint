using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.SharePoint.User;
using NovaPointLibrary.Core.Context;
using System.Dynamic;
using System.Linq.Expressions;
using System.Text;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveSiteUserAuto : ISolution
    {
        public static readonly string s_SolutionName = "Remove user from Site";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveSiteUserAuto";

        private ContextSolution _ctx;
        private RemoveUserAutoParameters _param;

        private Expression<Func<User, object>>[] _userRetrievalExpressions = new Expression<Func<User, object>>[]
        {
            u => u.Email,
            u => u.IsSiteAdmin,
            u => u.LoginName,
            u => u.UserPrincipalName,
        };

        private RemoveSiteUserAuto(ContextSolution context, RemoveUserAutoParameters parameters)
        {
            _ctx = context;
            _param = parameters;
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new RemoveSiteUserAuto(context, (RemoveUserAutoParameters)parameters);
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
                    await RemoveSiteUserAsync(siteResults.SiteUrl, siteResults.Progress);
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Site", siteResults.SiteUrl, ex);
                    AddRecord(siteResults.SiteUrl, remarks: ex.Message);
                }
            }
        }

        private async Task RemoveSiteUserAsync(string siteUrl, ProgressTracker parentProgress)
        {
            _ctx.AppClient.IsCancelled();

            StringBuilder sb = new();

            await foreach (var oUser in new SPOSiteUserCSOM(_ctx.Logger, _ctx.AppClient).GetAsync(siteUrl, _param.UserParam, _userRetrievalExpressions))
            {
                _ctx.AppClient.IsCancelled();

                try
                {
                    if (oUser.IsSiteAdmin) { await new SPOSiteCollectionAdminCSOM(_ctx.Logger, _ctx.AppClient).RemoveAsync(siteUrl, oUser.UserPrincipalName); }
                    await new SPOSiteUserCSOM(_ctx.Logger, _ctx.AppClient).RemoveAsync(siteUrl, oUser);
                    sb.Append($"{oUser.Title}: {oUser.UserPrincipalName} ");
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Site", siteUrl, ex);
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

            _ctx.Logger.DynamicCSV(recordItem);
        }
    }

    public class RemoveUserAutoParameters : ISolutionParameters
    {
        public SPOSiteUserParameters UserParam {  get; set; }
        internal readonly SPOAdminAccessParameters AdminAccess;
        internal readonly SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }

        public RemoveUserAutoParameters(SPOSiteUserParameters userParam,
                                        SPOAdminAccessParameters adminAccess,
                                        SPOTenantSiteUrlsParameters siteParam)
        {
            UserParam = userParam;
            AdminAccess = adminAccess;
            SiteParam = siteParam;
            SiteParam.IncludeSubsites = false;
        }
    }
}
