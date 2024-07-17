using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Item
{
    public class SPOItemsParameters : ISolutionParameters
    {
        internal Expression<Func<ListItem, object>>[] ItemExpresions = new Expression<Func<ListItem, object>>[] { };
        internal Expression<Func<ListItem, object>>[] FileExpresions = new Expression<Func<ListItem, object>>[] { };

        public bool AllItems = true;
        private string _folderRelativeUrl = String.Empty;

        public string FolderRelativeUrl
        {
            get { return _folderRelativeUrl; }
            set
            {
                _folderRelativeUrl = value.Trim();
                if (!_folderRelativeUrl.StartsWith("/"))
                {
                    _folderRelativeUrl = "/" + _folderRelativeUrl;
                }
                if (_folderRelativeUrl.EndsWith("/"))
                {
                    _folderRelativeUrl = _folderRelativeUrl.Remove(_folderRelativeUrl.LastIndexOf("/"));
                }
            }
        }
    }
}
