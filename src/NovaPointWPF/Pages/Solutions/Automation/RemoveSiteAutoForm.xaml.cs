using Microsoft.Win32;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Permission;
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
    /// Interaction logic for RemoveSiteAutoForm.xaml
    /// </summary>
    public partial class RemoveSiteAutoForm : Page, ISolutionForm
    {
        private string _listOfSitesPath = string.Empty;
        public string ListOfSitesPath
        {
            get { return _listOfSitesPath; }
            set
            {
                _listOfSitesPath = value;
                PathLabel.Text = value;
            }
        }

        public RemoveSiteAutoForm()
        {
            InitializeComponent();

            DataContext = this;

            SolutionHeader.SolutionTitle = RemoveSiteAuto.s_SolutionName;
            SolutionHeader.SolutionCode = nameof(RemoveSiteAuto);
            SolutionHeader.SolutionDocs = RemoveSiteAuto.s_SolutionDocs;
        }

        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            if (openFileDialog.ShowDialog() == true)
                ListOfSitesPath = openFileDialog.FileName;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
        {
            RemoveSiteAutoParameters parameters = new(ListOfSitesPath);

            await RemoveSiteAuto.RunAsync(parameters, uiLog, cancelTokenSource);
        }
    }
}
