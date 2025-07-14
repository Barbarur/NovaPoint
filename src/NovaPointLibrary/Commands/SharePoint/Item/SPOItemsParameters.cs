using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System.Linq.Expressions;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    public class SPOItemsParameters : ISolutionParameters
    {
        private readonly Expression<Func<ListItem, object>>[] _defaultExpressions =
        [
            i => i["FileRef"],
            i => i["Created"],
            i => i["Author"],
            i => i["Modified"],
            i => i["Editor"],
        ];

        private Expression<Func<ListItem, object>>[] _itemExpressions = [];
        internal Expression<Func<ListItem, object>>[] ItemExpressions
        {
            get
            {
                return _itemExpressions;
            }
            set
            {
                _itemExpressions = _defaultExpressions.Union(value).ToArray();
            } 
        }

        private Expression<Func<ListItem, object>>[] _fileExpressions = [];
        internal Expression<Func<ListItem, object>>[] FileExpressions
        {
            get
            {
                return _fileExpressions;
            }
            set
            {
                _fileExpressions = _defaultExpressions.Union(value).ToArray();
            }
        }

        public bool AllItems { get; set; } = true;

        public DateTime CreatedAfter { get; set; } = DateTime.MinValue;
        public DateTime CreatedBefore { get; set; } = DateTime.MaxValue;
        
        private string _createdByEmail = string.Empty;
        public string CreatedByEmail
        {
            get { return _createdByEmail; }
            set { _createdByEmail = value.Trim(); }
        }
        
        public DateTime ModifiedAfter { get; set; } = DateTime.MinValue;
        public DateTime ModifiedBefore { get; set; } = DateTime.MaxValue;

        private string _modifiedByEmail = string.Empty;
        public string ModifiedByEmail
        {
            get { return _modifiedByEmail; }
            set { _modifiedByEmail = value.Trim(); }
        }

        private string _folderSiteRelativeUrl = String.Empty;
        public string FolderSiteRelativeUrl
        {
            get { return _folderSiteRelativeUrl; }
            set
            {
                _folderSiteRelativeUrl = value.Trim();
                if (!string.IsNullOrWhiteSpace(_folderSiteRelativeUrl))
                {
                    if (!_folderSiteRelativeUrl.StartsWith('/'))
                    {
                        _folderSiteRelativeUrl = "/" + _folderSiteRelativeUrl;
                    }
                    if (_folderSiteRelativeUrl.EndsWith('/'))
                    {
                        _folderSiteRelativeUrl = _folderSiteRelativeUrl.Remove(_folderSiteRelativeUrl.LastIndexOf("/"));
                    }
                }
            }
        }



        internal string GetFolderServerRelativeURL(string siteUrl)
        {
            string siteUrlClean = siteUrl.Trim();
            if (siteUrlClean.EndsWith('/'))
            {
                siteUrlClean = siteUrlClean.Remove(siteUrlClean.LastIndexOf('/'));
            }

            string folderUrl = siteUrlClean + FolderSiteRelativeUrl;
            string folderServerRelativeUrl = folderUrl[(folderUrl.IndexOf(".com") + 4)..];

            return folderServerRelativeUrl;
        }

        internal bool MatchParameters(ListItem oItem)
        {
            if (AllItems)
            {
                return true;
            }
            else
            {
                bool matchCreated = false;
                if ((DateTime)oItem["Created"] > CreatedAfter && (DateTime)oItem["Created"] < CreatedBefore)
                {
                    matchCreated = true;
                }

                bool matchAuthor;
                if (!string.IsNullOrWhiteSpace(CreatedByEmail))
                {
                    FieldUserValue author = (FieldUserValue)oItem["Author"];
                    if (CreatedByEmail.Equals(author.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        matchAuthor = true;
                    }
                    else { matchAuthor = false; }
                }
                else { matchAuthor = true; }

                bool matchModified = false;
                if ((DateTime)oItem["Modified"] > ModifiedAfter && (DateTime)oItem["Modified"] < ModifiedBefore)
                {
                    matchModified = true;
                }

                bool matchEditor;
                if (!string.IsNullOrWhiteSpace(ModifiedByEmail))
                {
                    FieldUserValue editor = (FieldUserValue)oItem["Editor"];
                    if (ModifiedByEmail.Equals(editor.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        matchEditor = true;
                    }
                    else { matchEditor = false; }
                }
                else { matchEditor = true; }

                bool matchFolder;
                if (!String.IsNullOrWhiteSpace(FolderSiteRelativeUrl))
                {
                    string itemPath = (string)oItem["FileRef"];
                    if (itemPath.Contains(FolderSiteRelativeUrl, StringComparison.OrdinalIgnoreCase)) { matchFolder = true; }
                    else { matchFolder = false; }
                }
                else { matchFolder = true; }


                if (matchCreated && matchModified && matchAuthor && matchEditor && matchFolder)
                {
                    return true;
                }
                else { return false; }
            }

        }

    }
}
