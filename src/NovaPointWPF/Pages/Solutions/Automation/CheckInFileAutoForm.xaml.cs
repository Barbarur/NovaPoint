using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Directory;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Solutions.Automation
{
    public partial class CheckInFileAutoForm : Page, ISolutionForm
    {
        public string SolutionName { get; init; }
        public string SolutionCode { get; init; }
        public string SolutionDocs { get; init; }

        public Func<ContextSolution, ISolutionParameters, ISolution> SolutionCreate { get; init; }

        public string CheckingType { get; set; }
        public bool Major {  get; set; }
        public bool Minor { get; set; }
        public bool Discard { get; set; }
        public string Comment { get; set; }

        public CheckInFileAutoForm()
        {
            InitializeComponent();

            SolutionName = CheckInFileAuto.s_SolutionName;
            SolutionCode = nameof(CheckInFileAuto);
            SolutionDocs = CheckInFileAuto.s_SolutionDocs;

            SolutionCreate = CheckInFileAuto.Create;

            DataContext = this;

            this.CheckingType = "Major";
            this.Major = true;
            this.Minor = false;
            this.Discard = false;
            this.Comment = string.Empty;
        }

        private void CheckInTypeClick(object sender, RoutedEventArgs e)
        {
            if (Major) { this.CheckingType = "Major"; }
            if (Minor) { this.CheckingType = "Minor"; }
            if (Discard)
            {
                this.CheckingType = "Discard";
                CommentTextBox.Text = string.Empty;
                this.Comment = string.Empty;
                CheckInCommentForm.Visibility = Visibility.Collapsed;
            }
            if (!Discard) { CheckInCommentForm.Visibility = Visibility.Visible; }
        }

        public ISolutionParameters GetParameters()
        {
            CheckInFileAutoParameters parameters = new(Mode.ReportMode, this.CheckingType, this.Comment,
                AdminF.Parameters, SiteF.Parameters, ListForm.Parameters, ItemForm.Parameters);
            return parameters;
        }

    }
}
