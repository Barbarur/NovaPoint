//using NovaPointLibrary.Solutions.Reports;
//using NovaPointLibrary.Solutions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
//using NovaPointLibrary.Solutions.Automation;
//using NovaPointLibrary.Commands.Authentication;
//using System.Threading;

//namespace NovaPointWPF.Pages.Solutions.Automation
//{
//    /// <summary>
//    /// Interaction logic for RemoveUserSingleSiteAllAutoForm.xaml
//    /// </summary>
//    public partial class RemoveUserSingleSiteAllAutoForm : Page, ISolutionForm
//    {
//        // Required parameters for the current report
//        public string AdminUPN { get; set; }
//        public string DeleteUserUPN { get; set; }
//        // Optional parameters for the current report to filter sites
//        public bool RemoveAdmin { get; set; }
//        public bool IncludePersonalSite { get; set; }
//        public bool IncludeShareSite { get; set; }
//        public bool GroupIdDefined { get; set; }

//        public RemoveUserSingleSiteAllAutoForm()
//        {
//            InitializeComponent();

//            DataContext = this;

//            AdminUPN = string.Empty;
//            DeleteUserUPN = string.Empty;

//            RemoveAdmin = false;
//            IncludePersonalSite = false;
//            IncludeShareSite = true;
//            GroupIdDefined = false;

//        }

//        public async Task RunSolutionAsync(Action<LogInfo> uiLog, CancellationTokenSource cancelTokenSource)
//        {

//            RemoveUserSingleSiteAllAutoParameters parameters = new(AdminUPN, DeleteUserUPN)
//            {
//                RemoveAdmin = RemoveAdmin,
//                IncludePersonalSite = IncludePersonalSite,
//                IncludeShareSite = IncludeShareSite,
//                GroupIdDefined = GroupIdDefined,
//            };
//            await new RemoveUserSingleSiteAllAuto(parameters, uiLog, cancelTokenSource).RunAsync();

//        }

//        private void CheckBoxIncludePersonalSites_Checked(object sender, RoutedEventArgs e)
//        {
//            CheckBoxIncludeGroupIdDefined.IsChecked = false;
//        }

//        private void CheckBoxIncludeShareSites_Unchecked(object sender, RoutedEventArgs e)
//        {
//            CheckBoxIncludeGroupIdDefined.IsChecked = false;
//        }

//        private void CheckBoxIncludeGroupIdDefined_Checked(object sender, RoutedEventArgs e)
//        {
//            CheckBoxIncludePersonalSites.IsChecked = false;
//            CheckBoxIncludeShareSites.IsChecked = true;
//        }
//    }

//}
