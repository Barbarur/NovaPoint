using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    public partial class RestoreRecycleBinAutoForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public bool RenameFile { get; set; }

        public RestoreRecycleBinAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = RestoreRecycleBinAuto.s_SolutionName;
            SolutionCode = nameof(RestoreRecycleBinAuto);
            SolutionDocs = RestoreRecycleBinAuto.s_SolutionDocs;

            SolutionCreate = RestoreRecycleBinAuto.Create;
        }

        public ISolutionParameters GetParameters()
        {
            RestoreRecycleBinAutoParameters parameters = new(this.RenameFile, RecycleF.Parameters, AdminF.Parameters, SiteF.Parameters);
            return parameters;
        }

    }
}
