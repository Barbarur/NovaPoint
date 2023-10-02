using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions
{
    public interface ISolution
    {
        public static string s_SolutionName = "Solution";
        public static string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/";

        public ISolutionParameters Parameters { get; set; }

        public async Task RunAsync()
        {
        }

    }
}
