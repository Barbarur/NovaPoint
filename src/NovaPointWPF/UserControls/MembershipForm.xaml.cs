using NovaPointLibrary.Solutions.Report;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;


namespace NovaPointWPF.UserControls
{
    /// <summary>
    /// Interaction logic for MembershipForm.xaml
    /// </summary>
    public partial class MembershipForm : UserControl, INotifyPropertyChanged
    {
        public MembershipParameters Parameters { get; set; } = new();


        private bool _siteAdmins;
        public bool SiteAdmins
        {
            get { return _siteAdmins; }
            set
            {
                _siteAdmins = value;
                Parameters.SiteAdmins = value;
                OnPropertyChanged();
            }
        }

        private bool _owners;
        public bool Owners
        {
            get { return _owners; }
            set
            {
                _owners = value;
                Parameters.Owners = value;
                OnPropertyChanged();
            }
        }

        private bool _members = false;
        public bool Members
        {
            get { return _members; }
            set
            {
                _members = value;
                Parameters.Members = value;
                OnPropertyChanged();
            }
        }

        private bool _siteOwners = false;
        public bool SiteOwners
        {
            get { return _siteOwners; }
            set
            {
                _siteOwners = value;
                Parameters.SiteOwners = value;
                OnPropertyChanged();
            }
        }

        private bool _siteMembers = false;
        public bool SiteMembers
        {
            get { return _siteMembers; }
            set
            {
                _siteMembers = value;
                Parameters.SiteMembers = value;
                OnPropertyChanged();
            }
        }

        private bool _siteVisitors = false;
        public bool SiteVisitors
        {
            get { return _siteVisitors; }
            set
            {
                _siteVisitors = value;
                Parameters.SiteVisitors = value;
                OnPropertyChanged();
            }
        }

        public MembershipForm()
        {
            InitializeComponent();

            Owners = true;
            SiteAdmins = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
