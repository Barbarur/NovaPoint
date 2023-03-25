using AngleSharp.Css.Dom;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Item;
using NovaPointLibrary.Solutions.Reports;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveVersionItemAllListSingleSiteSingleAuto
    {
        // Baic parameters required for all reports
        private readonly LogHelper _logHelper;
        private readonly Commands.Authentication.AppInfo AppInfo;
        // Required parameters for the current report
        private readonly string _siteUrl;
        private readonly string _ListName;
        // Optional parameters for the current report
        private readonly bool DeleteAll;
        private readonly int VersionsToKeep;
        private readonly bool Recycle;

        public RemoveVersionItemAllListSingleSiteSingleAuto(Action<LogInfo> uiAddLog, AppInfo appInfo, RemoveVersionItemAllListSingleSiteSingleAutoParameters parameters)
        {
            // Baic parameters required for all reports
            _logHelper = new(uiAddLog, "Reports", GetType().Name);
            AppInfo = appInfo;
            // Required parameters for the current report
            _siteUrl = parameters.SiteUrl;
            _ListName = parameters.ListName;
            // Optional parameters for the current report
            DeleteAll = parameters.DeleteAll;
            VersionsToKeep = parameters.VersionsToKeep;
            Recycle = parameters.Recycle;
        }


        public async Task RunAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_siteUrl) || string.IsNullOrWhiteSpace(_ListName))
                {
                    throw new Exception($"FORM INCOMPLETED: Please fill up the form");
                }
                else if (!DeleteAll && string.IsNullOrWhiteSpace(VersionsToKeep.ToString()))
                {
                    throw new Exception($"FORM INCOMPLETED: Number of versions to keep cannot be empty");
                }
                else
                {
                    await RunScriptAsync();
                }
            }
            catch (Exception ex)
            {
                _logHelper.ScriptFinishErrorNotice(ex);
            }
        }
        
        private async Task RunScriptAsync()
        {
            _logHelper.ScriptStartNotice();

            string rootUrl = _siteUrl.Substring(0, _siteUrl.IndexOf(".com") + 4);
            string rootSiteAccessToken = await new GetAccessToken(_logHelper, AppInfo).SpoInteractiveAsync(rootUrl);

            List<ListItem> collItems = new GetItem(_logHelper, rootSiteAccessToken).CsomAllItems(_siteUrl, _ListName);
            double counter = 0;
            foreach (ListItem oItem in collItems)
            {
                counter++;
                double progress = Math.Round(counter * 100 / collItems.Count, 2);
                _logHelper.AddProgressToUI(progress);
                _logHelper.AddLogToUI($"Processing Item '{oItem["FileLeafRef"]}'");

                dynamic recordSite = new ExpandoObject();
                recordSite.SiteUrl = _siteUrl;
                recordSite.ListName = _ListName;
                
                if (DeleteAll)
                {
                    try
                    {
                        new RemoveItemVersion(_logHelper.AddLog, rootSiteAccessToken).Csom(_siteUrl, (string)oItem["FileRef"], DeleteAll);
                        
                        recordSite.Remarks = "All versions deleted";
                    }
                    catch (Exception ex)
                    {
                        _logHelper.AddLogToUI($"Error: {ex.Message}");
                        _logHelper.AddLogToTxt($"{ex.StackTrace}");

                        recordSite.Remarks = $"{ex.Message}";
                    }
                }
                else
                {
                    FileVersionCollection itemVersion = new GetItemVersion(_logHelper.AddLog, rootSiteAccessToken).Csom(_siteUrl, _ListName);

                    int versionToDelete = itemVersion.Count - VersionsToKeep;

                    if (versionToDelete > 0)
                    {
                        int errorsCount = 0;
                        for (int i = 0; i < versionToDelete; i++)
                        {
                            try
                            {
                                new RemoveItemVersion(_logHelper.AddLog, rootSiteAccessToken).Csom(_siteUrl, (string)oItem["FileRef"], DeleteAll, versionToDelete, Recycle);
                            }
                            catch(Exception ex)
                            {
                                _logHelper.AddLogToUI($"Error: {ex.Message}");
                                _logHelper.AddLogToTxt($"{ex.StackTrace}");

                                errorsCount++;
                            }

                            int versionsDeleted = versionToDelete - errorsCount;
                            recordSite.Remarks = $"Versions deleted: {versionsDeleted}. Error while deleting versions: {errorsCount}";
                        }
                    }
                    else
                    {
                        recordSite.Remarks = "No versions to delete";
                    }
                }
                _logHelper.AddRecordToCSV(recordSite);
            }
            _logHelper.ScriptFinishSuccessfulNotice();
        }
    }


    public class RemoveVersionItemAllListSingleSiteSingleAutoParameters
    {
        // Required parameters for the current report
        internal string SiteUrl;
        internal string ListName;
        // Optional parameters related to filter sites
        public bool DeleteAll { get; set; } = false;
        public int VersionsToKeep { get; set; } = 100;
        public bool Recycle { get; set; } = false;

        public RemoveVersionItemAllListSingleSiteSingleAutoParameters(string siteUrl, string listName)
        {
            SiteUrl = siteUrl;
            ListName = listName;
        }
    }
}
