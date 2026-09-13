using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Messenger_Vasanov_ISP33
{
    public partial class Login : Page
    {
        private Random random = new Random();
        private string currentCaptchaText;
        private string generatedSmsCode;
        private bool isUpdatingText = false;

        public Login()
        {
            InitializeComponent();
            GenerateCaptcha();
        }

        private Color GetRandomNoiseColor()
        {
            return Color.FromRgb(
                (byte)random.Next(160, 220),
                (byte)random.Next(160, 220),
                (byte)random.Next(160, 220)
            );
        }

        private Color GetRandomLineColor()
        {
            return Color.FromRgb(
                (byte)random.Next(50, 120),
                (byte)random.Next(50, 120),
                (byte)random.Next(50, 120)
            );
        }

        private string GenerateRandomCaptcha()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            int length = random.Next(6, 9);
            char[] result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }

            return string.Concat(result);
        }

        private void GenerateCaptcha()
        {
            CaptchaContainer.Child = null;
            Grid grid = new Grid();
            currentCaptchaText = GenerateRandomCaptcha();

            TextBlock textBlock = new TextBlock
            {
                Text = currentCaptchaText,
                Foreground = Brushes.Black,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            double angle = random.Next(-15, 16);
            textBlock.RenderTransform = new RotateTransform(angle);
            grid.Children.Add(textBlock);

            CaptchaContainer.Background = new SolidColorBrush(GetRandomNoiseColor());

            int lineCount = random.Next(5, 9);
            for (int i = 0; i < lineCount; i++)
            {
                Line line = new Line
                {
                    X1 = random.Next(0, 150),
                    Y1 = random.Next(0, 40),
                    X2 = random.Next(0, 150),
                    Y2 = random.Next(0, 40),
                    Stroke = new SolidColorBrush(GetRandomLineColor()),
                    StrokeThickness = random.Next(1, 3)
                };
                grid.Children.Add(line);
            }

            CaptchaContainer.Child = grid;
        }

        private void CaptchaRefresh_Click(object sender, RoutedEventArgs e)
        {
            GenerateCaptcha();
            CaptchaInput.Clear();
        }

        private void LoginPhone_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingText)
            {
                return;
            }   

            isUpdatingText = true;

            string digits = Regex.Replace(LoginPhone.Text, @"[^\d]", "");

            if (!digits.StartsWith("7"))
            {
                digits = "7" + digits;
            }

            if (digits.Length > 11)
            {
                digits = digits.Substring(0, 11);
            }

            StringBuilder formatted = new StringBuilder("+7");

            if (digits.Length > 1)
            {
                formatted.Append("-").Append(digits.Substring(1, Math.Min(3, digits.Length - 1)));
            }
            if (digits.Length > 4)
            {
                formatted.Append("-").Append(digits.Substring(4, Math.Min(3, digits.Length - 4)));
            }
            if (digits.Length > 7)
            {
                formatted.Append("-").Append(digits.Substring(7, Math.Min(2, digits.Length - 7)));
            }
            if (digits.Length > 9)
            {
                formatted.Append("-").Append(digits.Substring(9, Math.Min(2, digits.Length - 9)));
            }

            int selectionStart = LoginPhone.SelectionStart;
            int oldLength = LoginPhone.Text.Length;

            LoginPhone.Text = formatted.ToString();

            int newPosition = selectionStart + (LoginPhone.Text.Length - oldLength);
            LoginPhone.SelectionStart = Math.Max(0, Math.Min(LoginPhone.Text.Length, newPosition));

            isUpdatingText = false;
        }

        private void SendSmsBtn_Click(object sender, RoutedEventArgs e)
        {
            string phone = LoginPhone.Text;

            if (phone.Length < 16)
            {
                MessageBox.Show("Введите корректный номер телефона полностью!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CaptchaInput.Text != currentCaptchaText)
            {
                MessageBox.Show("Неверный код капчи!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                GenerateCaptcha();
                CaptchaInput.Clear();
                return;
            }

            generatedSmsCode = random.Next(1000, 9999).ToString();
            MessageBox.Show($"На телефон {phone} отправлен код подтверждения: {generatedSmsCode}", "СМС-код отправлен", MessageBoxButton.OK, MessageBoxImage.Information);

            AuthStepPanel.Visibility = Visibility.Collapsed;
            SmsStepPanel.Visibility = Visibility.Visible;
            SmsCodeInput.Focus();
        }

        private void VerifyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SmsCodeInput.Text != generatedSmsCode)
            {
                MessageBox.Show("Неверный СМС-код подтверждения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                SmsCodeInput.Clear();
                return;
            }

            string phone = LoginPhone.Text;

            try
            {
                using (var context = new Entities.Messenger_Vasanov_ISP33Entities5())
                {
                    var user = context.Users.FirstOrDefault(u => u.Phone_number == phone);

                    if (user != null)
                    {
                        context.SaveChanges();

                        UserSession.CurrentUser = user;

                        string userName = "Пользователь";
                        if (user.First_name != null && user.First_name.Length > 0)
                        {
                            userName = DataEncryption.Decrypt(user.First_name);
                        }

                        MessageBox.Show($"Вы успешно вошли! С возвращением, {userName}!", "Успешный вход", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        var newUser = new Entities.Users
                        {
                            Phone_number = phone,
                            Last_name = DataEncryption.Encrypt(""),
                            First_name = DataEncryption.Encrypt("Пользователь"),
                            Middle_name = DataEncryption.Encrypt(""),
                        };

                        context.Users.Add(newUser);
                        context.SaveChanges();

                        UserSession.CurrentUser = newUser;

                        MessageBox.Show("Вы успешно зарегистрировались в мессенджере!\nПожалуйста, перейдите в раздел 'Профиль', чтобы заполнить ФИО.", "Успешная регистрация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    mainWindow?.NavigateToMainApp();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка работы с базой данных: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetAuthForm();
            }
        }

        private void BackToAuthBtn_Click(object sender, RoutedEventArgs e)
        {
            ResetAuthForm();
        }

        private void ResetAuthForm()
        {
            SmsStepPanel.Visibility = Visibility.Collapsed;
            AuthStepPanel.Visibility = Visibility.Visible;
            CaptchaInput.Clear();
            SmsCodeInput.Clear();
            GenerateCaptcha();
        }
    }
}