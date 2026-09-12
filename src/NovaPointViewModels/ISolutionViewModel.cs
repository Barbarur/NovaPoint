using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using System;

namespace NovaPointViewModels
{
    public interface ISolutionViewModel
    {
        string SolutionName { get; init; }
        string SolutionCode { get; init; }
        string SolutionDocs { get; init; }

        Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        ISolutionParameters GetParameters();
    }
}
