using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    public class SPOAdminAccessParameters : ISolutionParameters
    {
        public bool AddAdmin { get; set; } = true;
        public bool RemoveAdmin { get; set; } = true;
    }
}
