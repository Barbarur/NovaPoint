using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.SharePoint.List;
using System.Linq.Expressions;


namespace NovaPointLibrary.Commands.SharePoint.Item
{
    public class SPOTenantItemsParameters : SPOTenantListsParameters
    {
        internal Expression<Func<ListItem, object>>[] ItemExpresions = new Expression<Func<ListItem, object>>[] { };
        internal Expression<Func<ListItem, object>>[] FileExpresions = new Expression<Func<ListItem, object>>[] { };

        private string _folderRelativeUrl = String.Empty;
        public string FolderRelativeUrl
        {
            get { return _folderRelativeUrl; }
            set { _folderRelativeUrl = value.Trim(); }
        }
    }
}
