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
    /// Interaction logic for ReportModeForm.xaml
    /// </summary>
    public partial class ReportModeForm : UserControl
    {
        public ReportModeForm()
        {
            InitializeComponent();
        }

        private bool _reportMode = true;
        public bool ReportMode
        {
            get { return _reportMode; }
            set
            {
                _reportMode = value;
            }
        }

    }
}
