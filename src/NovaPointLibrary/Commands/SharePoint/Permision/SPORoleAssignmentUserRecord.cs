using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permission
{
    internal class SPORoleAssignmentUserRecord
    {
        internal string AccessType { get; set; }
        internal string GroupId { get; set; }
        internal string AccountType { get; set; } = "Unknown";
        internal string Users { get; set; } = "Unknown";
        internal string PermissionLevels { get; set; }
        internal string Remarks { get; set; } = string.Empty;

        internal SPORoleAssignmentUserRecord(string accessType, string groupId, string accountType, string user, string permissionLevel, string remarks)
        {
            AccessType = accessType;
            GroupId = groupId;
            AccountType = accountType;
            Users = user;
            PermissionLevels = permissionLevel;
            Remarks = remarks;
        }

        internal SPORoleAssignmentUserRecord(string accessType, string groupId, string permissionLevel)
        {
            AccessType = accessType;
            GroupId = groupId;
            PermissionLevels = permissionLevel;
        }

        internal SPORoleAssignmentUserRecord GetRecordWithUsers(string accountType, string user, string remarks)
        {
            SPORoleAssignmentUserRecord record = GetRecordWithUsers(accountType, user);
            record.Remarks = remarks;
            return record;
        }

        internal SPORoleAssignmentUserRecord GetRecordWithUsers(string accountType, string user)
        {
            SPORoleAssignmentUserRecord record = new(AccessType, GroupId, PermissionLevels)
            {
                AccountType = accountType,
                Users = user
            };
            
            return record;
        }

        internal static SPORoleAssignmentUserRecord GetRecordUserDirectPermissions(string user, string permissionLevel)
        {
            SPORoleAssignmentUserRecord record = new("Direct Permissions", "NA", permissionLevel)
            {
                AccountType = "User",
                Users = user,
            };
            return record;
        }

        internal static SPORoleAssignmentUserRecord GetRecordInherits()
        {
            return GetRecordBlank("Inherits permissions");
        }

        internal static SPORoleAssignmentUserRecord GetRecordNoAccess()
        {
            return GetRecordBlank("No user access");
        }

        internal static SPORoleAssignmentUserRecord GetRecordBlankException(string message)
        {
            var record = GetRecordBlank("");
            record.Remarks = message;
            return record;
        }

        internal static SPORoleAssignmentUserRecord GetRecordBlank(string blank)
        {
            SPORoleAssignmentUserRecord record = new(blank, blank, blank)
            {
                AccountType = blank,
                Users = blank,
            };
            return record;
        }
    }
}
