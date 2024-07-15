using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for CheckInFileAutoForm.xaml
    /// </summary>
    public partial class CheckInFileAutoForm : Page, ISolutionForm
    {
        public string CheckinType { get; set; }
        public bool Major {  get; set; }
        public bool Minor { get; set; }
        public bool Discard { get; set; }
        public string Comment { get; set; }

        public CheckInFileAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = CheckInFileAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(CheckInFileAuto);
            SolutionHeader.SolutionDocs = CheckInFileAuto.s_SolutionDocs;

            this.CheckinType = "Major";
            this.Major = true;
            this.Minor = false;
            this.Discard = false;
            this.Comment = string.Empty;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            SPOTenantSiteUrlsWithAccessParameters siteAccParam = new()
            {
                AdminAccess = AdminF.Parameters,
                SiteParam = SiteF.Parameters,
            };

            CheckInFileAutoParameters parameters = new(siteAccParam, ListForm.Parameters, ItemForm.Parameters)
            {
                ReportMode = Mode.ReportMode,
                CheckinType = this.CheckinType,
                Comment = this.Comment,
            };

            await CheckInFileAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }

        private void CheckInTypeClick(object sender, RoutedEventArgs e)
        {
            if (Major) { this.CheckinType = "Major"; }
            if (Minor) { this.CheckinType = "Minor"; }
            if (Discard)
            {
                this.CheckinType = "Discard";
                CommentTextBox.Text = string.Empty;
                this.Comment = string.Empty;
                CheckInCommentForm.Visibility = Visibility.Collapsed;
            }
            if (!Discard) { CheckInCommentForm.Visibility = Visibility.Visible; }
        }

    }
}
