using NovaPointLibrary.Commands.SharePoint.Site;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace NovaPointWPF.UserControls
{

    public partial class AdminForm : UserControl, INotifyPropertyChanged
    {
        public SPOAdminAccessParameters Parameters { get; set; } = new();
        public bool AddAdmin
        {
            get { return Parameters.AddAdmin; }
            set { Parameters.AddAdmin = value; }
        }

        public bool RemoveAdmin
        {
            get { return Parameters.RemoveAdmin; }
            set { Parameters.RemoveAdmin = value; }
        }

        public AdminForm()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
