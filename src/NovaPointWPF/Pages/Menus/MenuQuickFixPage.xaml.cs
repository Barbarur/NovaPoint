using NovaPointWPF.Pages.Solutions;
using NovaPointWPF.Pages.Solutions.Automation;
using NovaPointWPF.Pages.Solutions.QuickFix;
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
    /// Interaction logic for MenuTroubleshootPage.xaml
    /// </summary>
    public partial class MenuQuickFixPage : Page
    {
        public MenuQuickFixPage()
        {
            InitializeComponent();
        }

        private void GoToSolutionForm(ISolutionForm solutionForm)
        {
            Frame? mainFrame = Application.Current.MainWindow.FindName("MainWindowMainFrame") as Frame;

            if (mainFrame is not null) { mainFrame.Content = new SolutionBasePage(solutionForm); }
        }

        // USER
        private void GoIdMismatchTroubleForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new IdMismatchTroubleForm());
        }

    }
}
