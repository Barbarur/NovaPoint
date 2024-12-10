using Microsoft.SharePoint.Client;


namespace NovaPointLibrary.Commands.AzureAD.Groups
{
    internal class AADGroupUserEmails
    {
        internal string GroupID { get; set; }
        internal string GroupName { get; set; }
        internal string AccountType { get; set; }
        internal string Users { get; set; }
        internal string Remarks { get; set; }

        internal AADGroupUserEmails(string groupID, string groupName, string user, string remarks = "")
        {
            GroupID = groupID;
            GroupName = groupName;
            AccountType = $"{groupName}";
            Users = user;
            Remarks = remarks;
        }

        internal AADGroupUserEmails(string groupID, string groupName, AADGroupUserEmails subgroup)
        {
            GroupID = groupID;
            GroupName = groupName;
            AccountType = $"{groupName} holds {subgroup.AccountType}";
            Users = subgroup.Users;
            Remarks = subgroup.Remarks;
        }
    }
}
