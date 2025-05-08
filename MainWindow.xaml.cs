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
using System.Web;
using System.Net.Mime;
using System.ComponentModel;
using Microsoft.VisualBasic;

namespace alesya_rassylka
{
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

    public class Template
    {
        public string Name { get; set; }
        public string Content { get; set; }
    }

    public class TemplateCategory
    {
        public string Name { get; set; }
        public List<Template> Templates { get; set; } = new List<Template>();
    }

    public class DataStore
    {
        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string>();
        public List<Recipient> Recipients { get; set; } = new List<Recipient>();
        public List<Sender> Senders { get; set; } = new List<Sender>();
    }

    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public class AttachedFileInfo
        {
            public string FullPath { get; set; }
            public string FileName => System.IO.Path.GetFileName(FullPath);
        }

        private DataStore dataStore;
        private ObservableCollection<TemplateCategory> templateCategories;
        private const string JsonFilePath = "customers.json";
        private const string TemplatesFilePath = "templates.json";
        private const string LogFilePath = "error.log";
        private Sender selectedSender;
        private List<string> attachedFiles = new List<string>();
        private const string DefaultSubject = "Тема: Сообщение от компании"; // Значение темы по умолчанию
        private const string SubjectPrefix = "Тема: "; // Префикс, который нельзя удалить

        public MainWindow()
        {
            InitializeComponent();
            LoadCustomers();
            LoadTemplates();
            selectedSender = dataStore.Senders.Find(s => s.IsDefault);
            if (selectedSender != null)
            {
                SenderTextBox.Text = selectedSender.Email;
            }
            attachedFiles = new List<string>();
            DataContext = this;

            // Инициализируем MessageRichTextBox с пустым абзацем
            if (MessageRichTextBox.Document.Blocks.Count == 0)
            {
                MessageRichTextBox.Document.Blocks.Add(new Paragraph());
            }

            // Устанавливаем тему по умолчанию
            SubjectTextBox.Text = DefaultSubject;
        }

