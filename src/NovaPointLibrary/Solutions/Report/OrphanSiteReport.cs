using Microsoft.Online.SharePoint.TenantAdministration;
using NovaPointLibrary.Commands.AzureAD.User;
using NovaPointLibrary.Commands.Directory;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using System.Linq.Expressions;


namespace NovaPointLibrary.Solutions.Report
{
    public class OrphanSiteReport : ISolution
    {
        public static readonly string s_SolutionName = "Orphan sites report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-OrphanSiteReport";

        private ContextSolution _ctx;
        private OrphanSiteReportParameters _param;

        private readonly Expression<Func<SiteProperties, object>>[] _siteExpressions = new Expression<Func<SiteProperties, object>>[]
        {
            p => p.Url,
            p => p.Title,
            p => p.Template,
            p => p.IsTeamsConnected,
            p => p.Owner,
            p => p.OwnerEmail,
            p => p.OwnerLoginName,
            p => p.OwnerName,

            p => p.IsGroupOwnerSiteAdmin,
        };

        private OrphanSiteReport(ContextSolution context, OrphanSiteReportParameters parameters)
        {
            _ctx = context;
            _param = parameters;

            Dictionary<Type, string> solutionReports = new()
            {
                { typeof(OrphanSiteReportRecord), "Report" },
            };
            _ctx.DbHandler.AddSolutionReports(solutionReports);
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new OrphanSiteReport(context, (OrphanSiteReportParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            _param.SiteParam.IncludePersonalSite = false;
            _param.SiteParam.IncludeSubsites = false;

            await foreach (var siteRecord in new SPOTenantSiteUrlsCSOM(_ctx.Logger, _ctx.AppClient, _param.SiteParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();

                if (siteRecord.Ex != null)
                {
                    AddRecord(new(siteRecord.SiteUrl, siteRecord.Ex.Message));
                    continue;
                }

                try
                {
                    var siteProperties = await new SPOSiteCollectionCSOM(_ctx.Logger, _ctx.AppClient).GetAsync(siteRecord.SiteUrl, _siteExpressions);
                    
                    _ctx.Logger.Debug(GetType().Name, $"Site Template: {siteProperties.Template}");
                    if (siteProperties.Template.Contains("SPSPERS", StringComparison.OrdinalIgnoreCase)) { continue; }

                    OrphanSiteReportRecord record = new(siteProperties.Title, siteProperties.Url, SPOWeb.GetSiteTemplateName(siteProperties.Template, siteProperties.IsTeamsConnected), siteProperties.OwnerName, siteProperties.Owner, siteProperties.OwnerLoginName, siteProperties.OwnerEmail);

                    _ctx.Logger.Debug(GetType().Name, $"Owner: {siteProperties.Owner}");
                    _ctx.Logger.Debug(GetType().Name, $"OwnerEmail: {siteProperties.OwnerEmail}");
                    _ctx.Logger.Debug(GetType().Name, $"OwnerLoginName: {siteProperties.OwnerLoginName}");
                    _ctx.Logger.Debug(GetType().Name, $"OwnerName: {siteProperties.OwnerName}");
                    _ctx.Logger.Debug(GetType().Name, $"IsGroupOwnerSiteAdmin: {siteProperties.IsGroupOwnerSiteAdmin}");

                    if (string.IsNullOrWhiteSpace(siteProperties.Owner))
                    {
                        AddRecord(record.ReportUsers("Unknown", "Unknown", "No Primary Admin"));
                    }
                    else if (Guid.TryParse(siteProperties.Owner, out Guid guid) || (siteProperties.Owner.Contains("_o") && Guid.TryParse(siteProperties.Owner[..siteProperties.Owner.IndexOf("_o")], out guid)))
                    {
                        try
                        {
                            var listSecGroupUsers = await new DirectoryGroupUser(_ctx.Logger, _ctx.AppClient).GetUsersAsync(siteProperties.OwnerName, guid, true);

                            if (!listSecGroupUsers.Users.Contains("@"))
                            {
                                if (listSecGroupUsers.Remarks.Contains("ResourceNotFound")) { AddRecord(record.ReportUsers($"{listSecGroupUsers.AccountType}", $"Group", "Deleted Group", $"{listSecGroupUsers.Remarks}")); }

                                if (!string.IsNullOrWhiteSpace(listSecGroupUsers.Remarks)) { AddRecord(record.ReportUsers($"{listSecGroupUsers.AccountType}", $"Group", "Unknown", $"{listSecGroupUsers.Remarks}")); }

                                else { AddRecord(record.ReportUsers($"{listSecGroupUsers.AccountType}", $"Group", "Empty Group", $"{listSecGroupUsers.Remarks}")); }
                            }
                        }
                        catch (Exception ex)
                        {
                            AddRecord(record.ReportUsers($"{siteProperties.OwnerName}", $"Group", "Unknown", $"{ex.Message}"));
                        }
                    }
                    else if(siteProperties.Owner.Contains("@"))
                    {
                        try
                        {
                            var user = await new AADUser(_ctx.Logger, _ctx.AppClient).GetUserAsync(siteProperties.Owner, "accountEnabled,displayName,mail");
                            if (!user.AccountEnabled)
                            {
                                AddRecord(record.ReportUsers($"{siteProperties.OwnerName}", "User", "Blocked sign-in"));
                            }
                        }
                        catch
                        {
                            AddRecord(record.ReportUsers($"{siteProperties.OwnerName}", "User", "Deleted Account"));
                        }
                    }
                    else
                    {
                        AddRecord(record.ReportUsers("Unknown", "Unknown", "Unknown"));
                    }

                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Site", siteRecord.SiteUrl, ex);
                    AddRecord(new(siteRecord.SiteUrl, ex.Message));
                }
            }
        }

        private void AddRecord(OrphanSiteReportRecord record)
        {
            _ctx.DbHandler.WriteRecord(record);
        }
    }

    internal class OrphanSiteReportRecord : ISolutionRecord
    {
        public string SiteTitle { get; set; } = String.Empty;
        public string SiteUrl { get; set; } = String.Empty;
        public string SiteTemplate { get; set; } = String.Empty;
        public string AdminName { get; set; } = String.Empty;
        public string AdminUpnOrId { get; set; } = String.Empty;
        public string AdminLoginName { get; set; } = String.Empty;
        public string AdminEmail { get; set; } = String.Empty;
        public string AdminInfo { get; set; } = String.Empty;
        public string AccountType {  get; set; } = String.Empty;
        public string Status {  get; set; } = String.Empty;
        public string Remarks { get; set; } = String.Empty;

        public OrphanSiteReportRecord() { }

        internal OrphanSiteReportRecord(string siteUrl, string errorMessage)
        {
            SiteUrl = siteUrl;
            Remarks = errorMessage;
        }

        internal OrphanSiteReportRecord(string siteTitle, string siteUrl, string siteTemplate, string adminName, string adminUpnOrId, string adminLoginName, string adminEmail)
        {
            SiteTitle = siteTitle;
            SiteUrl = siteUrl;
            SiteTemplate = siteTemplate;
            AdminName = adminName;
            AdminUpnOrId = adminUpnOrId;
            AdminLoginName = adminLoginName;
            AdminEmail = adminEmail;
        }

        internal OrphanSiteReportRecord ReportUsers(string siteAdmins, string accountType, string status, string remarks = "")
        {
            OrphanSiteReportRecord r = new(SiteTitle, SiteUrl, SiteTemplate, AdminName, AdminUpnOrId, AdminLoginName, AdminEmail)
            {
                AdminInfo = siteAdmins,
                AccountType = accountType,
                Status = status,
                Remarks = remarks,
            };

            return r;
        }

    }

    public class OrphanSiteReportParameters : ISolutionParameters
    {
        public SPOTenantSiteUrlsParameters SiteParam { get; set; }

        public OrphanSiteReportParameters(SPOTenantSiteUrlsParameters siteParam)
        {
            SiteParam = siteParam;
        }
    }

}
