using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permision
{
    internal class SPOLocationPermissionsRecord
    {
        internal string LocationType { get; set; }
        internal string LocationName { get; set; }
        internal string LocationUrl { get; set; }
        internal List<SPORoleAssignmentRecord> SPORoleAssignmentUsersList { get; set; }

        internal SPOLocationPermissionsRecord(string locationType, string locationName, string locationUrl, List<SPORoleAssignmentRecord> usersList)
        {
            LocationType = locationType;
            LocationName = locationName;
            LocationUrl = locationUrl;

            if (usersList.Count == 0)
            {
                SPORoleAssignmentUsersList = new() { new("", "", "", "", "No user has access to this location"), };
            }
            else
            {
                SPORoleAssignmentUsersList = usersList;
            }
        }
    }
}