        public ObservableCollection<TemplateCategory> TemplateCategories
        {
            get { return templateCategories; }
            set
            {
                templateCategories = value;
                OnPropertyChanged(nameof(TemplateCategories));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                            string text = run.Text;
                            if (string.IsNullOrEmpty(text)) continue;

                            bool isBold = run.FontWeight == FontWeights.Bold;
                            bool isItalic = run.FontStyle == FontStyles.Italic;
                            bool isUnderlined = run.TextDecorations != null && run.TextDecorations.Contains(TextDecorations.Underline[0]);

                            if (isBold) htmlBody.Append("<b>");
                            if (isItalic) htmlBody.Append("<i>");
                            if (isUnderlined) htmlBody.Append("<u>");

                            text = System.Web.HttpUtility.HtmlEncode(text);
                            text = text.Replace("\r\n", "<br>").Replace("\n", "<br>");
                            htmlBody.Append(text);

                            if (isUnderlined) htmlBody.Append("</u>");
                            if (isItalic) htmlBody.Append("</i>");
                            if (isBold) htmlBody.Append("</b>");
                        }
                        else if (inline is InlineUIContainer container && container.Child is Image image)
                        {
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
                    string json = File.ReadAllText(JsonFilePath, System.Text.Encoding.UTF8);
                    var loadedData = JsonSerializer.Deserialize<DataStore>(json);
                    if (loadedData != null)
                    {
                        dataStore = loadedData;
                    }
                    else
                    {
                        dataStore = new DataStore();
                        SaveCustomers();
                    }
                }
                else
                {
                    dataStore = new DataStore();
                    SaveCustomers();
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке JSON (customers.json)", ex);
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

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(dataStore, options);
                File.WriteAllText(JsonFilePath, json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogError("Ошибка при сохранении JSON (customers.json)", ex);
                ShowDetailedError("Ошибка сохранения данных", ex);
            }
        }

        private void LoadTemplates()
        {
            try
            {
                if (File.Exists(TemplatesFilePath))
                {
                    string json = File.ReadAllText(TemplatesFilePath, System.Text.Encoding.UTF8);
                    var loadedTemplates = JsonSerializer.Deserialize<List<TemplateCategory>>(json);
                    TemplateCategories = new ObservableCollection<TemplateCategory>(loadedTemplates ?? new List<TemplateCategory>());
                }
                else
                {
                    TemplateCategories = new ObservableCollection<TemplateCategory>();
                    InitializeDefaultTemplates();
                    SaveTemplates();
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке JSON (templates.json)", ex);
                ShowDetailedError("Ошибка загрузки шаблонов", ex);
            }
        }

        private void SaveTemplates()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(templateCategories, options);
                File.WriteAllText(TemplatesFilePath, json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogError("Ошибка при сохранении JSON (templates.json)", ex);
                ShowDetailedError("Ошибка сохранения шаблонов", ex);
            }
        }

        private void InitializeDefaultTemplates()
        {
            TemplateCategories.Clear();
            TemplateCategories.Add(new TemplateCategory
            {
                Name = "Предложение о сотрудничестве",
                Templates = new List<Template>
                {
                    new Template
                    {
                        Name = "Стандартное предложение",
                        Content = "Уважаемый(ая) [Имя получателя],\n\n" +
                                  "Мы рады предложить вам сотрудничество с компанией ATLANT! " +
                                  "Наша компания специализируется на [указать сферу деятельности]. " +
                                  "Мы предлагаем выгодные условия для партнеров, включая:\n" +
                                  "- Скидки на оптовые заказы\n" +
                                  "- Быструю доставку\n" +
                                  "- Индивидуальный подход\n\n" +
                                  "Будем рады обсудить детали! Свяжитесь с нами по телефону [ваш номер] или email [ваш email].\n\n" +
                                  "С уважением,\nКоманда ATLANT"
                    }
                }
            });

            TemplateCategories.Add(new TemplateCategory
            {
                Name = "Специальные условия для оптовиков",
                Templates = new List<Template>
                {
                    new Template
                    {
                        Name = "Скидки и доставка",
                        Content = "Уважаемый(ая) [Имя получателя],\n\n" +
                                  "Компания ATLANT рада предложить специальные условия для оптовиков!\n" +
                                  "Мы подготовили для вас:\n" +
                                  "- Скидку 20% на заказы от 100 единиц\n" +
                                  "- Бесплатную доставку при заказе от 500 единиц\n" +
                                  "- Персонального менеджера для вашего удобства\n\n" +
                                  "Не упустите возможность! Свяжитесь с нами для оформления заказа: [ваш номер] или [ваш email].\n\n" +
                                  "С уважением,\nКоманда ATLANT"
                    }
                }
            });

            string[] otherCategories = new[]
            {
                "Анонс новой продукции для оптовиков",
                "Специальные акции для оптовиков",
                "Информация о логистике и доставке",
                "Приглашение на встречу или выставку",
                "Образовательный контент для закупщиков",
                "Благодарность за сотрудничество"
            };

            foreach (var categoryName in otherCategories)
            {
                TemplateCategories.Add(new TemplateCategory
                {
                    Name = categoryName,
                    Templates = new List<Template>()
                });
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            string categoryName = Interaction.InputBox("Введите название новой категории:",
                                                       "Добавление категории",
                                                     "");
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                MessageBox.Show("Название категории не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TemplateCategories.Any(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Категория с таким названием уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TemplateCategories.Add(new TemplateCategory
            {
                Name = categoryName,
                Templates = new List<Template>()
            });

            SaveTemplates();
            MessageBox.Show("Категория успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteTemplateCategory_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteTemplateCategory_Click called. Sender type: {sender.GetType().Name}");
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is Button button && button.Tag is TemplateCategory category)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить категорию '{category.Name}' и все её шаблоны?",
                                            "Подтверждение удаления",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    TemplateCategories.Remove(category);
                    SaveTemplates();
                    MessageBox.Show("Категория успешно удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected sender in DeleteTemplateCategory_Click: {sender.GetType().Name}");
            }
        }

