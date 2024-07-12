//using CamlBuilder;
//using Newtonsoft.Json.Linq;
//using NovaPointLibrary.Commands.SharePoint.User;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;

//namespace NovaPointWPF.UserControls
//{
//    /// <summary>
//    /// Interaction logic for UserForm.xaml
//    /// </summary>
//    public partial class UserForm : UserControl
//    {
//        private void AllUsersClick(object sender, RoutedEventArgs e)
//        {
//            SwapButtons(true);
//        }

//        public bool AllUsers
//        {
//            get { return (bool)GetValue(AllUsersProperty); }
//            set { SetValue(AllUsersProperty, value); }
//        }
//        public static readonly DependencyProperty AllUsersProperty =
//            DependencyProperty.Register("AllUsers", typeof(bool), typeof(UserForm), new FrameworkPropertyMetadata(defaultValue: true));



//        private void SingleUserClick(object sender, RoutedEventArgs e)
//        {
//            SwapButtons(false);
//        }

//        public bool SingleUser
//        {
//            get { return (bool)GetValue(SingleUserProperty); }
//            set { SetValue(SingleUserProperty, value); }
//        }
//        public static readonly DependencyProperty SingleUserProperty =
//            DependencyProperty.Register("SingleUser", typeof(bool), typeof(UserForm), new PropertyMetadata(defaultValue: false));

//        public string IncludeUserUPN
//        {
//            get { return (string)GetValue(IncludeUserUPNProperty); }
//            set { SetValue(IncludeUserUPNProperty, value); }
//        }
//        public static readonly DependencyProperty IncludeUserUPNProperty =
//            DependencyProperty.Register("IncludeUserUPN", typeof(string), typeof(UserForm), new PropertyMetadata(defaultValue: string.Empty));



//        private void ExternalUsersClick(object sender, RoutedEventArgs e)
//        {
//            SwapButtons(false);
//        }

//        public bool IncludeExternalUsers
//        {
//            get { return (bool)GetValue(IncludeExternalUsersProperty); }
//            set { SetValue(IncludeExternalUsersProperty, value); }
//        }
//        public static readonly DependencyProperty IncludeExternalUsersProperty =
//            DependencyProperty.Register("IncludeExternalUsers", typeof(bool), typeof(UserForm), new FrameworkPropertyMetadata(defaultValue: false));



//        private void SystemGroupsClick(object sender, RoutedEventArgs e)
//        {
//            SwapButtons(false);
//        }

//        public bool IncludeSystemGroups
//        {
//            get { return (bool)GetValue(IncludeSystemGroupsProperty); }
//            set { SetValue(IncludeSystemGroupsProperty, value); }
//        }
//        public static readonly DependencyProperty IncludeSystemGroupsProperty =
//            DependencyProperty.Register("IncludeSystemGroups", typeof(bool), typeof(UserForm), new FrameworkPropertyMetadata(defaultValue: false));

//        public bool IncludeEveryone
//        {
//            get { return (bool)GetValue(IncludeEveryoneProperty); }
//            set { SetValue(IncludeEveryoneProperty, value); }
//        }
//        public static readonly DependencyProperty IncludeEveryoneProperty =
//            DependencyProperty.Register("IncludeEveryone", typeof(bool), typeof(UserForm), new FrameworkPropertyMetadata(defaultValue: false));

//        public bool IncludeEveryoneExceptExternal
//        {
//            get { return (bool)GetValue(IncludeEveryoneExceptExternalProperty); }
//            set { SetValue(IncludeEveryoneExceptExternalProperty, value); }
//        }
//        public static readonly DependencyProperty IncludeEveryoneExceptExternalProperty =
//            DependencyProperty.Register("IncludeEveryoneExceptExternal", typeof(bool), typeof(UserForm), new FrameworkPropertyMetadata(defaultValue: false));



//        private void SwapButtons(bool allUsers)
//        {
//            if (allUsers)
//            {
//                SingleUser = false;
//                IncludeExternalUsers = false;
//                IncludeSystemGroups = false;
//                AllUsers = true;
//            }
//            else
//            {
//                AllUsers = false;
//            }

//            if (AllUsers) { AllUsersLabel.Visibility = Visibility.Visible; }
//            else { AllUsersLabel.Visibility = Visibility.Collapsed; }

//            if (SingleUser) { SingleUserPanel.Visibility = Visibility.Visible; }
//            else
//            {
//                IncludeUserUPN = string.Empty;
//                SingleUserPanel.Visibility = Visibility.Collapsed;
//            }

//            if (IncludeExternalUsers) { ExternalLabel.Visibility = Visibility.Visible; }
//            else { ExternalLabel.Visibility = Visibility.Collapsed; }

//            if (IncludeSystemGroups) { SystemGroupPanel.Visibility = Visibility.Visible; }
//            else
//            {
//                IncludeEveryone = false;
//                IncludeEveryoneExceptExternal = false;
//                SystemGroupPanel.Visibility = Visibility.Collapsed;
//            }
//        }

//        public UserForm()
//        {
//            InitializeComponent();
//        }

//    }
//}
