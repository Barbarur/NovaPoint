using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace NovaPointWPF.Pages.DesignMaterial
{
    /// <summary>
    /// Interaction logic for NPSolutionFormHeader.xaml
    /// </summary>
    public partial class NPSolutionFormHeader : UserControl
    {

        public string SolutionTitle
        {
            get { return (string)GetValue(SolutionTitleProperty); }
            set { SetValue(SolutionTitleProperty, value); }
        }

        public static readonly DependencyProperty SolutionTitleProperty =
            DependencyProperty.Register("SolutionTitle", typeof(string), typeof(NPSolutionFormHeader),
                new PropertyMetadata("Solution"));


        public string SolutionCode
        {
            get { return (string)GetValue(SolutionCodeProperty); }
            set { SetValue(SolutionCodeProperty, value); }
        }

        public static readonly DependencyProperty SolutionCodeProperty =
            DependencyProperty.Register("SolutionCode", typeof(string), typeof(NPSolutionFormHeader),
                new PropertyMetadata(string.Empty));

        public string SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki";

        private void GoToDocumentation(object sender, RoutedEventArgs e)
        {
            var url = SolutionDocs.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }


        public NPSolutionFormHeader()
        {
            InitializeComponent();
        }
    }
}
