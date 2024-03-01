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

        private RemoveFileVersionAutoParameters _param;
        private readonly NPLogger _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private readonly Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] _fileExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
        {
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
            i => i.Versions,
            i => i["_UIVersionString"],
        };

        public RemoveFileVersionAuto(RemoveFileVersionAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            _param = parameters;
            _param.ItemsParam.FileExpresions = _fileExpressions;
            _param.TListsParam.ListParam.IncludeLibraries = true;
            _param.TListsParam.ListParam.IncludeLists = false;
            _param.TListsParam.ListParam.IncludeHiddenLists = false;
            _param.TListsParam.ListParam.IncludeSystemLists = false;

            _logger = new(uiAddLog, this.GetType().Name, _param);
            _appInfo = new(_logger, cancelTokenSource);
        }

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
        }

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
                    await ProcessItems(results.SiteUrl, results.List, results.Progress);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(results.List.BaseType.ToString(), results.List.DefaultViewUrl, ex);
                    AddRecord(results.SiteUrl, results.List, remarks: ex.Message);
                }
            }
        }

        private async Task ProcessItems(string siteUrl, List oList, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();

            ProgressTracker progress = new(parentProgress, oList.ItemCount);

            var spoItem = new SPOListItemCSOM(_logger, _appInfo);
            await foreach (ListItem oItem in spoItem.GetAsync(siteUrl, oList, _param.ItemsParam))
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
                var versionsDeletedMB = Math.Round((Convert.ToDouble(oItem["File_x0020_Size"]) * numberVersionsToDelete) / Math.Pow(1024, 2), 2);

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
                    var versionsDeletedMB = Math.Round( (Convert.ToDouble(oItem["File_x0020_Size"]) * versionsDeletedCount) / Math.Pow(1024, 2), 2);

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

            _logger.DynamicCSV(recordItem);
        }
    }

    public class RemoveFileVersionAutoParameters : ISolutionParameters
    {
        public bool ReportMode { get; set; } = true;
        public bool DeleteAll { get; set; } = false;
        public int VersionsToKeep { get; set; } = 500;
        public bool Recycle { get; set; } = true;

        public SPOTenantListsParameters TListsParam {  get; set; }
        public SPOItemsParameters ItemsParam {  get; set; }

        public RemoveFileVersionAutoParameters(SPOTenantListsParameters listsParam,
                                               SPOItemsParameters itemsParam)
        {
            TListsParam = listsParam;
            ItemsParam = itemsParam;
        }
        internal void ParametersCheck()
        {
            if (!DeleteAll && string.IsNullOrWhiteSpace(VersionsToKeep.ToString()))
            {
                throw new Exception($"FORM INCOMPLETED: Number of versions to keep cannot be empty when no deleting all versions");

            }
        }
    }
}
