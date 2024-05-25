using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.PreservationHoldLibrary;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveFileVersionAuto : ISolution
    {
        public readonly static String s_SolutionName =  "Remove file versions";
        public readonly static String s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveFileVersionAuto";

        private RemoveFileVersionAutoParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static readonly Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] _fileExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
        {
            //i => i["Author"],
            //i => i["Created"],
            //i => i["Editor"],
            //i => i["ID"],
            i => i.FileSystemObjectType,
            //i => i["FileLeafRef"],
            //i => i["FileRef"],
            //i => i["File_x0020_Size"],
            //i => i["Modified"],
            i => i["SMTotalSize"],
            //i => i["_UIVersionString"],

            i => i.Id,
            i => i.File.Name,
            i => i.File.ServerRelativeUrl,

            //i => i.File.UIVersion,
            i => i.File.UIVersionLabel,
            i => i.File.Versions,
            //i => i.Versions,
            i => i.File.Length,
            //i => i.File.VersionEvents,
            //i => i.File.Versions,


        };

        private RemoveFileVersionAuto(NPLogger logger, Commands.Authentication.AppInfo appInfo, RemoveFileVersionAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(RemoveFileVersionAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            parameters.ItemsParam.FileExpresions = _fileExpressions;
            parameters.ListsParam.IncludeLibraries = true;
            parameters.ListsParam.IncludeLists = false;
            parameters.ListsParam.IncludeHiddenLists = false;
            parameters.ListsParam.IncludeSystemLists = false;

            NPLogger logger = new(uiAddLog, "RemoveFileVersionAuto", parameters);
            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new RemoveFileVersionAuto(logger, appInfo, parameters).RunScriptAsync();

                logger.ScriptFinish();

            }
            catch (Exception ex)
            {
                logger.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var resultItem in new SPOTenantItemsCSOM(_logger, _appInfo, _param.TItemsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(resultItem.ErrorMessage))
                {
                    RemoveFileVersionAutoRecord record = new(resultItem);
                    _logger.RecordCSV(record);
                    continue;
                }

                if (resultItem.Item == null || resultItem.List == null) { continue; }

                if (resultItem.Item.FileSystemObjectType.ToString() == "Folder") { continue; }
                
                try
                {
                    await RemoveFileVersions(resultItem);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Item", (string)resultItem.Item["FileRef"], ex);
                    
                    RemoveFileVersionAutoRecord record = new(resultItem, ex.Message);
                    _logger.RecordCSV(record);
                }
            }
        }

        //private async Task RunScriptAsync()
        //{
        //    _appInfo.IsCancelled();

        //    await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param.TListsParam).GetAsync())
        //    {
        //        _appInfo.IsCancelled();

        //        if (!String.IsNullOrWhiteSpace(results.ErrorMessage) || results.List == null)
        //        {
        //            AddRecord(results.SiteUrl, results.List, remarks: results.ErrorMessage);
        //            continue;
        //        }

        //        try
        //        {
        //            await ProcessItems(results.SiteUrl, results.List, results.Progress);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.ReportError(results.List.BaseType.ToString(), results.List.DefaultViewUrl, ex);
        //            AddRecord(results.SiteUrl, results.List, remarks: ex.Message);
        //        }
        //    }
        //}

        //private async Task ProcessItems(string siteUrl, List oList, ProgressTracker parentProgress)
        //{
        //    _appInfo.IsCancelled();

        //    ProgressTracker progress = new(parentProgress, oList.ItemCount);

        //    var spoItem = new SPOListItemCSOM(_logger, _appInfo);
        //    await foreach (ListItem oItem in spoItem.GetAsync(siteUrl, oList, _param.ItemsParam))
        //    {
        //        _appInfo.IsCancelled();

        //        if (oItem.FileSystemObjectType.ToString() == "Folder") { continue; }

        //        try
        //        {
        //            await RemoveFileVersions(siteUrl, oList, oItem);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.ReportError("Item", (string)oItem["FileRef"], ex);

        //            AddRecord(siteUrl, oList, oItem, remarks: ex.Message);
        //        }

        //        progress.ProgressUpdateReport();
        //    }
        //}

        //private async Task RemoveFileVersions(string siteUrl, List oList, ListItem oItem)
        
        private async Task RemoveFileVersions(SPOTenantItemRecord resultItem)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start processing File '{resultItem.Item.File.Name}'");
            
            ClientContext clientContext = await _appInfo.GetContext(resultItem.SiteUrl);

            string fileURL = resultItem.Item.File.ServerRelativeUrl;
            Microsoft.SharePoint.Client.File file = clientContext.Web.GetFileByServerRelativePath(ResourcePath.FromDecodedUrl(fileURL));

            clientContext.Load(file, f => f.Exists, f => f.Versions.IncludeWithDefaultProperties(i => i.CreatedBy));
            FileVersionCollection fileVersionCollection = file.Versions;
            clientContext.ExecuteQueryRetry();

            if (_param.DeleteAll) { _param.VersionsToKeep = 0; }
            int numberVersionsToDelete = fileVersionCollection.Count - _param.VersionsToKeep;
            if (numberVersionsToDelete < 1)
            {
                RemoveFileVersionAutoRecord record = new(resultItem, "No versions to delete");
                record.AddFileDetails(resultItem.Item);
                _logger.RecordCSV(record);
                return;
            }

            if (_param.DeleteAll)
            {
                _logger.LogTxt(GetType().Name, $"Deleting all version '{resultItem.Item.File.Name}'");

                //int numberVersionsToDelete = resultItem.Item.Versions.Count - 1;
                var versionsDeletedMB = Math.Round((Convert.ToDouble(resultItem.Item.File.Length) * numberVersionsToDelete) / Math.Pow(1024, 2), 2);

                if (!_param.ReportMode && numberVersionsToDelete > 0)
                {
                    fileVersionCollection.DeleteAll();
                    clientContext.ExecuteQueryRetry();
                }

                RemoveFileVersionAutoRecord record = new(resultItem);
                record.AddFileDetails(resultItem.Item, numberVersionsToDelete.ToString(), versionsDeletedMB.ToString());
                _logger.RecordCSV(record);
                //AddRecord(siteUrl, oList, oItem, numberVersionsToDelete.ToString(), versionsDeletedMB.ToString());
            }
            else
            {
                //int numberVersionsToDelete = resultItem.Item.Versions.Count - _param.VersionsToKeep - 1;

                int errorsCount = 0;
                string remarks = String.Empty;

                for (int i = 0; i < numberVersionsToDelete; i++)
                {
                    _appInfo.IsCancelled();

                    try
                    {
                        if (!_param.ReportMode)
                        {
                            FileVersion fileVersionToDelete = fileVersionCollection.ElementAt(i);

                            if (_param.Recycle)
                            {
                                _logger.LogTxt(GetType().Name, $"Recycling version '{fileVersionToDelete.ID}' from '{fileVersionToDelete.Url}'");
                                fileVersionCollection.RecycleByID(fileVersionToDelete.ID);
                            }
                            else
                            {
                                _logger.LogTxt(GetType().Name, $"Deleting version '{fileVersionToDelete.ID}' from '{fileVersionToDelete.Url}'");
                                fileVersionCollection.DeleteByID(fileVersionToDelete.ID);
                            }
                            clientContext.ExecuteQueryRetry();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ReportError("Item", resultItem.Item.File.Name, ex);

                        RemoveFileVersionAutoRecord errorRecord = new(resultItem, ex.Message);
                        _logger.RecordCSV(errorRecord);
                        //AddRecord(siteUrl, oList, oItem, remarks: ex.Message);

                        errorsCount++;
                    }
                }

                int versionsDeletedCount = numberVersionsToDelete - errorsCount;
                var versionsDeletedMB = Math.Round( (Convert.ToDouble(resultItem.Item.File.Length) * versionsDeletedCount) / Math.Pow(1024, 2), 2);

                if (errorsCount > 0) { remarks = $"Error while deleting {errorsCount} versions"; }

                RemoveFileVersionAutoRecord record = new(resultItem, remarks);
                record.AddFileDetails(resultItem.Item, numberVersionsToDelete.ToString(), versionsDeletedMB.ToString());
                _logger.RecordCSV(record);
                //AddRecord(siteUrl, oList, oItem, versionsDeletedCount.ToString(), versionsDeletedMB.ToString(), remarks);

                //if (numberVersionsToDelete > 0) 
                //{
                //}
                //else
                //{
                //    RemoveFileVersionAutoRecord record = new(resultItem, "No versions to delete");
                //    record.AddFileDetails(resultItem.Item);
                //    _logger.RecordCSV(record);
                //    //AddRecord(siteUrl, oList, oItem, remarks: "No versions to delete"); 
                //}
            }
        }

        //private void AddRecord(string siteUrl,
        //                       Microsoft.SharePoint.Client.List? oList = null,
        //                       Microsoft.SharePoint.Client.ListItem? oItem = null,
        //                       string versionsDeletedCount = "NA",
        //                       string versionsDeletedMB = "NA",
        //                       string remarks = "")
        //{
            
        //    dynamic recordItem = new ExpandoObject();
        //    //recordItem.SiteUrl = siteUrl;
        //    //recordItem.ListTitle = oList != null ? oList.Title : String.Empty;
        //    //recordItem.ListType = oList != null ? oList.BaseType.ToString() : String.Empty;

        //    //recordItem.FileID = oItem != null ? oItem["ID"] : string.Empty;
        //    //recordItem.FileName = oItem != null ? oItem["FileLeafRef"] : string.Empty;
        //    //recordItem.FilePath = oItem != null ? oItem["FileRef"] : string.Empty;

        //    recordItem.FileVersionNo = oItem != null ? oItem["_UIVersionString"] : string.Empty;
        //    recordItem.FileVersionsCount = oItem != null ? oItem.Versions.Count.ToString() : string.Empty; ;

        //    recordItem.ItemSizeMb = oItem != null ? Math.Round( Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2 ).ToString() : string.Empty;

        //    if ( oItem != null)
        //    {
        //        FieldLookupValue MTotalSize =(FieldLookupValue)oItem["SMTotalSize"];
        //        recordItem.ItemSizeTotalMB = Math.Round(MTotalSize.LookupId / Math.Pow(1024, 2), 2);
        //    }
        //    else { recordItem.ItemSizeTotalMB = String.Empty; }

        //    recordItem.DeletedVersionsCount = versionsDeletedCount;
        //    recordItem.DeletedVersionsMB = versionsDeletedMB;

        //    recordItem.Remarks = remarks;

        //    _logger.DynamicCSV(recordItem);
        //}
    }

    public class RemoveFileVersionAutoRecord : ISolutionRecord
    {
        internal string SiteUrl { get; set; } = String.Empty;
        internal string ListTitle { get; set; } = String.Empty;

        internal string FileID { get; set; } = String.Empty;
        internal string FileTitle { get; set; } = String.Empty;
        internal string FilePath { get; set; } = String.Empty;

        internal string FileVersionNo { get; set; } = String.Empty;
        internal string FileVersionsCount { get; set; } = String.Empty;
        internal string ItemSizeMb { get; set; } = String.Empty;
        internal string ItemSizeTotalMB { get; set; } = String.Empty;
        internal string DeletedVersionsCount { get; set; } = String.Empty;
        internal string DeletedVersionsMB { get; set; } = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

        internal RemoveFileVersionAutoRecord(SPOTenantItemRecord resultItem,
                                             string remarks = "")
        {
            SiteUrl = resultItem.SiteUrl;
            if (String.IsNullOrWhiteSpace(remarks)) { Remarks = resultItem.ErrorMessage; }
            else { Remarks = remarks; }
            
            if (resultItem.List != null)
            {
                ListTitle = resultItem.List.Title;
            }
            
            if (resultItem.Item != null)
            {
                FileID = resultItem.Item.Id.ToString();
                FileTitle = resultItem.Item.File.Name;
                FilePath = resultItem.Item.File.ServerRelativeUrl;
            }
        }

        internal void AddFileDetails(ListItem oItem,
                                     string versionsDeletedCount = "",
                                     string versionsDeletedMB = "")
        {
            //FileVersionNo = (string)oItem["_UIVersionString"];
            FileVersionNo = oItem.File.UIVersionLabel;
            FileVersionsCount = ( oItem.File.Versions.Count + 1).ToString();

            //ItemSizeMb = Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2).ToString();
            ItemSizeMb = Math.Round(Convert.ToDouble(oItem.File.Length) / Math.Pow(1024, 2), 2).ToString();

            FieldLookupValue MTotalSize = (FieldLookupValue)oItem["SMTotalSize"];
            ItemSizeTotalMB = Math.Round(MTotalSize.LookupId / Math.Pow(1024, 2), 2).ToString();
            //if (oItem != null)
            //{
            //}
            //else { ItemSizeTotalMB = String.Empty; }

            DeletedVersionsCount = versionsDeletedCount;
            DeletedVersionsMB = versionsDeletedMB;
        }

    }

    public class RemoveFileVersionAutoParameters : ISolutionParameters
    {
        public bool ReportMode { get; set; } = true;
        public bool DeleteAll { get; set; } = false;
        public int VersionsToKeep { get; set; } = 500;
        public bool Recycle { get; set; } = true;

        //public SPOTenantListsParameters TListsParam {  get; set; }
        //public SPOItemsParameters ItemsParam {  get; set; }


        public SPOTenantSiteUrlsWithAccessParameters SitesAccParam { get; set; }
        public SPOListsParameters ListsParam { get; set; }
        public SPOItemsParameters ItemsParam { get; set; }
        internal SPOTenantItemsParameters TItemsParam
        {
            get { return new(SitesAccParam, ListsParam, ItemsParam); }
        }

        public RemoveFileVersionAutoParameters(SPOTenantSiteUrlsWithAccessParameters sitesParam,
                                               SPOListsParameters listsParam,
                                               SPOItemsParameters itemsParam)
        {
            SitesAccParam = sitesParam;
            ListsParam = listsParam;
            ItemsParam = itemsParam;
        }

        //public RemoveFileVersionAutoParameters(SPOTenantListsParameters listsParam,
        //                                       SPOItemsParameters itemsParam)
        //{
        //    TListsParam = listsParam;
        //    ItemsParam = itemsParam;
        //}
        internal void ParametersCheck()
        {
            if (!DeleteAll && string.IsNullOrWhiteSpace(VersionsToKeep.ToString()))
            {
                throw new Exception($"FORM INCOMPLETED: Number of versions to keep cannot be empty when no deleting all versions");

            }
        }
    }
}
