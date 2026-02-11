using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using System;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    public partial class RestorePHLItemAutoForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        internal bool _restoreOriginalLocation = true;
        public bool RestoreOriginalLocation
        { 
            get { return _restoreOriginalLocation; }
            set
            {
                _restoreOriginalLocation = value;
                if (value)
                {
                    RestoreTargetLocation = string.Empty;
                    PathTextBox.Text = string.Empty;
                    PathTextBoxVisibility.Visibility = Visibility.Collapsed;
                }
                else { PathTextBoxVisibility.Visibility = Visibility.Visible; }
            }
        }
        public string RestoreTargetLocation { get; set; } = string.Empty;

        public RestorePHLItemAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionName = RestorePHLItemAuto.s_SolutionName;
            SolutionCode = nameof(RestorePHLItemAuto);
            SolutionDocs = RestorePHLItemAuto.s_SolutionDocs;

            SolutionCreate = RestorePHLItemAuto.Create;
        }

        public ISolutionParameters GetParameters()
        {
            RestorePHLItemAutoParameters parameters = new(RestoreOriginalLocation, RestoreTargetLocation,
                PHLForm.Parameters, AdminF.Parameters, SiteF.Parameters);
            return parameters;
        }
    }
}
