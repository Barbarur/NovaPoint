using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
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

        private RemoveFileVersionAutoParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (RemoveFileVersionAutoParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

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

        public RemoveFileVersionAuto(RemoveFileVersionAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            Parameters = parameters;
            _param.FileExpresions = _fileExpressions;
            _logger = new(uiAddLog, this.GetType().Name, parameters);
            _appInfo = new(_logger, cancelTokenSource);
        }

        //private Main _main;

        //public RemoveFileVersionAuto(Commands.Authentication.AppInfo appInfo, Action<LogInfo> uiAddLog, ISolutionParameters parameters)
        //{
        //    Parameters = parameters;

        //    _main = new(this, appInfo, uiAddLog);
        //}

        public async Task RunAsync()
        {
            try
            {
                await RunScriptAsync();

                _logger.ScriptFinish();

            }
            catch (Exception ex)
            {
                _logger.ScriptFinish(ex);
            }
            //try
            //{
            //    if (string.IsNullOrWhiteSpace(_param.SiteUrl) && !_param.SiteAll)
            //    {
            //        throw new Exception($"FORM INCOMPLETED: Site URL cannot be empty when no processing all sites");
            //    }
            //    else if (!_param.ListAll && String.IsNullOrWhiteSpace(_param.ListTitle))
            //    {
            //        throw new Exception($"FORM INCOMPLETED: Library name cannot be empty when not processing all Libraries");
            //    }
            //    else if (_param.ListAll && !_param.ItemsAll)
            //    {
            //        throw new Exception($"FORM ERROR: You cannot target specific Relative URL when running the solution across all Libraries");
            //    }
            //    else if (!_param.ItemsAll && String.IsNullOrWhiteSpace(_param.FolderRelativeUrl))
            //    {
            //        throw new Exception($"FORM INCOMPLETED: Relative Path cannot be empty when not collecting all Files");
            //    }
            //    else if (!_param.DeleteAll && string.IsNullOrWhiteSpace(_param.VersionsToKeep.ToString()))
            //    {
            //        throw new Exception($"FORM INCOMPLETED: Number of versions to keep cannot be empty when no deleting all versions");
            //    }
            //    else
            //    {
            //        await RunScriptAsync();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _main.ScriptFinish(ex);
            //}
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            await foreach (var results in new SPOTenantListsCSOM(_logger, _appInfo, _param).GetListsAsync())
            {
                _appInfo.IsCancelled();

                if (!String.IsNullOrWhiteSpace(results.Remarks) || results.List == null)
                {
                    AddRecord(results.SiteUrl, results.List, remarks: results.Remarks);
                    continue;
                }

                try
                {
                    await ProcessItems(results.SiteUrl, results.List, results.Progress);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(results.List.BaseType.ToString(), results.List.DefaultViewUrl, ex);
                    AddRecord(results.SiteUrl, results.List, remarks: ex.Message);
                }
            }


            //_main.IsCancelled();

            //ProgressTracker progress;
            //if (!String.IsNullOrWhiteSpace(_param.SiteUrl))
            //{
            //    Web oSite = await new SPOSiteCSOM(_main).GetToDeprecate(_param.SiteUrl);

            //    progress = new(_main, 1);
            //    await ProcessSite(oSite.Url, progress);
            //}
            //else
            //{
            //    List<SiteProperties> collSiteCollections = await new SPOSiteCollectionCSOM(_main).GetDeprecated(_param.SiteUrl, _param.IncludeShareSite, _param.IncludePersonalSite, _param.OnlyGroupIdDefined);

            //    progress = new(_main, collSiteCollections.Count);
            //    foreach (var oSiteCollection in collSiteCollections)
            //    {
            //        await ProcessSite(oSiteCollection.Url, progress);
            //        progress.ProgressUpdateReport();
            //    }
            //}

            //_main.ScriptFinish();
        }

        //private async Task ProcessSite(string siteUrl, ProgressTracker progress)
        //{
        //    _main.IsCancelled();
        //    string methodName = $"{GetType().Name}.ProcessSite";

        //    try
        //    {
        //        _main.AddLogToUI(methodName, $"Processing Site '{siteUrl}'");

        //        await new SPOSiteCollectionAdminCSOM(_main).SetDEPRECATED(siteUrl, _param.AdminUPN);

        //        await ProcessLists(siteUrl, progress);

        //        await ProcessSubsites(siteUrl, progress);

        //        if (_param.RemoveAdmin)
        //        {
        //            await new SPOSiteCollectionAdminCSOM(_main).RemoveDEPRECATED(siteUrl, _param.AdminUPN);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _main.ReportError("Site", siteUrl, ex);

        //        AddRecord(siteUrl, remarks: ex.Message);
        //    }
        //}

        //private async Task ProcessSubsites(string siteUrl, ProgressTracker progress)
        //{
        //    _main.IsCancelled();
        //    string methodName = $"{GetType().Name}.ProcessSubsites";

        //    if (!_param.IncludeSubsites) { return; }

        //    var collSubsites = await new SPOSubsiteCSOM(_main).GetDEPRECATED(siteUrl);

        //    progress.IncreaseTotalCount(collSubsites.Count);
        //    foreach (var oSubsite in collSubsites)
        //    {
        //        _main.AddLogToUI(methodName, $"Processing Subsite '{oSubsite.Title}'");

        //        try
        //        {
        //            await ProcessLists(oSubsite.Url, progress);
        //        }
        //        catch (Exception ex)
        //        {
        //            _main.ReportError("Subsite", oSubsite.Url, ex);

        //            AddRecord(oSubsite.Url, remarks: ex.Message);
        //        }

        //        progress.ProgressUpdateReport();
        //    }
        //}

        //private async Task ProcessLists(string siteUrl, ProgressTracker parentPprogress)
        //{
        //    _main.IsCancelled();
        //    string methodName = $"{GetType().Name}.ProcessLists";

        //    var collList = await new SPOListCSOM(_main).GetDEPRECATED(siteUrl, _param.ListTitle, _param.IncludeHiddenLists, _param.IncludeSystemLists);

        //    ProgressTracker progress = new(parentPprogress, collList.Count);
        //    foreach (var oList in collList)
        //    {
        //        _main.IsCancelled();

        //        _main.AddLogToUI(methodName, $"Processing Library '{oList.Title}'");

        //        if(oList.BaseType != BaseType.DocumentLibrary)
        //        {
        //            AddRecord(siteUrl, oList, remarks: "Skipped; This is not a Document Library");

        //            continue;
        //        }

        //        try
        //        {
        //            await ProcessItems(siteUrl, oList, progress);
        //        }
        //        catch (Exception ex)
        //        {
        //            _main.ReportError(oList.BaseType.ToString(), oList.DefaultViewUrl, ex);

        //            AddRecord(siteUrl, oList, remarks: ex.Message);
        //        }

        //        progress.ProgressUpdateReport();


        //    }
        //}


        private async Task ProcessItems(string siteUrl, List oList, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}.ProcessItems";


            //Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] fileExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
            //{
            //    i => i.HasUniqueRoleAssignments,
            //    i => i["Author"],
            //    i => i["Created"],
            //    i => i["Editor"],
            //    i => i["ID"],
            //    i => i.FileSystemObjectType,
            //    i => i["FileLeafRef"],
            //    i => i["FileRef"],
            //    i => i["File_x0020_Size"],
            //    i => i["Modified"],
            //    i => i["SMTotalSize"],
            //    i => i["Title"],
            //    i => i.Versions,
            //    i => i["_UIVersionString"],
            //};


            ProgressTracker progress = new(parentProgress, oList.ItemCount);

            var spoItem = new SPOListItemCSOM(_logger, _appInfo);
            await foreach (ListItem oItem in spoItem.GetAsync(siteUrl, oList, _param))
            {
                if (oItem.FileSystemObjectType.ToString() == "Folder") { continue; }

                try
                {
                    await RemoveFileVersions(siteUrl, oList, oItem);
                }
                catch (Exception ex)
                {
                    _logger.ReportError("Item", (string)oItem["FileRef"], ex);

                    AddRecord(siteUrl, oList, oItem, remarks: ex.Message);
                }

                progress.ProgressUpdateReport();
            }
        }

        private async Task RemoveFileVersions(string siteUrl, List oList, ListItem oItem)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"Start processing File '{oItem["FileLeafRef"]}'");
            
            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            string fileURL = (string)oItem["FileRef"];
            Microsoft.SharePoint.Client.File file = clientContext.Web.GetFileByServerRelativePath(ResourcePath.FromDecodedUrl(fileURL));

            clientContext.Load(file, f => f.Exists, f => f.Versions.IncludeWithDefaultProperties(i => i.CreatedBy));
            FileVersionCollection fileVersionCollection = file.Versions;
            clientContext.ExecuteQueryRetry();

            if (_param.DeleteAll)
            {
                _logger.LogTxt(GetType().Name, $"Deleting all version '{oItem["FileLeafRef"]}'");

                int numberVersionsToDelete = oItem.Versions.Count - 1;
                double itemSize = (double)Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);
                var versionsDeletedMB = itemSize * numberVersionsToDelete;

                if (!_param.ReportMode)
                {
                    fileVersionCollection.DeleteAll();
                    clientContext.ExecuteQueryRetry();
                }

                AddRecord(siteUrl, oList, oItem, numberVersionsToDelete.ToString(), versionsDeletedMB.ToString());
            }
            else
            {
                int numberVersionsToDelete = oItem.Versions.Count - _param.VersionsToKeep - 1;

                if (numberVersionsToDelete > 0) 
                {
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
                            _logger.ReportError("Item", (string)oItem["FileRef"], ex);

                            AddRecord(siteUrl, oList, oItem, remarks: ex.Message);

                            errorsCount++;
                        }
                    }

                    int versionsDeletedCount = numberVersionsToDelete - errorsCount;
                    var itemSize = Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);
                    var versionsDeletedMB = itemSize * versionsDeletedCount;

                    if (errorsCount > 0) { remarks = $"Error while deleting {errorsCount} versions"; }

                    AddRecord(siteUrl, oList, oItem, versionsDeletedCount.ToString(), versionsDeletedMB.ToString(), remarks);

                }
                else { AddRecord(siteUrl, oList, oItem, remarks: "No versions to delete"); }
            }
        }


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

            recordItem.FileID = oItem != null ? oItem["ID"] : string.Empty;
            recordItem.FileName = oItem != null ? oItem["FileLeafRef"] : string.Empty;
            recordItem.FilePath = oItem != null ? oItem["FileRef"] : string.Empty;

            recordItem.FileVersionNo = oItem != null ? oItem["_UIVersionString"] : string.Empty;
            recordItem.FileVersionsCount = oItem != null ? oItem.Versions.Count.ToString() : string.Empty; ;

            recordItem.ItemSizeMb = oItem != null ? Math.Round( Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2 ).ToString() : string.Empty;

            if ( oItem != null)
            {
                FieldLookupValue MTotalSize =(FieldLookupValue)oItem["SMTotalSize"];
                recordItem.ItemSizeTotalMB = Math.Round(MTotalSize.LookupId / Math.Pow(1024, 2), 2);
            }
            else { recordItem.ItemSizeTotalMB = String.Empty; }

            recordItem.DeletedVersionsCount = versionsDeletedCount;
            recordItem.DeletedVersionsMB = versionsDeletedMB;

            recordItem.Remarks = remarks;

            _logger.RecordCSV(recordItem);
        }
    }

    public class RemoveFileVersionAutoParameters : SPOTenantItemsParameters
    {
        public bool DeleteAll { get; set; } = false;
        public int VersionsToKeep { get; set; } = 500;
        public bool Recycle { get; set; } = true;

        public bool ReportMode { get; set; } = true;

        internal new void ParametersCheck()
        {
            base.ParametersCheck();
            if (!DeleteAll && string.IsNullOrWhiteSpace(VersionsToKeep.ToString()))
            {
                throw new Exception($"FORM INCOMPLETED: Number of versions to keep cannot be empty when no deleting all versions");

            }
        }
    }
}
