using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;
using System.Dynamic;

namespace NovaPointLibrary.Solutions.Automation
{
    public class SetSiteCollectionAdminAuto : ISolution
    {
        public static readonly string s_SolutionName = "Add or Remove user as Admin";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-SetSiteCollectionAdminAuto";

        private ContextSolution _ctx;
        private SetSiteCollectionAdminAutoParameters _param;

        private SetSiteCollectionAdminAuto(ContextSolution context, SetSiteCollectionAdminAutoParameters parameters)
        {
            _ctx = context;
            _param = parameters;
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new SetSiteCollectionAdminAuto(context, (SetSiteCollectionAdminAutoParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            GraphUser signedInUser = await new GetAADUser(_ctx.Logger, _ctx.AppClient).GetUserAsync(_param.TargetUserUPN);
            _param.TargetUserUPN = signedInUser.UserPrincipalName;

            await foreach (var recordSite in new SPOTenantSiteUrlsCSOM(_ctx.Logger, _ctx.AppClient, _param.SiteParam).GetAsync())
            {
                await SetAdmin(recordSite.SiteUrl);
            }

        }

        private async Task SetAdmin(string siteUrl)
        {
            _ctx.AppClient.IsCancelled();

            try
            {
                if (_param.IsSiteAdmin)
                {
                    await new SPOSiteCollectionAdminCSOM(_ctx.Logger, _ctx.AppClient).AddAsync(siteUrl, _param.TargetUserUPN);
                    AddRecord(siteUrl, $"User '{_param.TargetUserUPN}' added as Site Collection Admin");
                }
                else
                {
                    await new SPOSiteCollectionAdminCSOM(_ctx.Logger, _ctx.AppClient).RemoveAsync(siteUrl, _param.TargetUserUPN);
                    AddRecord(siteUrl, $"User '{_param.TargetUserUPN}' removed as Site Collection Admin");
                }
            }
            catch (Exception ex)
            {
                _ctx.Logger.Error(GetType().Name, "Site", siteUrl, ex);
                AddRecord(siteUrl, ex.Message);
            }

        }

        private void AddRecord(string siteUrl, string remarks)
        {
            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;

            recordItem.Remarks = remarks;

            _ctx.Logger.DynamicCSV(recordItem);
        }
    }

    public class SetSiteCollectionAdminAutoParameters : ISolutionParameters
    {
        public string TargetUserUPN { get; set; } = string.Empty;

        public bool IsSiteAdmin { get; set; } = false;

        public SPOTenantSiteUrlsParameters SiteParam { get; set; }
        public SetSiteCollectionAdminAutoParameters(SPOTenantSiteUrlsParameters siteParam)
        {
            SiteParam = siteParam;
        }

        public void ParametersCheck()
        {
            if (String.IsNullOrWhiteSpace(TargetUserUPN))
            {
                throw new Exception($"User Principal Name cannot be empty");
            }
        }
    }
}
