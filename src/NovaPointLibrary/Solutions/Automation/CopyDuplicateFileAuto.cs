using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Core.SQLite;
using System.Linq.Expressions;
using Web = Microsoft.SharePoint.Client.Web;

namespace NovaPointLibrary.Solutions.Automation
{
    public class CopyDuplicateFileAuto : ISolution
    {
        public readonly static string s_SolutionName = "Copy or Duplicate Files across Sites";
        public readonly static string s_SolutionDocs = $"https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-{nameof(CopyDuplicateFileAuto)}";

        private ContextSolution _ctx;
        private readonly CopyDuplicateFileAutoParameters _param;

        private static readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] ListExpressions =
        [
            l => l.Hidden,
            l => l.IsSystemList,
            l => l.ParentWeb.Url,

            l => l.BaseType,
            l => l.DefaultViewUrl,
            l => l.Id,
            l => l.ItemCount,
            l => l.Title,


            l => l.RootFolder,
            l => l.RootFolder.ServerRelativeUrl,

        ];

        private static readonly Expression<Func<ListItem, object>>[] FileExpressions =
        [
            i => i.FileSystemObjectType,
            f => f["File_x0020_Size"],
            i => i["SMTotalSize"],

            i => i.Id,
            i => i.File.Name,
            i => i.File.ServerRelativeUrl,
            i => i.File.UIVersionLabel,
            i => i.File.Versions,
            i => i.File.Length,

            i => i.ParentList.RootFolder.ServerRelativeUrl,
        ];

        private double _averageWaitingTimeMillisecondsPerByte = 0.000001;

        private int _count = 0;

