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

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for CheckInFileAutoForm.xaml
    /// </summary>
    public partial class CheckInFileAutoForm : Page, ISolutionForm
    {
        public bool ReportMode { get; set; }

        public string CheckinType { get; set; }
        public bool Major {  get; set; }
        public bool Minor { get; set; }
        public bool Discard { get; set; }
        public string Comment { get; set; }

        public bool RemoveAdmin { get; set; }

        public bool IncludePersonalSite { get; set; }
        public bool IncludeShareSite { get; set; }
        public bool OnlyGroupIdDefined { get; set; }
        public string SiteUrl { get; set; }
        public bool IncludeSubsites { get; set; }

        public bool ListAll { get; set; }
        public string ListTitle { get; set; }


        public bool ItemsAll { get; set; }
        public string FolderRelativeUrl { get; set; }


        public CheckInFileAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = CheckInFileAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(CheckInFileAuto);
            SolutionHeader.SolutionDocs = CheckInFileAuto.s_SolutionDocs;

            this.ReportMode = true;

            this.CheckinType = "Major";
            this.Major = true;
            this.Minor = false;
            this.Discard = false;
            this.Comment = string.Empty;

            this.RemoveAdmin = true;

            this.IncludePersonalSite = false;
            this.IncludeShareSite = true;
            this.OnlyGroupIdDefined = false;
            this.SiteUrl = String.Empty;
            this.IncludeSubsites = false;

            this.ListAll = true;
            this.ListTitle = String.Empty;

            this.ItemsAll = true;
            this.FolderRelativeUrl = String.Empty;

        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            CheckInFileAutoParameters parameters = new()
            {
                ReportMode = this.ReportMode,
                CheckinType =  this.CheckinType,
                Comment =  this.Comment,

                RemoveAdmin = this.RemoveAdmin,

                IncludePersonalSite = this.IncludePersonalSite,
                IncludeShareSite = this.IncludeShareSite,
                OnlyGroupIdDefined = this.OnlyGroupIdDefined,
                SiteUrl = this.SiteUrl,
                IncludeSubsites = this.IncludeSubsites,

                ListTitle = this.ListTitle,

                FolderRelativeUrl = this.FolderRelativeUrl,
            };
            //uiLog(LogInfo.ErrorNotification($"CheckinType: {CheckinType}"));
            //uiLog(LogInfo.ErrorNotification($"Comment: {Comment}"));
            await new CheckInFileAuto(parameters, uiLog, cancelTokenSource).RunAsync();
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
