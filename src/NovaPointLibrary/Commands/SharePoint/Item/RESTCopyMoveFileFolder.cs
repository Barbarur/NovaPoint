using Newtonsoft.Json;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class RESTCopyMoveFileFolder
    {
        private readonly NPLogger _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal RESTCopyMoveFileFolder(NPLogger logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        // Reference:
        // https://gist.github.com/zplume/21248c3a8a5f840a366722442cf9ee97
        // https://learn.microsoft.com/en-us/sharepoint/dev/apis/spod-copy-move-api
        internal async Task CopyMoveAsync(
            string siteUrl, 
            string sourceServerRelativeUrl, 
            string destinationServerRelativeUrl, 
            bool isMove,
            bool sameWebCopyMoveOptimization)
        {
            _appInfo.IsCancelled();
            _logger.LogTxt(GetType().Name, $"CopyMove file '{sourceServerRelativeUrl}' from site '{siteUrl}' to '{destinationServerRelativeUrl}.");

            Uri sourceUri = new(new(siteUrl), EncodePath(sourceServerRelativeUrl));
            Uri targetUri = new(new(siteUrl), EncodePath(destinationServerRelativeUrl));

            string api = siteUrl + "/_api/site/CreateCopyJobs";

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

            var content = JsonConvert.SerializeObject(x);

            string response = await new RESTAPIHandler(_logger, _appInfo).PostAsync(api, content);

            var resultCollection = JsonConvert.DeserializeObject<RESTResultCollection<RESTCreateCopyJobs>>(response);

            if (resultCollection == null || !resultCollection.Items.Any())
            {
                throw new("Copy job creation response is empty");
            }

            var createCopyJob = resultCollection.Items.First();

            var copyJobInfo = new
            {
                copyJobInfo = createCopyJob
            };

            var contentcopyJobInfo = JsonConvert.SerializeObject(copyJobInfo);

            api = siteUrl + "/_api/site/GetCopyJobProgress";
            response = await new RESTAPIHandler(_logger, _appInfo).PostAsync(api, contentcopyJobInfo);

            var copyJobProgress = JsonConvert.DeserializeObject<RESTCopyJobProgress>(response);
            if (copyJobProgress == null)
            {
                throw new("Copy job progress respose is empty.");
            }

            while (copyJobProgress.JobState != 0)
            {
                // sleep 1 second
                await Task.Delay(1000);
                response = await new RESTAPIHandler(_logger, _appInfo).PostAsync(api, contentcopyJobInfo);

                copyJobProgress = JsonConvert.DeserializeObject<RESTCopyJobProgress>(response);
                if (copyJobProgress == null)
                {
                    throw new("Copy job progress respose is empty.");
                }
            }

            if (copyJobProgress.Logs != null)
            {
                foreach (var log in copyJobProgress.Logs)
                {
                    if (log.Contains("JobError"))
                    {
                        _logger.LogTxt(GetType().Name, $"Error log: {log}");
                        throw new($"Error while processing CopyJob. Check error logs for more details.");
                    }
                }
            }


        }

        private string EncodePath(string path)
        {
            var parts = path.Split("/");
            var encodedPath = string.Join("/", parts.Select(p => Uri.EscapeDataString(p)));
            _logger.LogTxt(GetType().Name, $"Encoded path {encodedPath}");
            return encodedPath;
        }
    }
}
