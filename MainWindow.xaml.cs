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
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace alesya_rassylka
{

    public class ImageParameters
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public string FilePath { get; set; }
        public string StretchMode { get; set; } = "Uniform";
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
        private const string DefaultSubject = "Тема:"; // Значение темы по умолчанию
        private const string SubjectPrefix = "Тема: "; // Префикс, который нельзя удалить

        private FontFamily currentFontFamily = new FontFamily("Times New Roman");
        private double currentFontSize = 12;
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

            // Инициализируем MessageRichTextBox с пустым абзацем и текущими свойствами шрифта
            if (MessageRichTextBox.Document.Blocks.Count == 0)
            {
                var paragraph = new Paragraph();
                paragraph.FontFamily = currentFontFamily; // Используем currentFontFamily
                paragraph.FontSize = currentFontSize;     // Используем currentFontSize
                MessageRichTextBox.Document.Blocks.Add(paragraph);
            }

            // Устанавливаем тему по умолчанию
            SubjectTextBox.Text = DefaultSubject;

            // Устанавливаем начальные значения для ComboBox
            if (FontFamilyComboBox.Items.Count > 0)
            {
                FontFamilyComboBox.SelectedIndex = 1; // Устанавливаем "Times New Roman" как начальное значение
                currentFontFamily = new FontFamily("Times New Roman");
            }
            if (FontSizeComboBox.Items.Count > 0)
            {
                FontSizeComboBox.SelectedIndex = 1; // Устанавливаем "12" как начальное значение
                currentFontSize = 12;
            }

            // Подписываемся на события
            MessageRichTextBox.SelectionChanged += MessageRichTextBox_SelectionChanged;
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
    var embeddedImages = new List<(string cid, string filePath)>();
    int imageCounter = 0;

    var htmlBody = new System.Text.StringBuilder();
    htmlBody.Append("<html><body>");

    void ProcessBlocks(BlockCollection blocks)
    {
        foreach (Block block in blocks)
        {
            if (block is Paragraph paragraph)
            {
                htmlBody.Append("<p>");

                foreach (Inline inline in paragraph.Inlines)
                {
                    ProcessInline(inline);
                }

                htmlBody.Append("</p>");
            }
            else if (block is List list)
            {
                ProcessList(list);
            }
        }
    }

    void ProcessInline(Inline inline)
    {
        if (inline is Run run)
        {
            string text = run.Text;
            if (string.IsNullOrEmpty(text)) return;

            bool isBold = run.FontWeight == FontWeights.Bold;
            bool isItalic = run.FontStyle == FontStyles.Italic;
            bool isUnderlined = run.TextDecorations != null && run.TextDecorations.Contains(TextDecorations.Underline[0]);
            string fontFamily = run.FontFamily?.Source ?? "Times New Roman";
            double fontSize = run.FontSize > 0 ? run.FontSize : 12;

            htmlBody.Append($"<span style=\"font-family: {fontFamily}; font-size: {fontSize}pt;");
            if (isBold) htmlBody.Append(" font-weight: bold;");
            if (isItalic) htmlBody.Append(" font-style: italic;");
            if (isUnderlined) htmlBody.Append(" text-decoration: underline;");
            htmlBody.Append("\">");

            text = System.Web.HttpUtility.HtmlEncode(text);
            text = text.Replace("\r\n", "<br>").Replace("\n", "<br>");
            htmlBody.Append(text);

            htmlBody.Append("</span>");
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

    void ProcessList(List list)
    {
        string tag;
        switch (list.MarkerStyle)
        {
            case TextMarkerStyle.Disc:
                tag = "ul style=\"list-style-type: disc;\"";  // ●
                break;
            case TextMarkerStyle.Circle:
                tag = "ul style=\"list-style-type: circle;\""; // ○
                break;
            case TextMarkerStyle.Square:
                tag = "ul style=\"list-style-type: square;\""; // ■
                break;
            case TextMarkerStyle.Decimal:
                tag = "ol";
                break;
            default:
                tag = "ul";
                break;
        }

        htmlBody.Append($"<{tag}>");

        foreach (ListItem listItem in list.ListItems)
        {
            htmlBody.Append("<li>");

            // В ListItem может быть как Paragraph, так и вложенный List (рекурсивно)
            foreach (Block itemBlock in listItem.Blocks)
            {
                if (itemBlock is Paragraph para)
                {
                    foreach (Inline inline in para.Inlines)
                    {
                        ProcessInline(inline);
                    }
                }
                else if (itemBlock is List nestedList)
                {
                    ProcessList(nestedList);  // рекурсивно для вложенного списка
                }
            }

            htmlBody.Append("</li>");
        }

        htmlBody.Append($"</{tag.Split(' ')[0]}>"); // Закрываем тег, например, ul или ol
    }

    ProcessBlocks(richTextBox.Document.Blocks);

    htmlBody.Append("</body></html>");
    return (htmlBody.ToString(), embeddedImages);
}

        private void ListButton_Click(object sender, RoutedEventArgs e)
        {
            ListButton.ContextMenu.PlacementTarget = ListButton;
            ListButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            ListButton.ContextMenu.IsOpen = true;
        }

        public void RefreshTemplateCategories()
        {
            LoadTemplates(); // Перезагружаем шаблоны, чтобы обновить UI
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
                    //InitializeDefaultTemplates();
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

        //private void InitializeDefaultTemplates()
        //{
        //TemplateCategories.Clear();
        //TemplateCategories.Add(new TemplateCategory
        //{
        //    Name = "Предложение о сотрудничестве",
        //    Templates = new List<Template>
        //    {
        //        new Template
        //        {
        //            Name = "Стандартное предложение",
        //            Content = "Уважаемый(ая) [Имя получателя],\n\n" +
        //                      "Мы рады предложить вам сотрудничество с компанией ATLANT! " +
        //                      "Наша компания специализируется на [указать сферу деятельности]. " +
        //                      "Мы предлагаем выгодные условия для партнеров, включая:\n" +
        //                      "- Скидки на оптовые заказы\n" +
        //                      "- Быструю доставку\n" +
        //                      "- Индивидуальный подход\n\n" +
        //                      "Будем рады обсудить детали! Свяжитесь с нами по телефону [ваш номер] или email [ваш email].\n\n" +
        //                      "С уважением,\nКоманда ATLANT"
        //        }
        //    }
        //});

        //TemplateCategories.Add(new TemplateCategory
        //{
        //    Name = "Специальные условия для оптовиков",
        //    Templates = new List<Template>
        //    {
        //        new Template
        //        {
        //            Name = "Скидки и доставка",
        //            Content = "Уважаемый(ая) [Имя получателя],\n\n" +
        //                      "Компания ATLANT рада предложить специальные условия для оптовиков!\n" +
        //                      "Мы подготовили для вас:\n" +
        //                      "- Скидку 20% на заказы от 100 единиц\n" +
        //                      "- Бесплатную доставку при заказе от 500 единиц\n" +
        //                      "- Персонального менеджера для вашего удобства\n\n" +
        //                      "Не упустите возможность! Свяжитесь с нами для оформления заказа: [ваш номер] или [ваш email].\n\n" +
        //                      "С уважением,\nКоманда ATLANT"
        //        }
        //    }
        //});

        //string[] otherCategories = new[]
        //{
        //    "Анонс новой продукции для оптовиков",
        //    "Специальные акции для оптовиков",
        //    "Информация о логистике и доставке",
        //    "Приглашение на встречу или выставку",
        //    "Образовательный контент для закупщиков",
        //    "Благодарность за сотрудничество"
        //};

        //foreach (var categoryName in otherCategories)
        //{
        //    TemplateCategories.Add(new TemplateCategory
        //    {
        //        Name = categoryName,
        //        Templates = new List<Template>()
        //    });
        //}
        //}



        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            string categoryName = Interaction.InputBox(
                "Введите название новой категории:",
                "Добавление категории",
                "");

            // Если нажата "Отмена" или окно закрыто — не продолжаем
            if (categoryName == null)
                return;

            // Если строка пуста или состоит из пробелов — тоже не продолжаем
            if (string.IsNullOrWhiteSpace(categoryName))
                return;

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
            bool isSubjectEmpty = subject == SubjectPrefix;

            if (isSubjectEmpty)
            {
                // Показываем диалоговое окно подтверждения
                var result = MessageBox.Show("В этом сообщении не указана тема. Хотите отправить его?",
                                             "Подтверждение отправки",
                                             MessageBoxButton.OKCancel,
                                             MessageBoxImage.Question);

                if (result != MessageBoxResult.OK)
                {
                    return; // Прерываем отправку, если пользователь выбрал "Отменить"
                }

                subject = ""; // Устанавливаем пустую тему
            }
            else if (subject.StartsWith(SubjectPrefix))
            {
                subject = subject.Substring(SubjectPrefix.Length).Trim(); // Убираем префикс "Тема: "
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

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    string template = "Уважаемый(ая) [Имя получателя],\n\n" +
        //                     "Мы рады предложить вам сотрудничество с компанией ATLANT! " +
        //                     "Наша компания специализируется на [указать сферу деятельности]. " +
        //                     "Мы предлагаем выгодные условия для партнеров, включая:\n" +
        //                     "- Скидки на оптовые заказы\n" +
        //                     "- Быструю доставку\n" +
        //                     "- Индивидуальный подход\n\n" +
        //                     "Будем рады обсудить детали! Свяжитесь с нами по телефону [ваш номер] или email [ваш email].\n\n" +
        //                     "С уважением,\nКоманда ATLANT";

        //    MessageRichTextBox.Document.Blocks.Clear();
        //    MessageRichTextBox.Document.Blocks.Add(new Paragraph(new Run(template)));
        //}

        //private void Button_Click_1(object sender, RoutedEventArgs e)
        //{
        //    string template = "Уважаемый(ая) [Имя получателя],\n\n" +
        //                     "Компания ATLANT рада предложить специальные условия для оптовиков!\n" +
        //                     "Мы подготовили для вас:\n" +
        //                     "- Скидку 20% на заказы от 100 единиц\n" +
        //                     "- Бесплатную доставку при заказе от 500 единиц\n" +
        //                     "- Персонального менеджера для вашего удобства\n\n" +
        //                     "Не упустите возможность! Свяжитесь с нами для оформления заказа: [ваш номер] или [ваш email].\n\n" +
        //                     "С уважением,\nКоманда ATLANT";

        //    MessageRichTextBox.Document.Blocks.Clear();
        //    MessageRichTextBox.Document.Blocks.Add(new Paragraph(new Run(template)));
        //}

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

            var senderSelectionWindow = new MetroWindow
            {
                Title = "Выбор отправителя",
                Width = 350,
                Height = 403,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE3EB")),
                ResizeMode = ResizeMode.NoResize,
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png")),
                TitleForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1)
            };

            senderSelectionWindow.TitleCharacterCasing = CharacterCasing.Normal;

            var mainStackPanel = new StackPanel
            {
                Margin = new Thickness(10, 10, 10, 0), // уменьшаем отступы сверху и снизу
                VerticalAlignment = VerticalAlignment.Top
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

            // Используем существующий стиль ActionButton из XAML
            var confirmButton = new Button
            {
                Content = "Применить",
                Width = 200,
                Height = 40,
                Margin = new Thickness(0, 10, 0, 0),
                Style = (Style)FindResource("ActionButton")
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
                    // Загружаем изображение
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.EndInit();

                    // Определяем исходные размеры изображения
                    double originalWidth = bitmap.PixelWidth;
                    double originalHeight = bitmap.PixelHeight;

                    // Устанавливаем максимальные размеры
                    const double MaxWidth = 300;
                    const double MaxHeight = 200;

                    // Вычисляем новые размеры, сохраняя пропорции
                    double scale = Math.Min(MaxWidth / originalWidth, MaxHeight / originalHeight);
                    double newWidth = originalWidth * scale;
                    double newHeight = originalHeight * scale;

                    // Создаём изображение с новыми размерами
                    var image = new Image
                    {
                        Source = bitmap,
                        Width = newWidth,
                        Height = newHeight,
                        Stretch = Stretch.Uniform // По умолчанию
                    };

                    // Сохраняем параметры изображения
                    var parameters = new ImageParameters
                    {
                        Width = newWidth,
                        Height = newHeight,
                        FilePath = openFileDialog.FileName,
                        StretchMode = "Uniform"
                    };
                    image.Tag = parameters;

                    // Создаём контекстное меню для изображения
                    var contextMenu = new ContextMenu();
                    var editMenuItem = new MenuItem { Header = "Изменить параметры" };
                    editMenuItem.Click += (s, args) => EditImageParameters(image);
                    var deleteMenuItem = new MenuItem { Header = "Удалить" };
                    deleteMenuItem.Click += (s, args) => DeleteImage(image);
                    contextMenu.Items.Add(editMenuItem);
                    contextMenu.Items.Add(deleteMenuItem);
                    image.ContextMenu = contextMenu;

                    // Добавляем изображение в RichTextBox
                    InlineUIContainer container = new InlineUIContainer(image);
                    TextPointer caretPosition = MessageRichTextBox.CaretPosition;

                    Paragraph currentParagraph = caretPosition.Paragraph;
                    if (currentParagraph == null)
                    {
                        currentParagraph = new Paragraph();
                        MessageRichTextBox.Document.Blocks.Add(currentParagraph);
                        caretPosition = currentParagraph.ContentStart;
                    }

                    currentParagraph.Inlines.Add(container);
                    MessageRichTextBox.CaretPosition = caretPosition.GetNextInsertionPosition(LogicalDirection.Forward) ?? caretPosition;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при вставке изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MessageRichTextBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Получаем позицию клика в RichTextBox
                Point mousePosition = e.GetPosition(MessageRichTextBox);
                TextPointer pointer = MessageRichTextBox.GetPositionFromPoint(mousePosition, true);

                // Если не удалось определить позицию клика, прерываем обработку
                if (pointer == null)
                {
                    e.Handled = true;
                    return;
                }

                // Находим ближайший Inline элемент
                Inline inline = null;
                if (pointer.Parent is Inline parentInline)
                {
                    inline = parentInline;
                }
                else if (pointer.Parent is Paragraph paragraph)
                {
                    // Если кликнули на уровне Paragraph, ищем ближайший Inline
                    TextPointer current = pointer;
                    int maxIterations = 100; // Ограничение на количество итераций, чтобы избежать бесконечного цикла
                    while (current != null && current.Parent != null && !(current.Parent is Inline) && maxIterations > 0)
                    {
                        current = current.GetPositionAtOffset(0, LogicalDirection.Backward);
                        maxIterations--;
                    }

                    if (maxIterations == 0)
                    {
                        // Если превышено количество итераций, прерываем обработку
                        e.Handled = true;
                        return;
                    }

                    if (current != null && current.Parent is Inline foundInline)
                    {
                        inline = foundInline;
                    }
                }

                // Проверяем, является ли Inline элементом InlineUIContainer с изображением
                if (inline is InlineUIContainer container && container.Child is Image image && image.ContextMenu != null)
                {
                    // Открываем контекстное меню изображения
                    image.ContextMenu.PlacementTarget = image;
                    image.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                    image.ContextMenu.IsOpen = true;
                    e.Handled = true; // Предотвращаем дальнейшую обработку события
                }
                else
                {
                    // Если клик не на изображении, отключаем стандартное меню
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, чтобы понять, что пошло не так
                LogError("Ошибка при обработке клика правой кнопкой в RichTextBox", ex);
                e.Handled = true;
            }
        }

        private void EditImageParameters(Image image)
        {
            var parameters = (ImageParameters)image.Tag;
            var window = new Window
            {
                Title = "Параметры изображения",
                Width = 350,
                Height = 300, // Увеличиваем высоту для нового чекбокса
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png"))
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            // Поле для ширины
            stackPanel.Children.Add(new TextBlock { Text = "Ширина (px):" });
            var widthBox = new TextBox { Text = parameters.Width.ToString(), Margin = new Thickness(0, 5, 0, 10), Width = 100 };
            stackPanel.Children.Add(widthBox);

            // Поле для высоты
            stackPanel.Children.Add(new TextBlock { Text = "Высота (px):" });
            var heightBox = new TextBox { Text = parameters.Height.ToString(), Margin = new Thickness(0, 5, 0, 10), Width = 100 };
            stackPanel.Children.Add(heightBox);

            // Чекбокс для сохранения пропорций
            var keepAspectRatioCheckBox = new CheckBox
            {
                Content = "Сохранять пропорции",
                IsChecked = true,
                Margin = new Thickness(0, 5, 0, 10)
            };
            stackPanel.Children.Add(keepAspectRatioCheckBox);

            // Вычисляем исходное соотношение сторон
            double originalAspectRatio = parameters.Width / parameters.Height;

            // Обработчики изменения текста для сохранения пропорций
            widthBox.TextChanged += (s, e) =>
            {
                if (keepAspectRatioCheckBox.IsChecked == true)
                {
                    if (double.TryParse(widthBox.Text, out double newWidth) && newWidth > 0)
                    {
                        heightBox.Text = Math.Round(newWidth / originalAspectRatio).ToString();
                    }
                }
            };

            heightBox.TextChanged += (s, e) =>
            {
                if (keepAspectRatioCheckBox.IsChecked == true)
                {
                    if (double.TryParse(heightBox.Text, out double newHeight) && newHeight > 0)
                    {
                        widthBox.Text = Math.Round(newHeight * originalAspectRatio).ToString();
                    }
                }
            };

            // Выбор режима растяжения
            stackPanel.Children.Add(new TextBlock { Text = "Режим растяжения:" });
            var stretchCombo = new ComboBox
            {
                ItemsSource = new[] { "Uniform (пропорционально)", "Fill (растянуть полностью)", "UniformToFill (пропорционально с обрезкой)" },
                SelectedIndex = parameters.StretchMode switch
                {
                    "Fill" => 1,
                    "UniformToFill" => 2,
                    _ => 0 // Uniform по умолчанию
                },
                Margin = new Thickness(0, 5, 0, 10)
            };
            stackPanel.Children.Add(stretchCombo);

            // Кнопка подтверждения
            var confirmButton = new Button
            {
                Content = "Применить",
                Width = 100,
                Height = 25,
                Margin = new Thickness(0, 20, 0, 0),
                FontSize = 12,
                Style = (Style)FindResource("ActionButton")
            };
            confirmButton.Click += (s, args) =>
            {
                try
                {
                    parameters.Width = double.Parse(widthBox.Text);
                    parameters.Height = double.Parse(heightBox.Text);
                    parameters.StretchMode = stretchCombo.SelectedIndex switch
                    {
                        0 => "Uniform",
                        1 => "Fill",
                        2 => "UniformToFill",
                        _ => "Uniform"
                    };

                    image.Width = parameters.Width;
                    image.Height = parameters.Height;
                    if (parameters.StretchMode == "Uniform")
                        image.Stretch = Stretch.Uniform;
                    else if (parameters.StretchMode == "Fill")
                        image.Stretch = Stretch.Fill;
                    else if (parameters.StretchMode == "UniformToFill")
                        image.Stretch = Stretch.UniformToFill;

                    window.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при применении параметров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            stackPanel.Children.Add(confirmButton);

            window.Content = stackPanel;
            window.ShowDialog();
        }


        private void DeleteImage(Image image)
        {
            var container = image.Parent as InlineUIContainer;
            var paragraph = container?.Parent as Paragraph;
            if (paragraph != null && container != null)
            {
                // Удаляем изображение из абзаца
                paragraph.Inlines.Remove(container);
            }
        }

        private void FormatBold_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = MessageRichTextBox;
            var selection = richTextBox.Selection;

            object fontWeight = selection.GetPropertyValue(TextElement.FontWeightProperty);
            bool isBold = fontWeight != DependencyProperty.UnsetValue && Equals(fontWeight, FontWeights.Bold);

            if (!selection.IsEmpty)
            {
                selection.ApplyPropertyValue(TextElement.FontWeightProperty, isBold ? FontWeights.Normal : FontWeights.Bold);
            }
            else
            {
                TextPointer caret = richTextBox.CaretPosition;
                selection.Select(caret, caret);
                selection.ApplyPropertyValue(TextElement.FontWeightProperty, isBold ? FontWeights.Normal : FontWeights.Bold);
                richTextBox.CaretPosition = caret;
                richTextBox.Focus();
            }
        }


        private void FormatItalic_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = MessageRichTextBox;
            var selection = richTextBox.Selection;

            object fontStyle = selection.GetPropertyValue(TextElement.FontStyleProperty);
            bool isItalic = fontStyle != DependencyProperty.UnsetValue && Equals(fontStyle, FontStyles.Italic);

            if (!selection.IsEmpty)
            {
                selection.ApplyPropertyValue(TextElement.FontStyleProperty, isItalic ? FontStyles.Normal : FontStyles.Italic);
            }
            else
            {
                TextPointer caret = richTextBox.CaretPosition;
                selection.Select(caret, caret);
                selection.ApplyPropertyValue(TextElement.FontStyleProperty, isItalic ? FontStyles.Normal : FontStyles.Italic);
                richTextBox.CaretPosition = caret;
                richTextBox.Focus();
            }
        }


        private void FormatUnderline_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = MessageRichTextBox;
            var selection = richTextBox.Selection;

            // Определим, нужно ли убрать подчеркивание
            object current = selection.GetPropertyValue(Inline.TextDecorationsProperty);
            bool isUnderlined = current != DependencyProperty.UnsetValue &&
                                current is TextDecorationCollection decorations &&
                                decorations.Contains(TextDecorations.Underline[0]);

            // Если выделен текст — меняем его стиль
            if (!selection.IsEmpty)
            {
                selection.ApplyPropertyValue(Inline.TextDecorationsProperty, isUnderlined ? null : TextDecorations.Underline);
            }
            else
            {
                // При пустом выделении — устанавливаем стиль для позиции курсора
                TextPointer caret = richTextBox.CaretPosition;

                // Создаем пустую выделенную позицию (0 длины) — для применения свойства
                selection.Select(caret, caret);
                selection.ApplyPropertyValue(Inline.TextDecorationsProperty, isUnderlined ? null : TextDecorations.Underline);

                // Вернём каретку на место (иначе она может "сброситься")
                richTextBox.CaretPosition = caret;
                richTextBox.Focus(); // Вернём фокус, если ушёл
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

        private void MessageRichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (MessageRichTextBox == null || FontFamilyComboBox == null || FontSizeComboBox == null)
                return;

            var selection = MessageRichTextBox.Selection;

            // Получаем текущие значения из выделенного текста
            object fontFamilyObj = selection.GetPropertyValue(TextElement.FontFamilyProperty);
            object fontSizeObj = selection.GetPropertyValue(TextElement.FontSizeProperty);

            if (fontFamilyObj != DependencyProperty.UnsetValue && fontFamilyObj is FontFamily fontFamily)
            {
                var matchingFontItem = FontFamilyComboBox.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(item => item.Content.ToString() == fontFamily.ToString());
                if (matchingFontItem != null)
                {
                    FontFamilyComboBox.SelectionChanged -= FontFamilyComboBox_SelectionChanged;
                    FontFamilyComboBox.SelectedItem = matchingFontItem;
                    FontFamilyComboBox.SelectionChanged += FontFamilyComboBox_SelectionChanged;
                }

                // ⚠ Не обновляем currentFontFamily — он меняется только при ручной смене!
            }

            if (fontSizeObj != DependencyProperty.UnsetValue && fontSizeObj is double fontSize)
            {
                var matchingSizeItem = FontSizeComboBox.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(item => item.Content.ToString() == fontSize.ToString());
                if (matchingSizeItem != null)
                {
                    FontSizeComboBox.SelectionChanged -= FontSizeComboBox_SelectionChanged;
                    FontSizeComboBox.SelectedItem = matchingSizeItem;
                    FontSizeComboBox.SelectionChanged += FontSizeComboBox_SelectionChanged;
                }

                // ⚠ Не обновляем currentFontSize — он меняется только при ручной смене!
            }
        }



        private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontFamilyComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                currentFontFamily = new FontFamily(selectedItem.Content.ToString());

                var selection = MessageRichTextBox.Selection;
                if (!selection.IsEmpty)
                {
                    selection.ApplyPropertyValue(TextElement.FontFamilyProperty, currentFontFamily);
                }
                else
                {
                    // Не трогаем существующий текст! Просто применим к пустому диапазону,
                    // чтобы следующий ввод был с новым шрифтом
                    MessageRichTextBox.Focus();
                    MessageRichTextBox.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, currentFontFamily);
                }
            }
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontSizeComboBox.SelectedItem is ComboBoxItem selectedItem &&
                double.TryParse(selectedItem.Content.ToString(), out double fontSize))
            {
                currentFontSize = fontSize;

                var selection = MessageRichTextBox.Selection;
                if (!selection.IsEmpty)
                {
                    selection.ApplyPropertyValue(TextElement.FontSizeProperty, currentFontSize);
                }
                else
                {
                    // Только для последующего ввода
                    MessageRichTextBox.Focus();
                    MessageRichTextBox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, currentFontSize);
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

        private void CreateBulletList_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = MessageRichTextBox;
            TextPointer caretPosition = richTextBox.CaretPosition;

            // Создаем новый список с маркерами (точками)
            List list = new List
            {
                MarkerStyle = TextMarkerStyle.Disc
            };

            // Добавляем начальный элемент списка
            ListItem listItem = new ListItem(new Paragraph());
            list.ListItems.Add(listItem);

            // Заменяем текущий абзац или вставляем список
            if (caretPosition.Paragraph != null)
            {
                BlockCollection blocks = richTextBox.Document.Blocks;
                int index = -1;
                int currentIndex = 0;
                foreach (Block block in blocks)
                {
                    if (block == caretPosition.Paragraph)
                    {
                        index = currentIndex;
                        break;
                    }
                    currentIndex++;
                }

                if (index >= 0)
                {
                    blocks.InsertBefore(caretPosition.Paragraph, list);
                    blocks.Remove(caretPosition.Paragraph);
                }
                else
                {
                    blocks.Add(list);
                }
            }
            else
            {
                richTextBox.Document.Blocks.Add(list);
            }

            richTextBox.CaretPosition = listItem.ContentStart;
            richTextBox.Focus();
        }

        private void CreateNumberedList_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = MessageRichTextBox;
            TextPointer caretPosition = richTextBox.CaretPosition;

            // Создаем новый нумерованный список
            List list = new List
            {
                MarkerStyle = TextMarkerStyle.Decimal
            };

            // Добавляем начальный элемент списка
            ListItem listItem = new ListItem(new Paragraph());
            list.ListItems.Add(listItem);

            // Заменяем текущий абзац или вставляем список
            if (caretPosition.Paragraph != null)
            {
                BlockCollection blocks = richTextBox.Document.Blocks;
                int index = -1;
                int currentIndex = 0;
                foreach (Block block in blocks)
                {
                    if (block == caretPosition.Paragraph)
                    {
                        index = currentIndex;
                        break;
                    }
                    currentIndex++;
                }

                if (index >= 0)
                {
                    blocks.InsertBefore(caretPosition.Paragraph, list);
                    blocks.Remove(caretPosition.Paragraph);
                }
                else
                {
                    blocks.Add(list);
                }
            }
            else
            {
                richTextBox.Document.Blocks.Add(list);
            }

            richTextBox.CaretPosition = listItem.ContentStart;
            richTextBox.Focus();
        }

        private void CreateList(TextMarkerStyle markerStyle)
        {
            var richTextBox = MessageRichTextBox;
            TextPointer caretPosition = richTextBox.CaretPosition;

            List list = new List
            {
                MarkerStyle = markerStyle
            };
            ListItem listItem = new ListItem(new Paragraph());
            list.ListItems.Add(listItem);

            var blocks = richTextBox.Document.Blocks;
            if (caretPosition.Paragraph != null)
            {
                Block target = caretPosition.Paragraph;
                if (blocks.Contains(target))
                {
                    blocks.InsertBefore(target, list);
                    blocks.Remove(target);
                }
                else
                {
                    blocks.Add(list);
                }
            }
            else
            {
                blocks.Add(list);
            }

            richTextBox.CaretPosition = listItem.ContentStart;
            richTextBox.Focus();
        }

        private void CreateDiscList_Click(object sender, RoutedEventArgs e) =>
            CreateList(TextMarkerStyle.Disc);

        private void CreateCircleList_Click(object sender, RoutedEventArgs e) =>
            CreateList(TextMarkerStyle.Circle);

        private void CreateSquareList_Click(object sender, RoutedEventArgs e) =>
            CreateList(TextMarkerStyle.Square);


 private void MessageRichTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Tab)
    {
        var rtb = MessageRichTextBox;
        var caret = rtb.CaretPosition;
        var currentParagraph = caret.Paragraph;

        if (currentParagraph == null) return;

        // Проверяем, что курсор в ListItem
        if (currentParagraph.Parent is ListItem listItem)
        {
            e.Handled = true;

            // Сохраняем позицию курсора относительно ListItem
            var caretOffset = listItem.ContentStart.GetOffsetToPosition(caret);

            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                // Уменьшить уровень вложенности
                var parentList = listItem.Parent as List;
                if (parentList?.Parent is ListItem grandParentItem)
                {
                    // Удаляем из текущего списка
                    parentList.ListItems.Remove(listItem);

                    // Добавляем в родительский список после grandParentItem
                    var grandParentList = grandParentItem.Parent as List;
                    if (grandParentList != null)
                    {
                        int index = GetListItemIndex(grandParentList.ListItems, grandParentItem);
                        if (index >= 0)
                        {
                            InsertListItemAt(grandParentList.ListItems, index + 1, listItem);
                        }
                        else
                        {
                            grandParentList.ListItems.Add(listItem);
                        }
                    }
                }
            }
            else
            {
                // Увеличить уровень вложенности
                var parentList = listItem.Parent as List;
                int index = GetListItemIndex(parentList.ListItems, listItem);
                if (index > 0)
                {
                    var previousItem = GetListItemAt(parentList.ListItems, index - 1);
                    if (previousItem != null)
                    {
                        var nestedList = new List { MarkerStyle = parentList.MarkerStyle };
                        parentList.ListItems.Remove(listItem);
                        nestedList.ListItems.Add(listItem);

                        previousItem.Blocks.Add(nestedList);
                    }
                }
            }

            // После изменений восстановим позицию курсора
            rtb.Focus();
            var newCaretPos = listItem.ContentStart.GetPositionAtOffset(caretOffset);
            if (newCaretPos != null)
                rtb.CaretPosition = newCaretPos;
        }
    }
}

// Методы для работы с ListItemCollection
private int GetListItemIndex(ListItemCollection collection, ListItem item)
{
    int i = 0;
    foreach (var listItem in collection)
    {
        if (listItem == item) return i;
        i++;
    }
    return -1;
}

private ListItem GetListItemAt(ListItemCollection collection, int index)
{
    int i = 0;
    foreach (var item in collection)
    {
        if (i == index) return item;
        i++;
    }
    return null;
}

private void InsertListItemAt(ListItemCollection collection, int index, ListItem item)
{
    // ListItemCollection не поддерживает Insert, приходится пересоздавать коллекцию
    var tempList = new List<ListItem>(collection.Count + 1);
    int i = 0;
    foreach (var listItem in collection)
    {
        if (i == index)
            tempList.Add(item);
        tempList.Add(listItem);
        i++;
    }
    if (index >= collection.Count)
        tempList.Add(item);

    collection.Clear();
    foreach (var li in tempList)
        collection.Add(li);
}


    }
}

