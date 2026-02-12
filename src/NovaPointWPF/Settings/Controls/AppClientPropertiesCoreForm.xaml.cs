using NovaPointLibrary.Core.Authentication;
using System.Windows.Controls;


namespace NovaPointWPF.Settings.Controls
{
    public partial class AppClientPropertiesCoreForm : UserControl
    {
        public IAppClientProperties Properties { get; init; }
        
        public AppClientPropertiesCoreForm(IAppClientProperties properties)
        {
            InitializeComponent();

            DataContext = properties;

            Properties = properties;
        }

        public void EnableForm()
        {
            TextBoxAppTitle.IsReadOnly = false;
            TextBoxAppTenantId.IsReadOnly = false;
            TextBoxAppClientId.IsReadOnly = false;
        }

        public void DisableForm()
        {
            TextBoxAppTitle.IsReadOnly = true;
            TextBoxAppTenantId.IsReadOnly = true;
            TextBoxAppClientId.IsReadOnly = true;
        }

    }
}
