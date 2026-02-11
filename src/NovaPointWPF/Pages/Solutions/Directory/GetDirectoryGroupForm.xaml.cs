using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Directory;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Directory
{
    public partial class GetDirectoryGroupForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public GetDirectoryGroupForm()
        {
            InitializeComponent();

            SolutionName = GetDirectoryGroup.s_SolutionName;
            SolutionCode = nameof(GetDirectoryGroup);
            SolutionDocs = GetDirectoryGroup.s_SolutionDocs;

            SolutionCreate = GetDirectoryGroup.Create;
        }

        public ISolutionParameters GetParameters()
        {
            GetDirectoryGroupParameters parameters = new(GroupF.Parameters);
            return parameters;
        }

    }
}
