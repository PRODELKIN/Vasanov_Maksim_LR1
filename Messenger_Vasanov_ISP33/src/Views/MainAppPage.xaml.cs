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

namespace Messenger_Vasanov_ISP33
{
    public partial class MainAppPage : Page
    {
        public MainAppPage()
        {
            InitializeComponent();
            OpenChats();
        }

        private void ChatsTabBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenChats();
        }

        private void ProfileTabBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenProfile();
        }

        private void OpenChats()
        {
            ChatsTabBtn.Style = (Style)Application.Current.FindResource("Button");
            ProfileTabBtn.Style = (Style)Application.Current.FindResource("DarkButton");
            ContentFrame.Navigate(new ChatsPage());
        }

        private void OpenProfile()
        {
            ChatsTabBtn.Style = (Style)Application.Current.FindResource("DarkButton");
            ProfileTabBtn.Style = (Style)Application.Current.FindResource("Button");
            ContentFrame.Navigate(new ProfilePage());
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            UserSession.CurrentUser = null;
            NavigationService.Navigate(new Login());
        }
    }
}
