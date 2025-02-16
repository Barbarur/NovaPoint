using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using NovaPointLibrary.Solutions;
using System.Text;


namespace NovaPointLibrary.Commands.SharePoint.SharingLinks
{
    internal class SpoSharingLinksRecord : ISolutionRecord
    {
        internal string SiteTitle { get; set; } = String.Empty;
        internal string SiteUrl { get; set; } = String.Empty;

        internal string ListTitle { get; set; } = String.Empty;
        internal Guid ListId { get; set; } = Guid.Empty;

        internal int ItemId { get; set; } = -1;
        internal Guid ItemUniqueId { get; set; } = Guid.Empty;
        internal string ItemPath { get; set; } = String.Empty;


        internal string SharingLink { get; set; } = String.Empty;
        internal string SharingLinkRequiresPassword { get; set; } = String.Empty;
        internal string SharingLinkExpiration { get; set; } = String.Empty;


        internal string SharingLinkIsActive { get; set; } = String.Empty;
        internal DateTime SharingLinkCreated { get; set; } = DateTime.MinValue;
        internal string SharingLinkCreatedBy { get; set; } = String.Empty;
        internal DateTime SharingLinkModified { get; set; } = DateTime.MinValue;
        internal string SharingLinkModifiedBy { get; set; } = String.Empty;
        internal string SharingLinkUrl { get; set; } = String.Empty;


        internal string GroupId { get; set; } = String.Empty;
        internal string GroupTitle { get; set; } = String.Empty;
        internal string ShareId = String.Empty;
        internal string Users { get; set; } = String.Empty;

        internal string Remarks { get; set; } = String.Empty;

        public string GroupDescription { get; init; } = String.Empty;
        public bool LinkDetailsAnonymous { get; set; } = false;
        public bool LinkDetailsOrganization { get; set; } = false;
        public bool LinkDetailsCanEdit { get; set; } = false;
        public bool LinkDetailsCaReview { get; set; } = false;
        public bool LinkDetailsCanNotDownload { get; set; } = false;

        public Link? Link { get; set; } = null;


        internal SpoSharingLinksRecord(string siteUrl, Exception ex)
        {
            SiteUrl = siteUrl;
            Remarks = ex.Message;
        }

        internal SpoSharingLinksRecord(string siteUrl, Group oGroup)
        {
            SiteUrl = siteUrl;

            GroupId = oGroup.Id.ToString();
            GroupTitle = oGroup.Title;

            var titleComponents = oGroup.Title.Split(".");
            ItemUniqueId = Guid.Parse(titleComponents[1]);
            ShareId = titleComponents[3];

            StringBuilder sbUsers = new();
            foreach (var user in oGroup.Users)
            {
                sbUsers.Append($"{user.Email} ");
            }
            Users = sbUsers.ToString();

            GroupDescription = oGroup.Description;
            //int i = oGroup.Description.IndexOf("'") + 1;
            //int l = oGroup.Description.Length - i - 1;
            //ItemPath = UrlUtility.Combine(SiteUrl, oGroup.Description.Substring(i, l));
        }

        internal void AddListAndItem(Microsoft.SharePoint.Client.List oList, ListItem oItem)
        {
            ListTitle = oList.Title;
            ListId = oList.Id;

            ItemId = oItem.Id;
            ItemPath = (string)oItem["FileRef"];
        }

        internal void AddLink(Link oLink)
        {
            Link = oLink;

            if (oLink.linkDetails.AllowsAnonymousAccess)
            {
                LinkDetailsAnonymous = true;
                SharingLink = "Anyone with the link";
                Users = "Anyone with the link";
            }
            else if (!oLink.linkDetails.RestrictedShareMembership)
            {
                LinkDetailsOrganization = true;
                SharingLink = "People in your organization with the link";
                Users = "People in your organization with the link";
            }
            else
            {
                SharingLink = "Specific People with the link";

                StringBuilder sbUsers = new();
                foreach (var invitation in oLink.linkDetails.Invitations)
                {
                    sbUsers.Append($"{invitation.invitee.email} ");
                }
                Users = sbUsers.ToString();
            }

            if (oLink.linkDetails.IsEditLink)
            {
                LinkDetailsCanEdit = true;
                SharingLink += " can edit";
            }
            else if (oLink.linkDetails.IsReviewLink)
            {
                LinkDetailsCaReview = true;
                SharingLink += " can review";
            }
            else if (oLink.linkDetails.BlocksDownload)
            {
                LinkDetailsCanNotDownload = true;
                SharingLink += " can view but can't download";
            }
            else
            {
                SharingLink += " can view";
            }

            SharingLinkRequiresPassword = oLink.linkDetails.RequiresPassword.ToString();
            SharingLinkExpiration = oLink.linkDetails.Expiration.ToString();

            SharingLinkIsActive = oLink.linkDetails.IsActive.ToString();

            SharingLinkCreated = DateTime.Parse(oLink.linkDetails.Created);
            SharingLinkCreatedBy = oLink.linkDetails.CreatedBy.email;
            SharingLinkModified = DateTime.Parse(oLink.linkDetails.LastModified);
            SharingLinkModifiedBy = oLink.linkDetails.LastModifiedBy.email;
            SharingLinkUrl = oLink.linkDetails.Url;
        }

    }
}
