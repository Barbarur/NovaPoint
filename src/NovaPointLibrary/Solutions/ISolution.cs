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
        public static string s_solutionName = "Solution";
        public static string s_solutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/";

        public async Task RunAsync()
        {
        }

    }
}
