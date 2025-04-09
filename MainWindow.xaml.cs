using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using System.Web; // Для HttpUtility.HtmlEncode
using System.Net.Mime; // Для LinkedResource

namespace alesya_rassylka
{
    public partial class MainWindow : MetroWindow
    {
        private DataStore dataStore;
        private const string JsonFilePath = "customers.json";
        private const string LogFilePath = "error.log";
        private Sender selectedSender; // Храним выбранного отправителя
        private List<string> attachedFiles = new List<string>(); // Список путей к прикрепленным файлам

        public MainWindow()
        {
            InitializeComponent();
            LoadCustomers();
            // Устанавливаем стандартного отправителя при запуске
            selectedSender = dataStore.Senders.Find(s => s.IsDefault);
            if (selectedSender != null)
            {
                SenderTextBox.Text = selectedSender.Email;
            }
        }

        private (string htmlBody, List<(string cid, string filePath)> embeddedImages) ConvertRichTextBoxToHtml(RichTextBox richTextBox)
        {
            var htmlBody = new System.Text.StringBuilder();
            var embeddedImages = new List<(string cid, string filePath)>();
            int imageCounter = 0;

            htmlBody.Append("<html><body>");

            foreach (Block block in richTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    htmlBody.Append("<p>");
                    foreach (Inline inline in paragraph.Inlines)
                    {
                        if (inline is Run run)
                        {
                            // Обрабатываем текст с форматированием
                            string text = run.Text;
                            if (string.IsNullOrEmpty(text)) continue;

                            bool isBold = run.FontWeight == FontWeights.Bold;
                            bool isItalic = run.FontStyle == FontStyles.Italic;
                            bool isUnderlined = run.TextDecorations != null && run.TextDecorations.Contains(TextDecorations.Underline[0]);

                            if (isBold) htmlBody.Append("<b>");
                            if (isItalic) htmlBody.Append("<i>");
                            if (isUnderlined) htmlBody.Append("<u>");

                            // Экранируем специальные символы
                            text = System.Web.HttpUtility.HtmlEncode(text);
                            // Заменяем переносы строк на <br>
                            text = text.Replace("\r\n", "<br>").Replace("\n", "<br>");
                            htmlBody.Append(text);

                            if (isUnderlined) htmlBody.Append("</u>");
                            if (isItalic) htmlBody.Append("</i>");
                            if (isBold) htmlBody.Append("</b>");
                        }
                        else if (inline is InlineUIContainer container && container.Child is Image image)
                        {
                            // Обрабатываем изображение
                            if (image.Source is BitmapImage bitmapImage)
                            {
                                string imagePath = bitmapImage.UriSource.LocalPath;
                                string cid = $"image{imageCounter++}";
                                embeddedImages.Add((cid, imagePath));
                                htmlBody.Append($"<img src=\"cid:{cid}\" width=\"{image.Width}\" height=\"{image.Height}\" />");
                            }
                        }
                    }
                    htmlBody.Append("</p>");
                }
            }

            htmlBody.Append("</body></html>");
            return (htmlBody.ToString(), embeddedImages);
        }

