using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.AzureAD;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Logging;
using NovaPointLibrary.Core.SQLite;
using System.Linq.Expressions;

namespace NovaPointLibrary.Solutions.Automation
{
    public class CopyDuplicateFileAuto
    {
        public readonly static String s_SolutionName = "Copy or Duplicate Files across Sites";
        public readonly static String s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/Solution-Automation-CopyDuplicateFileAuto";

        private CopyDuplicateFileAutoParameters _param;
        private readonly LoggerSolution _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private static readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _listExpressions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        {
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
        };

        private static readonly Expression<Func<ListItem, object>>[] _fileExpressions = new Expression<Func<ListItem, object>>[]
        {
            i => i.FileSystemObjectType,
            i => i["SMTotalSize"],

            i => i.Id,
            i => i.File.Name,
            i => i.File.ServerRelativeUrl,
            i => i.File.UIVersionLabel,
            i => i.File.Versions,
            i => i.File.Length,

            i => i.ParentList.RootFolder.ServerRelativeUrl,
        };

        private CopyDuplicateFileAuto(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, CopyDuplicateFileAutoParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(CopyDuplicateFileAutoParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            parameters.SourceItemsParam.FileExpresions = _fileExpressions;

            LoggerSolution logger = new(uiAddLog, "CopyDuplicateFileAuto", parameters);

            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                await new CopyDuplicateFileAuto(logger, appInfo, parameters).RunScriptAsync();

                logger.SolutionFinish();

            }
            catch (Exception ex)
            {
                logger.SolutionFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            GraphUser signedInUser = await new GetAADUser(_logger, _appInfo).GetSignedInUserAsync();
            string adminUPN = signedInUser.UserPrincipalName;

            if (_param.AdminAccess.AddAdmin)
            {
                _logger.UI(GetType().Name, "Adding Site Collection Admin to source site.");
                await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).AddAsync(_param.SourceSiteURL, adminUPN);
                _logger.UI(GetType().Name, "Adding Site Collection Admin to destination site.");
                await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).AddAsync(_param.DestinationSiteURL, adminUPN);
            }

            _logger.UI(GetType().Name, "Getting source site.");
            var oSourceWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.SourceSiteURL);
            _logger.UI(GetType().Name, "Getting destination site.");
            var oDestinationWeb = await new SPOWebCSOM(_logger, _appInfo).GetAsync(_param.DestinationSiteURL);

            if (oSourceWeb.Url == oDestinationWeb.Url)
            {
                _param.SameWebCopyMoveOptimization = true;
            }

            _logger.UI(GetType().Name, "Getting source library.");
            var oSourceList = oSourceWeb.GetListByTitle(_param.SourceListTitle, _listExpressions);
            if (oSourceList == null)
            {
                throw new($"Source library '{_param.SourceListTitle}' does not exist.");
            }
            else if (oSourceList.BaseType != BaseType.DocumentLibrary)
            {
                throw new($"'{_param.SourceListTitle}' is not a library.");
            }

            _logger.UI(GetType().Name, "Getting destination library.");
            var oDestinationList = oDestinationWeb.GetListByTitle(_param.DestinationListTitle, _listExpressions);
            if (oDestinationList == null)
            {
                throw new($"Destination library '{_param.DestinationListTitle}' does not exist.");
            }
            else if (oSourceList.BaseType != BaseType.DocumentLibrary)
            {
                throw new($"'{_param.DestinationListTitle}' is not a library.");
            }


            string destinationServerRelativeUrl = oDestinationList.RootFolder.ServerRelativeUrl;

            if (!String.IsNullOrWhiteSpace(_param.DestinationFolderServerRelativeUrl))
            {
                var oDestinationFolder = await new SPOFolderCSOM(_logger, _appInfo).GetFolderAsync(oDestinationWeb.Url, _param.DestinationFolderServerRelativeUrl);
                if (oDestinationFolder == null)
                {
                    throw new($"Destination folder '{_param.DestinationFolderServerRelativeUrl}' does not exists.");
                }

                destinationServerRelativeUrl = oDestinationFolder.ServerRelativeUrl;
            }

            _logger.UI(GetType().Name, "Getting Files from source locaton.");
            var sql = new SqliteHandler(_logger);
            try
            {
                await CopyMoveAsync(sql, oSourceWeb, oSourceList, destinationServerRelativeUrl);
            }
            finally
            {
                sql.DropTable(typeof(RESTCopyMoveFileFolder));

            }

