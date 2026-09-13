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
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
            LoadUserData();
        }

        private void LoadUserData()
        {
            var user = UserSession.CurrentUser;
            if (user == null) return;

            PhoneTxt.Text = user.Phone_number;

            LastNameTxt.Text = DataEncryption.Decrypt(user.Last_name);
            FirstNameTxt.Text = DataEncryption.Decrypt(user.First_name);
            MiddleNameTxt.Text = DataEncryption.Decrypt(user.Middle_name);
        }

        private void SaveProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameTxt.Text))
            {
                MessageBox.Show("Поле 'Имя' обязательно для заполнения.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new Entities.Messenger_Vasanov_ISP33Entities5())
                {
                    int currentUserId = UserSession.CurrentUser.ID_User;
                    var userInDb = context.Users.Find(currentUserId);

                    if (userInDb != null)
                    {
                        userInDb.Last_name = DataEncryption.Encrypt(LastNameTxt.Text.Trim());
                        userInDb.First_name = DataEncryption.Encrypt(FirstNameTxt.Text.Trim());
                        userInDb.Middle_name = DataEncryption.Encrypt(MiddleNameTxt.Text.Trim());

                        context.SaveChanges();

                        UserSession.CurrentUser = userInDb;

                        MessageBox.Show("Данные профиля успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
