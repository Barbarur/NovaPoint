using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Permission
{
    internal class SPOLocationPermissionsRecord
    {
        internal string _locationType;
        internal string _locationName;
        internal string _locationUrl;
        internal SPORoleAssignmentUserRecord _role;

        internal SPOLocationPermissionsRecord(string locationType, string locationName, string locationUrl, SPORoleAssignmentUserRecord role)
        {
            _locationType = locationType;
            _locationName = locationName;
            _locationUrl = locationUrl;
            _role = role;
        }
    }
}
