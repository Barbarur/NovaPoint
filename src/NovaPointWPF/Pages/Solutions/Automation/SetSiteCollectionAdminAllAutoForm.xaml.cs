using Newtonsoft.Json;
using NovaPointLibrary.Solutions.Automation;
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
using NovaPointLibrary.Commands.Authentication;

namespace NovaPointWPF.Pages.Solutions.Automation
{
    /// <summary>
    /// Interaction logic for SetSiteCollectionAdminAllAutoForm.xaml
    /// </summary>
    public partial class SetSiteCollectionAdminAllAutoForm : Page, ISolutionForm
    {
        // Required parameters for the current report
        public string TargetUserUPN { get; set; } = string.Empty;
        public bool IsSiteAdmin { get; set; } = true;
        public bool AddAdmin { get; set; } = true;
        public bool RemoveAdmin { get; set; } = false;

        // Optional parameters for the current report to filter sites
        public bool IncludePersonalSite { get; set; } = false;
        public bool IncludeShareSite { get; set; } = true;
        public bool GroupIdDefined { get; set; } = false;
        public SetSiteCollectionAdminAllAutoForm()
        {
            InitializeComponent();

            DataContext = this;
        }

        public async Task RunSolutionAsync(Action<LogInfo> uiLog, AppInfo appInfo)
        {

            SetSiteCollectionAdminAllAutoParameters parameters = new(TargetUserUPN, IsSiteAdmin)
            {
                IncludePersonalSite = IncludePersonalSite,
                IncludeShareSite = IncludeShareSite,
                GroupIdDefined = GroupIdDefined,
            };
            await new SetSiteCollectionAdminAllAuto(uiLog, appInfo, parameters).RunAsync();

        }
        private void CheckBox_AddAdmin_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxRemoveAdmin.IsChecked = false;
        }

        private void CheckBox_RemoveAdmin_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxAddAdmin.IsChecked = false;
        }

        private void CheckBox_IncludePersonalSites_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxIncludeGroupIdDefined.IsChecked = false;
        }

        private void CheckBox_IncludeShareSites_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBoxIncludePersonalSites.IsChecked = true;
            CheckBoxIncludeGroupIdDefined.IsChecked = false;
        }

        private void CheckBox_IncludeGroupIdDefined_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxIncludePersonalSites.IsChecked = false;
            CheckBoxIncludeShareSites.IsChecked = true;
        }

    }
}
