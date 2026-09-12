using NovaPointViewModels;
using NovaPointWPF.Pages.Solutions;
using NovaPointWPF.Pages.Solutions.Directory;
using NovaPointWPF.Platform;
using System.Windows;
using System.Windows.Controls;

namespace NovaPointWPF.Pages.Menus
{
    public partial class MenuDirectoryPage : Page
    {
        private readonly INavigationService _navigationService = new WpfNavigationService();

        public MenuDirectoryPage()
        {
            InitializeComponent();
        }

        private void GoToSolutionForm(ISolutionViewModel solutionForm)
        {
            _navigationService.NavigateTo(new SolutionPreparationPage(solutionForm));
        }

        private void GoGetDirectoryGroupForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new GetDirectoryGroupForm());
        }

    }
}
