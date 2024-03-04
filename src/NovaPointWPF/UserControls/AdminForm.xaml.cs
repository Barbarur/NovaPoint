using NovaPointLibrary.Commands.SharePoint.Site;
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

namespace NovaPointWPF.UserControls
{
    /// <summary>
    /// Interaction logic for AdminForm.xaml
    /// </summary>
    public partial class AdminForm : UserControl
    {
        public AdminForm()
        {
            InitializeComponent();
        }

        public SPOTenantSiteUrlsWithAccessParameters Parameters { get; set; } = new();

        private bool _removeAdmin = true;
        public bool RemoveAdmin
        {
            get { return _removeAdmin; }
            set
            {
                _removeAdmin = value;
                Parameters.RemoveAdmin = value;
            }
        }
    }
}
