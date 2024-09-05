using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
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
            i => i.FileSystemObjectType,
            i => i["SMTotalSize"],

            i => i.Id,
            i => i.File.Name,
            i => i.File.ServerRelativeUrl,

            i => i.File.UIVersionLabel,
            i => i.File.Versions,
            i => i.File.Length,
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

            await foreach (var tenantItemRecord in new SPOTenantItemsCSOM(_logger, _appInfo, _param.TItemsParam).GetAsync())
            {
                _appInfo.IsCancelled();

                if (tenantItemRecord.Ex != null)
                {
                    RemoveFileVersionAutoRecord record = new(tenantItemRecord);
                    RecordCSV(record);
                    continue;
                }
                
                try
                {
                    await RemoveFileVersions(tenantItemRecord);
                }
                catch (Exception ex)
                {
                    _logger.ReportError(GetType().Name, "Item", (string)tenantItemRecord.Item["FileRef"], ex);
                    
                    RemoveFileVersionAutoRecord record = new(tenantItemRecord, ex.Message);
                    RecordCSV(record);
                }
            }
        }

        private async Task RemoveFileVersions(SPOTenantItemRecord resultItem)
        {
            _appInfo.IsCancelled();

            if (resultItem.Item == null || resultItem.List == null) { return; }
            if (resultItem.Item.FileSystemObjectType.ToString() == "Folder") { return; }

            _logger.LogTxt(GetType().Name, $"Start processing File '{resultItem.Item.File.Name}'");
            
            ClientContext clientContext = await _appInfo.GetContext(resultItem.SiteUrl);

            string fileURL = resultItem.Item.File.ServerRelativeUrl;
            Microsoft.SharePoint.Client.File file = clientContext.Web.GetFileByServerRelativePath(ResourcePath.FromDecodedUrl(fileURL));

            clientContext.Load(file, f => f.Exists, f => f.Versions.IncludeWithDefaultProperties(i => i.CreatedBy));
            FileVersionCollection fileVersionCollection = file.Versions;
            clientContext.ExecuteQueryRetry();

            if (_param.FileVersionParam.DeleteAll)
            {
                _logger.LogTxt(GetType().Name, $"Deleting all version '{resultItem.Item.File.Name}'");

                if (fileVersionCollection.Count < 1)
                {
                    _logger.LogTxt(GetType().Name, $"NO VERSIONS");
                    RemoveFileVersionAutoRecord record = new(resultItem, "No versions to delete");
                    record.AddFileDetails(resultItem.Item, "0", "0");
                    RecordCSV(record);
                    return;
                }
                else
                {
                    double fileSize = resultItem.Item.File.Length;
                    FieldLookupValue MTotalSize = (FieldLookupValue)resultItem.Item["SMTotalSize"];
                    
                    double versionsDeletedMB = Math.Round((MTotalSize.LookupId - fileSize) / Math.Pow(1024, 2), 2);

                    if (!_param.ReportMode)
                    {
                        fileVersionCollection.DeleteAll();
                        clientContext.ExecuteQueryRetry();
                    }

                    RemoveFileVersionAutoRecord record = new(resultItem);
                    record.AddFileDetails(resultItem.Item, fileVersionCollection.Count.ToString(), versionsDeletedMB.ToString());
                    RecordCSV(record);
                }
            }
            else
            {
                int numberVersionsToDelete = fileVersionCollection.Count - _param.FileVersionParam.KeepNumVersions;

                

                int errorsCount = 0;
                string remarks = String.Empty;
                int versionsDeletedCount = 0;
                double versionsDeletedMB = 0;

                for (int i = 0; i < numberVersionsToDelete; i++)
                {
                    _appInfo.IsCancelled();

                    FileVersion fileVersionToDelete = fileVersionCollection.ElementAt(i);
                    
                    if (_param.FileVersionParam.KeepCreatedAfter < fileVersionToDelete.Created)
                    {
                        break;
                    }

                    try
                    {
                        string versionIdentity = $"version '{fileVersionToDelete.VersionLabel}' from '{fileVersionToDelete.Url}'";

                        if (!_param.ReportMode)
                        {
                            if (_param.FileVersionParam.Recycle)
                            {
                                _logger.LogTxt(GetType().Name, $"Recycling {versionIdentity}");
                                fileVersionCollection.RecycleByID(fileVersionToDelete.ID);
                            }
                            else
                            {
                                _logger.LogTxt(GetType().Name, $"Deleting {versionIdentity}");
                                fileVersionCollection.DeleteByID(fileVersionToDelete.ID);
                            }
                            clientContext.ExecuteQueryRetry();
                        }

                        versionsDeletedMB += fileVersionToDelete.Length;
                        versionsDeletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.ReportError(GetType().Name, "Item", resultItem.Item.File.Name, ex);

                        RemoveFileVersionAutoRecord errorRecord = new(resultItem, ex.Message);
                        RecordCSV(errorRecord);

                        errorsCount++;
                    }
                }
                versionsDeletedMB = Math.Round(versionsDeletedMB / Math.Pow(1024, 2), 2);

                if (errorsCount > 0) { remarks = $"Error while deleting {errorsCount} versions"; }
                else if ((versionsDeletedCount + errorsCount) < 1) { remarks = $"No versions to delete"; }

                RemoveFileVersionAutoRecord record = new(resultItem, remarks);
                record.AddFileDetails(resultItem.Item, versionsDeletedCount.ToString(), versionsDeletedMB.ToString());
                RecordCSV(record);

            }
        }

        private void RecordCSV(RemoveFileVersionAutoRecord record)
        {
            _logger.RecordCSV(record);
        }

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

        internal RemoveFileVersionAutoRecord(SPOTenantItemRecord tenantItemRecord,
                                             string remarks = "")
        {
            SiteUrl = tenantItemRecord.SiteUrl;
            if (tenantItemRecord.Ex != null) { Remarks = tenantItemRecord.Ex.Message; }
            else { Remarks = remarks; }
            
            if (tenantItemRecord.List != null)
            {
                ListTitle = tenantItemRecord.List.Title;
            }
            
            if (tenantItemRecord.Item != null)
            {
                FileID = tenantItemRecord.Item.Id.ToString();
                FileTitle = tenantItemRecord.Item.File.Name;
                FilePath = tenantItemRecord.Item.File.ServerRelativeUrl;
            }
        }

        internal void AddFileDetails(ListItem oItem,
                                     string versionsDeletedCount = "",
                                     string versionsDeletedMB = "")
        {
            FileVersionNo = oItem.File.UIVersionLabel;
            FileVersionsCount = ( oItem.File.Versions.Count + 1).ToString();

            ItemSizeMb = Math.Round(Convert.ToDouble(oItem.File.Length) / Math.Pow(1024, 2), 2).ToString();

            FieldLookupValue MTotalSize = (FieldLookupValue)oItem["SMTotalSize"];
            ItemSizeTotalMB = Math.Round(Convert.ToDouble(MTotalSize.LookupId) / Math.Pow(1024, 2), 2).ToString();

            DeletedVersionsCount = versionsDeletedCount;
            DeletedVersionsMB = versionsDeletedMB;
        }

    }

    public class SPOFileVersionParameters : ISolutionParameters
    {
        public bool DeleteAll { get; set; } = false;
        public int KeepNumVersions { get; set; } = 500;
        public DateTime KeepCreatedAfter { get; set; } = DateTime.MinValue;
        public bool Recycle { get; set; } = true;

        public void ParametersCheck()
        {
            if (!DeleteAll && string.IsNullOrWhiteSpace(KeepNumVersions.ToString()))
            {
                throw new Exception($"FORM INCOMPLETED: Number of versions to keep cannot be empty when no deleting all versions");
            }
        }
    }

    public class RemoveFileVersionAutoParameters : ISolutionParameters
    {
        public bool ReportMode { get; set; } = true;

        public SPOFileVersionParameters FileVersionParam { get; set; }
        internal SPOTenantSiteUrlsWithAccessParameters SitesAccParam { get; set; }
        internal SPOListsParameters ListsParam { get; set; }
        internal SPOItemsParameters ItemsParam { get; set; }
        public SPOTenantItemsParameters TItemsParam
        {
            get { return new(SitesAccParam, ListsParam, ItemsParam); }
        }

        public RemoveFileVersionAutoParameters(
                        bool reportMode,
                        SPOFileVersionParameters fileVersionParam,
                        SPOTenantSiteUrlsWithAccessParameters sitesParam,
                        SPOListsParameters listsParam,
                        SPOItemsParameters itemsParam)
        {
            ReportMode = reportMode;
            FileVersionParam = fileVersionParam;
            SitesAccParam = sitesParam;
            ListsParam = listsParam;
            ItemsParam = itemsParam;
        }
    }
}
