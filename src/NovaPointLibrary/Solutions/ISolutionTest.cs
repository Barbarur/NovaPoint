using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions.Directory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions
{
    public interface ISolutionTest
    {
        public static readonly string s_SolutionName = "Solution";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/";

        //static abstract ISolutionTest Create(ContextSolution context, ISolutionParameters parameters);

        Task RunAsync();

    }
}
