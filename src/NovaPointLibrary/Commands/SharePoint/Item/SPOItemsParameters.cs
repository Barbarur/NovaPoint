using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

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

        private string _folderRelativeUrl = String.Empty;
        // Path of the folder relative to the root of the List or Library, so the
        // name of the List or Library is not part of it. i.e. '/FolderName/Subfolder'
        public string FolderRelativeUrl
        {
            get { return _folderRelativeUrl; }
            set
            {
                _folderRelativeUrl = value.Trim();
                if (!string.IsNullOrWhiteSpace(_folderRelativeUrl))
                {
                    if (!_folderRelativeUrl.StartsWith('/'))
                    {
                        _folderRelativeUrl = "/" + _folderRelativeUrl;
                    }
                    if (_folderRelativeUrl.EndsWith('/'))
                    {
                        _folderRelativeUrl = _folderRelativeUrl.Remove(_folderRelativeUrl.LastIndexOf('/'));
                    }
                }
            }
        }



        internal string GetFolderServerRelativeURL(Microsoft.SharePoint.Client.List oList)
        {
            string listRootUrl = oList.RootFolder.ServerRelativeUrl;
            if (listRootUrl.EndsWith('/'))
            {
                listRootUrl = listRootUrl.Remove(listRootUrl.LastIndexOf('/'));
            }

            return listRootUrl + FolderRelativeUrl;
        }

        internal bool FolderPathHasWildcard => FolderRelativeUrl.Contains('*');

        internal string[] GetFolderPathSegments()
        {
            return FolderRelativeUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        }

        internal static Regex SegmentPatternToRegex(string segment)
        {
            string pattern = "^" + Regex.Escape(segment).Replace("\\*", ".*") + "$";
            return new Regex(pattern, RegexOptions.IgnoreCase);
        }

        internal bool MatchParameters(ListItem oItem, string? folderServerRelativeUrl)
        {
            if (AllItems)
            {
                return true;
            }

            if (!MatchNonFolderParameters(oItem))
            {
                return false;
            }

            bool matchFolder;
            if (!String.IsNullOrWhiteSpace(folderServerRelativeUrl))
            {
                string itemPath = (string)oItem["FileRef"];
                if (!itemPath.StartsWith('/')) { itemPath = itemPath.Insert(0, "/"); }

                matchFolder = itemPath.StartsWith(folderServerRelativeUrl + "/", StringComparison.OrdinalIgnoreCase)
                    || itemPath.Equals(folderServerRelativeUrl, StringComparison.OrdinalIgnoreCase);
            }
            else { matchFolder = true; }

            return matchFolder;
        }

        // Used when a wildcard folder path matched too many folders to query each one
        // individually: every item of the list is collected instead, and filtered here
        // by testing its path segments against the folder path pattern directly.
        internal bool MatchParametersWithFolderPattern(ListItem oItem, string listRootServerRelativeUrl)
        {
            if (AllItems)
            {
                return true;
            }

            if (!MatchNonFolderParameters(oItem))
            {
                return false;
            }

            return MatchesFolderPattern(oItem, listRootServerRelativeUrl);
        }

        private bool MatchesFolderPattern(ListItem oItem, string listRootServerRelativeUrl)
        {
            string[] patternSegments = GetFolderPathSegments();

            string itemPath = (string)oItem["FileRef"];
            if (!itemPath.StartsWith('/')) { itemPath = itemPath.Insert(0, "/"); }

            string relativePath = itemPath.StartsWith(listRootServerRelativeUrl, StringComparison.OrdinalIgnoreCase)
                ? itemPath.Remove(0, listRootServerRelativeUrl.Length)
                : itemPath;

            string[] itemSegments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (itemSegments.Length < patternSegments.Length)
            {
                return false;
            }

            for (int i = 0; i < patternSegments.Length; i++)
            {
                if (!SegmentPatternToRegex(patternSegments[i]).IsMatch(itemSegments[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchNonFolderParameters(ListItem oItem)
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

            return matchCreated && matchModified && matchAuthor && matchEditor;
        }

    }
}