        private void TemplateCategory_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"TemplateCategory_Click called. Sender type: {sender.GetType().Name}");
            if (sender is Button button && button.Tag is TemplateCategory category)
            {
                System.Diagnostics.Debug.WriteLine($"Opening TemplateManagerWindow for category: {category.Name}");
                var templateWindow = new TemplateManagerWindow(category, SaveTemplates)
                {
                    Owner = this
                };

                if (templateWindow.ShowDialog() == true && templateWindow.SelectedTemplate != null)
                {
                    MessageRichTextBox.Document.Blocks.Clear();
                    MessageRichTextBox.Document.Blocks.Add(new Paragraph(new Run(templateWindow.SelectedTemplate.Content)));
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected sender in TemplateCategory_Click: {sender.GetType().Name}");
            }
        }

        private void CategoryButton_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                System.Diagnostics.Debug.WriteLine("Opening ContextMenu for category button");
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                button.ContextMenu.IsOpen = true;
                e.Handled = true; // Предотвращаем дальнейшую маршрутизацию события
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

            // Проверяем тему
            string subject = SubjectTextBox.Text;
            if (string.IsNullOrWhiteSpace(subject) || subject == DefaultSubject)
            {
                subject = "Сообщение от компании"; // Если тема пустая или равна плейсхолдеру, используем значение по умолчанию
            }
            else if (subject.StartsWith(SubjectPrefix))
            {
                subject = subject.Substring(SubjectPrefix.Length).Trim(); // Убираем префикс "Тема: "
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                MessageBox.Show("Введите тему сообщения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                foreach (var recipient in recipients)
                {
                    string recipientEmail = recipient.Split(new[] { " - " }, StringSplitOptions.None).Last().Trim();
                    SendEmail(recipientEmail, message, subject);
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
            SubjectTextBox.Text = DefaultSubject; // Сбрасываем тему на значение по умолчанию
        }

        private void SendEmail(string recipientEmail, string message, string subject)
        {
            try
            {
                using (MailMessage mail = new MailMessage())
                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    mail.From = new MailAddress(selectedSender.Email);
                    mail.To.Add(recipientEmail);
                    mail.Subject = subject; // Используем переданную тему

                    var (htmlBody, embeddedImages) = ConvertRichTextBoxToHtml(MessageRichTextBox);
                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");

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
            File.AppendAllText(LogFilePath, logMessage, System.Text.Encoding.UTF8);
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
                MessageBox.Show("Нет доступных отправителей. Добавьте отправителя в настройках.",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var senderSelectionWindow = new Window
            {
                Title = "Выбор отправителя",
                Width = 350,
                Height = 440,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE3EB")),
                ResizeMode = ResizeMode.NoResize
            };

            var mainStackPanel = new StackPanel
            {
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Создаем стиль для ListBox
            var listBoxStyle = new Style(typeof(ListBox));

            // Основные свойства ListBox
            listBoxStyle.Setters.Add(new Setter(ListBox.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
            listBoxStyle.Setters.Add(new Setter(ListBox.BorderThicknessProperty, new Thickness(1)));
            listBoxStyle.Setters.Add(new Setter(ListBox.BackgroundProperty, Brushes.White));

            // Шаблон ListBox
            var listBoxTemplate = new ControlTemplate(typeof(ListBox));
            var listBoxBorder = new FrameworkElementFactory(typeof(Border));
            listBoxBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(ListBox.BackgroundProperty));
            listBoxBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(ListBox.BorderBrushProperty));
            listBoxBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(ListBox.BorderThicknessProperty));
            listBoxBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            var itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            scrollViewer.AppendChild(itemsPresenter);
            listBoxBorder.AppendChild(scrollViewer);

            listBoxTemplate.VisualTree = listBoxBorder;
            listBoxStyle.Setters.Add(new Setter(ListBox.TemplateProperty, listBoxTemplate));

            // Стиль для элементов ListBoxItem
            var listBoxItemStyle = new Style(typeof(ListBoxItem));

            // Шаблон ListBoxItem
            var listBoxItemTemplate = new ControlTemplate(typeof(ListBoxItem));
            var itemBorder = new FrameworkElementFactory(typeof(Border));
            itemBorder.Name = "Bd";
            itemBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(ListBoxItem.BackgroundProperty));
            itemBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(ListBoxItem.BorderBrushProperty));
            itemBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(ListBoxItem.BorderThicknessProperty));
            itemBorder.SetValue(Border.PaddingProperty, new TemplateBindingExtension(ListBoxItem.PaddingProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, new TemplateBindingExtension(ListBoxItem.HorizontalContentAlignmentProperty));
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, new TemplateBindingExtension(ListBoxItem.VerticalContentAlignmentProperty));
            itemBorder.AppendChild(contentPresenter);

            listBoxItemTemplate.VisualTree = itemBorder;

            // Триггеры для ListBoxItem
            var isSelectedTrigger = new Trigger()
            {
                Property = ListBoxItem.IsSelectedProperty,
                Value = true
            };
            isSelectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0D0F0")), "Bd"));
            isSelectedTrigger.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.Black));

            var isMouseOverTrigger = new Trigger()
            {
                Property = ListBoxItem.IsMouseOverProperty,
                Value = true
            };
            isMouseOverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E6F8")), "Bd"));

            listBoxItemTemplate.Triggers.Add(isSelectedTrigger);
            listBoxItemTemplate.Triggers.Add(isMouseOverTrigger);

            listBoxItemStyle.Setters.Add(new Setter(ListBoxItem.TemplateProperty, listBoxItemTemplate));
            listBoxStyle.Setters.Add(new Setter(ListBox.ItemContainerStyleProperty, listBoxItemStyle));

            // Создаем ListBox
            var senderListBox = new ListBox
            {
                SelectionMode = SelectionMode.Single,
                ItemsSource = dataStore.Senders,
                Height = 300,
                Style = listBoxStyle
            };

            // Создаем DataTemplate для элементов ListBox
            var dataTemplate = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            factory.SetValue(TextBlock.TextProperty, new Binding("Email"));
            factory.SetValue(TextBlock.MarginProperty, new Thickness(5));
            factory.SetValue(TextBlock.FontSizeProperty, 14.0);
            dataTemplate.VisualTree = factory;

            senderListBox.ItemTemplate = dataTemplate;

            // Устанавливаем выбранного отправителя по умолчанию
            var defaultSender = dataStore.Senders.FirstOrDefault(s => s.IsDefault);
            if (defaultSender != null)
            {
                senderListBox.SelectedItem = defaultSender;
            }

            // Стиль для кнопки с закруглёнными углами
            var buttonStyle = new Style(typeof(Button));
            var buttonBorderFactory = new FrameworkElementFactory(typeof(Border));
            buttonBorderFactory.Name = "border";
            buttonBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            buttonBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            buttonBorderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            buttonBorderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.Name = "contentPresenter";
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);

            buttonBorderFactory.AppendChild(contentPresenterFactory);

            var buttonTemplate = new ControlTemplate(typeof(Button));
            buttonTemplate.VisualTree = buttonBorderFactory;
            buttonStyle.Setters.Add(new Setter(Control.TemplateProperty, buttonTemplate));

            // Создаем кнопку подтверждения
            var confirmButton = new Button
            {
                Content = "Подтвердить",
                Width = 150,
                Height = 40,
                Margin = new Thickness(0, 20, 0, 0),
                Background = Brushes.White,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Style = buttonStyle
            };

            Sender selectedSenderFromWindow = null;
            confirmButton.Click += (s, args) =>
            {
                selectedSenderFromWindow = senderListBox.SelectedItem as Sender;
                if (selectedSenderFromWindow == null)
                {
                    MessageBox.Show("Выберите отправителя!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                senderSelectionWindow.DialogResult = true;
                senderSelectionWindow.Close();
            };

            // Добавляем элементы на окно
            mainStackPanel.Children.Add(senderListBox);
            mainStackPanel.Children.Add(confirmButton);
            senderSelectionWindow.Content = mainStackPanel;

            // Показываем окно и обрабатываем результат
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
                        Width = 100,
                        Height = 100
                    };
                    InlineUIContainer container = new InlineUIContainer(image);
                    TextPointer caretPosition = MessageRichTextBox.CaretPosition;

                    // Проверяем, есть ли текущий абзац
                    Paragraph currentParagraph = caretPosition.Paragraph;
                    if (currentParagraph == null)
                    {
                        // Если абзаца нет, создаем новый
                        currentParagraph = new Paragraph();
                        MessageRichTextBox.Document.Blocks.Add(currentParagraph);
                        caretPosition = currentParagraph.ContentStart; // Обновляем позицию курсора
                    }

                    // Добавляем изображение в абзац
                    currentParagraph.Inlines.Add(container);
                    MessageRichTextBox.CaretPosition = caretPosition.GetNextInsertionPosition(LogicalDirection.Forward) ?? caretPosition;
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
                object fontWeight = selection.GetPropertyValue(TextElement.FontWeightProperty);
                if (fontWeight != DependencyProperty.UnsetValue && Equals(fontWeight, FontWeights.Bold))
                {
                    selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                }
                else
                {
                    selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                }
            }
        }

        private void FormatItalic_Click(object sender, RoutedEventArgs e)
        {
            TextSelection selection = MessageRichTextBox.Selection;
            if (!selection.IsEmpty)
            {
                object fontStyle = selection.GetPropertyValue(TextElement.FontStyleProperty);
                if (fontStyle != DependencyProperty.UnsetValue && Equals(fontStyle, FontStyles.Italic))
                {
                    selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
                }
                else
                {
                    selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
                }
            }
        }

        private void FormatUnderline_Click(object sender, RoutedEventArgs e)
        {
            TextSelection selection = MessageRichTextBox.Selection;
            if (!selection.IsEmpty)
            {
                object textDecorations = selection.GetPropertyValue(Inline.TextDecorationsProperty);
                if (textDecorations != DependencyProperty.UnsetValue && textDecorations is TextDecorationCollection decorations && decorations.Contains(TextDecorations.Underline[0]))
                {
                    selection.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
                }
                else
                {
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
                UpdateAttachedFilesList();
            }
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AttachedFileInfo fileInfo)
            {
                attachedFiles.Remove(fileInfo.FullPath);
                UpdateAttachedFilesList();
            }
        }

        private void RemoveFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is MenuItem menuItem && menuItem.DataContext is AttachedFileInfo fileInfo)
            {
                attachedFiles.Remove(fileInfo.FullPath);
                UpdateAttachedFilesList();
            }
        }

        private void OpenFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is MenuItem menuItem && menuItem.DataContext is AttachedFileInfo fileInfo)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fileInfo.FullPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть файл: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowInFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is MenuItem menuItem && menuItem.DataContext is AttachedFileInfo fileInfo)
            {
                try
                {
                    string folderPath = System.IO.Path.GetDirectoryName(fileInfo.FullPath);
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fileInfo.FullPath}\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть папку: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateAttachedFilesList()
        {
            AttachedFilesList.ItemsSource = attachedFiles
                .Select(path => new AttachedFileInfo { FullPath = path })
                .ToList();
        }

        // Обработчик события GotFocus для SubjectTextBox
        private void SubjectTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SubjectTextBox.Text == DefaultSubject)
            {
                SubjectTextBox.Text = SubjectPrefix; // Оставляем только префикс "Тема: "
                SubjectTextBox.Foreground = Brushes.Black;
                SubjectTextBox.CaretIndex = SubjectTextBox.Text.Length; // Перемещаем курсор в конец
            }
        }

        // Обработчик события LostFocus для SubjectTextBox
        private void SubjectTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SubjectTextBox.Text == SubjectPrefix)
            {
                SubjectTextBox.Text = DefaultSubject; // Возвращаем значение по умолчанию
                SubjectTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#808080"));
            }
        }

        // Обработчик события TextChanged для SubjectTextBox
        private void SubjectTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Проверяем, начинается ли текст с "Тема: "
            if (!SubjectTextBox.Text.StartsWith(SubjectPrefix))
            {
                // Если префикс удалён, восстанавливаем его
                string userText = SubjectTextBox.Text.Length >= SubjectPrefix.Length
                    ? SubjectTextBox.Text.Substring(SubjectPrefix.Length)
                    : "";
                SubjectTextBox.Text = SubjectPrefix + userText;
                SubjectTextBox.CaretIndex = SubjectTextBox.Text.Length; // Перемещаем курсор в конец
            }

            // Если текст начинается с "Тема: ", но пользователь пытается удалить часть префикса
            string currentText = SubjectTextBox.Text;
            if (currentText.Length < SubjectPrefix.Length)
            {
                SubjectTextBox.Text = SubjectPrefix;
                SubjectTextBox.CaretIndex = SubjectTextBox.Text.Length; // Перемещаем курсор в конец
            }
        }
    }
}