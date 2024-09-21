using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Solutions;
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Solutions.Report;
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

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RestorePHLItemAutoForm.xaml
    /// </summary>
    public partial class RestorePHLItemAutoForm : Page, ISolutionForm
    {
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

            SolutionHeader.SolutionTitle = RestorePHLItemAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RestorePHLItemAuto);
            SolutionHeader.SolutionDocs = RestorePHLItemAuto.s_SolutionDocs;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            RestorePHLItemAutoParameters parameters = new(RestoreOriginalLocation, RestoreTargetLocation,
                PHLForm.Parameters, AdminF.Parameters, SiteF.Parameters);

            await RestorePHLItemAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
