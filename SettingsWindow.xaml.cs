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
        private List<Sender> tempSenders;
        SolidColorBrush originalBackgroundBrush = null;
        SolidColorBrush originalLeftBorderBrush = null;

        // Статическое свойство для текущего цвета темы
        public static Color CurrentThemeColor { get; private set; } = (Color)ColorConverter.ConvertFromString("#DFE3EB");

        // Событие для уведомления об изменении темы
        public static event Action ThemeChanged;

        public SettingsWindow(DataStore dataStore, Action saveCallback, MainWindow mainWindow)
        {
            InitializeComponent();
            this.dataStore = dataStore;
            this.saveCallback = saveCallback;
            this.mainWindow = mainWindow;

            // Применяем текущую тему сразу при создании окна
            this.Loaded += (s, e) =>
            {
                this.Background = new SolidColorBrush(CurrentThemeColor);
            };

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

                // Загружаем текущую тему и выбираем соответствующий элемент в ComboBox
                string currentColorHex = CurrentThemeColor.ToString();
                foreach (ComboBoxItem item in ColorComboBox.Items)
                {
                    string itemColorHex = item.Tag.ToString();
                    if (currentColorHex.Replace("#FF", "#").Equals(itemColorHex, StringComparison.OrdinalIgnoreCase))
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
                    CurrentThemeColor = (Color)ColorConverter.ConvertFromString(colorHex);
                    ApplyThemeToAllWindows(CurrentThemeColor);
                    ThemeChanged?.Invoke();

                    // Применяем тему к текущему окну
                    this.Background = new SolidColorBrush(CurrentThemeColor);
                }
            };

            this.Closing += SettingsWindow_Closing;

            // Подписываемся на изменения темы
            ThemeChanged += () =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.Background = new SolidColorBrush(CurrentThemeColor);
                });
            };
        }

        private void ApplyThemeToAllWindows(Color color)
        {
            var newBrush = new SolidColorBrush(color);

            // Применяем тему ко всем окнам
            foreach (Window window in Application.Current.Windows)
            {
                window.Background = newBrush;

                // Особые стили для главного окна
                if (window is MainWindow mainWin)
                {
                    var leftBorder = FindVisualChild<Border>(mainWin, "LeftBorder");
                    if (leftBorder != null)
                    {
                        string colorHex = color.ToString();
                        leftBorder.Background = colorHex != "#FFDFE3EB" && colorHex != "#FFFFFFFF"
                            ? new SolidColorBrush(DarkenColor(color, 0.5))
                            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"));
                    }
                }
            }
        }


        private void SettingsWindow_Closing(object sender, CancelEventArgs e)
        {
            if (DialogResult != true)
            {
                // Восстанавливаем оригинальные цвета
                if (originalBackgroundBrush != null)
                {
                    CurrentThemeColor = originalBackgroundBrush.Color;
                    ApplyThemeToAllWindows(CurrentThemeColor);
                }

                var leftBorder = FindVisualChild<Border>(mainWindow, "LeftBorder");
                if (leftBorder != null && originalLeftBorderBrush != null)
                {
                    leftBorder.Background = originalLeftBorderBrush;
                }
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
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
                    Height = 255,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush(SettingsWindow.CurrentThemeColor),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                    BorderThickness = new Thickness(1),
                    Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png")),
                    TitleForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                    TitleCharacterCasing = CharacterCasing.Normal
                };

                // Подписка на изменение темы
                SettingsWindow.ThemeChanged += UpdateWindowTheme;
                editSenderWindow.Closed += (s, e) => SettingsWindow.ThemeChanged -= UpdateWindowTheme;

                void UpdateWindowTheme()
                {
                    editSenderWindow.Dispatcher.Invoke(() =>
                    {
                        editSenderWindow.Background = new SolidColorBrush(SettingsWindow.CurrentThemeColor);
                    });
                }

                Style CreateActionButtonStyle()
                {
                    var style = new Style(typeof(Button));
                    style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
                    style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
                    style.Setters.Add(new Setter(Control.FontSizeProperty, 16.0));
                    style.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Arial Black")));
                    style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
                    style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
                    style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
                    style.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
                    style.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
                    style.Setters.Add(new Setter(Control.MinHeightProperty, 30.0));
                    style.Setters.Add(new Setter(Control.TemplateProperty, CreateButtonTemplate()));
                    return style;
                }

                ControlTemplate CreateButtonTemplate()
                {
                    var template = new ControlTemplate(typeof(Button));
                    var border = new FrameworkElementFactory(typeof(Border));
                    border.Name = "border";
                    border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
                    border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
                    border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
                    border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
                    border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

                    var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                    contentPresenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    contentPresenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                    border.AppendChild(contentPresenter);

                    template.VisualTree = border;

                    var mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                    mouseOverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E6F8")), "border"));
                    template.Triggers.Add(mouseOverTrigger);

                    var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
                    pressedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0D0F0")), "border"));
                    template.Triggers.Add(pressedTrigger);

                    return template;
                }

                ControlTemplate CreateRoundedTextBoxTemplate()
                {
                    var template = new ControlTemplate(typeof(TextBox));
                    var border = new FrameworkElementFactory(typeof(Border));
                    border.Name = "Border";
                    border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
                    border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
                    border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
                    border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

                    var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
                    scrollViewer.Name = "PART_ContentHost";
                    scrollViewer.SetValue(ScrollViewer.MarginProperty, new Thickness(0));
                    border.AppendChild(scrollViewer);

                    template.VisualTree = border;
                    return template;
                }
                var stackPanel = new StackPanel { Margin = new Thickness(10) };

                var title = new TextBlock
                {
                    Text = "Редактирование отправителя:",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))
                };
                stackPanel.Children.Add(title);

                var emailLabel = new TextBlock
                {
                    Text = "Email:",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 5),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))
                };

                var emailTextBox = new TextBox
                {
                    
                    FontSize = 14,
                    Text = senderToEdit.Email,
                    Margin = new Thickness(0, 0, 0, 10),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                    BorderThickness = new Thickness(1),
                    Background = Brushes.White,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                    Padding = new Thickness(5),
                    Template = CreateRoundedTextBoxTemplate()
                };

                stackPanel.Children.Add(emailLabel);
                stackPanel.Children.Add(emailTextBox);

                var passwordLabel = new TextBlock
                {
                    Text = "Пароль:",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 5),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))
                };

                var passwordTextBox = new TextBox
                {
                    
                    FontSize = 14,
                    Text = senderToEdit.Password,
                    Margin = new Thickness(0, 0, 0, 15),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                    BorderThickness = new Thickness(1),
                    Background = Brushes.White,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                    Padding = new Thickness(5),
                    Template = CreateRoundedTextBoxTemplate()
                };

                stackPanel.Children.Add(passwordLabel);
                stackPanel.Children.Add(passwordTextBox);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var saveButton = new Button
                {
                    Content = "Применить",
                    Width = 125,
                    Height = 35,
                    Margin = new Thickness(0, 0, 10, 0),
                    Style = CreateActionButtonStyle()
                };

                var cancelButton = new Button
                {
                    Content = "Отменить",
                    Width = 125,
                    Height = 35,
                    Style = CreateActionButtonStyle()
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