            if (_param.AdminAccess.RemoveAdmin)
            {
                try
                {
                    _logger.UI(GetType().Name, "Adding Site Collection Admin from source site.");
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(oSourceWeb.Url, adminUPN);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", oSourceWeb.Url, ex);
                    string errorMessage = $"Error removing Site Collection Admin fromm site {oSourceWeb.Url}. {ex.Message}";

                    RecordCSV(new(_param, "Failed", remarks: errorMessage));
                }

                try
                {
                    _logger.UI(GetType().Name, "Adding Site Collection Admin from destination site.");
                    await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).RemoveAsync(oDestinationWeb.Url, adminUPN);
                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Site", oDestinationWeb.Url, ex);
                    string errorMessage = $"Error removing Site Collection Admin fromm site {oDestinationWeb.Url}. {ex.Message}";

                    RecordCSV(new(_param, "Failed", remarks: errorMessage));
                }
            }
            
        }

        private async Task CopyMoveAsync(SqliteHandler sql, Web oSourceWeb, List oSourceList, string destinationServerRelativeUrl)
        {
            sql.ResetTableQuery(typeof(RESTCopyMoveFileFolder));
            await foreach (var oListItem in new SPOListItemCSOM(_logger, _appInfo).GetAsync(oSourceWeb.Url, oSourceList, _param.SourceItemsParam))
            {
                var itemServerRelativeUrlAtDestination = GetItemDestinationServerRelativeUrl(oListItem, destinationServerRelativeUrl);

                RESTCopyMoveFileFolder obj = new(oSourceWeb.Url, (string)oListItem["FileRef"], itemServerRelativeUrlAtDestination);
                sql.InsertValue(obj);
            }

            await CopyMoveListItemsAsync(sql);
        }

        private async Task CopyMoveListItemsAsync(SqliteHandler sql)
        {
            int deepest = sql.GetMaxValue(typeof(RESTCopyMoveFileFolder), "Depth");
            int tableFloor = sql.GetMinValue(typeof(RESTCopyMoveFileFolder), "Depth");

            int totalCount = sql.GetCountTotalRecord(typeof(RESTCopyMoveFileFolder));
            ProgressTracker progress = new(_logger, totalCount);

            _logger.UI(GetType().Name, "Coping items...");
            for (int depth = tableFloor; depth <= deepest; depth++)
            {
                int batchCount = 0;
                var batch = GetBatch(sql, depth, batchCount);

                while (batch.Any())
                {
                    _logger.Info(GetType().Name, $"Processing depth {depth}");
                    await CopyMoveDepthBatchListItemAsync(batch, progress);
                    batchCount++;
                    batch = GetBatch(sql, depth, batchCount);
                }
            }
        }

        private async Task CopyMoveDepthBatchListItemAsync(IEnumerable<RESTCopyMoveFileFolder> batch, ProgressTracker progress)
        {
            _appInfo.IsCancelled();

            batch = batch.OrderBy(i => i.SourceServerRelativeUrl).ToList();

            ParallelOptions par = new()
            {
                MaxDegreeOfParallelism = 9,
                CancellationToken = _appInfo.CancelToken,
            };
            await Parallel.ForEachAsync(batch, par, async (oListItem, _) =>
            {
                _appInfo.IsCancelled();

                var loggerThread = await _logger.GetSubThreadLogger();
                try
                {
                    if (!_param.ReportMode)
                    {
                        await oListItem.CopyMoveAsync(loggerThread, _appInfo, _param.IsMove, _param.SameWebCopyMoveOptimization);
                    }

                    RecordCSV(new(_param, "Success", oListItem.SourceServerRelativeUrl, oListItem.DestinationServerRelativeUrl));
                }
                catch (Exception ex)
                {
                    loggerThread.Error(GetType().Name, "Item", oListItem.SourceServerRelativeUrl, ex);

                    RecordCSV(new(_param, "Failed", oListItem.SourceServerRelativeUrl, oListItem.DestinationServerRelativeUrl, ex.Message));
                }
                progress.ProgressUpdateReport();
            });

        }

        private string GetItemDestinationServerRelativeUrl(ListItem oListItem, string destinationServerRelativeUrl)
        {
            string listItemServerRelativeUrl = (string)oListItem["FileRef"];
            string sourceFolderRelativeUrl = listItemServerRelativeUrl.Remove(0, oListItem.ParentList.RootFolder.ServerRelativeUrl.Length);

            if (!String.IsNullOrWhiteSpace(_param.SourceItemsParam.FolderRelativeUrl))
            {
                sourceFolderRelativeUrl = listItemServerRelativeUrl.Remove(0, _param.SourceItemsParam.FolderRelativeUrl.Length);
            }
            return string.Concat(destinationServerRelativeUrl, sourceFolderRelativeUrl);
        }

        private IEnumerable<RESTCopyMoveFileFolder> GetBatch(SqliteHandler sql, int depth, int batchCount)
        {
            int batchSize = 5;
            int offset = batchSize * batchCount;
            string query = @$"
                    SELECT * 
                    FROM {typeof(RESTCopyMoveFileFolder).Name} 
                    WHERE Depth = {depth} 
                    LIMIT {batchSize} OFFSET {offset};";

            return sql.GetRecords<RESTCopyMoveFileFolder>(query);
        }

        private void RecordCSV(CopyDuplicateFileAutoRecord record)
        {
            _logger.RecordCSV(record);
        }

    }

    internal class CopyDuplicateFileAutoRecord : ISolutionRecord
    {
        internal int _depth;
        internal string SourceSiteURL { get; set; }
        internal string SourceListTitle { get; set; }
        internal string SourceItemsServerRelativeUrl { get; set; }

        internal string DestinationSiteURL { get; set; }
        internal string DestinationListTitle { get; set; }
        internal string DestinationItemsServerRelativeUrl { get; set; }

        internal string Status { get; set; }
        internal string Remarks { get; set; }

        internal CopyDuplicateFileAutoRecord(
            CopyDuplicateFileAutoParameters param,
            string status,
            string sourceItemsServerRelativeUrl = "",
            string destinationItemsServerRelativeUrl = "",
            string remarks = "",
            int depth = 0
            )
        {
            SourceSiteURL = param.SourceSiteURL;
            SourceListTitle = param.SourceListTitle;
            SourceItemsServerRelativeUrl = sourceItemsServerRelativeUrl;

            DestinationSiteURL = param.DestinationSiteURL;
            DestinationListTitle = param.DestinationListTitle;
            DestinationItemsServerRelativeUrl = destinationItemsServerRelativeUrl;

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

        private string _sourceSiteURL = string.Empty;
        public string SourceSiteURL
        {
            get { return _sourceSiteURL; }
            set
            {
                _sourceSiteURL = value.Trim();
                if (_destinationFolderServerRelativeUrl.EndsWith("/"))
                {
                    _destinationFolderServerRelativeUrl = _destinationFolderServerRelativeUrl.Remove(_destinationFolderServerRelativeUrl.LastIndexOf("/"));
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

        private string _destinationSiteURL = string.Empty;
        public string DestinationSiteURL
        {
            get { return _destinationSiteURL; }
            set
            {
                _destinationSiteURL = value.Trim();
                if (_destinationFolderServerRelativeUrl.EndsWith("/"))
                {
                    _destinationFolderServerRelativeUrl = _destinationFolderServerRelativeUrl.Remove(_destinationFolderServerRelativeUrl.LastIndexOf("/"));
                }
            }
        }
        private string _destinationListTitle = string.Empty;
        public string DestinationListTitle
        {
            get { return _destinationListTitle; }
            set { _destinationListTitle = value.Trim(); }
        }
        private string _destinationFolderServerRelativeUrl = String.Empty;
        public string DestinationFolderServerRelativeUrl
        {
            get { return _destinationFolderServerRelativeUrl; }
            set
            {
                _destinationFolderServerRelativeUrl = value.Trim();
                if (!_destinationFolderServerRelativeUrl.StartsWith("/"))
                {
                    _destinationFolderServerRelativeUrl = "/" + _destinationFolderServerRelativeUrl;
                }
                if (_destinationFolderServerRelativeUrl.EndsWith("/"))
                {
                    _destinationFolderServerRelativeUrl = _destinationFolderServerRelativeUrl.Remove(_destinationFolderServerRelativeUrl.LastIndexOf("/"));
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
            string destinationSiteURL,
            string destinationListTitle,
            string destinationFolderServerRelativeUrl)
        {
            ReportMode = reportMode;
            IsMove = isMove;

            AdminAccess = adminAccess;

            SourceSiteURL = sourceSiteUrl;
            SourceListTitle = sourceListTitle;
            SourceItemsParam = sourceItemsParam;
            
            DestinationSiteURL = destinationSiteURL;
            DestinationListTitle = destinationListTitle;
            DestinationFolderServerRelativeUrl = destinationFolderServerRelativeUrl;

        }

    }
}
