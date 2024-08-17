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
using NovaPointWPF.UserControls;

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



        private void SiteReportFormClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new SiteReportForm());
        }



        private void ListReportClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new ListReportForm());
        }



        private void ItemReportClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new ItemReportForm());
        }

        private void ShortcutODReportClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new ShortcutODReportForm());
        }

        private void PHLReportClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new PHLItemReportForm());
        }


        private void RecycleBinReportClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new RecycleBinReportForm());
        }



        private void PermissionsReportFormClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new PermissionsReportForm());
        }

        private void SharingLinksReportFormClick(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new SharingLinksReportForm());
        }

    }
}
