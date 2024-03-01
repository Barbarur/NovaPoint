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
        public static readonly string s_SolutionName = "Solution";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/";

        public async Task RunAsync()
        {
        }

    }
}
