using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Report;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
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

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RemoveFileVersionAutoForm.xaml
    /// </summary>
    public partial class RemoveFileVersionAutoForm : Page, ISolutionForm
    {
        public bool DeleteAll { get; set; }
        public int VersionsToKeep { get; set; }

        private bool _keepVersions;
        public bool KeepVersions
        {
            get { return _keepVersions; }
            set
            {
                _keepVersions = value;
                if (value) { KeepVersionsForm.Visibility = Visibility.Visible; }
                else { KeepVersionsForm.Visibility = Visibility.Collapsed; }
            }
        }
        public bool Recycle { get; set; }

        public RemoveFileVersionAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RemoveFileVersionAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RemoveFileVersionAuto);
            SolutionHeader.SolutionDocs = RemoveFileVersionAuto.s_SolutionDocs;

            this.DeleteAll = false;
            this.KeepVersions = true;
            this.VersionsToKeep = 500;
            this.Recycle = true;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            var siteAccParam = AdminF.Parameters;
            var siteParam = SiteF.Parameters;
            siteAccParam.SiteParam = siteParam;

            RemoveFileVersionAutoParameters parameters = new(siteAccParam, ListForm.Parameters, ItemForm.Parameters)

            {
                DeleteAll = this.DeleteAll,
                VersionsToKeep = this.VersionsToKeep,
                Recycle = this.Recycle,
                
                ReportMode = Mode.ReportMode,
            };

            await RemoveFileVersionAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }


    }
}
