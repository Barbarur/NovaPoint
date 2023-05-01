using NovaPointWPF.Pages.Solutions.Report;
using NovaPointWPF.Pages.Solutions;
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
    /// Interaction logic for MenuReportPage.xaml
    /// </summary>
    public partial class MenuReportPage : Page
    {
        public MenuReportPage()
        {
            InitializeComponent();
        }
        private void GoToSolutionForm(ISolutionForm solutionForm)
        {
            Frame? mainFrame = Application.Current.MainWindow.FindName("MainWindowMainFrame") as Frame;

            if (mainFrame is not null) { mainFrame.Content = new SolutionBasePage(solutionForm); }
        }

        


        private void GoSiteCollAllReportForm(object sender, RoutedEventArgs e)
        {
            
            GoToSolutionForm(new SiteCollAllReportForm());
        }

        private void GoSiteAllReportForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new SiteAllReportForm());
        }

        private void GoAdminAllSiteSingleReportForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new AdminAllSiteSingleReportForm());
        }



        private void GoListAllSiteSingleReportForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new ListAllSiteSingleReportForm());
        }

        private void GoListAllSiteAllReportForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new ListAllSiteAllReportForm());
        }



        private void GoItemAllListSingleSiteSingleReportForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new ItemAllListSingleSiteSingleReportForm());
        }
        private void GoItemAllListAllSiteSingleReportForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new ItemAllListAllSiteSingleReportForm());
        }



        private void GoUserAllSiteSingleReportForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new UserAllSiteSingleReportForm());
        }

    }
}
