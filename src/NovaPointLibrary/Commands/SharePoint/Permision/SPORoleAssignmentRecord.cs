using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permision
{
    internal class SPORoleAssignmentRecord
    {
        internal string AccessType { get; set; }
        internal string AccountType { get; set; }
        internal string Users { get; set; }
        internal string PermissionLevels { get; set; }
        internal string Remarks { get; set; } = string.Empty;

        internal SPORoleAssignmentRecord(string accessType, string accountType, string user, string permissionLevel, string remarks)
        {
            AccessType = accessType;
            AccountType = accountType;
            Users = user;
            PermissionLevels = permissionLevel;
            Remarks = remarks;
        }

        internal SPORoleAssignmentRecord()
        {
            AccessType = string.Empty;
            AccountType = string.Empty;
            Users = string.Empty;
            PermissionLevels = string.Empty;
            Remarks = string.Empty;
        }
    }
}
