using NovaPointWPF.Pages.Solutions;
using NovaPointWPF.Pages.Solutions.QuickFix;
using System.Windows;
using System.Windows.Controls;


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
            Application.Current.MainWindow.Content = new SolutionPreparationPage(solutionForm);
        }

        // USER
        private void GoIdMismatchTroubleForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new IdMismatchTroubleForm());
        }

    }
}
