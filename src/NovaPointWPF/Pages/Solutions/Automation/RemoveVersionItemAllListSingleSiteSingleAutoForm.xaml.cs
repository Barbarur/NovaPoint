using NovaPointLibrary.Solutions.Reports;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using NovaPointLibrary.Solutions.Automation;
using NovaPointLibrary.Commands.Authentication;

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for RemoveVersionItemAllListSingleSiteSingleAutoForm.xaml
    /// </summary>
    public partial class RemoveVersionItemAllListSingleSiteSingleAutoForm : Page, ISolutionForm
    {

        // Required parameters for the current report
        internal string SiteUrl { get; set; } = String.Empty;
        internal string ListName { get; set; } = String.Empty;
        // Optional parameters related to filter sites
        public bool DeleteAll { get; set; } = true;
        public int VersionsToKeep { get; set; } = 100;
        public bool Recycle { get; set; } = false;

        public RemoveVersionItemAllListSingleSiteSingleAutoForm()
        {
            InitializeComponent();

            DataContext = this;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {
            RemoveVersionItemAllListSingleSiteSingleAutoParameters parameters = new(SiteUrl, ListName)
            {
                DeleteAll = DeleteAll,
                VersionsToKeep = VersionsToKeep,
                Recycle = Recycle

            };
            await new RemoveVersionItemAllListSingleSiteSingleAuto(uiLog, appInfo, parameters).RunAsync();
        }


        private void CheckBox_DeleteAll_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxDeleteFew.IsChecked = false;
            TextBoxVersionsToKeep.Visibility = Visibility.Collapsed;
            CheckBoxRecycle.IsChecked = false;
        }

        private void CheckBox_DeleteFew_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxDeleteAll.IsChecked = false;
            TextBoxVersionsToKeep.Visibility = Visibility.Visible;

        }
    }
}
