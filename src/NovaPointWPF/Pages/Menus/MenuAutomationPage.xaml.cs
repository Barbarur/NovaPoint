using NovaPointLibrary.Solutions.Automation;
using NovaPointWPF.Pages.Solutions;
using NovaPointWPF.Pages.Solutions.Automation;
using NovaPointWPF.Pages.Solutions.Report;
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

namespace NovaPointWPF.Pages.Menus
{
    /// <summary>
    /// Interaction logic for MenuAutomationPage.xaml
    /// </summary>
    public partial class MenuAutomationPage : Page
    {
        public MenuAutomationPage()
        {
            InitializeComponent();
        }

        private void GoToSolutionForm(ISolutionForm solutionForm)
        {
            Frame? mainFrame = Application.Current.MainWindow.FindName("MainWindowMainFrame") as Frame;

            if (mainFrame is not null) { mainFrame.Content = new SolutionBasePage(solutionForm); }
        }


        // SITES
        private void GoSetSiteCollectionAdminAllAutoForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new SetSiteCollectionAdminAllAutoForm());
        }

        // LISTS
        private void SetVersioningLimitAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new SetVersioningLimitAutoForm());
        }

        // ITEMS
        private void RemoveFileVersionAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new RemoveFileVersionAutoForm());
        }

        // RECYCLE BIN
        private void ClearRecycleBinAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new ClearRecycleBinAutoForm());
        }
        private void RestoreRecycleBinAutoClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new RestoreRecycleBinAutoForm());
        }


        // USER
        private void GoRemoveUserSingleSiteAllAutoForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new RemoveUserSingleSiteAllAutoForm());
        }

    }
}
