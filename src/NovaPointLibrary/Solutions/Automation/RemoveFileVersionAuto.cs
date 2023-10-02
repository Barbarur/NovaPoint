using Microsoft.IdentityModel.Logging;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Publishing;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveFileVersionAuto : ISolution
    {
        public readonly static String s_SolutionName =  "Remove file versions";
        public readonly static String s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveFileVersionAuto";

        private RemoveFileVersionAutoParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (RemoveFileVersionAutoParameters)value; }
        }

        private Main _main;

        public RemoveFileVersionAuto(Commands.Authentication.AppInfo appInfo, Action<LogInfo> uiAddLog, ISolutionParameters parameters)
        {
            Parameters = parameters;

            _main = new(this, appInfo, uiAddLog);
        }

        public async Task RunAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_param.SiteUrl) && !_param.SiteAll)
                {
                    throw new Exception($"FORM INCOMPLETED: Site URL cannot be empty when no processing all sites");
                }
                else if (!_param.ListAll && String.IsNullOrWhiteSpace(_param.ListTitle))
                {
                    throw new Exception($"FORM INCOMPLETED: Library name cannot be empty when not processing all Libraries");
                }
                else if (_param.ListAll && !_param.ItemsAll)
                {
                    throw new Exception($"FORM ERROR: You cannot target specific Relative URL when running the solution across all Libraries");
                }
                else if (!_param.ItemsAll && String.IsNullOrWhiteSpace(_param.FolderRelativeUrl))
                {
                    throw new Exception($"FORM INCOMPLETED: Relative Path cannot be empty when not collecting all Files");
                }
                else if (!_param.DeleteAll && string.IsNullOrWhiteSpace(_param.VersionsToKeep.ToString()))
                {
                    throw new Exception($"FORM INCOMPLETED: Number of versions to keep cannot be empty when no deleting all versions");
                }
                else
                {
                    await RunScriptAsync();
                }
            }
            catch (Exception ex)
            {
                _main.ScriptFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _main.IsCancelled();

            SolutionProgressTracker progress;
            if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
            {
                Web oSite = await new SPOSiteCSOM(_main).Get(_param.SiteUrl);

                progress = new(_main, 1);
                await ProcessSite(oSite.Url, progress);
            }
            else
            {
                List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_main).Get(_param.SiteUrl, _param.IncludeShareSite, _param.IncludePersonalSite, _param.OnlyGroupIdDefined);

                progress = new(_main, collSiteCollections.Count);
                foreach (var oSiteCollection in collSiteCollections)
                {
                    await ProcessSite(oSiteCollection.Url, progress);
                    progress.ProgressUpdateReport();
                }
            }

            _main.ScriptFinish();
        }

        private async Task ProcessSite(string siteUrl, SolutionProgressTracker progress)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessSite";

            try
            {
                _main.AddLogToUI(methodName, $"Processing Site '{siteUrl}'");

                await new SPOSiteCollectionAdminCSOM(_main).Set(siteUrl, _param.AdminUPN);

                await ProcessLists(siteUrl, progress);

                await ProcessSubsites(siteUrl, progress);

                if (_param.RemoveAdmin)
                {
                    await new SPOSiteCollectionAdminCSOM(_main).Remove(siteUrl, _param.AdminUPN);
                }
            }
            catch (Exception ex)
            {
                _main.ReportError("Site", siteUrl, ex);

                AddRecord(siteUrl, remarks: ex.Message);
            }
        }

        private async Task ProcessSubsites(string siteUrl, SolutionProgressTracker progress)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessSubsites";

            if (!_param.IncludeSubsites) { return; }

            var collSubsites = await new SPOSubsiteCSOM(_main).Get(siteUrl);

            progress.IncreaseTotalCount(collSubsites.Count);
            foreach (var oSubsite in collSubsites)
            {
                _main.AddLogToUI(methodName, $"Processing Subsite '{oSubsite.Title}'");

                try
                {
                    await ProcessLists(oSubsite.Url, progress);
                }
                catch (Exception ex)
                {
                    _main.ReportError("Subsite", oSubsite.Url, ex);

                    AddRecord(oSubsite.Url, remarks: ex.Message);
                }

                progress.ProgressUpdateReport();
            }
        }

        private async Task ProcessLists(string siteUrl, SolutionProgressTracker parentPprogress)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessLists";

            var collList = await new SPOListCSOM(_main).Get(siteUrl, _param.ListTitle, _param.IncludeHiddenLists, _param.IncludeSystemLists);

            SolutionProgressTracker progress = new(parentPprogress, collList.Count);
            foreach (var oList in collList)
            {
                _main.IsCancelled();

                _main.AddLogToUI(methodName, $"Processing Library '{oList.Title}'");

                if(oList.BaseType != BaseType.DocumentLibrary)
                {
                    AddRecord(siteUrl, oList, remarks: "Skipped; This is not a Document Library");

                    continue;
                }

                try
                {
                    await ProcessItems(siteUrl, oList, progress);
                }
                catch (Exception ex)
                {
                    _main.ReportError(oList.BaseType.ToString(), oList.DefaultViewUrl, ex);

                    AddRecord(siteUrl, oList, remarks: ex.Message);
                }

                progress.ProgressUpdateReport();


            }
        }

        private readonly Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] _fileExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
        {
            i => i.HasUniqueRoleAssignments,
                    i => i["Author"],
                    i => i["Created"],
                    i => i["Editor"],
                    i => i["ID"],
                    i => i.FileSystemObjectType,
                    i => i["FileLeafRef"],
                    i => i["FileRef"],
                    i => i["File_x0020_Size"],
                    i => i["Modified"],
                    i => i["SMTotalSize"],
                    i => i["Title"],
                    i => i.Versions,
                    i => i["_UIVersionString"],
        };

        private async Task ProcessItems(string siteUrl, List oList, SolutionProgressTracker parentProgress)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessItems";

            SolutionProgressTracker progress = new(parentProgress, oList.ItemCount);

            var spoItem = new SPOItemCSOM(_main);
            await foreach (ListItem oItem in spoItem.Get(siteUrl, oList.Title, _fileExpressions))
            {
                _main.IsCancelled();
                _main.AddLogToTxt(methodName, $"Processing Item '{oList.Title}'");

                if (oItem.FileSystemObjectType.ToString() == "Folder")
                {
                    // NEED TEST; if Folder name change depending on being located in a Library or a List
                    AddRecord(siteUrl, oList, oItem, "NA", "NA");

                    continue;
                }
                else if (_param.DeleteAll)
                {
                    try
                    {
                        int versionsDeletedCount = oItem.Versions.Count - 1;
                        double itemSize = (double)Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);
                        var versionsDeletedMB = itemSize * versionsDeletedCount;

                        if (!_param.ReportMode)
                        {
                            await RemoveFileVersionAll(siteUrl, oItem);
                            //new RemoveSPOItemVersion(_logHelper, _appInfo, rootSiteAccessToken).CSOMAll(_param.SiteUrl, oItem);
                        }

                        AddRecord(siteUrl, oList, oItem, versionsDeletedCount.ToString(), versionsDeletedMB.ToString());
                    }
                    catch (Exception ex)
                    {
                        _main.ReportError("Item", (string)oItem["FileRef"], ex);

                        AddRecord(siteUrl, oList, oItem, remarks: ex.Message);
                    }
                }
                else
                {
                    FileVersionCollection fileVersionCollection = await spoItem.GetFileVersion(siteUrl, oItem);
                    //FileVersionCollection fileVersionCollection = new GetSPOFileVersion(_logHelper, _appInfo, rootSiteAccessToken).CSOM(_param.SiteUrl, oItem);

                    int collVersionsToDelete = fileVersionCollection.Count - _param.VersionsToKeep - 1;

                    if (collVersionsToDelete > 0)
                    {
                        int errorsCount = 0;
                        string remarks = String.Empty;

                        for (int i = 0; i < collVersionsToDelete; i++)
                        {
                            _main.IsCancelled();

                            try
                            {
                                if (!_param.ReportMode)
                                {
                                    await RemoveFileVersion(siteUrl, fileVersionCollection, i, _param.Recycle);
                                    //new RemoveSPOItemVersion(_logHelper, _appInfo, rootSiteAccessToken).CSOMSingle(_param.SiteUrl, fileVersionCollection, i, _param.Recycle);
                                }
                            }
                            catch (Exception ex)
                            {

                                _main.ReportError("Item", (string)oItem["FileRef"], ex);

                                AddRecord(siteUrl, oList, oItem, remarks: ex.Message);

                                errorsCount++;
                            }
                        }

                        int versionsDeletedCount = collVersionsToDelete - errorsCount;
                        var itemSize = Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);
                        var versionsDeletedMB = itemSize * versionsDeletedCount;

                        if (errorsCount > 0) { remarks = $"Error while deleting {errorsCount} versions"; }

                        AddRecord(siteUrl, oList, oItem, versionsDeletedCount.ToString(), versionsDeletedMB.ToString(), remarks);
                    }
                    else
                    {
                        AddRecord(siteUrl, oList, oItem, remarks: "No versions to delete");
                    }
                }

                progress.ProgressUpdateReport();
            }
        }

        private async Task RemoveFileVersionAll(string siteUrl, ListItem oItem)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.RemoveFileVersionAll";
            _main.AddLogToTxt(methodName, $"Start deleting all versions of the item '{oItem["FileRef"]}'");

            ClientContext clientContext = await _main.GetContext(siteUrl);

            string fileURL = (string)oItem["FileRef"];
            Microsoft.SharePoint.Client.File file = clientContext.Web.GetFileByServerRelativePath(ResourcePath.FromDecodedUrl(fileURL));

            clientContext.Load(file, f => f.Exists, f => f.Versions.IncludeWithDefaultProperties(i => i.CreatedBy));
            clientContext.ExecuteQueryRetry();

            if (file.Exists)
            {
                var versions = file.Versions;

                _main.AddLogToTxt(methodName, $"Start deleting all the versions from '{fileURL}'");
                
                versions.DeleteAll();
                clientContext.ExecuteQueryRetry();
                
                _main.AddLogToTxt(methodName, $"Finish deleting all the versions from '{fileURL}'");
            }
            else
            {
                throw new Exception($"File '{fileURL}' doesn't exist");
            }

            _main.AddLogToTxt(methodName, $"Finish deleting all versions of the item '{oItem["FileRef"]}'");
        }

        private async Task RemoveFileVersion(string siteUrl, FileVersionCollection fileVersionCollection, int index, bool recycle)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.RemoveItemVersion";
            FileVersion fileVersionToDelete = fileVersionCollection.ElementAt(index);
            _main.AddLogToTxt(methodName, $"Start removing file version '{fileVersionToDelete.Url}'");

            ClientContext clientContext = await _main.GetContext(siteUrl);

            if (recycle)
            {
                _main.AddLogToTxt(methodName, $"Start recycling version {fileVersionToDelete.ID} from '{fileVersionToDelete.Url}'");
                fileVersionCollection.RecycleByID(fileVersionToDelete.ID);
            }
            else
            {
                _main.AddLogToTxt(methodName, $"Start deleting version {fileVersionToDelete.ID} from '{fileVersionToDelete.Url}'");
                fileVersionCollection.DeleteByID(fileVersionToDelete.ID);
            }
            clientContext.ExecuteQueryRetry();

            _main.AddLogToTxt(methodName, $"Finish removing file version '{fileVersionToDelete.Url}'");
        }



        //private async Task RunScriptAsync()
        //{
        //    _appInfo.IsCancelled();
        //    _logHelper.ScriptStartNotice();

        //    string rootUrl = _param.SiteUrl.Substring(0, _param.SiteUrl.IndexOf(".com") + 4);
        //    string rootSiteAccessToken = await new GetAccessToken(_logHelper, _appInfo).SpoInteractiveAsync(rootUrl);

        //    List<List> collList = new();
        //    if (_param.ListAll)
        //    {
        //        var allList = new GetSPOList(_logHelper, _appInfo, rootSiteAccessToken).CSOMAll(_param.SiteUrl, _param.IncludeSystemLists, _param.IncludeResourceLists);
        //        var librariesOnly = allList.Where(l => l.BaseType == BaseType.DocumentLibrary).ToList();
        //        collList.AddRange( librariesOnly );
        //    }
        //    else
        //    {
        //        var targetList = new GetSPOList(_logHelper, _appInfo, rootSiteAccessToken).CSOMSingleStandard(_param.SiteUrl, _param.ListName);
        //        if (targetList.BaseType != BaseType.DocumentLibrary)
        //        {
        //            throw new Exception($"Error: '{_param.ListName}' is not a Document Library");
        //        }
        //        collList.Add(targetList);
        //    }


        //    ProgressTracker progress = new(_logHelper, collList.Count);
        //    foreach (List oList in collList)
        //    {
        //        _appInfo.IsCancelled();

        //        progress.MainReportProgress($"Processing Library '{oList.Title}'");

        //        try
        //        {
        //            List<ListItem> collItems = new GetSPOItem(_logHelper, _appInfo, rootSiteAccessToken).CSOM(_param.SiteUrl, oList, _fileExpressions);
        //            if (!_param.ItemsAll)
        //            {
        //                collItems = collItems.Where(i => ((string)i["FileRef"]).Contains(_param.RelativePath)).ToList();
        //            }

        //            progress.SubTaskProgressReset(collItems.Count);
        //            foreach (ListItem oItem in collItems)
        //            {
        //                _appInfo.IsCancelled();

        //                if (oItem.FileSystemObjectType.ToString() == "Folder") { }
        //                else if (_param.DeleteAll)
        //                {
        //                    try
        //                    {
        //                        int versionsDeletedCount = oItem.Versions.Count - 1;
        //                        double itemSize = (double)Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);
        //                        var versionsDeletedMB = itemSize * versionsDeletedCount;

        //                        if (!_param.ReportMode)
        //                        {
        //                            new RemoveSPOItemVersion(_logHelper, _appInfo, rootSiteAccessToken).CSOMAll(_param.SiteUrl, oItem);
        //                        }

        //                        AddItemRecord(oList, oItem, versionsDeletedCount.ToString(), versionsDeletedMB.ToString());
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        _logHelper.AddLogToUI($"Error processing Item '{oItem["FileRef"]}'");
        //                        _logHelper.AddLogToTxt($"Exception: {ex.Message}");
        //                        _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");

        //                        AddItemRecord(oList, oItem, remarks: ex.Message);
        //                    }
        //                }
        //                else
        //                {
        //                    FileVersionCollection fileVersionCollection = new GetSPOFileVersion(_logHelper, _appInfo, rootSiteAccessToken).CSOM(_param.SiteUrl, oItem);

        //                    int collVersionsToDelete = fileVersionCollection.Count - _param.VersionsToKeep - 1;

        //                    if (collVersionsToDelete > 0)
        //                    {
        //                        int errorsCount = 0;
        //                        string remarks = String.Empty;

        //                        for (int i = 0; i < collVersionsToDelete; i++)
        //                        {
        //                            _appInfo.IsCancelled();

        //                            try
        //                            {

        //                                if (!_param.ReportMode)
        //                                {
        //                                    new RemoveSPOItemVersion(_logHelper, _appInfo, rootSiteAccessToken).CSOMSingle(_param.SiteUrl, fileVersionCollection, i, _param.Recycle);
        //                                }
        //                            }
        //                            catch (Exception ex)
        //                            {

        //                                _logHelper.AddLogToUI($"Error processing Item '{oItem["FileRef"]}'");
        //                                _logHelper.AddLogToTxt($"Exception: {ex.Message}");
        //                                _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");

        //                                AddItemRecord(oList, oItem, remarks: ex.Message);

        //                                errorsCount++;
        //                            }
        //                        }

        //                        int versionsDeletedCount = collVersionsToDelete - errorsCount;
        //                        var itemSize = Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);
        //                        var versionsDeletedMB = itemSize * versionsDeletedCount;

        //                        if (errorsCount > 0) { remarks = $"Error while deleting {errorsCount} versions"; }

        //                        AddItemRecord(oList, oItem, versionsDeletedCount.ToString(), versionsDeletedMB.ToString(), remarks);
        //                    }
        //                    else
        //                    {
        //                        AddItemRecord(oList, oItem, remarks: "No versions to delete");
        //                    }
        //                }

        //                progress.SubTaskCounterIncrement();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logHelper.AddLogToUI($"Error processing Library '{oList.Title}'");
        //            _logHelper.AddLogToTxt($"Exception: {ex.Message}");
        //            _logHelper.AddLogToTxt($"Trace: {ex.StackTrace}");

        //            AddItemRecord(oList, null, remarks: ex.Message);
        //        }

        //        progress.MainCounterIncrement();

        //    }

        //    _logHelper.ScriptFinishSuccessfulNotice();
        //}


        private void AddRecord(string siteUrl,
                               Microsoft.SharePoint.Client.List? oList = null,
                               Microsoft.SharePoint.Client.ListItem? oItem = null,
                               string versionsDeletedCount = "NA",
                               string versionsDeletedMB = "NA",
                               string remarks = "")
        {
            
            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;
            recordItem.ListTitle = oList != null ? oList.Title : String.Empty;
            recordItem.ListType = oList != null ? oList.BaseType.ToString() : String.Empty;

            recordItem.ItemID = oItem != null ? oItem["ID"] : string.Empty;
            recordItem.ItemName = oItem != null ? oItem["Title"] : string.Empty;
            recordItem.ItemPath = oItem != null ? oItem["FileRef"] : string.Empty;

            recordItem.ItemVersion = oItem != null ? oItem["_UIVersionString"] : string.Empty;
            recordItem.ItemVersionsCount = oItem != null ? oItem.Versions.Count.ToString() : string.Empty; ;

            recordItem.ItemSizeMb = oItem != null ? Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2).ToString() : string.Empty;

            if( oItem != null)
            {
                FieldLookupValue MTotalSize =(FieldLookupValue)oItem["SMTotalSize"];
                recordItem.ItemSizeTotalMB = Math.Round(MTotalSize.LookupId / Math.Pow(1024, 2), 2);
            }
            else { recordItem.ItemSizeTotalMB = String.Empty; }

            recordItem.DeletedVersionsCount = versionsDeletedCount;
            recordItem.DeletedVersionsMB = versionsDeletedMB;

            recordItem.Remarks = remarks;

            _main.AddRecordToCSV(recordItem);
        }
    }

    public class RemoveFileVersionAutoParameters : ISolutionParameters
    {
        public string AdminUPN { get; set; } = String.Empty;
        public bool RemoveAdmin { get; set; } = false;

        public bool SiteAll { get; set; } = true;
        public bool IncludePersonalSite { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool OnlyGroupIdDefined { get; set; } = false;
        public string SiteUrl { get; set; } = String.Empty;
        public bool IncludeSubsites { get; set; } = false;

        public bool ListAll { get; set; } = true;
        public bool IncludeHiddenLists { get; set; } = false;
        public bool IncludeSystemLists { get; set; } = false;
        public string ListTitle { get; set; } = String.Empty;

        public bool ItemsAll { get; set; } = true;
        public string FolderRelativeUrl { get; set; } = String.Empty;

        public bool DeleteAll { get; set; } = false;
        public int VersionsToKeep { get; set; } = 500;
        public bool Recycle { get; set; } = true;

        public bool ReportMode { get; set; } = true;
    }
}
