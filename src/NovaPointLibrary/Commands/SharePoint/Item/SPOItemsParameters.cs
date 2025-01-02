using CamlBuilder;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System.Linq.Expressions;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    public class SPOItemsParameters : ISolutionParameters
    {
        internal Expression<Func<ListItem, object>>[] ItemExpresions = new Expression<Func<ListItem, object>>[] { };
        internal Expression<Func<ListItem, object>>[] FileExpresions = new Expression<Func<ListItem, object>>[] { };

        public bool AllItems { get; set; } = true;

        private string _folderSiteRelativeUrl = String.Empty;
        public string FolderSiteRelativeUrl
        {
            get { return _folderSiteRelativeUrl; }
            set
            {
                _folderSiteRelativeUrl = value.Trim();
                if (!_folderSiteRelativeUrl.StartsWith("/"))
                {
                    _folderSiteRelativeUrl = "/" + _folderSiteRelativeUrl;
                }
                if (_folderSiteRelativeUrl.EndsWith("/"))
                {
                    _folderSiteRelativeUrl = _folderSiteRelativeUrl.Remove(_folderSiteRelativeUrl.LastIndexOf("/"));
                }
            }
        }

        internal string GetFolderServerRelativeURL(string siteUrl)
        {
            string siteUrlClean = siteUrl.Trim();
            if (siteUrlClean.EndsWith("/"))
            {
                siteUrlClean = siteUrlClean.Remove(siteUrlClean.LastIndexOf("/"));
            }

            string folderUrl = siteUrlClean + FolderSiteRelativeUrl;
            string folderServerRelativeUrl = folderUrl[(folderUrl.IndexOf(".com") + 4)..];

            return folderServerRelativeUrl;
        }
    }
}
