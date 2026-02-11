using NovaPointWPF.Pages.Solutions;
using NovaPointWPF.Pages.Solutions.Directory;
using System.Windows;
using System.Windows.Controls;

namespace NovaPointWPF.Pages.Menus
{
    public partial class MenuDirectoryPage : Page
    {
        public MenuDirectoryPage()
        {
            InitializeComponent();
        }

        private void GoToSolutionForm(ISolutionForm solutionForm)
        {
            Application.Current.MainWindow.Content = new SolutionPreparationPage(solutionForm);
        }

        private void GoGetDirectoryGroupForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new GetDirectoryGroupForm());
        }

    }
}
