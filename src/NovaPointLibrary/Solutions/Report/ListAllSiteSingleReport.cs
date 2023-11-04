using Microsoft.Extensions.Primitives;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Reports
{
    // TO BE DEPRECATED ONCE ListReport IS ON PRODUCTION
    //public class ListAllSiteSingleReport
    //{
    //    // Baic parameters required for all reports
    //    private readonly LogHelper _logHelper;
    //    private readonly AppInfo AppInfo;
    //    // Required parameters for the current report
    //    private readonly string SiteUrl;
    //    // Optional parameters for the current report
    //    private readonly bool IncludeSystemLists;
    //    private readonly bool IncludeResourceLists;

    //    public ListAllSiteSingleReport(Action<LogInfo> uiAddLog, AppInfo appInfo, ListAllSiteSingleReportParameters parameters)
    //    {
    //        _logHelper = new(uiAddLog, "Reports", GetType().Name);
    //        AppInfo = appInfo;
    //        SiteUrl = parameters.SiteUrl;
    //        IncludeSystemLists = parameters.IncludeSystemLists;
    //        IncludeResourceLists = parameters.IncludeResourceLists;
    //    }

    //    public async Task RunAsync()
    //    {
    //        try
    //        {
    //            if (String.IsNullOrEmpty(SiteUrl) || String.IsNullOrWhiteSpace(SiteUrl))
    //            {
    //                string message = "FORM INCOMPLETED: Site URL cannot be empty if you need to obtain the Site Lists/Libraries";
    //                Exception ex = new(message);
    //                throw ex;
    //            }
    //            else
    //            {
    //                await RunScriptAsync();
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logHelper.ScriptFinishErrorNotice(ex);
    //        }
    //    }

    //    private async Task RunScriptAsync()
    //    {
    //        _logHelper.ScriptStartNotice();

    //        string rootUrl = SiteUrl.Substring(0, SiteUrl.IndexOf(".com") + 4);
    //        string accessToken = await new GetAccessToken(_logHelper, AppInfo).SpoInteractiveAsync(rootUrl);

    //        double counterList = 0;
    //        if (AppInfo.CancelToken.IsCancellationRequested) { AppInfo.CancelToken.ThrowIfCancellationRequested(); };
    //        List<List> collList = new GetList(_logHelper, accessToken).CSOM_All(SiteUrl, IncludeSystemLists, IncludeResourceLists);
    //        foreach (List oList in collList)
    //        {
    //            if (AppInfo.CancelToken.IsCancellationRequested) { AppInfo.CancelToken.ThrowIfCancellationRequested(); };

    //            double progress = Math.Round(counterList * 100 / collList.Count, 2);
    //            counterList++;
    //            _logHelper.AddProgressToUI(progress);
    //            _logHelper.AddLogToUI($"Processong List '{oList.Title}'");

    //            dynamic recordList = new ExpandoObject();
    //            recordList.SiteUrl = SiteUrl;
    //            recordList.LibraryName = oList.Title;
    //            recordList.LibraryType = oList.BaseType;
    //            recordList.MajorVersionLimit = oList.MajorVersionLimit;
    //            recordList.MinorVersionLimit = oList.EnableMinorVersions;
    //            recordList.MinorVersionsLimit = oList.MajorWithMinorVersionsLimit;
    //            recordList.IRM_Emabled = oList.IrmEnabled;

    //            _logHelper.AddRecordToCSV(recordList);

    //        }

    //        _logHelper.ScriptFinishSuccessfulNotice();

    //    }
    //}


    //public class ListAllSiteSingleReportParameters
    //{
    //    internal string SiteUrl;
    //    public bool IncludeSystemLists { get; set; } = false;
    //    public bool IncludeResourceLists { get; set; } = false;

    //    public ListAllSiteSingleReportParameters(string siteUrl)
    //    {
    //        SiteUrl = siteUrl;
    //    }
    //}
}
