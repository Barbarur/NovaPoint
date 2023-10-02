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


        private void SiteAllReportFormClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new SiteAllReportForm());
        }



        private void ListReportClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new ListReportForm());
        }



        private void ItemReportClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new ItemReportForm());
        }



        private void UserAllSiteSingleReportFormClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new UserAllSiteSingleReportForm());
        }

        private void PermissionsAllSiteSingleReportFormClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new PermissionsAllSiteSingleReportForm());
        }

    }
}
