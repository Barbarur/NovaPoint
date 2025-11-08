

namespace NovaPointLibrary.Commands.Directory
{
    internal class DirectoryGroupUserEmails
    {
        internal Guid GroupID { get; set; }
        internal string GroupName { get; set; }
        internal bool IsOwners { get; set; }
        internal string AccountType { get; set; }
        internal string Users { get; set; }
        internal string Remarks { get; set; }

        internal DirectoryGroupUserEmails(Guid groupID, string groupName, bool isOwner, string user, string remarks = "")
        {
            GroupID = groupID;
            GroupName = groupName;
            IsOwners = isOwner;
            AccountType = $"Directory Group '{groupName}' ({groupID})";
            Users = user;
            Remarks = remarks;
        }

    }
}
