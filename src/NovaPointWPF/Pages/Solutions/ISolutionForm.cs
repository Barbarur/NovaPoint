using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using System;


namespace NovaPointWPF.Pages.Solutions
{
    public interface ISolutionForm
    {
        string SolutionName { get; init; }
        string SolutionCode { get; init; }
        string SolutionDocs { get; init; }

        Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        ISolutionParameters GetParameters();

    }
}
