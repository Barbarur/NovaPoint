using Microsoft.Graph;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    internal class SPOItemCSOM
    {
        private readonly Main _main;

        internal SPOItemCSOM(Main main)
        {
            _main = main;
        }

        private async IAsyncEnumerable<ListItemCollection> GetBatch(string siteUrl,
                                                                    string listTitle,
                                                                    Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.GetBatch";
            _main.AddLogToTxt(methodName, $"Start getting Items by batch");

            CamlQuery camlQuery = CamlQuery.CreateAllItemsQuery();

            var queryElement = XElement.Parse(camlQuery.ViewXml);

            var rowLimit = queryElement.Descendants("RowLimit").FirstOrDefault();
            if (rowLimit != null)
            {
                rowLimit.RemoveAll();
            }
            else
            {
                rowLimit = new XElement("RowLimit");
                queryElement.Add(rowLimit);
            }

            rowLimit.SetAttributeValue("Paged", "TRUE");
            rowLimit.SetValue(5000);

            camlQuery.ViewXml = queryElement.ToString();

            ClientContext clientContext = await _main.GetContext(siteUrl);

            Microsoft.SharePoint.Client.List oList = clientContext.Web.Lists.GetByTitle(listTitle);

            var defaultExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
            {
                i => i.ParentList.Title,
                i => i.ParentList.ParentWeb.Url,

            };

            var expressions = retrievalExpressions.Union(defaultExpressions).ToArray();

            _main.AddLogToTxt(methodName, $"Start Loop");
            int counter = 0;
            do
            {
                _main.IsCancelled();

                ListItemCollection subcollListItem = oList.GetItems(camlQuery);

                clientContext.Load(subcollListItem,
                    sci => sci.ListItemCollectionPosition,
                    sci => sci.Include(expressions));

                clientContext.ExecuteQueryRetry();

                counter += subcollListItem.Count;
                _main.AddLogToUI(methodName, $"Collected {counter} items...");
                camlQuery.ListItemCollectionPosition = subcollListItem.ListItemCollectionPosition;

                yield return subcollListItem;

                clientContext = await _main.GetContext(siteUrl);
                oList = clientContext.Web.Lists.GetByTitle(listTitle);

            }
            while (camlQuery.ListItemCollectionPosition != null);

            _main.AddLogToTxt(methodName, $"Finish getting Items by batch");
        }

        internal async IAsyncEnumerable<Microsoft.SharePoint.Client.ListItem> Get(string siteUrl, string listTitle, Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] retrievalExpressions)
        {
            await foreach (var listItemCollection in GetBatch(siteUrl, listTitle, retrievalExpressions))
            {
                foreach (var oItem in listItemCollection)
                {
                    yield return oItem;
                }
            }
        }

        internal async Task<Microsoft.SharePoint.Client.File> GetAttachmentFile(string siteUrl, string attachmentServerRelativeUrl)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.GetAttachmentFile";
            _main.AddLogToTxt(methodName, $"Start getting attachment file '{attachmentServerRelativeUrl}'");

            ClientContext clientContext = await _main.GetContext(siteUrl);
            var file = clientContext.Web.GetFileByServerRelativeUrl(attachmentServerRelativeUrl);
            clientContext.Load(file);
            clientContext.ExecuteQuery();

            _main.AddLogToTxt(methodName, $"Finish getting attachment file '{attachmentServerRelativeUrl}'");
            return file;
        }

        internal async Task<FileVersionCollection> GetFileVersion(string siteUrl, Microsoft.SharePoint.Client.ListItem oItem)
        {
            _main.IsCancelled();
            string methodName = $"{GetType().Name}.CSOM";
            string fileURL = (string)oItem["GetFileVersion"];
            _main.AddLogToTxt(methodName, $"Start getting all version of the file '{fileURL}'");

            ClientContext clientContext = await _main.GetContext(siteUrl);

            Microsoft.SharePoint.Client.File file = clientContext.Web.GetFileByServerRelativePath(ResourcePath.FromDecodedUrl(fileURL));

            clientContext.Load(file, f => f.Exists, f => f.Versions.IncludeWithDefaultProperties(i => i.CreatedBy));
            clientContext.ExecuteQueryRetry();

            if (file.Exists)
            {
                FileVersionCollection versions = file.Versions;
                clientContext.ExecuteQueryRetry();

                _main.AddLogToTxt(methodName, $"Finish getting all version of the file '{fileURL}'");
                return versions;
            }
            else
            {
                throw new Exception($"File '{fileURL}' doesn't exist");
            }
        }
    }
}
