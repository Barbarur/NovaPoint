using AngleSharp.Css.Dom;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.Item;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
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
        // TO BE DEPRECATED ONCE RemoveFileVersionAuto IS ON PRODUCTION

        //public static string _solutionName = "Report of all Files/Items in all Libraries/Lists of a Site";
        //public static string _solutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ItemAllListAllSiteSingleReport";

        private readonly LogHelper _logHelper;
        private readonly Commands.Authentication.AppInfo AppInfo;

        private readonly string _siteUrl;
        private readonly string _ListName;

        private readonly bool DeleteAll;
        private readonly int VersionsToKeep;
        private readonly bool Recycle;

        public RemoveVersionItemAllListSingleSiteSingleAuto(Action<LogInfo> uiAddLog, AppInfo appInfo, RemoveVersionItemAllListSingleSiteSingleAutoParameters parameters)
        {
            _logHelper = new(uiAddLog, "Automation", GetType().Name);
            AppInfo = appInfo;
            
            _siteUrl = parameters.SiteUrl;
            _ListName = parameters.ListName;
            
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
            //    AppInfo.IsCancelled();
            //    _logHelper.ScriptStartNotice();

            //    string rootUrl = _siteUrl.Substring(0, _siteUrl.IndexOf(".com") + 4);
            //    string rootSiteAccessToken = await new GetAccessToken(_logHelper, AppInfo).SpoInteractiveAsync(rootUrl);

            //    var oList = new GetSPOList(_logHelper, AppInfo, rootSiteAccessToken).CSOMSingleStandard(_siteUrl, _ListName);

            //    var collItems = new GetSPOItem(_logHelper, AppInfo, rootSiteAccessToken).CSOMAllDetailReportInfo(_siteUrl, oList);
            //    ProgressTracker progress = new(_logHelper, collItems.Count);
            //    foreach (ListItem oItem in collItems)
            //    {
            //        AppInfo.IsCancelled();

            //        if(oItem.TypedObject.ToString() == "Folder")
            //        {
            //            progress.MainCounterIncrement();
            //            continue;
            //        }

            //        else if (DeleteAll)
            //        {
            //            try
            //            {
            //                new RemoveSPOItemVersion(_logHelper, AppInfo, rootSiteAccessToken).CSOM(_siteUrl, oItem["FileRef"].ToString(), DeleteAll);

            //                int versionsDeletedCount = oItem.Versions.Count;
            //                var itemSize = Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);
            //                var versionsDeletedMB = itemSize * versionsDeletedCount;

            //                AddItemRecord(oList, oItem, versionsDeletedCount.ToString(), versionsDeletedMB.ToString());
            //            }
            //            catch (Exception ex)
            //            {
            //                _logHelper.AddLogToUI($"Error processing Item '{oItem["FileRef"]}'");
            //                _logHelper.AddLogToTxt($"Exception: {ex.Message}");
            //                _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");

            //                AddItemRecord(oList, oItem, remarks: ex.Message);
            //            }
            //        }
            //        else
            //        {
            //            FileVersionCollection itemVersion = new GetSPOFileVersion(_logHelper, AppInfo, rootSiteAccessToken).CSOM(_siteUrl, _ListName);

            //            int versionToDelete = itemVersion.Count - VersionsToKeep;

            //            if (versionToDelete > 0)
            //            {
            //                int errorsCount = 0;
            //                string remarks = String.Empty;
            //                for (int i = 0; i < versionToDelete; i++)
            //                {
            //                    AppInfo.IsCancelled();

            //                    try
            //                    {
            //                        new RemoveSPOItemVersion(_logHelper, AppInfo, rootSiteAccessToken).CSOM(_siteUrl, (string)oItem["FileRef"], versionId: versionToDelete, recycle: Recycle);
            //                    }
            //                    catch(Exception ex)
            //                    {

            //                        _logHelper.AddLogToUI($"Error processing Item '{oItem["FileRef"]}'");
            //                        _logHelper.AddLogToTxt($"Exception: {ex.Message}");
            //                        _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");

            //                        AddItemRecord(oList, oItem, remarks: ex.Message);

            //                        errorsCount++;
            //                    }
            //                }

            //                int versionsDeletedCount = versionToDelete - errorsCount;
            //                var itemSize = Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);
            //                var versionsDeletedMB = itemSize * versionsDeletedCount;

            //                if (errorsCount > 0) { remarks = $"Error while deleting {errorsCount} versions"; }

            //                AddItemRecord(oList, oItem, versionsDeletedCount.ToString(), versionsDeletedMB.ToString(), remarks);
            //            }
            //            else
            //            {
            //                AddItemRecord(oList, oItem, remarks: "No versions to delete");
            //            }
            //        }
            //        progress.MainCounterIncrement();
            //    }
            //    _logHelper.ScriptFinishSuccessfulNotice();
        }


        private void AddItemRecord(List oList,
                                   ListItem oItem,
                                   string versionsDeletedCount = "NA",
                                   string versionsDeletedMB = "NA",
                                   string remarks = "")
        {
            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = _siteUrl;
            recordItem.ListTitle = oList.Title;
            recordItem.ListType = oList.BaseType;

            recordItem.ItemID = oItem["ID"];
            recordItem.ItemName = oItem["FileLeafRef"];
            recordItem.ItemPath = oItem["FileRef"];

            recordItem.ItemVersion = oItem["_UIVersionString"];
            recordItem.ItemVersionsCount = oItem.Versions.Count.ToString();

            recordItem.ItemSizeMb = Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);
            FieldLookupValue MTotalSize = (FieldLookupValue)oItem["SMTotalSize"];
            recordItem.ItemSizeTotalMB = Math.Round(MTotalSize.LookupId / Math.Pow(1024, 2), 2);

            recordItem.DeletedVersionsCount = versionsDeletedCount;
            recordItem.DeletedVersionsMB = versionsDeletedMB;

            recordItem.Remarks = remarks;

            _logHelper.AddRecordToCSV(recordItem);
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
