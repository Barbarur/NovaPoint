using Microsoft.Graph;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Reports
{
    // TO DEPRECATED
    //public class UserAllSiteSingleReport
    //{
    //    public static readonly string s_SolutionName = "Users in a single Site report";
    //    public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-UserAllSiteSingleReport";

    //    private UserAllSiteSingleReportParameters _param = new();
    //    public ISolutionParameters Parameters
    //    {
    //        get { return _param; }
    //        set { _param = (UserAllSiteSingleReportParameters)value; }
    //    }

    //    private readonly NPLogger _logger;
    //    private readonly Commands.Authentication.AppInfo _appInfo;

    //    private readonly string SiteUrl;

    //    public UserAllSiteSingleReport(UserAllSiteSingleReportParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
    //    {
    //        Parameters = parameters;
    //        _logger = new(uiAddLog, this.GetType().Name, parameters);
    //        _appInfo = new(_logger, cancelTokenSource);
    //    }

    //    public async Task RunAsync()
    //    {
    //        try
    //        {
    //            if (String.IsNullOrEmpty(SiteUrl) || String.IsNullOrWhiteSpace(SiteUrl))
    //            {
    //                string message = "FORM INCOMPLETED: Site URL cannot be empty if you need to obtain the Site Users";
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
    //            _logger.ScriptFinish(ex);
    //        }
    //    }
    //    private async Task RunScriptAsync()
    //    {
    //        _logger.ScriptStartNotice();
    //        _appInfo.IsCancelled();

    //        string rootUrl = SiteUrl.Substring(0, SiteUrl.IndexOf(".com") + 4);
    //        string rootSiteAccessToken = await new GetAccessToken(_logger, _appInfo).SpoInteractiveAsync(rootUrl);
            

    //        List<Microsoft.SharePoint.Client.User> collUsers = new GetUser(_logger, rootSiteAccessToken).CsomAll(SiteUrl, false);
    //        foreach (Microsoft.SharePoint.Client.User oUser in collUsers)
    //        {
    //            _appInfo.IsCancelled();

    //            dynamic recordUser = new ExpandoObject();
    //            recordUser.SiteUrl = SiteUrl;
    //            recordUser.Title = oUser.Title;
    //            recordUser.UserPrincipalName = oUser.UserPrincipalName;
    //            recordUser.Email = oUser.Email;
    //            recordUser.IsSiteAdmin = oUser.IsSiteAdmin;
    //            recordUser.UserSPOID = GetUserId(oUser.UserId);

    //            _logger.RecordCSV(recordUser);

    //        }

    //        _logger.ScriptFinish();
    //    }
    //    private static string GetUserId(UserIdInfo userIdInfo)
    //    {
    //        if (userIdInfo == null) { return ""; }
    //        else { return userIdInfo.NameId.ToString(); }
    //    }
    //}

    //public class UserAllSiteSingleReportParameters : ISolutionParameters
    //{
    //    public string SiteUrl { get; set; } = string.Empty;
    //}
}
