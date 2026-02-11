using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Directory;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    public partial class ClearRecycleBinAutoForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public ClearRecycleBinAutoForm()
        {
            InitializeComponent();

            SolutionName = ClearRecycleBinAuto.s_SolutionName;
            SolutionCode = nameof(ClearRecycleBinAuto);
            SolutionDocs = ClearRecycleBinAuto.s_SolutionDocs;

            SolutionCreate = ClearRecycleBinAuto.Create;

            DataContext = this;

        }

        public ISolutionParameters GetParameters()
        {
            return new ClearRecycleBinAutoParameters(RecycleF.Parameters, AdminF.Parameters, SiteF.Parameters);
        }
    }
}
