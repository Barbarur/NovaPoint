using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using NovaPointLibrary.Core.Logging;
using System.Linq.Expressions;
using File = Microsoft.SharePoint.Client.File;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOFileCSOM
    {
        private readonly LoggerSolution _logger;
        private readonly Authentication.AppInfo _appInfo;

        internal SPOFileCSOM(LoggerSolution logger, Authentication.AppInfo appInfo)
        {
            _logger = logger;
            _appInfo = appInfo;
        }

        internal async Task<File> GetFileAsync(string siteUrl, string fileServerRelativeUrl)
        {
            _appInfo.IsCancelled();
            string methodName = $"{GetType().Name}";
            _logger.Info(methodName, $"Start getting Item '{fileServerRelativeUrl}' from '{siteUrl}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            return GetFileAsync(clientContext, fileServerRelativeUrl);
        }

        private File GetFileAsync(ClientContext clientContext, string fileServerRelativeUrl)
        {
            _appInfo.IsCancelled();

            if (!fileServerRelativeUrl.StartsWith("/"))
            {
                fileServerRelativeUrl = fileServerRelativeUrl.Insert(0, "/");
            }

            var defaultExpressions = new Expression<Func<File, object>>[]
            {
                f => f.Exists,
                f => f.Name,
                f => f.ServerRelativePath,
                f => f.ServerRelativeUrl,
            };

            File oFile = clientContext.Web.GetFileByServerRelativeUrl(fileServerRelativeUrl);
            clientContext.Load(oFile, defaultExpressions);
            clientContext.ExecuteQueryRetry();

            return oFile;
        }

        internal async Task RenameFileAsync(string siteUrl, string fileServerRelativeUrl, string newName)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Start renaming file '{fileServerRelativeUrl}' to '{newName}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            File oFile = GetFileAsync(clientContext, fileServerRelativeUrl);
            
            var targetPath = string.Concat(oFile.ServerRelativePath.DecodedUrl.Remove(oFile.ServerRelativePath.DecodedUrl.Length - oFile.Name.Length), newName);
            oFile.MoveToUsingPath(ResourcePath.FromDecodedUrl(targetPath), MoveOperations.None);
            clientContext.ExecuteQueryRetry();
        }

        internal async Task CheckInAsync(string siteUrl, ListItem oFile, CheckinType checkinType, string comment)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Check-in file '{oFile["FileRef"]}' at '{siteUrl}'");

            ClientContext clientContext = await _appInfo.GetContext(siteUrl);

            clientContext.Web.CheckInFile($"{oFile["FileRef"]}", checkinType, comment);

        }

        internal async Task<string> FindAvailableNameAsync(string siteUrl, string fileServerRelativeUrl)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Finding available name");


            string parentFolderPath = fileServerRelativeUrl.Remove(fileServerRelativeUrl.LastIndexOf("/") + 1);

            string potentialName = fileServerRelativeUrl.Substring(fileServerRelativeUrl.LastIndexOf("/") + 1);
            string availableName = string.Empty;

            while (string.IsNullOrWhiteSpace(availableName))
            {
                string potentialNamePath = parentFolderPath + potentialName;
                var oFile = await GetFileAsync(siteUrl, potentialNamePath);
                if (!oFile.Exists)
                {
                    availableName = potentialName;
                }
                else
                {
                    string itemNameOnly = Path.GetFileNameWithoutExtension(potentialName);
                    if (itemNameOnly[^3] == '('
                        && int.TryParse(itemNameOnly[^2].ToString(), out int unit)
                        && itemNameOnly[^1] == ')'
                        && unit >= 9)
                    {
                        throw new Exception($"Too many files with the same name on that location. We couldn't find an available name for the file.");
                    }

                    potentialName = GetNewName(potentialName);
                }
            }

            _logger.Info(GetType().Name, $"File name {availableName} is available.");
            return availableName;
        }

        internal string GetNewName(string itemName)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting new name for item '{itemName}'");

            string itemNameOnly = Path.GetFileNameWithoutExtension(itemName);
            var extension = Path.GetExtension(itemName);

            bool isDuplicatedName = false;
            int unit = 1;
            if (itemNameOnly[^3] == '(' && int.TryParse(itemNameOnly[^2].ToString(), out unit) && itemNameOnly[^1] == ')')
            {
                isDuplicatedName = true;
            }

            string newName;
            if (isDuplicatedName)
            {
                unit++;
                string baseName = itemNameOnly.Substring(0, itemNameOnly.Length - 3);
                newName = baseName + $"({unit})";
            }
            else
            {
                newName = itemNameOnly + "(1)";
            }

            return newName + extension;
        }


        // Reference:
        // https://gist.github.com/zplume/21248c3a8a5f840a366722442cf9ee97
        // https://learn.microsoft.com/en-us/sharepoint/dev/apis/spod-copy-move-api
        
        // Keep in mind potential same folder optimization for future updates
        internal async Task CopyAsync(string siteUrl, string sourceServerRelativeUrl, string destinationServerRelativeUrl, bool noWait)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Coping file '{sourceServerRelativeUrl}' from site '{siteUrl}' to '{destinationServerRelativeUrl}.");

            Uri sourceUri = new(new(siteUrl), EncodePath(sourceServerRelativeUrl));
            Uri targetUri = new(new(siteUrl), EncodePath(destinationServerRelativeUrl));

            string api = siteUrl + "/_api/site/CreateCopyJobs";

            var x = new
            {
                exportObjectUris = new[] { sourceUri },
                destinationUri = targetUri,
                options = new
                {
                    IgnoreVersionHistory = true,
                    IsMoveMode = false,
                    AllowSchemaMismatch = true,
                    NameConflictBehavior = 0,
                    AllowSmallerVersionLimitOnDestination = true
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

            if (noWait)
            {
                return;
            }

            var copyJobProgress = JsonConvert.DeserializeObject<RESTCopyJobProgress>(response);
            if (copyJobProgress == null)
            {
                throw new("Copy job progress respose is empty");
            }
            
            while (copyJobProgress.JobState != 0)
            {
                // sleep 1 second
                await Task.Delay(1000);
                response = await new RESTAPIHandler(_logger, _appInfo).PostAsync(api, contentcopyJobInfo);

                copyJobProgress = JsonConvert.DeserializeObject<RESTCopyJobProgress>(response);
                if (copyJobProgress == null)
                {
                    throw new("Copy job progress respose is empty");
                }
            }

        }

        private string EncodePath(string path)
        {
            var parts = path.Split("/");
            var encodedPath = string.Join("/", parts.Select(p => Uri.EscapeDataString(p)));
            _logger.Info(GetType().Name, $"Encoded path {encodedPath}");
            return encodedPath;
        }
    }

    internal class CopyJobs
    {
        public string EncryptionKey { get; set; }
        public string JobId { get; set; }
        public string JobQueueUri { get; set; }
        public string[] SourceListItemUniqueIds { get; set; }
    }
}
