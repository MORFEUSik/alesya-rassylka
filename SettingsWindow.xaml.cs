using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;
using Microsoft.VisualBasic;

namespace alesya_rassylka
{
    public partial class SettingsWindow : MetroWindow
    {
        private DataStore dataStore;
        private Action saveCallback;
        private MainWindow mainWindow;
        private List<Sender> tempSenders; // Временная копия списка отправителей

        public SettingsWindow(DataStore dataStore, Action saveCallback, MainWindow mainWindow)
        {
            System.Diagnostics.Debug.WriteLine("Starting SettingsWindow initialization");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("InitializeComponent completed");
            this.dataStore = dataStore;
            this.saveCallback = saveCallback;
            this.mainWindow = mainWindow;

            // Создаём глубокую копию списка отправителей
            tempSenders = dataStore.Senders.Select(s => new Sender
            {
                Email = s.Email,
                Password = s.Password,
                IsDefault = s.IsDefault
            }).ToList();

            // Заполняем UI временной копией
            SendersListBox.ItemsSource = tempSenders;
            DefaultSenderComboBox.ItemsSource = tempSenders;
            DefaultSenderComboBox.DisplayMemberPath = "Email";
            DefaultSenderComboBox.SelectedItem = tempSenders.Find(s => s.IsDefault);

            // Устанавливаем текущий цвет приложения
            if (mainWindow.Background is SolidColorBrush backgroundBrush)
            {
                string currentColorHex = backgroundBrush.Color.ToString();
                foreach (ComboBoxItem item in ColorComboBox.Items)
                {
                    string itemColorHex = item.Tag.ToString();
                    if (currentColorHex.Replace("#FF", "#") == itemColorHex)
                    {
                        ColorComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // Подписываемся на событие закрытия окна
            Closing += SettingsWindow_Closing;
        }

        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // При закрытии крестиком сбрасываем изменения, не сохраняя
            DialogResult = false;
        }

        private void AddSender_Click(object sender, RoutedEventArgs e)
        {
            string email = SenderEmailTextBox.Text?.Trim();
            string password = SenderPasswordTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                MessageBox.Show("Введите корректный email!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите пароль приложения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (tempSenders.Any(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Отправитель с таким email уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newSender = new Sender
            {
                Email = email,
                Password = password,
                IsDefault = tempSenders.Count == 0
            };

            tempSenders.Add(newSender);
            SendersListBox.ItemsSource = null;
            SendersListBox.ItemsSource = tempSenders;
            DefaultSenderComboBox.ItemsSource = null;
            DefaultSenderComboBox.ItemsSource = tempSenders;
            DefaultSenderComboBox.DisplayMemberPath = "Email";
            DefaultSenderComboBox.SelectedItem = newSender;

            SenderEmailTextBox.Text = string.Empty;
            SenderPasswordTextBox.Text = string.Empty;

            MessageBox.Show("Отправитель успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteSender_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteSender_Click: Sender type={sender.GetType().Name}, Tag={((sender as Button)?.Tag)?.GetType().Name}");
            if (sender is Button button && button.Tag is Sender selectedSender)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить отправителя {selectedSender.Email}?",
                                            "Подтверждение удаления",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    tempSenders.Remove(selectedSender);
                    if (selectedSender.IsDefault && tempSenders.Count > 0)
                    {
                        tempSenders[0].IsDefault = true;
                    }

                    SendersListBox.ItemsSource = null;
                    SendersListBox.ItemsSource = tempSenders;
                    DefaultSenderComboBox.ItemsSource = null;
                    DefaultSenderComboBox.ItemsSource = tempSenders;
                    DefaultSenderComboBox.SelectedItem = tempSenders.Find(s => s.IsDefault);

                    MessageBox.Show("Отправитель успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DeleteSender_Click: Invalid sender or tag");
                MessageBox.Show("Ошибка: отправитель не выбран.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EditSender_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"EditSender_Click: Sender type={sender.GetType().Name}, Tag={((sender as Button)?.Tag)?.GetType().Name}");
            if (sender is Button button && button.Tag is Sender senderToEdit)
            {
                string newEmail = Interaction.InputBox("Введите новый email отправителя:",
                                                     "Редактирование отправителя",
                                                     senderToEdit.Email);
                if (string.IsNullOrWhiteSpace(newEmail) || !newEmail.Contains("@"))
                {
                    MessageBox.Show("Некорректный email.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (tempSenders.Any(s => s != senderToEdit && s.Email.Equals(newEmail, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Отправитель с таким email уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string newPassword = Interaction.InputBox("Введите новый пароль приложения:",
                                                        "Редактирование отправителя",
                                                        senderToEdit.Password);
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    MessageBox.Show("Пароль не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                senderToEdit.Email = newEmail;
                senderToEdit.Password = newPassword;

                SendersListBox.ItemsSource = null;
                SendersListBox.ItemsSource = tempSenders;
                DefaultSenderComboBox.ItemsSource = null;
                DefaultSenderComboBox.ItemsSource = tempSenders;
                DefaultSenderComboBox.SelectedItem = tempSenders.Find(s => s.IsDefault);

                MessageBox.Show("Отправитель успешно отредактирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("EditSender_Click: Invalid sender or tag");
                MessageBox.Show("Ошибка: отправитель не выбран.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Save_Click(object s, RoutedEventArgs e)
        {
            // Обновляем основной список отправителей временной копией
            dataStore.Senders.Clear();
            foreach (var sender in tempSenders)
            {
                dataStore.Senders.Add(new Sender
                {
                    Email = sender.Email,
                    Password = sender.Password,
                    IsDefault = sender.IsDefault
                });
            }

            var selectedSender = DefaultSenderComboBox.SelectedItem as Sender;
            if (selectedSender != null)
            {
                foreach (var sender in dataStore.Senders)
                {
                    sender.IsDefault = false;
                }
                dataStore.Senders.First(sender => sender.Email == selectedSender.Email).IsDefault = true;
            }

            var selectedColorItem = ColorComboBox.SelectedItem as ComboBoxItem;
            if (selectedColorItem != null)
            {
                string colorHex = selectedColorItem.Tag.ToString();
                mainWindow.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            }

            // Сохраняем изменения в файл
            saveCallback();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Сбрасываем изменения, не сохраняя
            DialogResult = false;
            Close();
        }
    }
}