        private void LoadCustomers()
        {
            try
            {
                if (File.Exists(JsonFilePath))
                {
                    string json = File.ReadAllText(JsonFilePath);
                    dataStore = JsonSerializer.Deserialize<DataStore>(json) ?? new DataStore();
                }
                else
                {
                    dataStore = new DataStore();
                    SaveCustomers();
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке JSON", ex);
                ShowDetailedError("Ошибка загрузки данных", ex);
            }
        }

        private void SaveCustomers()
        {
            try
            {
                var allCategoriesFromRecipients = dataStore.Recipients
                    .SelectMany(r => r.Categories)
                    .Distinct()
                    .ToList();
                foreach (var category in allCategoriesFromRecipients)
                {
                    if (!dataStore.Categories.Contains(category))
                    {
                        dataStore.Categories.Add(category);
                    }
                }

                string json = JsonSerializer.Serialize(dataStore, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(JsonFilePath, json);
            }
            catch (Exception ex)
            {
                LogError("Ошибка при сохранении JSON", ex);
                ShowDetailedError("Ошибка сохранения данных", ex);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var recipients = RecipientList.ItemsSource as IEnumerable<string>;

            if (recipients == null || !recipients.Any())
            {
                MessageBox.Show("Выберите хотя бы одного получателя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, есть ли содержимое в RichTextBox
            string message = new TextRange(MessageRichTextBox.Document.ContentStart, MessageRichTextBox.Document.ContentEnd).Text.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show("Введите сообщение!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedSender == null)
            {
                MessageBox.Show("Выберите отправителя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                foreach (var recipient in recipients)
                {
                    string recipientEmail = recipient.Split(new[] { " - " }, StringSplitOptions.None).Last().Trim();
                    SendEmail(recipientEmail, message); // Здесь message используется только для проверки, реальный контент обрабатывается в SendEmail
                }
                MessageBox.Show("Сообщения успешно отправлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogError("Ошибка отправки письма", ex);
                ShowDetailedError("Ошибка отправки письма", ex);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MessageRichTextBox.Document.Blocks.Clear();
            selectedSender = dataStore.Senders.Find(s => s.IsDefault);
            SenderTextBox.Text = selectedSender?.Email ?? string.Empty;
            RecipientList.ItemsSource = null;
            attachedFiles.Clear();
            AttachedFilesList.ItemsSource = null;
        }

        private void SendEmail(string recipientEmail, string message)
        {
            try
            {
                using (MailMessage mail = new MailMessage())
                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    mail.From = new MailAddress(selectedSender.Email);
                    mail.To.Add(recipientEmail);
                    mail.Subject = "Сообщение от компании";

                    // Преобразуем содержимое RichTextBox в HTML
                    var (htmlBody, embeddedImages) = ConvertRichTextBoxToHtml(MessageRichTextBox);

                    // Создаем альтернативное представление для HTML
                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");

                    // Добавляем встроенные изображения
                    foreach (var (cid, filePath) in embeddedImages)
                    {
                        if (File.Exists(filePath))
                        {
                            LinkedResource imageResource = new LinkedResource(filePath, "image/jpeg")
                            {
                                ContentId = cid,
                                TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                            };
                            htmlView.LinkedResources.Add(imageResource);
                        }
                    }

                    mail.AlternateViews.Add(htmlView);
                    mail.IsBodyHtml = true;

                    // Добавляем прикрепленные файлы (отдельные вложения)
                    foreach (var filePath in attachedFiles)
                    {
                        if (File.Exists(filePath))
                        {
                            mail.Attachments.Add(new Attachment(filePath));
                        }
                    }

                    smtp.Credentials = new NetworkCredential(selectedSender.Email, selectedSender.Password);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при отправке email", ex);
                throw;
            }
        }

        private void LogError(string context, Exception ex)
        {
            string logMessage = $"{DateTime.Now}: [{context}] {ex.Message}\n";
            File.AppendAllText(LogFilePath, logMessage);
        }

        private void ShowDetailedError(string title, Exception ex)
        {
            MessageBox.Show($"{ex.Message}\n{ex.StackTrace}", title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string template = "Уважаемый(ая) [Имя получателя],\n\n" +
                             "Мы рады предложить вам сотрудничество с компанией ATLANT! " +
                             "Наша компания специализируется на [указать сферу деятельности]. " +
                             "Мы предлагаем выгодные условия для партнеров, включая:\n" +
                             "- Скидки на оптовые заказы\n" +
                             "- Быструю доставку\n" +
                             "- Индивидуальный подход\n\n" +
                             "Будем рады обсудить детали! Свяжитесь с нами по телефону [ваш номер] или email [ваш email].\n\n" +
                             "С уважением,\nКоманда ATLANT";

            MessageRichTextBox.Document.Blocks.Clear();
            MessageRichTextBox.Document.Blocks.Add(new Paragraph(new Run(template)));
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string template = "Уважаемый(ая) [Имя получателя],\n\n" +
                             "Компания ATLANT рада предложить специальные условия для оптовиков!\n" +
                             "Мы подготовили для вас:\n" +
                             "- Скидку 20% на заказы от 100 единиц\n" +
                             "- Бесплатную доставку при заказе от 500 единиц\n" +
                             "- Персонального менеджера для вашего удобства\n\n" +
                             "Не упустите возможность! Свяжитесь с нами для оформления заказа: [ваш номер] или [ваш email].\n\n" +
                             "С уважением,\nКоманда ATLANT";

            MessageRichTextBox.Document.Blocks.Clear();
            MessageRichTextBox.Document.Blocks.Add(new Paragraph(new Run(template)));
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(dataStore, SaveCustomers, this)
            {
                Owner = this
            };
            settingsWindow.ShowDialog();

            // После закрытия окна настроек обновляем стандартного отправителя
            selectedSender = dataStore.Senders.Find(s => s.IsDefault);
            SenderTextBox.Text = selectedSender?.Email ?? string.Empty;
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Открываем справку");
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("О программе");
        }

        private void SelectRecipient_Click(object sender, RoutedEventArgs e)
        {
            var window = new SelectRecipientWindow(dataStore, SaveCustomers)
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                RecipientList.ItemsSource = window.SelectedRecipients.Select(r => $"{r.Name} - {r.Email}");
                SaveCustomers();
            }
        }

        private void SelectSender_Click(object sender, RoutedEventArgs e)
        {
            if (dataStore.Senders.Count == 0)
            {
                MessageBox.Show("Нет доступных отправителей. Добавьте отправителя в настройках.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Создаем диалоговое окно "на лету"
            var senderSelectionWindow = new Window
            {
                Title = "Выбор отправителя",
                Width = 300,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE3EB"))
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            var senderListBox = new ListBox
            {
                SelectionMode = SelectionMode.Single,
                ItemsSource = dataStore.Senders,
                Height = 300,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White
            };

            // Настраиваем шаблон для отображения email отправителя
            senderListBox.ItemTemplate = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            factory.SetValue(TextBlock.TextProperty, new Binding("Email"));
            factory.SetValue(TextBlock.MarginProperty, new Thickness(5));
            factory.SetValue(TextBlock.FontSizeProperty, 14.0);
            senderListBox.ItemTemplate.VisualTree = factory;

            // Отмечаем текущего стандартного отправителя
            var defaultSender = dataStore.Senders.Find(s => s.IsDefault);
            if (defaultSender != null)
            {
                senderListBox.SelectedItem = defaultSender;
            }

            var confirmButton = new Button
            {
                Content = "Подтвердить",
                Width = 100,
                Margin = new Thickness(0, 10, 0, 0),
                Background = Brushes.White,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                FontSize = 14,
                FontFamily = new FontFamily("Arial Black"),
                FontWeight = FontWeights.Bold,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5, 2, 5, 2)
            };

            Sender selectedSenderFromWindow = null;
            confirmButton.Click += (s, args) =>
            {
                selectedSenderFromWindow = senderListBox.SelectedItem as Sender;
                if (selectedSenderFromWindow == null)
                {
                    MessageBox.Show("Выберите отправителя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                senderSelectionWindow.DialogResult = true;
                senderSelectionWindow.Close();
            };

            stackPanel.Children.Add(senderListBox);
            stackPanel.Children.Add(confirmButton);
            senderSelectionWindow.Content = stackPanel;

            if (senderSelectionWindow.ShowDialog() == true)
            {
                selectedSender = selectedSenderFromWindow;
                SenderTextBox.Text = selectedSender.Email;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void InsertSmiley_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string smiley)
            {
                TextPointer caretPosition = MessageRichTextBox.CaretPosition;
                caretPosition.InsertTextInRun(smiley);
                MessageRichTextBox.CaretPosition = caretPosition.GetPositionAtOffset(smiley.Length);
            }
        }

        private void InsertImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg;*.gif)|*.png;*.jpeg;*.jpg;*.gif",
                Title = "Выберите изображение для вставки"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(openFileDialog.FileName)),
                        Width = 100, // Ограничиваем размер изображения
                        Height = 100
                    };
                    InlineUIContainer container = new InlineUIContainer(image);
                    TextPointer caretPosition = MessageRichTextBox.CaretPosition;
                    caretPosition.Paragraph.Inlines.Add(container);
                    MessageRichTextBox.CaretPosition = caretPosition.GetNextInsertionPosition(LogicalDirection.Forward);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при вставке изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FormatBold_Click(object sender, RoutedEventArgs e)
        {
            TextSelection selection = MessageRichTextBox.Selection;
            if (!selection.IsEmpty)
            {
                // Проверяем текущее состояние жирного шрифта
                object fontWeight = selection.GetPropertyValue(TextElement.FontWeightProperty);
                if (fontWeight != DependencyProperty.UnsetValue && Equals(fontWeight, FontWeights.Bold))
                {
                    // Если текст уже жирный, снимаем стиль
                    selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                }
                else
                {
                    // Если текст не жирный, применяем стиль
                    selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                }
            }
        }

        private void FormatItalic_Click(object sender, RoutedEventArgs e)
        {
            TextSelection selection = MessageRichTextBox.Selection;
            if (!selection.IsEmpty)
            {
                // Проверяем текущее состояние курсива
                object fontStyle = selection.GetPropertyValue(TextElement.FontStyleProperty);
                if (fontStyle != DependencyProperty.UnsetValue && Equals(fontStyle, FontStyles.Italic))
                {
                    // Если текст уже курсивный, снимаем стиль
                    selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
                }
                else
                {
                    // Если текст не курсивный, применяем стиль
                    selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
                }
            }
        }

        private void FormatUnderline_Click(object sender, RoutedEventArgs e)
        {
            TextSelection selection = MessageRichTextBox.Selection;
            if (!selection.IsEmpty)
            {
                // Проверяем текущее состояние подчеркивания
                object textDecorations = selection.GetPropertyValue(Inline.TextDecorationsProperty);
                if (textDecorations != DependencyProperty.UnsetValue && textDecorations is TextDecorationCollection decorations && decorations.Contains(TextDecorations.Underline[0]))
                {
                    // Если текст уже подчеркнутый, снимаем подчеркивание
                    selection.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
                }
                else
                {
                    // Если текст не подчеркнутый, применяем подчеркивание
                    selection.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Underline);
                }
            }
        }

        private void AttachFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Title = "Выберите файлы для прикрепления"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var filePath in openFileDialog.FileNames)
                {
                    attachedFiles.Add(filePath);
                }
                AttachedFilesList.ItemsSource = null;
                AttachedFilesList.ItemsSource = attachedFiles;
            }
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is string filePath)
            {
                attachedFiles.Remove(filePath);
                AttachedFilesList.ItemsSource = null;
                AttachedFilesList.ItemsSource = attachedFiles;
            }
        }
    }

    public class Sender
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsDefault { get; set; }
    }

    public class Recipient
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public List<string> Categories { get; set; } = new List<string>();
    }

    public class DataStore
    {
        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string>();
        public List<Recipient> Recipients { get; set; } = new List<Recipient>();
        public List<Sender> Senders { get; set; } = new List<Sender>();
    }
}