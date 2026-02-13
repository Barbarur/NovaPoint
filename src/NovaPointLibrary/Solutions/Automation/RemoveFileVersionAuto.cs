using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class RemoveFileVersionAuto : ISolution
    {
        public readonly static String s_SolutionName =  "Remove file versions";
        public readonly static String s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-RemoveFileVersionAuto";

        private ContextSolution _ctx;
        private RemoveFileVersionAutoParameters _param;

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

        private RemoveFileVersionAuto(ContextSolution context, RemoveFileVersionAutoParameters parameters)
        {
            _ctx = context;

            parameters.ItemsParam.FileExpressions = _fileExpressions;
            parameters.ListsParam.IncludeLibraries = true;
            parameters.ListsParam.IncludeLists = false;
            parameters.ListsParam.IncludeHiddenLists = false;
            parameters.ListsParam.IncludeSystemLists = false;
            _param = parameters;

            Dictionary<Type, string> solutionReports = new()
            {
                { typeof(RemoveFileVersionAutoRecord), "Report" },
            };
            _ctx.DbHandler.AddSolutionReports(solutionReports);
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new RemoveFileVersionAuto(context, (RemoveFileVersionAutoParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            await foreach (var tenantItemRecord in new SPOTenantItemsCSOM(_ctx.Logger, _ctx.AppClient, _param.TItemsParam).GetAsync())
            {
                _ctx.AppClient.IsCancelled();

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
                    _ctx.Logger.Error(GetType().Name, "Item", (string)tenantItemRecord.Item["FileRef"], ex);
                    
                    RemoveFileVersionAutoRecord record = new(tenantItemRecord, ex.Message);
                    RecordCSV(record);
                }
            }
        }

        private async Task RemoveFileVersions(SPOTenantItemRecord resultItem)
        {
            _ctx.AppClient.IsCancelled();

            if (resultItem.Item == null || resultItem.List == null) { return; }
            if (resultItem.Item.FileSystemObjectType.ToString() == "Folder") { return; }

            _ctx.Logger.Info(GetType().Name, $"Start processing File '{resultItem.Item.File.Name}'");
            
            ClientContext clientContext = await _ctx.AppClient.GetContext(resultItem.SiteUrl);

            string fileURL = resultItem.Item.File.ServerRelativeUrl;
            Microsoft.SharePoint.Client.File file = clientContext.Web.GetFileByServerRelativePath(ResourcePath.FromDecodedUrl(fileURL));

            clientContext.Load(file, f => f.Exists, f => f.Versions.IncludeWithDefaultProperties(i => i.CreatedBy));
            FileVersionCollection fileVersionCollection = file.Versions;
            clientContext.ExecuteQueryRetry();

            if (_param.FileVersionParam.DeleteAll)
            {
                _ctx.Logger.Info(GetType().Name, $"Deleting all version '{resultItem.Item.File.Name}'");

                if (fileVersionCollection.Count < 1)
                {
                    _ctx.Logger.Info(GetType().Name, $"NO VERSIONS");
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
                    _ctx.AppClient.IsCancelled();

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
                                _ctx.Logger.Info(GetType().Name, $"Recycling {versionIdentity}");
                                fileVersionCollection.RecycleByID(fileVersionToDelete.ID);
                            }
                            else
                            {
                                _ctx.Logger.Info(GetType().Name, $"Deleting {versionIdentity}");
                                fileVersionCollection.DeleteByID(fileVersionToDelete.ID);
                            }
                            clientContext.ExecuteQueryRetry();
                        }

                        versionsDeletedMB += fileVersionToDelete.Length;
                        versionsDeletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _ctx.Logger.Error(GetType().Name, "Item", resultItem.Item.File.Name, ex);

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
            _ctx.DbHandler.WriteRecord(record);
        }

    }

    public class RemoveFileVersionAutoRecord : ISolutionRecord
    {
        public string SiteUrl { get; set; } = String.Empty;
        public string ListTitle { get; set; } = String.Empty;

        public string FileID { get; set; } = String.Empty;
        public string FileTitle { get; set; } = String.Empty;
        public string FilePath { get; set; } = String.Empty;

        public string FileVersionNo { get; set; } = String.Empty;
        public string FileVersionsCount { get; set; } = String.Empty;
        public string ItemSizeMb { get; set; } = String.Empty;
        public string ItemSizeTotalMB { get; set; } = String.Empty;
        public string DeletedVersionsCount { get; set; } = String.Empty;
        public string DeletedVersionsMB { get; set; } = String.Empty;

        public string Remarks { get; set; } = String.Empty;

        public RemoveFileVersionAutoRecord() { }

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
        public bool ReportMode { get; set; }

        public SPOFileVersionParameters FileVersionParam { get; set; }
        internal SPOAdminAccessParameters AdminAccess;
        internal SPOTenantSiteUrlsParameters SiteParam;
        public SPOTenantSiteUrlsWithAccessParameters SiteAccParam
        {
            get
            {
                return new(AdminAccess, SiteParam);
            }
        }
        internal SPOListsParameters ListsParam { get; set; }
        internal SPOItemsParameters ItemsParam { get; set; }
        public SPOTenantItemsParameters TItemsParam
        {
            get { return new(SiteAccParam, ListsParam, ItemsParam); }
        }

        public RemoveFileVersionAutoParameters(
                        bool reportMode,
                        SPOFileVersionParameters fileVersionParam,
                        SPOAdminAccessParameters adminAccess,
                        SPOTenantSiteUrlsParameters siteParam,
                        SPOListsParameters listsParam,
                        SPOItemsParameters itemsParam)
        {
            ReportMode = reportMode;
            FileVersionParam = fileVersionParam;
            AdminAccess = adminAccess;
            SiteParam = siteParam;
            ListsParam = listsParam;
            ItemsParam = itemsParam;
        }
    }
}
