using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.User
{
    public class SPOSiteUserParameters : ISolutionParameters
    {
        public bool AllUsers { get; set; } = false;
        private string _includeUserUPN = string.Empty;
        public string IncludeUserUPN
        {
            get { return _includeUserUPN; }
            set { _includeUserUPN = value.Trim(); }
        }
        public bool IncludeExternalUsers { get; set; } = false;
        public bool IncludeEveryone { get; set; } = false;
        public bool IncludeEveryoneExceptExternal { get; set; } = false;

        public bool Detailed { get; set; } = false;
    }
}
