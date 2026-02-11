using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    public partial class RemoveSiteUserAutoForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public RemoveSiteUserAutoForm()
        {
            InitializeComponent();

            SolutionName = RemoveSiteUserAuto.s_SolutionName;
            SolutionCode = nameof(RemoveSiteUserAuto);
            SolutionDocs = RemoveSiteUserAuto.s_SolutionDocs;

            SolutionCreate = RemoveSiteUserAuto.Create;

            DataContext = this;
        }

        public ISolutionParameters GetParameters()
        {
            RemoveUserAutoParameters parameters = new(UserF.Parameters, AdminF.Parameters, SiteF.Parameters);
            return parameters;
        }

    }
}
