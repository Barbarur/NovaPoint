using Microsoft.Graph;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Permision;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class SetVersioningLimitAuto : ISolution
    {
        public static readonly string s_SolutionName = "Set versioning limit";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-SetVersioningLimitAuto";

        private SetVersioningLimitAutoParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _listExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        {

            l => l.Hidden,

            l => l.BaseType,
            l => l.Title,
            l => l.DefaultViewUrl,
            l => l.Id,

            l => l.EnableVersioning,
            l => l.MajorVersionLimit,
            l => l.EnableMinorVersions,
            l => l.MajorWithMinorVersionsLimit,
        };

        private SetVersioningLimitAuto(NPLogger logger, Commands.Authentication.AppInfo appInfo, SetVersioningLimitAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(SetVersioningLimitAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            parameters.TListsParam.ListParam.ListExpresions = _listExpresions;

            NPLogger logger = new(uiAddLog, "SetVersioningLimitAuto", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new SetVersioningLimitAuto(logger, appInfo, parameters).RunScriptAsync();

                logger.ScriptFinish();

            }
            catch (Exception ex)
            {
                logger.ScriptFinish(ex);
            }
        }

        //public SetVersioningLimitAuto(SetVersioningLimitAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        //{
        //    _param = parameters;
        //    _param.TListsParam.ListParam.ListExpresions = _listExpresions;
        //    _logger = new(uiAddLog, this.GetType().Name, _param);
        //    _appInfo = new(_logger, cancelTokenSource);
        //}

        //public async Task RunAsync()
        //{
        //    try
        //    {
        //        await RunScriptAsync();

        //        _logger.ScriptFinish();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.ScriptFinish(ex);
        //    }
        //}

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param.TListsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(results.ErrorMessage) || results.List == null)
                {
                    AddRecord(results.SiteUrl, results.List, remarks: results.ErrorMessage);
                    continue;
                }

                try
                {
                    await SetVersioning(results.SiteUrl, results.List);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(results.List.BaseType.ToString(), results.List.DefaultViewUrl, ex);
                    AddRecord(results.SiteUrl, results.List, remarks: ex.Message);
                }
            }

        }

        private async Task SetVersioning(string siteUrl, Microsoft.SharePoint.Client.List olist)
        {
            _appInfo.IsCancelled();

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            Microsoft.SharePoint.Client.List oList = clientContext.Web.GetListByTitle(olist.Title, _listExpresions);

            int majorVersions = 0;
            int minorVersions = 0;

            if (oList.BaseType == BaseType.DocumentLibrary)
            {
                majorVersions = _param.LibraryMajorVersionLimit;
                minorVersions = _param.LibraryMinorVersionLimit;

            }
            else if (oList.BaseType == BaseType.GenericList)
            {
                majorVersions = _param.ListMajorVersionLimit;
                minorVersions = 0;
            }

            bool enableVersioning = majorVersions > 0;
            bool enableMinorVersions = minorVersions > 0;
            bool updateRequired = false;

            if (enableVersioning != oList.EnableVersioning)
            {
                oList.EnableVersioning = enableVersioning;
                updateRequired = true;
            }

            if (enableVersioning)
            {
                oList.MajorVersionLimit = majorVersions;
                updateRequired = true;
            }

            if (oList.BaseType == BaseType.DocumentLibrary)
            {
                //if (enableVersioning && enableMinorVersions)
                //{
                //    oList.EnableMinorVersions = enableMinorVersions;
                //    oList.MajorWithMinorVersionsLimit = (int)minorVersions;
                //    updateRequired = true;
                //}
                //else
                //{
                //    oList.EnableMinorVersions = false;
                //    oList.MajorWithMinorVersionsLimit = 0;
                //    updateRequired = true;
                //}


                if (enableVersioning && enableMinorVersions != oList.EnableMinorVersions)
                {
                    oList.EnableMinorVersions = enableMinorVersions;
                    updateRequired = true;
                }

                if (enableVersioning && enableMinorVersions)
                {
                    oList.MajorWithMinorVersionsLimit = (int)minorVersions;
                    updateRequired = true;
                }
            }

            if (updateRequired)
            {
                _logger.LogTxt(GetType().Name, $"Updating '{oList.BaseType}' - '{oList.Title}', Major versions {enableVersioning}, Major versions limit {majorVersions}, Minor versions {enableMinorVersions}, Minor versions limit {minorVersions}");
                oList.Update();
                clientContext.ExecuteQuery();
            }

        }

        private void AddRecord(string siteUrl,
                               Microsoft.SharePoint.Client.List? oList = null,
                               string remarks = "")
        {
            dynamic dynamicRecord = new ExpandoObject();
            dynamicRecord.SiteUrl = siteUrl;
            dynamicRecord.ListTitle = oList != null ? oList.Title : String.Empty;
            dynamicRecord.ListType = oList != null ? oList.BaseType.ToString() : String.Empty;

            dynamicRecord.Remarks = remarks;

            _logger.DynamicCSV(dynamicRecord);
        }
    }

    public class SetVersioningLimitAutoParameters : ISolutionParameters
    {
        public int LibraryMajorVersionLimit { get; set; } = 500;
        public int LibraryMinorVersionLimit { get; set; } = 0;
        public int ListMajorVersionLimit { get; set; } = 500;

        public SPOTenantListsParameters TListsParam {  get; set; }

        public SetVersioningLimitAutoParameters(SPOTenantListsParameters listsParameters)
        {
            TListsParam = listsParameters;
        }

        internal void ParametersCheck()
        {
            if (LibraryMajorVersionLimit < 1 && LibraryMinorVersionLimit > 0)
            {
                throw new Exception($"FORM INCOMPLETED: You cannot set Minor verion limit for a library without setting Major version limit.");
            }
        }
    }
}