        private CopyDuplicateFileAuto(ContextSolution context, CopyDuplicateFileAutoParameters parameters)
        {
            _ctx = context;

            parameters.SourceItemsParam.FileExpressions = FileExpressions;
            _param = parameters;

            Dictionary<Type, string> solutionReports = new()
            {
                { typeof(CopyDuplicateFileAutoRecord), "Report" },
            };
            _ctx.DbHandler.AddSolutionReports(solutionReports);
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new CopyDuplicateFileAuto(context, (CopyDuplicateFileAutoParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            GraphUser signedInUser = await new GetAADUser(_ctx.Logger, _ctx.AppClient).GetSignedInUserAsync();
            string adminUpn = signedInUser.UserPrincipalName;

            if (_param.AdminAccess.AddAdmin)
            {
                _ctx.Logger.UI(GetType().Name, "Adding Site Collection Admin to source site.");
                await new SPOSiteCollectionAdminCSOM(_ctx.Logger, _ctx.AppClient).AddAsync(_param.SourceSiteUrl, adminUpn);
                _ctx.Logger.UI(GetType().Name, "Adding Site Collection Admin to destination site.");
                await new SPOSiteCollectionAdminCSOM(_ctx.Logger, _ctx.AppClient).AddAsync(_param.DestinationSiteUrl, adminUpn);
            }

            _ctx.Logger.UI(GetType().Name, "Getting source site.");
            var oSourceWeb = await new SPOWebCSOM(_ctx.Logger, _ctx.AppClient).GetAsync(_param.SourceSiteUrl);
            _ctx.Logger.UI(GetType().Name, "Getting destination site.");
            var oDestinationWeb = await new SPOWebCSOM(_ctx.Logger, _ctx.AppClient).GetAsync(_param.DestinationSiteUrl);

            if (oSourceWeb.Url == oDestinationWeb.Url)
            {
                _param.SameWebCopyMoveOptimization = true;
            }

            _ctx.Logger.UI(GetType().Name, "Getting source library.");
            var oSourceList = oSourceWeb.GetListByTitle(_param.SourceListTitle, ListExpressions);
            if (oSourceList == null)
            {
                throw new($"Source library '{_param.SourceListTitle}' does not exist.");
            }
            else if (oSourceList.BaseType != BaseType.DocumentLibrary)
            {
                throw new($"'{_param.SourceListTitle}' is not a library.");
            }

            _ctx.Logger.UI(GetType().Name, "Getting destination library.");
            var oDestinationList = oDestinationWeb.GetListByTitle(_param.DestinationListTitle, ListExpressions);
            if (oDestinationList == null)
            {
                throw new($"Destination library '{_param.DestinationListTitle}' does not exist.");
            }
            else if (oSourceList.BaseType != BaseType.DocumentLibrary)
            {
                throw new($"'{_param.DestinationListTitle}' is not a library.");
            }


            string destinationServerRelativeUrl = oDestinationList.RootFolder.ServerRelativeUrl;

            if (!string.IsNullOrWhiteSpace(_param.DestinationLibraryRelativeUrl))
            {
                string folderServerRelativeUrl = oDestinationList.RootFolder.ServerRelativeUrl + _param.DestinationLibraryRelativeUrl;
                var oDestinationFolder = await new SPOFolderCSOM(_ctx.Logger, _ctx.AppClient).GetFolderAsync(oDestinationWeb.Url, folderServerRelativeUrl);
                if (oDestinationFolder == null)
                {
                    throw new($"Destination folder '{folderServerRelativeUrl}' does not exists.");
                }

                destinationServerRelativeUrl = oDestinationFolder.ServerRelativeUrl;
            }

            _ctx.Logger.UI(GetType().Name, "Getting Files from source location.");
            var sql = SqliteHandler.GetCacheHandler();
            try
            {
                await CopyMoveAsync(sql, oSourceWeb, oSourceList, destinationServerRelativeUrl);
            }
            finally
            {
                sql.DropTable(_ctx.Logger, typeof(RESTCopyMoveFileFolder));

            }

            if (_param.AdminAccess.RemoveAdmin)
            {
                try
                {
                    _ctx.Logger.UI(GetType().Name, "Removing Site Collection Admin from source site.");
                    await new SPOSiteCollectionAdminCSOM(_ctx.Logger, _ctx.AppClient).RemoveAsync(oSourceWeb.Url, adminUpn);
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Site", oSourceWeb.Url, ex);
                    string errorMessage = $"Error removing Site Collection Admin from site {oSourceWeb.Url}. {ex.Message}";

                    RecordCsv(new(_param, "Failed", remarks: errorMessage));
                }

                try
                {
                    _ctx.Logger.UI(GetType().Name, "Removing Site Collection Admin from destination site.");
                    await new SPOSiteCollectionAdminCSOM(_ctx.Logger, _ctx.AppClient).RemoveAsync(oDestinationWeb.Url, adminUpn);
                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Site", oDestinationWeb.Url, ex);
                    string errorMessage = $"Error removing Site Collection Admin from site {oDestinationWeb.Url}. {ex.Message}";

                    RecordCsv(new(_param, "Failed", remarks: errorMessage));
                }
            }
            
        }

        private async Task CopyMoveAsync(SqliteHandler sql, Web oSourceWeb, List oSourceList, string destinationServerRelativeUrl)
        {
            sql.ResetTable(_ctx.Logger, typeof(RESTCopyMoveFileFolder));
            await foreach (var oListItem in new SPOListItemCSOM(_ctx.Logger, _ctx.AppClient).GetAsync(oSourceWeb.Url, oSourceList, _param.SourceItemsParam))
            {
                _ctx.Logger.Debug(GetType().Name, $"Caching information for item ({oListItem.Id}) '{oListItem["FileRef"]}'");

                string listItemServerRelativeUrl = (string)oListItem["FileRef"];
                string listItemFolderRelativeUrl = listItemServerRelativeUrl.Remove(0, oListItem.ParentList.RootFolder.ServerRelativeUrl.Length);

                if (!string.IsNullOrWhiteSpace(_param.SourceItemsParam.FolderSiteRelativeUrl))
                {
                    string folderServerRelativeUrl = _param.SourceItemsParam.GetFolderServerRelativeURL(oSourceWeb.Url);

                    listItemFolderRelativeUrl = listItemServerRelativeUrl.Remove(0, folderServerRelativeUrl.Length);
                }

                var itemServerRelativeUrlAtDestination = string.Concat(destinationServerRelativeUrl, listItemFolderRelativeUrl);

                RESTCopyMoveFileFolder obj = new(oSourceWeb.Url, oListItem, itemServerRelativeUrlAtDestination);
                sql.InsertValue(_ctx.Logger, obj);
            }

            await CopyMoveListItemsAsync(sql);
        }

        private async Task CopyMoveListItemsAsync(SqliteHandler sql)
        {
            int deepest = sql.GetMaxValue(_ctx.Logger, typeof(RESTCopyMoveFileFolder), "Depth");
            int tableFloor = sql.GetMinValue(_ctx.Logger, typeof(RESTCopyMoveFileFolder), "Depth");

            int totalCount = sql.GetCountTotalRecord(_ctx.Logger, typeof(RESTCopyMoveFileFolder));
            ProgressTracker progress = new(_ctx.Logger, totalCount);

            _ctx.Logger.UI(GetType().Name, "Copying items...");
            for (int depth = tableFloor; depth <= deepest; depth++)
            {
                int batchCount = 0;
                _ctx.Logger.Info(GetType().Name, $"Processing depth {depth}");

                IEnumerable<RESTCopyMoveFileFolder> batch;
                do
                {
                    batch = GetBatch(sql, depth, batchCount);
                    await CopyMoveDepthBatchListItemAsync(batch, progress);
                    batchCount++;

                } while (batch.Any());
            }
        }

        private async Task CopyMoveDepthBatchListItemAsync(IEnumerable<RESTCopyMoveFileFolder> batch, ProgressTracker progress)
        {
            _ctx.AppClient.IsCancelled();

            batch = batch.OrderBy(i => i.SourceServerRelativeUrl).ToList();

            ParallelOptions par = new()
            {
                MaxDegreeOfParallelism = 2, // Changed from 9 to 2 to avoid throttling
                CancellationToken = _ctx.AppClient.CancelToken,
            };
            await Parallel.ForEachAsync(batch, par, async (copyMoveItem, _) =>
            {
                _ctx.AppClient.IsCancelled();
                //Stopwatch sw = new();
                //sw.Start();

                //copyMoveItem._waitingTime = GetWaitingTimeInMilliseconds(copyMoveItem);

                var loggerThread = await _ctx.Logger.GetSubThreadLogger();
                try
                {
                    if (!_param.ReportMode)
                    {
                        await copyMoveItem.CopyMoveAsync(loggerThread, _ctx.AppClient, _param.IsMove, _param.SameWebCopyMoveOptimization);
                    }

                    RecordCsv(new(_param, copyMoveItem, "Success"));
                }
                catch (Exception ex)
                {
                    loggerThread.Error(GetType().Name, "Item", copyMoveItem.SourceServerRelativeUrl, ex);

                    RecordCsv(new(_param, copyMoveItem, "Failed", ex.Message));
                }
                progress.ProgressUpdateReport();

                //CalculateAverageWaitingTime(sw.Elapsed.TotalMilliseconds, copyMoveItem);
                //sw.Stop();
            });

        }
        
        private IEnumerable<RESTCopyMoveFileFolder> GetBatch(SqliteHandler sql, int depth, int batchCount)
        {
            int batchSize = 5000;
            int offset = batchSize * batchCount;
            string query = @$"
                    SELECT * 
                    FROM {typeof(RESTCopyMoveFileFolder).Name} 
                    WHERE Depth = {depth} 
                    LIMIT {batchSize} OFFSET {offset};";

            return sql.GetRecords<RESTCopyMoveFileFolder>(_ctx.Logger, query);
        }

        private void RecordCsv(CopyDuplicateFileAutoRecord record)
        {
            _ctx.DbHandler.WriteRecord(record);
        }

    }

    internal class CopyDuplicateFileAutoRecord : ISolutionRecord
    {
        public string SourceSiteUrl { get; set; } = string.Empty;
        public string SourceListTitle { get; set; } = string.Empty;
        public string SourceItemsServerRelativeUrl { get; set; } = string.Empty;

        public string DestinationSiteUrl { get; set; } = string.Empty;
        public string DestinationListTitle { get; set; } = string.Empty;
        public string DestinationItemsServerRelativeUrl { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        public CopyDuplicateFileAutoRecord() { }

        internal CopyDuplicateFileAutoRecord(
            CopyDuplicateFileAutoParameters param,
            string status,
            string remarks
            )
        {
            SourceSiteUrl = param.SourceSiteUrl;
            SourceListTitle = param.SourceListTitle;
            SourceItemsServerRelativeUrl = string.Empty;

            DestinationSiteUrl = param.DestinationSiteUrl;
            DestinationListTitle = param.DestinationListTitle;
            DestinationItemsServerRelativeUrl = string.Empty;

            Status = status;
            Remarks = remarks;
        }

        internal CopyDuplicateFileAutoRecord(
            CopyDuplicateFileAutoParameters param,
            RESTCopyMoveFileFolder restObject,
            string status,
            string remarks = ""
            )
        {
            SourceSiteUrl = param.SourceSiteUrl;
            SourceListTitle = param.SourceListTitle;
            SourceItemsServerRelativeUrl = restObject.SourceServerRelativeUrl;

            DestinationSiteUrl = param.DestinationSiteUrl;
            DestinationListTitle = param.DestinationListTitle;
            DestinationItemsServerRelativeUrl = restObject.DestinationServerRelativeUrl;

            Status = status;
            Remarks = remarks;
        }

    }

    public class CopyDuplicateFileAutoParameters : ISolutionParameters
    {
        public bool ReportMode { get; set; } = true;
        
        public bool IsMove { get; set; }
        internal bool SameWebCopyMoveOptimization { get; set; } = false;

        public SPOAdminAccessParameters AdminAccess { get; set; }

        private string _sourceSiteUrl = string.Empty;
        public string SourceSiteUrl
        {
            get { return _sourceSiteUrl; }
            set
            {
                _sourceSiteUrl = value.Trim();
                if (_destinationLibraryRelativeUrl.EndsWith("/"))
                {
                    _destinationLibraryRelativeUrl =
                        _destinationLibraryRelativeUrl.Remove(_destinationLibraryRelativeUrl.LastIndexOf('/'));
                }
            }
        }
        private string _sourceListTitle = string.Empty;
        public string SourceListTitle
        {
            get { return _sourceListTitle; }
            set { _sourceListTitle = value.Trim(); }
        }
        public SPOItemsParameters SourceItemsParam { get; set; }

        private string _destinationSiteUrl = string.Empty;
        public string DestinationSiteUrl
        {
            get { return _destinationSiteUrl; }
            set
            {
                _destinationSiteUrl = value.Trim();
                if (_destinationLibraryRelativeUrl.EndsWith('/'))
                {
                    _destinationLibraryRelativeUrl = _destinationLibraryRelativeUrl.Remove(_destinationLibraryRelativeUrl.LastIndexOf('/'));
                }
            }
        }
        private string _destinationListTitle = string.Empty;
        public string DestinationListTitle
        {
            get { return _destinationListTitle; }
            set { _destinationListTitle = value.Trim(); }
        }
        private string _destinationLibraryRelativeUrl = string.Empty;
        public string DestinationLibraryRelativeUrl
        {
            get { return _destinationLibraryRelativeUrl; }
            set
            {
                _destinationLibraryRelativeUrl = value.Trim();
                if (!_destinationLibraryRelativeUrl.StartsWith("/"))
                {
                    _destinationLibraryRelativeUrl = '/' + _destinationLibraryRelativeUrl;
                }
                if (_destinationLibraryRelativeUrl.EndsWith('/'))
                {
                    _destinationLibraryRelativeUrl = _destinationLibraryRelativeUrl.Remove(_destinationLibraryRelativeUrl.LastIndexOf('/'));
                }
            }
        }


        public CopyDuplicateFileAutoParameters(
            bool reportMode,
            bool isMove,
            SPOAdminAccessParameters adminAccess,
            string sourceSiteUrl,
            string sourceListTitle,
            SPOItemsParameters sourceItemsParam,
            string destinationSiteUrl,
            string destinationListTitle,
            string destinationLibraryRelativeUrl)
        {
            ReportMode = reportMode;
            IsMove = isMove;

            AdminAccess = adminAccess;

            SourceSiteUrl = sourceSiteUrl;
            SourceListTitle = sourceListTitle;
            SourceItemsParam = sourceItemsParam;
            
            DestinationSiteUrl = destinationSiteUrl;
            DestinationListTitle = destinationListTitle;
            DestinationLibraryRelativeUrl = destinationLibraryRelativeUrl;
        }

    }
}
