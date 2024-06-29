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
                if ( String.IsNullOrWhiteSpace(value)) { _folderRelativeUrl = value.Trim(); }
                else if (value.StartsWith("/")) { _folderRelativeUrl = value.Trim(); }
                else { _folderRelativeUrl = "/" + value.Trim(); }
            }
        }
    }
}
