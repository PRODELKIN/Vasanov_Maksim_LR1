using Messenger_Vasanov_ISP33.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public partial class ChatsPage : Page
    {
        public class ChatItem
        {
            public int ChatId { get; set; }
            public int InterlocutorId { get; set; }
            public string InterlocutorName { get; set; }
            public string FirstLetter { get; set; }
            public bool HasUnread { get; set; }
        }

        public class MessageItem
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public bool IsOwnMessage { get; set; }
            public DateTime SentAt { get; set; }
            public bool IsRead { get; set; }
            public bool IsUnreadSeparator { get; set; }
            public bool IsDateSeparator { get; set; }
            public string DisplayTime => SentAt.ToString("HH:mm");
            public string DisplayDate => SentAt.ToString("dd MMMM yyyy");
        }

        int currentUserId;
        int? currentChatId;
        int? currentInterlocutorId;
        bool isUpdatingPhone = false;
        bool isChatBlocked = false;
        bool isBlockedByMe = false;

        List<ChatItem> chats = new List<ChatItem>();
        List<MessageItem> messages = new List<MessageItem>();

        public ChatsPage()
        {
            InitializeComponent();

            if (UserSession.CurrentUser == null)
            {
                NavigationService.Navigate(new Login());
                return;
            }

            currentUserId = UserSession.CurrentUser.ID_User;
            LoadChats();
        }

        private void SearchPhoneInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingPhone) return;
            isUpdatingPhone = true;

            string digits = Regex.Replace(SearchPhoneInput.Text, @"[^\d]", "");
            if (!digits.StartsWith("7")) digits = "7" + digits;
            if (digits.Length > 11) digits = digits.Substring(0, 11);

            string formatted = "+7";
            if (digits.Length > 1)
                formatted += "-" + digits.Substring(1, Math.Min(3, digits.Length - 1));
            if (digits.Length > 4)
                formatted += "-" + digits.Substring(4, Math.Min(3, digits.Length - 4));
            if (digits.Length > 7)
                formatted += "-" + digits.Substring(7, Math.Min(2, digits.Length - 7));
            if (digits.Length > 9)
                formatted += "-" + digits.Substring(9, Math.Min(2, digits.Length - 9));

            int pos = SearchPhoneInput.SelectionStart + (formatted.Length - SearchPhoneInput.Text.Length);
            SearchPhoneInput.Text = formatted;
            SearchPhoneInput.SelectionStart = Math.Max(0, Math.Min(SearchPhoneInput.Text.Length, pos));

            isUpdatingPhone = false;
        }

        private void AddChatBtn_Click(object sender, RoutedEventArgs e)
        {
            string phone = SearchPhoneInput.Text.Trim();

            if (phone.Length < 16)
            {
                ShowMessage("Введите полный номер!");
                return;
            }

            try
            {
                using (var db = new Messenger_Vasanov_ISP33Entities5())
                {
                    var user = db.Users.FirstOrDefault(u => u.Phone_number == phone);

                    if (user == null)
                    {
                        ShowMessage("Пользователь не найден!");
                        return;
                    }

                    if (user.ID_User == currentUserId)
                    {
                        ShowMessage("Это вы!");
                        return;
                    }

                    bool exists = db.Chats.Any(c =>
                        (c.ID_User1 == currentUserId && c.ID_User2 == user.ID_User) ||
                        (c.ID_User1 == user.ID_User && c.ID_User2 == currentUserId));

                    if (exists)
                    {
                        ShowMessage("Чат уже есть!");
                        return;
                    }

                    var chat = new Chats
                    {
                        ID_User1 = currentUserId,
                        ID_User2 = user.ID_User,
                        Created_at = DateTime.Now,
                        Is_blocked_by_user1 = false,
                        Is_blocked_by_user2 = false
                    };

                    db.Chats.Add(chat);
                    db.SaveChanges();

                    SearchPhoneInput.Text = "";
                    SearchMessage.Visibility = Visibility.Collapsed;
                    LoadChats();
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Ошибка: " + ex.Message);
            }
        }

        void ShowMessage(string text)
        {
            SearchMessage.Text = text;
            SearchMessage.Visibility = Visibility.Visible;
        }

        private void LoadChats()
        {
            try
            {
                using (var db = new Messenger_Vasanov_ISP33Entities5())
                {
                    var dbChats = db.Chats.Where(c =>
                        c.ID_User1 == currentUserId || c.ID_User2 == currentUserId).ToList().OrderByDescending(c => c.Created_at);

                    chats.Clear();

                    foreach (var chat in dbChats)
                    {
                        int interlocutorId = chat.ID_User1 == currentUserId ? chat.ID_User2 : chat.ID_User1;
                        var user = db.Users.Find(interlocutorId);
                        if (user == null) continue;

                        string lastName = DataEncryption.Decrypt(user.Last_name).Trim();
                        string firstName = DataEncryption.Decrypt(user.First_name).Trim();
                        string middleName = DataEncryption.Decrypt(user.Middle_name).Trim();

                        string shortName = lastName;
                        if (!string.IsNullOrEmpty(firstName))
                            shortName += " " + firstName[0] + ".";
                        if (!string.IsNullOrEmpty(middleName))
                            shortName += " " + middleName[0] + ".";

                        if (string.IsNullOrWhiteSpace(shortName) || shortName == ".")
                        {
                            shortName = user.Phone_number;
                        }

                        string displayName = $"{shortName} ({user.Phone_number})";

                        bool hasUnread = db.Messages.Any(m =>
                            m.ID_Chat == chat.ID_Chat &&
                            m.ID_Sender == interlocutorId &&
                            m.Is_read == false);

                        chats.Add(new ChatItem
                        {
                            ChatId = chat.ID_Chat,
                            InterlocutorId = interlocutorId,
                            InterlocutorName = displayName,
                            FirstLetter = !string.IsNullOrEmpty(lastName) ? lastName[0].ToString().ToUpper() :
                                         !string.IsNullOrEmpty(firstName) ? firstName[0].ToString().ToUpper() : "?",
                            HasUnread = hasUnread
                        });
                    }

                    ChatsListBox.ItemsSource = chats;

                    if (chats.Count == 0)
                    {
                        EmptyChatsPanel.Visibility = Visibility.Visible;
                        ChatsListBox.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        EmptyChatsPanel.Visibility = Visibility.Collapsed;
                        ChatsListBox.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void ChatsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatsListBox.SelectedItem is ChatItem chat)
            {
                currentChatId = chat.ChatId;
                currentInterlocutorId = chat.InterlocutorId;

                DialogPanel.Visibility = Visibility.Visible;
                NoChatSelectedPanel.Visibility = Visibility.Collapsed;

                DialogName.Text = chat.InterlocutorName.Split('(')[0].Trim();
                DialogAvatar.Text = chat.FirstLetter;

                CheckBlockStatus();

                LoadMessages();

                MarkMessagesAsRead();

                LoadChats();
            }
        }

        private void CheckBlockStatus()
        {
            if (!currentChatId.HasValue) return;

            try
            {
                using (var db = new Messenger_Vasanov_ISP33Entities5())
                {
                    var chat = db.Chats.Find(currentChatId.Value);
                    if (chat == null) return;

                    // Проверяем заблокировал ли текущий пользователь
                    bool blockedByMe = false;
                    if (chat.ID_User1 == currentUserId)
                        blockedByMe = chat.Is_blocked_by_user1 == true;
                    else
                        blockedByMe = chat.Is_blocked_by_user2 == true;

                    // Проверяет заблокировал ли собеседник
                    bool blockedByInterlocutor = false;
                    if (chat.ID_User1 == currentUserId)
                        blockedByInterlocutor = chat.Is_blocked_by_user2 == true;
                    else
                        blockedByInterlocutor = chat.Is_blocked_by_user1 == true;

                    // Чат заблокирован если кто-то из участников заблокировал
                    isChatBlocked = blockedByMe || blockedByInterlocutor;
                    isBlockedByMe = blockedByMe;

                    // Показываем кнопку блокировки
                    BlockBtn.Visibility = Visibility.Visible;

                    if (blockedByMe)
                    {
                        // Я заблокировал
                        BlockBtn.Content = "Разблокировать";
                        BlockBtn.Background = new SolidColorBrush(Color.FromRgb(68, 255, 68));
                        BlockedOverlay.Visibility = Visibility.Visible;
                        BlockedMessage.Text = "Вы добавили пользователя в чёрный список";
                        MessageInput.IsEnabled = false;
                        SendBtn.IsEnabled = false;
                    }
                    else if (blockedByInterlocutor)
                    {
                        // Собеседник заблокировал
                        BlockBtn.Content = "Заблокировать";
                        BlockBtn.Background = new SolidColorBrush(Color.FromRgb(255, 68, 68));
                        BlockedOverlay.Visibility = Visibility.Visible;
                        BlockedMessage.Text = "Пользователь добавил вас в чёрный список";
                        MessageInput.IsEnabled = false;
                        SendBtn.IsEnabled = false;
                    }
                    else
                    {
                        // Чат не заблокирован
                        BlockBtn.Content = "Заблокировать";
                        BlockBtn.Background = new SolidColorBrush(Color.FromRgb(255, 68, 68));
                        BlockedOverlay.Visibility = Visibility.Collapsed;
                        MessageInput.IsEnabled = true;
                        SendBtn.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка проверки блокировки: {ex.Message}");
            }
        }

        private void BlockBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!currentChatId.HasValue || !currentInterlocutorId.HasValue) return;

            try
            {
                using (var db = new Messenger_Vasanov_ISP33Entities5())
                {
                    var chat = db.Chats.Find(currentChatId.Value);
                    if (chat == null) return;

                    bool newBlockStatus;

                    if (chat.ID_User1 == currentUserId)
                    {
                        newBlockStatus = !(chat.Is_blocked_by_user1 == true);
                        chat.Is_blocked_by_user1 = newBlockStatus;
                    }
                    else
                    {
                        newBlockStatus = !(chat.Is_blocked_by_user2 == true);
                        chat.Is_blocked_by_user2 = newBlockStatus;
                    }

                    db.SaveChanges();

                    // Добавляем системное сообщение
                    string systemMessage = newBlockStatus ? "Вы добавили пользователя в чёрный список" : "Вы убрали пользователя из чёрного списка";
                    AddSystemMessage(systemMessage);

                    // Обновляем статус
                    CheckBlockStatus();
                    LoadMessages();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddSystemMessage(string text)
        {
            if (!currentChatId.HasValue) return;

            try
            {
                using (var db = new Messenger_Vasanov_ISP33Entities5())
                {
                    var msg = new Messages
                    {
                        ID_Chat = currentChatId.Value,
                        ID_Sender = currentUserId,
                        Text_content = text,
                        Sent_at = DateTime.Now,
                        Is_read = true
                    };

                    db.Messages.Add(msg);
                    db.SaveChanges();

                    messages.Add(new MessageItem
                    {
                        Id = msg.ID_Message,
                        Text = text,
                        IsOwnMessage = true,
                        SentAt = msg.Sent_at ?? DateTime.Now,
                        IsRead = true,
                        IsDateSeparator = false,
                        IsUnreadSeparator = false
                    });

                    MessagesListBox.ItemsSource = null;
                    MessagesListBox.ItemsSource = messages;

                    if (messages.Count > 0)
                        MessagesListBox.ScrollIntoView(messages[messages.Count - 1]);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка добавления системного сообщения: {ex.Message}");
            }
        }

        private void LoadMessages()
        {
            if (!currentChatId.HasValue) return;

            try
            {
                using (var db = new Messenger_Vasanov_ISP33Entities5())
                {
                    var dbMessages = db.Messages
                        .Where(m => m.ID_Chat == currentChatId.Value)
                        .OrderBy(m => m.Sent_at)
                        .ToList();

                    messages.Clear();

                    int firstUnreadIndex = -1;
                    for (int i = 0; i < dbMessages.Count; i++)
                    {
                        var msg = dbMessages[i];
                        if (msg.Is_read == false && msg.ID_Sender != currentUserId)
                        {
                            firstUnreadIndex = i;
                            break;
                        }
                    }

                    DateTime? lastDate = null;
                    int unreadAdded = 0;

                    for (int i = 0; i < dbMessages.Count; i++)
                    {
                        var msg = dbMessages[i];
                        DateTime msgDate = msg.Sent_at?.Date ?? DateTime.Now.Date;

                        if (lastDate == null || msgDate != lastDate.Value)
                        {
                            messages.Add(new MessageItem
                            {
                                Text = msgDate.ToString("dd MMMM yyyy"),
                                IsOwnMessage = false,
                                SentAt = msgDate,
                                IsRead = true,
                                IsDateSeparator = true,
                                IsUnreadSeparator = false
                            });
                            lastDate = msgDate;
                        }

                        if (i == firstUnreadIndex && firstUnreadIndex > 0 && unreadAdded == 0)
                        {
                            messages.Add(new MessageItem
                            {
                                Text = "Непрочитанные",
                                IsOwnMessage = false,
                                SentAt = DateTime.Now,
                                IsRead = true,
                                IsUnreadSeparator = true,
                                IsDateSeparator = false
                            });
                            unreadAdded = 1;
                        }

                        messages.Add(new MessageItem
                        {
                            Id = msg.ID_Message,
                            Text = msg.Text_content,
                            IsOwnMessage = msg.ID_Sender == currentUserId,
                            SentAt = msg.Sent_at ?? DateTime.Now,
                            IsRead = msg.Is_read == true,
                            IsDateSeparator = false,
                            IsUnreadSeparator = false
                        });
                    }

                    MessagesListBox.ItemsSource = messages;

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (firstUnreadIndex > 0)
                        {
                            for (int i = 0; i < messages.Count; i++)
                            {
                                if (messages[i].IsUnreadSeparator)
                                {
                                    MessagesListBox.ScrollIntoView(messages[i]);
                                    break;
                                }
                            }
                        }
                        else if (messages.Count > 0)
                        {
                            MessagesListBox.ScrollIntoView(messages[messages.Count - 1]);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void SendMessage()
        {
            if (isChatBlocked)
            {
                MessageBox.Show("Чат заблокирован! Вы не можете отправлять сообщения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (!currentChatId.HasValue) return;

            try
            {
                using (var db = new Messenger_Vasanov_ISP33Entities5())
                {
                    var msg = new Messages
                    {
                        ID_Chat = currentChatId.Value,
                        ID_Sender = currentUserId,
                        Text_content = text,
                        Sent_at = DateTime.Now,
                        Is_read = false
                    };

                    db.Messages.Add(msg);
                    db.SaveChanges();

                    messages.Add(new MessageItem
                    {
                        Id = msg.ID_Message,
                        Text = text,
                        IsOwnMessage = true,
                        SentAt = msg.Sent_at ?? DateTime.Now,
                        IsRead = false,
                        IsDateSeparator = false,
                        IsUnreadSeparator = false
                    });

                    MessagesListBox.ItemsSource = null;
                    MessagesListBox.ItemsSource = messages;

                    if (messages.Count > 0)
                        MessagesListBox.ScrollIntoView(messages[messages.Count - 1]);

                    MessageInput.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void MarkMessagesAsRead()
        {
            if (!currentChatId.HasValue) return;

            try
            {
                using (var db = new Messenger_Vasanov_ISP33Entities5())
                {
                    var unreadMessages = db.Messages
                        .Where(m => m.ID_Chat == currentChatId.Value
                                    && m.ID_Sender != currentUserId
                                    && m.Is_read == false)
                        .ToList();

                    foreach (var msg in unreadMessages)
                    {
                        msg.Is_read = true;
                    }

                    db.SaveChanges();

                    foreach (var msg in messages)
                    {
                        if (!msg.IsOwnMessage && !msg.IsRead && !msg.IsUnreadSeparator && !msg.IsDateSeparator)
                        {
                            msg.IsRead = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка отметки прочтения: {ex.Message}");
            }
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
                e.Handled = true;
            }
        }
    }
}