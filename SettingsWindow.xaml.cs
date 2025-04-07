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
        private Sender rightClickedSender; // Для хранения отправителя, на котором кликнули правой кнопкой

        public SettingsWindow(DataStore dataStore, Action saveCallback, MainWindow mainWindow)
        {
            InitializeComponent();
            this.dataStore = dataStore;
            this.saveCallback = saveCallback;
            this.mainWindow = mainWindow;

            // Заполняем список отправителей
            SendersListBox.ItemsSource = dataStore.Senders;
            DefaultSenderComboBox.ItemsSource = dataStore.Senders;
            DefaultSenderComboBox.DisplayMemberPath = "Email";
            DefaultSenderComboBox.SelectedItem = dataStore.Senders.Find(s => s.IsDefault);

            // Устанавливаем текущий цвет приложения
            if (mainWindow.Background is SolidColorBrush backgroundBrush)
            {
                string currentColorHex = backgroundBrush.Color.ToString(); // Получаем HEX-значение цвета (например, #FFDFE3EB)
                foreach (ComboBoxItem item in ColorComboBox.Items)
                {
                    string itemColorHex = item.Tag.ToString(); // Например, #DFE3EB
                                                               // Приводим оба значения к одному формату (убираем "FF" из начала, если оно есть)
                    if (currentColorHex.Replace("#FF", "#") == itemColorHex)
                    {
                        ColorComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
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

            if (dataStore.Senders.Any(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Отправитель с таким email уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newSender = new Sender
            {
                Email = email,
                Password = password,
                IsDefault = dataStore.Senders.Count == 0 // Если это первый отправитель, делаем его стандартным
            };

            dataStore.Senders.Add(newSender);
            SendersListBox.ItemsSource = null;
            SendersListBox.ItemsSource = dataStore.Senders;
            DefaultSenderComboBox.ItemsSource = null;
            DefaultSenderComboBox.ItemsSource = dataStore.Senders;
            DefaultSenderComboBox.DisplayMemberPath = "Email";
            DefaultSenderComboBox.SelectedItem = newSender;

            SenderEmailTextBox.Text = string.Empty;
            SenderPasswordTextBox.Text = string.Empty;

            MessageBox.Show("Отправитель успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SendersListBox_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox != null)
            {
                // Находим элемент под курсором
                var point = e.GetPosition(listBox);
                var result = VisualTreeHelper.HitTest(listBox, point);
                if (result != null)
                {
                    var listBoxItem = FindVisualParent<ListBoxItem>(result.VisualHit);
                    if (listBoxItem != null)
                    {
                        rightClickedSender = listBoxItem.DataContext as Sender;
                        listBox.SelectedItem = rightClickedSender; // Устанавливаем выделение для визуальной обратной связи
                        e.Handled = true; // Предотвращаем дальнейшую обработку события
                    }
                    else
                    {
                        rightClickedSender = null;
                    }
                }
            }
        }

        // Вспомогательный метод для поиска родительского ListBoxItem
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null && !(child is T))
            {
                child = VisualTreeHelper.GetParent(child);
            }
            return child as T;
        }

        private void DeleteSender_Click(object sender, RoutedEventArgs e)
        {
            if (rightClickedSender == null)
            {
                MessageBox.Show("Выберите отправителя для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить отправителя {rightClickedSender.Email}?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                dataStore.Senders.Remove(rightClickedSender);
                if (rightClickedSender.IsDefault && dataStore.Senders.Count > 0)
                {
                    dataStore.Senders[0].IsDefault = true; // Назначаем нового стандартного отправителя
                }

                SendersListBox.ItemsSource = null;
                SendersListBox.ItemsSource = dataStore.Senders;
                DefaultSenderComboBox.ItemsSource = null;
                DefaultSenderComboBox.ItemsSource = dataStore.Senders;
                DefaultSenderComboBox.SelectedItem = dataStore.Senders.Find(s => s.IsDefault);

                rightClickedSender = null;
                MessageBox.Show("Отправитель успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditSender_Click(object sender, RoutedEventArgs e)
        {
            if (rightClickedSender == null)
            {
                MessageBox.Show("Выберите отправителя для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var senderToEdit = rightClickedSender;

            // Ввод нового email
            string newEmail = Interaction.InputBox("Введите новый email отправителя:", "Редактирование отправителя", senderToEdit.Email);
            if (string.IsNullOrWhiteSpace(newEmail) || !newEmail.Contains("@"))
            {
                MessageBox.Show("Некорректный email.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка на уникальность email (кроме текущего отправителя)
            if (dataStore.Senders.Any(s => s != senderToEdit && s.Email.Equals(newEmail, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Отправитель с таким email уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ввод нового пароля
            string newPassword = Interaction.InputBox("Введите новый пароль приложения:", "Редактирование отправителя", senderToEdit.Password);
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Пароль не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Обновляем данные отправителя
            senderToEdit.Email = newEmail;
            senderToEdit.Password = newPassword;

            SendersListBox.ItemsSource = null;
            SendersListBox.ItemsSource = dataStore.Senders;
            DefaultSenderComboBox.ItemsSource = null;
            DefaultSenderComboBox.ItemsSource = dataStore.Senders;
            DefaultSenderComboBox.SelectedItem = dataStore.Senders.Find(s => s.IsDefault);

            MessageBox.Show("Отправитель успешно отредактирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Сохраняем стандартного отправителя
            var selectedSender = DefaultSenderComboBox.SelectedItem as Sender;
            if (selectedSender != null)
            {
                foreach (var s in dataStore.Senders)
                {
                    s.IsDefault = false;
                }
                selectedSender.IsDefault = true;
            }

            // Сохраняем цвет приложения
            var selectedColorItem = ColorComboBox.SelectedItem as ComboBoxItem;
            if (selectedColorItem != null)
            {
                string colorHex = selectedColorItem.Tag.ToString();
                mainWindow.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            }

            saveCallback();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}