using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.News.DataModel;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Site;
using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Report
{
    public class ItemReport : ISolution
    {
        public static readonly string s_SolutionName = "Files and Items report";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Report-ItemReport";

        private ItemReportParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (ItemReportParameters)value; }
        }

        private Main _main;

        public ItemReport(Commands.Authentication.AppInfo appInfo, Action<LogInfo> uiAddLog, ISolutionParameters parameters)
        {
            Parameters = parameters;

            _main = new(this, appInfo, uiAddLog);
        }

        public async Task RunAsync()
        {
            try
            {
                if (String.IsNullOrWhiteSpace(_param.AdminUPN))
                {
                    throw new Exception("FORM INCOMPLETED: Admin UPN cannot be empty.");
                }
                else if (string.IsNullOrWhiteSpace(_param.SiteUrl) && !_param.SiteAll)
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
                
                _main.AddLogToUI(methodName, $"Processing '{oList.BaseType}' - '{oList.Title}'");

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

        private readonly Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] _itemExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
        {
            i => i.HasUniqueRoleAssignments,
                    i => i.AttachmentFiles,
                    i => i["Author"],
                    i => i["Created"],
                    i => i["Editor"],
                    i => i["ID"],
                    i => i.FileSystemObjectType,
                    i => i["FileLeafRef"],
                    i => i["FileRef"],
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

            Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] currentExpressions;

            if (oList.BaseType == BaseType.DocumentLibrary)
            {
                currentExpressions = _fileExpressions;
            }
            else if (oList.BaseType == BaseType.GenericList)
            {
                currentExpressions = _itemExpressions;
            }
            else
            {
                AddRecord(siteUrl, oList, remarks: "This is not a List neither a Library");

                return;
            }

            SolutionProgressTracker progress = new(parentProgress, oList.ItemCount);

            var spoItem = new SPOItemCSOM(_main);
            await foreach (ListItem oItem in spoItem.Get(siteUrl, oList.Title, currentExpressions))
            {
                _main.IsCancelled();

                try
                {
                    if (oItem.FileSystemObjectType.ToString() == "Folder")
                    {
                        // NEED TEST; if Folder name change depending on being located in a Library or a List
                        AddRecord(siteUrl, oList, oItem, (string)oItem["FileLeafRef"], "0", "0");

                        continue;
                    }

                    if (oList.BaseType == BaseType.DocumentLibrary)
                    {
                        string itemName = (string)oItem["FileLeafRef"];

                        float itemSizeMb = (float)Math.Round(Convert.ToDouble(oItem["File_x0020_Size"]) / Math.Pow(1024, 2), 2);

                        FieldLookupValue FileSizeTotalBytes = (FieldLookupValue)oItem["SMTotalSize"];
                        float itemSizeTotalMb = (float)Math.Round(FileSizeTotalBytes.LookupId / Math.Pow(1024, 2), 2);

                        AddRecord(siteUrl, oList, oItem, itemName, itemSizeMb.ToString(), itemSizeTotalMb.ToString(), "");
                    }
                    else if (oList.BaseType == BaseType.GenericList)
                    {
                        string itemName = (string)oItem["Title"];

                        int itemSizeTotalBytes = 0;
                        foreach (var oAttachment in oItem.AttachmentFiles)
                        {
                            var oFileAttachment = await spoItem.GetAttachmentFile(siteUrl, oAttachment.ServerRelativeUrl);

                            itemSizeTotalBytes += (int)oFileAttachment.Length;
                        }
                        float itemSizeTotalMb = (float)Math.Round(itemSizeTotalBytes / Math.Pow(1024, 2), 2);

                        AddRecord(siteUrl, oList, oItem, itemName, itemSizeTotalMb.ToString(), itemSizeTotalMb.ToString(), "");
                    }
                }
                catch (Exception ex)
                {
                    _main.ReportError("Item", (string)oItem["FileRef"], ex);

                    AddRecord(siteUrl, oList, oItem, remarks: ex.Message);
                }

                progress.ProgressUpdateReport();
            }
        }


        private void AddRecord(string siteUrl,
                               Microsoft.SharePoint.Client.List? oList = null,
                               Microsoft.SharePoint.Client.ListItem? oItem = null,
                               string itemName = "",
                               string itemSizeMb = "",
                               string itemSizeTotalMb = "",
                               string remarks = "")
        {

            dynamic recordItem = new ExpandoObject();
            recordItem.SiteUrl = siteUrl;
            recordItem.ListTitle = oList != null ? oList.Title : String.Empty;
            recordItem.ListType = oList != null ? oList.BaseType.ToString() : String.Empty;

            recordItem.ItemID = oItem != null ? oItem["ID"] : string.Empty;
            recordItem.ItemName = oItem != null ? itemName : string.Empty;
            recordItem.ItemPath = oItem != null ? oItem["FileRef"] : string.Empty;
            recordItem.ItemType = oItem != null ? oItem.FileSystemObjectType.ToString() : string.Empty;

            recordItem.ItemCreated = oItem != null ? oItem["Created"] : string.Empty;
            FieldUserValue? author = oItem != null ? (FieldUserValue)oItem["Author"] : null;
            recordItem.ItemCreatedBy = author?.Email;

            recordItem.ItemModified = oItem != null ? oItem["Modified"] : string.Empty;
            FieldUserValue? editor = oItem != null ? (FieldUserValue)oItem["Editor"] : null;
            recordItem.ItemModifiedBy = editor?.Email;

            recordItem.ItemVersion = oItem != null ? oItem["_UIVersionString"] : string.Empty;
            recordItem.ItemVersionsCount = oItem != null ? oItem.Versions.Count.ToString() : string.Empty;

            recordItem.ItemSizeMb = oItem != null ? itemSizeMb : string.Empty;
            recordItem.ItemSizeTotalMB = oItem != null ? itemSizeTotalMb : string.Empty;

            recordItem.Remarks = remarks;

            _main.AddRecordToCSV(recordItem);
        }



    }

    public class ItemReportParameters : ISolutionParameters
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
    }
}
