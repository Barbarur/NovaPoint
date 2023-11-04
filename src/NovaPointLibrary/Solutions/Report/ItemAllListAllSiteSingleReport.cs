using Microsoft.SharePoint.Client;
using System.Dynamic;


namespace NovaPointLibrary.Solutions.Reports
{
    // TO BE DEPRECATED ONCE ItemReport IS ON PRODUCTION
    public class ItemAllListAllSiteSingleReport : ISolution
    {
        public static string _solutionName = "Report of all Files/Items in all Libraries/Lists of a Site";
        public static string _solutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ItemAllListAllSiteSingleReport";

        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo AppInfo;

        private ItemAllListAllSiteSingleReportParameters _param;

        public ISolutionParameters Parameters
        {
            get
            {
                return _param;
            }
            set
            {
                _param = (ItemAllListAllSiteSingleReportParameters)value;
            }
        }

        private readonly string SiteUrl;

        private readonly bool IncludeSystemLists = false;
        private readonly bool IncludeResourceLists = false;

        public ItemAllListAllSiteSingleReport(Action<LogInfo> uiAddLog, Commands.Authentication.AppInfo appInfo, ItemAllListAllSiteSingleReportParameters parameters)
        {
            _logger = new(uiAddLog, "Reports", GetType().Name);
            AppInfo = appInfo;

            SiteUrl = parameters.SiteUrl;

            IncludeSystemLists = parameters.IncludeSystemLists;
            IncludeResourceLists = parameters.IncludeResourceLists;
        }
        public async Task RunAsync()
        {
            try
            {
                if ( String.IsNullOrWhiteSpace(SiteUrl) )
                {
                    string message = "FORM INCOMPLETED: Site URL cannot be empty if you need to obtain the Site Collection Administrators";
                    Exception ex = new(message);
                    throw ex;
                }
                else
                {
                    await RunScriptAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            //AppInfo.IsCancelled();
            //_logHelper.ScriptStartNotice();


            //string rootUrl = SiteUrl.Substring(0, SiteUrl.IndexOf(".com") + 4);
            //string rootSiteAccessToken = await new GetAccessToken(_logHelper, AppInfo).SpoInteractiveAsync(rootUrl);


            //List<List> collList = new GetSPOList(_logHelper, AppInfo, rootSiteAccessToken).CSOMAll(SiteUrl, IncludeSystemLists, IncludeResourceLists);
            //ProgressTracker progress = new(_logHelper, collList.Count);
            //foreach (List oList in collList)
            //{
            //    AppInfo.IsCancelled();

            //    progress.MainReportProgress($"Processing List '{oList.Title}'");

            //    try
            //    {
            //        var collItems = new GetSPOItem(_logHelper, AppInfo, rootSiteAccessToken).CSOMAllDetailReportInfo(SiteUrl, oList);
            //        progress.SubTaskProgressReset(collItems.Count);
            //        foreach (ListItem oItem in collItems)
            //        {
            //            AppInfo.IsCancelled();

            //            try
            //            {
            //                if (oList.BaseType.ToString() == "DocumentLibrary")
            //                {
            //                    string itemName = (string)oItem["FileLeafRef"];

            //                    float itemSizeMb = oItem.FileSystemObjectType.ToString() == "Folder" ? 0 : (float)Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);

            //                    FieldLookupValue fvFileSizeTotalMb = (FieldLookupValue)oItem["SMTotalSize"];
            //                    float itemSizeTotalMb = oItem.FileSystemObjectType.ToString() == "Folder" ? 0 : (float)Math.Round(fvFileSizeTotalMb.LookupId / Math.Pow(1024, 2), 2);

            //                    AddItemRecord(oList, oItem, itemName, itemSizeMb.ToString(), itemSizeTotalMb.ToString(), "");
            //                }
            //                else if (oList.BaseType.ToString() == "GenericList")
            //                {
            //                    string itemName = (string)oItem["Title"];

            //                    if (oItem.FileSystemObjectType.ToString() == "Folder")
            //                    {
            //                        string itemSizeMb = "0";
                                
            //                        string itemSizeTotalMb = "0";
                                
            //                        AddItemRecord(oList, oItem, itemName, itemSizeMb.ToString(), itemSizeTotalMb.ToString(), "NA");
            //                    }
            //                    else
            //                    {
            //                        int itemSizeTotalBytes = 0;
            //                        foreach(var oAttachment in oItem.AttachmentFiles)
            //                        {
            //                            var oFileAttachment = new GetSPOItem(_logHelper, AppInfo, rootSiteAccessToken).CSOMAttachmentFile(SiteUrl, oAttachment.ServerRelativeUrl);

            //                            itemSizeTotalBytes += (int)oFileAttachment.Length;
            //                        }
            //                        float itemSizeTotalMb = (float)Math.Round(itemSizeTotalBytes / Math.Pow(1024, 2), 2);

            //                        AddItemRecord(oList, oItem, itemName, itemSizeTotalMb.ToString(), itemSizeTotalMb.ToString(), "");
            //                    }
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                _logHelper.AddLogToUI($"Error processing Item '{oItem["FileRef"]}'");
            //                _logHelper.AddLogToTxt($"Exception: {ex.Message}");
            //                _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");

            //                AddItemRecord(oList, oItem, "", "", "", ex.Message);
            //            }

            //            progress.SubTaskCounterIncrement();
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        _logHelper.AddLogToUI($"Error processing List '{oList.Title}'");
            //        _logHelper.AddLogToTxt($"Exception: {ex.Message}");
            //        _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");

            //        AddItemRecord(oList, null, "", "", "", ex.Message);
            //    }

            //    progress.MainCounterIncrement();
            //}

            //_logHelper.ScriptFinish();
        }
    
       
        private void AddItemRecord(List oList, ListItem? oItem = null, string itemName = "", string itemSizeMb  = "", string itemSizeTotalMb = "", string remarks = "")
        {
            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = SiteUrl;
            recordItem.ListTitle = oList.Title;
            recordItem.ListType = oList.BaseType;

            recordItem.ItemID = oItem != null ? oItem["ID"] : string.Empty;
            recordItem.ItemName = oItem != null ? itemName : string.Empty;
            recordItem.ItemPath = oItem != null ? oItem["FileRef"] : string.Empty;
            recordItem.ItemType = oItem != null ? oItem.FileSystemObjectType.ToString() : string.Empty;

            recordItem.ItemCreated = oItem != null ? oItem["Created"] : string.Empty;
            FieldUserValue? author = oItem != null ? (FieldUserValue)oItem["Author"] : null;
            recordItem.ItemCreatedBy = author != null ? author.Email : null;

            recordItem.ItemModified = oItem != null ? oItem["Modified"] : string.Empty;
            FieldUserValue? editor = oItem != null ? (FieldUserValue)oItem["Editor"] : null;
            recordItem.ItemModifiedBy = editor != null ? editor.Email : null;

            recordItem.ItemVersion = oItem != null ? oItem["_UIVersionString"] : string.Empty;
            recordItem.ItemVersionsCount = oItem != null ? oItem.Versions.Count.ToString() : string.Empty;

            recordItem.ItemSizeMb = itemSizeMb;
            recordItem.ItemSizeTotalMB = itemSizeTotalMb;

            recordItem.Remarks = remarks;

            _logger.RecordCSV(recordItem);
        }
    }


    public class ItemAllListAllSiteSingleReportParameters : ISolutionParameters
    {
        // Required parameters for the current report
        internal string SiteUrl;
        public bool IncludeSystemLists { get; set; } = false;
        public bool IncludeResourceLists { get; set; } = false;

        public ItemAllListAllSiteSingleReportParameters(string siteUrl)
        {
            SiteUrl = siteUrl;
        }
    }
}
