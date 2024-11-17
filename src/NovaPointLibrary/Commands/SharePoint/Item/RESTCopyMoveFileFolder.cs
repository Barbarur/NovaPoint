using Newtonsoft.Json;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Core.Logging;
using Microsoft.SharePoint.Client;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class RESTCopyMoveFileFolder
    {

        public int Depth { get; set; }
        public string SiteUrl { get; set; } =string.Empty;

        internal string _sourceServerRelativeUrl = string.Empty;
        public string SourceServerRelativeUrl
        {
            get { return _sourceServerRelativeUrl; }
            set
            {
                _sourceServerRelativeUrl = value;
                Depth = value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
            }
        }

        internal string _destinationServerRelativeUrl = string.Empty;
        public string DestinationServerRelativeUrl
        {
            get { return _destinationServerRelativeUrl; }
            set
            {
                _destinationServerRelativeUrl = value;
                _folderDestinationServerRelativeUrl = value.Remove(value.LastIndexOf("/"));
            }
        }
        private string _folderDestinationServerRelativeUrl = string.Empty;

        public int FileSizeBytes { get; set; } = -1;
        public int FileTotalSizeBytes { get; set; } = -1;

        internal double _waitingTime = 1000;

        internal RESTCopyMoveFileFolder() { }

        internal RESTCopyMoveFileFolder(
            string siteUrl,
            ListItem listItem,
            string destinationServerRelativeUrl)
        {
            SiteUrl = siteUrl;
            SourceServerRelativeUrl = (string)listItem["FileRef"];
            DestinationServerRelativeUrl = destinationServerRelativeUrl;

            if (listItem.FileSystemObjectType.ToString() != "Folder")
            {
                FileSizeBytes = Convert.ToInt32(listItem["File_x0020_Size"]);
                FieldLookupValue FileSizeTotalBytes = (FieldLookupValue)listItem["SMTotalSize"];
                FileTotalSizeBytes = FileSizeTotalBytes.LookupId;
            }

        }

        // Reference:
        // https://gist.github.com/zplume/21248c3a8a5f840a366722442cf9ee97
        // https://learn.microsoft.com/en-us/sharepoint/dev/apis/spod-copy-move-api
        internal async Task CopyMoveAsync(
            ILogger logger,
            Authentication.AppInfo appInfo,
            bool isMove,
            bool sameWebCopyMoveOptimization)
        {
            appInfo.IsCancelled();
            logger.Info(GetType().Name, $"CopyMove file '{SourceServerRelativeUrl}' from site '{SiteUrl}' to '{DestinationServerRelativeUrl}.");

            Uri sourceUri = new(new(SiteUrl), EncodePath(SourceServerRelativeUrl));
            Uri targetUri = new(new(SiteUrl), EncodePath(_folderDestinationServerRelativeUrl));

            string api = SiteUrl + "/_api/site/CreateCopyJobs";

            var x = new
            {
                exportObjectUris = new[] { sourceUri },
                destinationUri = targetUri,
                options = new
                {
                    IsMoveMode = isMove,
                    IgnoreVersionHistory = !isMove,
                    AllowSchemaMismatch = true,
                    AllowSmallerVersionLimitOnDestination = true,
                    NameConflictBehavior = 0,
                    MoveButKeepSource = true,
                    ExcludeChildren = true,
                    SameWebCopyMoveOptimization = sameWebCopyMoveOptimization,
                }
            };

            var contentCreateCopyJobs = JsonConvert.SerializeObject(x);

            string responseCreateCopyJobs = await new RESTAPIHandler(logger, appInfo).PostAsync(api, contentCreateCopyJobs);

            var resultCollection = JsonConvert.DeserializeObject<RESTResultCollection<RESTCreateCopyJobs>>(responseCreateCopyJobs);

            if (resultCollection == null || !resultCollection.Items.Any())
            {
                throw new($"Copy job creation response is empty");
            }

            var createCopyJob = resultCollection.Items.First();

            var copyJobInfo = new
            {
                copyJobInfo = createCopyJob
            };

            var contentGetCopyJobProgress = JsonConvert.SerializeObject(copyJobInfo);

            api = SiteUrl + "/_api/site/GetCopyJobProgress";
            logger.Info(GetType().Name, $"Waiting for job to complete {_waitingTime} milliseconds to process {FileSizeBytes} bytes and total {FileTotalSizeBytes} bytes.");
            await Task.Delay((int)_waitingTime);
            string responseGetCopyJobProgress = await new RESTAPIHandler(logger, appInfo).PostAsync(api, contentGetCopyJobProgress);
            logger.Debug(GetType().Name, $"Job progress for {contentCreateCopyJobs} is {responseGetCopyJobProgress}");

            var copyJobProgress = JsonConvert.DeserializeObject<RESTCopyJobProgress>(responseGetCopyJobProgress);
            if (copyJobProgress == null)
            {
                throw new($"Copy job progress respose is empty.");
            }

            while (copyJobProgress.JobState != 0)
            {
                // sleep 1 second
                await Task.Delay(1000);
                responseGetCopyJobProgress = await new RESTAPIHandler(logger, appInfo).PostAsync(api, contentGetCopyJobProgress);
                logger.Debug(GetType().Name, $"Job progress for {contentCreateCopyJobs} is {responseGetCopyJobProgress}");

                copyJobProgress = JsonConvert.DeserializeObject<RESTCopyJobProgress>(responseGetCopyJobProgress);
                if (copyJobProgress == null)
                {
                    throw new($"Copy job progress respose is empty.");
                }
            }

            if (copyJobProgress.Logs != null)
            {
                foreach (var log in copyJobProgress.Logs)
                {
                    if (log.Contains("JobError"))
                    {
                        logger.Info(GetType().Name, $"Error log: {log}");
                        throw new($"Error while processing CopyJob. Check error logs for more details.");
                    }
                }
            }

        }

        private string EncodePath(string path)
        {
            var parts = path.Split("/");
            var encodedPath = string.Join("/", parts.Select(p => Uri.EscapeDataString(p)));

            return encodedPath;
        }
    }

}
