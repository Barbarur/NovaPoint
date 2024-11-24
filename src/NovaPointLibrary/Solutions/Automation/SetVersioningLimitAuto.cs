using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Admin;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Solutions.Automation
{
    public class SetVersioningLimitAuto : ISolution
    {
        public static readonly string s_SolutionName = "Set versioning limit";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-SetVersioningLimitAuto";

        private SetVersioningLimitAutoParameters _param;
        private readonly LoggerSolution _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static readonly SPOListsParameters _allLibraries = new()
        {
            AllLists = true,
            IncludeLists = false,
            IncludeLibraries = true,
        };

        private readonly SPOListsParameters _listParameters;

        private SetVersioningLimitAuto(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, SetVersioningLimitAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;

            _listParameters = new()
            {
                AllLists = parameters.VersionParam.ListApplyToAllExistingLists,
                IncludeLists = true,
                IncludeLibraries = false,
                ListTitle = parameters.VersionParam.ListApplySingleListTitle,
            };
        }

        public static async Task RunAsync(SetVersioningLimitAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            LoggerSolution logger = new(uiAddLog, "SetVersioningLimitAuto", parameters);

            try
            {

                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new SetVersioningLimitAuto(logger, appInfo, parameters).RunScriptAsync();

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

            if (_param.VersionParam.LibraryInheritTenantVersionSettings && _param.VersionParam.LibraryExistingLibraries)
            {
                await GetTenantVersionLimitsAsync();
            }

            await foreach (var tenantSiteRecord in new SPOTenantSiteUrlsWithAccessCSOM(_logger, _appInfo, _param.SiteAccParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (tenantSiteRecord.Ex != null)
                {
                    SetVersioningLimitAutoRecord record = new(tenantSiteRecord.SiteUrl, remarks: tenantSiteRecord.Ex.Message);
                    RecordCSV(record);
                    continue;
                }

                try
                {
                    var site = await new SPOSiteCSOM(_logger, _appInfo).GetAsync(tenantSiteRecord.SiteUrl);

                    if (_param.VersionParam.LibrarySetVersioningSettings)
                    {
                        if (_param.VersionParam.LibraryNewLibraries) { SetLibraryVersioningLimitsNew(site); }
                        if (_param.VersionParam.LibraryExistingLibraries) { await SetLibraryVersioningLimitsExistingAsync(site); }
                    }

                    if (_param.VersionParam.ListSetVersioningSettings)
                    {
                        await SetListVersioningLimitsAsync(site);
                    }

                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", tenantSiteRecord.SiteUrl, ex);

                    RecordCSV(new(tenantSiteRecord.SiteUrl, remarks: ex.Message));
                }
            }

        }

        private async Task GetTenantVersionLimitsAsync()
        {
            _appInfo.IsCancelled();

            _logger.Info(GetType().Name, $"Getting Tenant version history limits");

            _param.VersionParam.LibraryEnableVersioning = true;
            _param.VersionParam.LibraryMinorVersionLimit = 0;

            var tenant = await new SPOTenant(_logger, _appInfo).GetAsync();
            _param.VersionParam.LibraryAutomaticVersionLimit = tenant.EnableAutoExpirationVersionTrim;
            _param.VersionParam.LibraryMajorVersionLimit = tenant.MajorVersionLimit;
            _param.VersionParam.LibraryExpirationDays = tenant.ExpireVersionsAfterDays;

            _logger.Info(GetType().Name, $"EnableAutoExpirationVersionTrim: {tenant.EnableAutoExpirationVersionTrim}");
            _logger.Info(GetType().Name, $"MajorVersionLimit: {tenant.MajorVersionLimit}");
            _logger.Info(GetType().Name, $"ExpireVersionsAfterDays: {tenant.ExpireVersionsAfterDays}");

        }

        private void SetLibraryVersioningLimitsNew(Site site)
        {
            _logger.UI(GetType().Name, $"Setting versioning limit on new libraries for site {site.Url}.");

            try
            {
                if (_param.VersionParam.LibraryInheritTenantVersionSettings)
                {
                    site.EnsureProperty(s => s.VersionPolicyForNewLibrariesTemplate);
                    site.VersionPolicyForNewLibrariesTemplate.InheritTenantSettings();
                    site.Context.ExecuteQueryRetry();
                }
                else if (_param.VersionParam.LibraryAutomaticVersionLimit)
                {
                    site.EnsureProperty(s => s.VersionPolicyForNewLibrariesTemplate);
                    site.VersionPolicyForNewLibrariesTemplate.SetAutoExpiration();
                    site.Context.ExecuteQueryRetry();
                }
                else
                {
                    site.EnsureProperty(s => s.VersionPolicyForNewLibrariesTemplate);
                    if (_param.VersionParam.LibraryExpirationDays == 0)
                    {
                        site.VersionPolicyForNewLibrariesTemplate.SetNoExpiration(_param.VersionParam.LibraryMajorVersionLimit);
                        site.Context.ExecuteQueryRetry();
                    }
                    else
                    {
                        site.VersionPolicyForNewLibrariesTemplate.SetExpireAfter(_param.VersionParam.LibraryMajorVersionLimit, _param.VersionParam.LibraryExpirationDays);
                        site.Context.ExecuteQueryRetry();
                    }
                }
                RecordCSV(new(site.Url, "New Libraries", "Successful"));
            }
            catch (Exception ex)
            {
                RecordCSV(new(site.Url, "New Libraries", "Failed", ex.Message));
            }
        }

        private async Task SetLibraryVersioningLimitsExistingAsync(Site site)
        {
            _logger.Info(GetType().Name, $"Setting versioning limit on existing libraries for site {site.Url}.");

            List<Microsoft.SharePoint.Client.List> collList = await new SPOListCSOM(_logger, _appInfo).GetAsync(site.Url, _param.LibraryParameters);
            foreach (var oList in collList)
            {
                try
                {
                    SetLibraryVersioningLimitsAsync(oList);
                    RecordCSV(new(site.Url, $"Document Library '{oList.Title}", "Successful"));
                }
                catch (Exception ex)
                {
                    RecordCSV(new(site.Url, $"Document Library '{oList.Title}", "Failed", ex.Message));
                }
            }
        }

        private void SetLibraryVersioningLimitsAsync(List oList)
        {
            _logger.Info(GetType().Name, $"Processing Library {oList.RootFolder.ServerRelativeUrl}.");

            oList.EnableVersioning = _param.VersionParam.LibraryEnableVersioning;

            if (_param.VersionParam.LibraryEnableVersioning)
            {
                if (_param.VersionParam.LibraryAutomaticVersionLimit)
                {
                    oList.VersionPolicies.DefaultTrimMode = VersionPolicyTrimMode.AutoExpiration;
                    oList.EnableMinorVersions = false;
                }
                else
                {
                    oList.MajorVersionLimit = _param.VersionParam.LibraryMajorVersionLimit;

                    if (_param.VersionParam.LibraryMinorVersionLimit > 0)
                    {
                        oList.EnableMinorVersions = true;
                        oList.MajorWithMinorVersionsLimit = _param.VersionParam.LibraryMinorVersionLimit;
                    }
                    else
                    {
                        oList.EnableMinorVersions = false;
                    }

                    if (_param.VersionParam.LibraryExpirationDays < 1)
                    {
                        oList.VersionPolicies.DefaultTrimMode = VersionPolicyTrimMode.NoExpiration;
                    }
                    else
                    {
                        oList.VersionPolicies.DefaultTrimMode = VersionPolicyTrimMode.ExpireAfter;
                        oList.VersionPolicies.DefaultExpireAfterDays = _param.VersionParam.LibraryExpirationDays;
                    }
                }
            }

            oList.Update();
            oList.Context.ExecuteQuery();
        }

        private async Task SetListVersioningLimitsAsync(Site site)
        {
            _logger.Info(GetType().Name, $"Setting versioning limit on existing lists for site {site.Url}.");

            List<Microsoft.SharePoint.Client.List> collList = await new SPOListCSOM(_logger, _appInfo).GetAsync(site.Url, _param.ListParameters);
            foreach (var oList in collList)
            {
                try
                {
                    SetListVersioningLimits(oList);
                    RecordCSV(new(site.Url, $"List '{oList.Title}", "Successful"));
                }
                catch (Exception ex)
                {
                    RecordCSV(new(site.Url, $"List '{oList.Title}", "Failed", ex.Message));
                }
            }
        }

        private void SetListVersioningLimits(List oList)
        {
            _logger.Info(GetType().Name, $"Processing list {oList.RootFolder.ServerRelativeUrl}.");

            oList.EnableVersioning = _param.VersionParam.ListEnableVersioning;
            if (_param.VersionParam.ListEnableVersioning)
            {
                oList.MajorVersionLimit = _param.VersionParam.ListMajorVersionLimit;
                // Review how to apply minor versions only when approval is enabled.
            }

            oList.Update();
            oList.Context.ExecuteQuery();
        }

        private void RecordCSV(SetVersioningLimitAutoRecord record)
        {
            _logger.RecordCSV(record);
        }

    }

    public class SetVersioningLimitAutoRecord : ISolutionRecord
    {
        internal string SiteUrl { get; set; } = String.Empty;
        internal string TargetList { get; set; } = String.Empty;
        internal string Status { get; set; } = String.Empty;
        internal string Remarks { get; set; } = String.Empty;

        internal SetVersioningLimitAutoRecord(string siteUrl, string target = "", string status = "", string remarks = "")
        {
            SiteUrl = siteUrl;
            TargetList = target;
            Status = status;
            Remarks = remarks;
        }

    }

    public class VersioningLimitParameters : ISolutionParameters
    {
        public bool LibrarySetVersioningSettings { get; set; } = false;
        public bool LibraryNewLibraries { get; set; } = false;
        public bool LibraryExistingLibraries { get; set; } = false;
        public bool LibraryApplyToAllExistingLibraries { get; set; } = false;
        public string LibraryApplyToSingleLibraryTitle { get; set; } = String.Empty;
        public bool LibraryInheritTenantVersionSettings { get; set; } = false;
        public bool LibraryEnableVersioning { get; set; } = true;

        public bool LibraryAutomaticVersionLimit { get; set; } = false;
        public int LibraryMajorVersionLimit { get; set; } = 500;
        public int LibraryExpirationDays { get; set; } = 0;
        public int LibraryMinorVersionLimit { get; set; } = 0;

        public bool ListSetVersioningSettings {  get; set; } = false;
        public bool ListApplyToAllExistingLists { get; set; } = false;
        public string ListApplySingleListTitle { get; set; } = String.Empty;
        public bool ListEnableVersioning { get; set; } = false;
        public int ListMajorVersionLimit { get; set; } = 500;


        public void ParametersCheck()
        {

            if (LibraryNewLibraries && !LibraryEnableVersioning && !LibraryInheritTenantVersionSettings)
            {
                throw new Exception($"You cannot disable versioning for new libraries. This is only available for existing ones.");
            }
            if (LibraryNewLibraries && LibraryMajorVersionLimit < 100)
            {
                throw new Exception($"Major version for bew libraries has to be 100 or above.");
            }
            if (LibraryExistingLibraries && !LibraryApplyToAllExistingLibraries && String.IsNullOrWhiteSpace(LibraryApplyToSingleLibraryTitle))
            {
                throw new Exception($"If selected Existing libraries, you need to apply either to all libraries or provide the title of a single library.");
            }
            if (LibraryInheritTenantVersionSettings && (LibraryAutomaticVersionLimit || LibraryMajorVersionLimit != 500 || LibraryExpirationDays != 0 || LibraryMinorVersionLimit != 0))
            {
                throw new Exception($"If selected to inherit limits from Tenant, you cannot set Automatic, Major, Minor or Expiration days version limit for Libraries.");
            }
            if (LibraryAutomaticVersionLimit && (LibraryMajorVersionLimit != 500 || LibraryExpirationDays != 0 || LibraryMinorVersionLimit != 0))
            {
                throw new Exception($"If selected Automatic limits, you cannot set Major, Minor or Expiration days version limit for Libraries .");
            }
            if (LibraryExpirationDays > 0)
            {
                if (LibraryExpirationDays < 30 || 36500 < LibraryExpirationDays )
                {
                    throw new Exception($"Expiration days needs to be between 30 and 36500.");
                }
                if (!LibraryEnableVersioning || LibraryMajorVersionLimit < 1)
                {
                    throw new Exception($"If selected Expiration days, you need to enable Versioning and Major version limit above 1 for libraries.");
                }
            }
            if (LibraryMinorVersionLimit > 0 && (!LibraryEnableVersioning || LibraryMajorVersionLimit < 1))
            {
                throw new Exception($"If selected Minor versions, you need to enable Versioning and Major version limit above 1 for libraries.");
            }
            if (LibraryEnableVersioning && !LibraryAutomaticVersionLimit && LibraryMajorVersionLimit < 1)
            {
                throw new Exception($"If enable versioning, you need to enable set automatic limit of set Majot version limits.");
            }

            if (ListSetVersioningSettings && !ListApplyToAllExistingLists && string.IsNullOrEmpty(ListApplySingleListTitle))
            {
                throw new Exception($"You need to apply either to all list or provide the title of a single list.");
            }
        }
    }

    public class SetVersioningLimitAutoParameters : ISolutionParameters
    {

        internal SPOAdminAccessParameters AdminAccess;
        internal SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        public VersioningLimitParameters VersionParam { get; set; }

        internal SPOListsParameters LibraryParameters;
        internal SPOListsParameters ListParameters;

        public SetVersioningLimitAutoParameters(SPOAdminAccessParameters adminAccess, SPOTenantSiteUrlsParameters siteParam, VersioningLimitParameters versionParam)
        {
            AdminAccess = adminAccess;
            SiteParam = siteParam;
            VersionParam = versionParam;

            LibraryParameters = new()
            {
                AllLists = versionParam.LibraryApplyToAllExistingLibraries,
                IncludeLibraries = versionParam.LibraryApplyToAllExistingLibraries,
                IncludeLists = false,
                ListTitle = versionParam.LibraryApplyToSingleLibraryTitle,
            };

            ListParameters = new()
            {
                AllLists = versionParam.ListApplyToAllExistingLists,
                IncludeLibraries = false,
                IncludeLists = versionParam.ListApplyToAllExistingLists,
                ListTitle = versionParam.ListApplySingleListTitle,
            };

        }

    }
}
