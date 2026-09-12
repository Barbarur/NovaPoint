using NovaPointViewModels;
using NovaPointWPF.Pages.Solutions;
using NovaPointWPF.Pages.Solutions.QuickFix;
using NovaPointWPF.Platform;
using System.Windows;
using System.Windows.Controls;


namespace NovaPointWPF.Pages.Menus
{
    /// <summary>
    /// Interaction logic for MenuTroubleshootPage.xaml
    /// </summary>
    public partial class MenuQuickFixPage : Page
    {
        private readonly INavigationService _navigationService = new WpfNavigationService();

        public MenuQuickFixPage()
        {
            InitializeComponent();
        }

        private void GoToSolutionForm(ISolutionViewModel solutionForm)
        {
            _navigationService.NavigateTo(new SolutionPreparationPage(solutionForm));
        }

        // USER
        private void GoIdMismatchTroubleForm(object sender, RoutedEventArgs e)
        {
            GoToSolutionForm(new IdMismatchTroubleForm());
        }

    }
}
