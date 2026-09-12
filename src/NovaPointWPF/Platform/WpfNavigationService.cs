using NovaPointViewModels;
using System.Windows;
using System.Windows.Controls;

namespace NovaPointWPF.Platform
{
    public class WpfNavigationService : INavigationService
    {
        private readonly ContentControl _host;

        // Defaults to the main window, the target for every top-level transition today
        // (Menu -> SolutionPreparationPage, SolutionPreparationPage -> MainPage). MainPage
        // passes its own SolutionListFrame instead, for navigation local to that page.
        public WpfNavigationService() : this(Application.Current.MainWindow) { }

        public WpfNavigationService(ContentControl host)
        {
            _host = host;
        }

        public void NavigateTo(object view) => _host.Content = view;

        public void GoBack()
        {
            if (_host is MainWindow mainWindow)
                _host.Content = mainWindow.MainPage;
        }
    }
}
