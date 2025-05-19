using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        SolidColorBrush originalBackgroundBrush = null;
        SolidColorBrush originalLeftBorderBrush = null;

        public SettingsWindow(DataStore dataStore, Action saveCallback, MainWindow mainWindow)
        {
            InitializeComponent();
            this.dataStore = dataStore;
            this.saveCallback = saveCallback;
            this.mainWindow = mainWindow;

            // Инициализация отправителей
            tempSenders = dataStore.Senders.Select(s => new Sender
            {
                Email = s.Email,
                Password = s.Password,
                IsDefault = s.IsDefault
            }).ToList();

            SendersListBox.ItemsSource = tempSenders;
            DefaultSenderComboBox.ItemsSource = tempSenders;
            DefaultSenderComboBox.DisplayMemberPath = "Email";
            DefaultSenderComboBox.SelectedItem = tempSenders.Find(s => s.IsDefault);


            if (mainWindow.Background is SolidColorBrush backgroundBrush)
            {
                originalBackgroundBrush = backgroundBrush.Clone();

                var leftBorder = FindVisualChild<Border>(mainWindow, "LeftBorder");
                if (leftBorder != null && leftBorder.Background is SolidColorBrush leftBrush)
                {
                    originalLeftBorderBrush = leftBrush.Clone();
                }

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



            ColorComboBox.SelectionChanged += (sender, e) =>
            {
                if (ColorComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string colorHex)
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorHex);
                    mainWindow.Background = new SolidColorBrush(color);

                    var leftBorder = FindVisualChild<Border>(mainWindow, "LeftBorder");
                    if (leftBorder != null)
                    {
                        if (colorHex != "#DFE3EB" && colorHex != "#FFFFFF")
                        {
                            leftBorder.Background = new SolidColorBrush(DarkenColor(color, 0.5));
                        }
                        else
                        {
                            leftBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"));
                        }
                    }
                }
            };
            this.Closing += SettingsWindow_Closing;
        }
        private void SettingsWindow_Closing(object sender, CancelEventArgs e)
        {
            if (DialogResult != true) // если не было Save_Click (где DialogResult = true)
            {
                if (originalBackgroundBrush != null)
                    mainWindow.Background = originalBackgroundBrush;

                var leftBorder = FindVisualChild<Border>(mainWindow, "LeftBorder");
                if (leftBorder != null && originalLeftBorderBrush != null)
                    leftBorder.Background = originalLeftBorderBrush;
            }
        }

        // Метод для поиска элементов в визуальном дереве (особенно важно для MahApps)
        private static T FindVisualChild<T>(DependencyObject parent, string childName)
        where T : DependencyObject
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result && (result as FrameworkElement)?.Name == childName)
                return result;

            var foundChild = FindVisualChild<T>(child, childName);
            if (foundChild != null)
                return foundChild;
        }
        return null;
    }

    // Затемнение цвета (умножение RGB на коэффициент)
    private Color DarkenColor(Color color, double factor = 0.7)
    {
        return Color.FromArgb(
            color.A,
            (byte)(color.R * factor),
            (byte)(color.G * factor),
            (byte)(color.B * factor));
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

        private void EditSender_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"EditSender_Click: Sender type={sender.GetType().Name}, Tag={((sender as Button)?.Tag)?.GetType().Name}");
            if (sender is Button button && button.Tag is Sender senderToEdit)
            {
                var editSenderWindow = new MetroWindow
                {
                    Title = "Редактирование отправителя",
                    Width = 350,
                    Height = 290,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString("#F5F6F5"), 0.0),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#E0E7E9"), 1.0)
                }
                    },
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                    BorderThickness = new Thickness(1),
                    Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png"))
                };

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                var title = new TextBlock
                {
                    Text = "Введите новые данные отправителя:",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 15),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"))
                };
                stackPanel.Children.Add(title);

                var emailLabel = new TextBlock { Text = "Email:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5), Foreground = Brushes.Black };
                var emailTextBox = new TextBox { Width = 250, Height = 30, FontSize = 14, Text = senderToEdit.Email, Margin = new Thickness(0, 0, 0, 15), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")), BorderThickness = new Thickness(1), Background = Brushes.White };
                stackPanel.Children.Add(emailLabel);
                stackPanel.Children.Add(emailTextBox);

                var passwordLabel = new TextBlock { Text = "Пароль:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5), Foreground = Brushes.Black };
                var passwordTextBox = new TextBox { Width = 250, Height = 30, FontSize = 14, Text = senderToEdit.Password, Margin = new Thickness(0, 0, 0, 15), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")), BorderThickness = new Thickness(1), Background = Brushes.White };
                stackPanel.Children.Add(passwordLabel);
                stackPanel.Children.Add(passwordTextBox);

                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

                var saveButton = new Button
                {
                    Content = "Сохранить",
                    Width = 100,
                    Height = 35,
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Medium,
                    Cursor = Cursors.Hand
                };

                var cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 100,
                    Height = 35,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D3D3D3")),
                    Foreground = Brushes.Black,
                    FontSize = 14,
                    FontWeight = FontWeights.Medium,
                    Cursor = Cursors.Hand
                };

                saveButton.Click += (s, args) =>
                {
                    string newEmail = emailTextBox.Text.Trim();
                    string newPassword = passwordTextBox.Text.Trim();

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
                    editSenderWindow.Close();
                };
                cancelButton.Click += (s, args) => editSenderWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);
                stackPanel.Children.Add(buttonPanel);
                editSenderWindow.Content = stackPanel;
                editSenderWindow.ShowDialog();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("EditSender_Click: Invalid sender or tag");
                MessageBox.Show("Ошибка: отправитель не выбран.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
            sender.IsDefault = false;

        dataStore.Senders.First(sender => sender.Email == selectedSender.Email).IsDefault = true;
    }

    var selectedColorItem = ColorComboBox.SelectedItem as ComboBoxItem;
    if (selectedColorItem != null)
    {
        string colorHex = selectedColorItem.Tag.ToString();
        var color = (Color)ColorConverter.ConvertFromString(colorHex);

        // Меняем фон главного окна
        mainWindow.Background = new SolidColorBrush(color);

        var leftBorder = FindVisualChild<Border>(mainWindow, "LeftBorder");
        if (leftBorder != null)
        {
            if (!colorHex.Equals("#DFE3EB", StringComparison.OrdinalIgnoreCase) &&
                !colorHex.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase))
            {
                // Меняем фон панели и бордер
                Color panelColor = DarkenColor(color, 0.5);
                leftBorder.Background = new SolidColorBrush(panelColor);

                Color darkerBorder = DarkenColor(color, 0.7);
                leftBorder.BorderBrush = new SolidColorBrush(darkerBorder);
                leftBorder.BorderThickness = new Thickness(1);
            }
            else
            {
                // Для голубого и белого — фиксируем синий фон и бордер из XAML
                var fixedBlue = (Color)ColorConverter.ConvertFromString("#172A74");
                leftBorder.Background = new SolidColorBrush(fixedBlue);

                // Сбрасываем бордер к стилю из XAML
                leftBorder.ClearValue(Border.BorderBrushProperty);
                leftBorder.ClearValue(Border.BorderThicknessProperty);
            }

            leftBorder.InvalidateVisual();
        }
    }

    // Сохраняем изменения в файл
    saveCallback();

    DialogResult = true;
    Close();
}

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Восстанавливаем исходный цвет фона окна
            if (originalBackgroundBrush != null)
                mainWindow.Background = originalBackgroundBrush;

            // Восстанавливаем фон левой панели
            var leftBorder = FindVisualChild<Border>(mainWindow, "LeftBorder");
            if (leftBorder != null && originalLeftBorderBrush != null)
                leftBorder.Background = originalLeftBorderBrush;

            DialogResult = false;
            Close();
        }

    }
}