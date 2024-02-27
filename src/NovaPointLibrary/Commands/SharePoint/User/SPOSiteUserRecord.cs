using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.User
{
    internal class SPOSiteUserRecord
    {
        internal Microsoft.SharePoint.Client.User? User { get; set; } = null;
        internal string ErrorMessage { get; set; } = string.Empty;

        internal SPOSiteUserRecord(Microsoft.SharePoint.Client.User? user, string errorMessage = "")
        {
            User = user;
            ErrorMessage = errorMessage;
        }
    }
}
