using MahApps.Metro.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Net.NetworkInformation;
using System.Net.Http;


namespace alesya_rassylka
{

    public static class RichTextSerializationHelper
    {
        public static string SerializeFlowDocument(FlowDocument doc)
        {
            if (doc == null)
            {
                System.Diagnostics.Debug.WriteLine("SerializeFlowDocument: Document is null.");
                return "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" />";
            }

            try
            {
                // Сериализуем FlowDocument с помощью XamlWriter
                string xamlContent = XamlWriter.Save(doc);
                System.Diagnostics.Debug.WriteLine($"SerializeFlowDocument: Serialized length: {xamlContent.Length}");
                System.Diagnostics.Debug.WriteLine($"SerializeFlowDocument: XAML: {xamlContent}");

                // Проверяем, содержит ли документ текст
                var text = new TextRange(doc.ContentStart, doc.ContentEnd).Text;
                System.Diagnostics.Debug.WriteLine($"SerializeFlowDocument: Document text: {text}");

                if (string.IsNullOrWhiteSpace(text) && doc.Blocks.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("SerializeFlowDocument: Warning: Document is empty.");
                }

                return xamlContent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SerializeFlowDocument error: {ex.Message}");
                return $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>Ошибка сериализации: {System.Security.SecurityElement.Escape(ex.Message)}</Paragraph></FlowDocument>";
            }
        }

        public static FlowDocument DeserializeFlowDocument(string xamlContent)
        {
            var doc = new FlowDocument();
            if (string.IsNullOrWhiteSpace(xamlContent))
            {
                System.Diagnostics.Debug.WriteLine("DeserializeFlowDocument: XAML content is empty or whitespace.");
                return doc;
            }

            try
            {
                using var stringReader = new StringReader(xamlContent);
                using var xmlReader = XmlReader.Create(stringReader);
                var deserializedObject = XamlReader.Load(xmlReader);

                if (deserializedObject is FlowDocument loadedDoc)
                {
                    doc = loadedDoc;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"DeserializeFlowDocument: Deserialized object is of unexpected type {deserializedObject?.GetType().Name}.");
                    throw new InvalidOperationException("Deserialized XAML is not a valid FlowDocument.");
                }

                var docText = new TextRange(doc.ContentStart, doc.ContentEnd).Text;
                System.Diagnostics.Debug.WriteLine($"DeserializeFlowDocument: Loaded text: {docText}, Blocks: {doc.Blocks.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeserializeFlowDocument error: {ex.Message}");
                var paragraph = new Paragraph(new Run($"Ошибка десериализации XAML: {ex.Message}"));
                doc.Blocks.Add(paragraph);
            }

            return doc;
        }
    }


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
        private const string TemplateNamePrefix = "Название: ";
        private string backgroundImagePath;

        private FontFamily currentFontFamily = new FontFamily("Times New Roman");
        private double currentFontSize = 12;
        private Brush currentForeground = Brushes.Black; // Текущий цвет текста
        private bool isTemplateEditMode;
        private Template editingTemplate;
        private TemplateManagerWindow templateManagerWindow;

        private ObservableCollection<SolidColorBrush> customColors = new ObservableCollection<SolidColorBrush>(); // Храним пользовательские цвета
        private const string CustomColorsFilePath = "custom_colors.json"; // Путь к файлу пользовательских цветов
        private SolidColorBrush selectedColor; // Временное хранение выбранного цвета до нажатия "Применить"
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

        private (string htmlBody, List<(string cid, string filePath)> embeddedImages) ConvertRichTextBoxToHtml(RichTextBox richTextBox, string backgroundImagePath)
        {
            var embeddedImages = new List<(string cid, string filePath)>();
            int imageCounter = 0;
            string backgroundCid = null;

            var htmlBody = new System.Text.StringBuilder();
            htmlBody.Append("<html><body>");

            if (!string.IsNullOrEmpty(backgroundImagePath))
            {
                backgroundCid = $"bg{imageCounter++}";
                embeddedImages.Add((backgroundCid, backgroundImagePath));

                htmlBody.Append($@"
        <!--[if gte mso 9]>
        <v:background xmlns:v=""urn:schemas-microsoft-com:vml"" fill=""true"" stroke=""false"">
            <v:fill type=""frame"" src=""cid:{backgroundCid}"" color=""#ffffff"" />
        </v:background>
        <![endif]-->

        <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-image: url(cid:{backgroundCid}); background-size: cover; background-position: center; background-repeat: no-repeat;'>
          <tr>
            <td style='padding: 40px; font-family: Arial, sans-serif; font-size: 16px; line-height: 1.5; color: #ffffff;'>
        ");
            }

            void ProcessBlocks(BlockCollection blocks)
            {
                foreach (Block block in blocks)
                {
                    if (block is Paragraph paragraph)
                    {
                        string alignStyle = GetTextAlignmentStyle(paragraph.TextAlignment);
                        string fontFamily = paragraph.FontFamily?.Source ?? "Arial";
                        double fontSize = paragraph.FontSize > 0 ? paragraph.FontSize : 14;
                        string color = paragraph.Foreground is SolidColorBrush brush ? ColorToHex(brush.Color) : "#ffffff";
                        htmlBody.Append($"<p style=\"margin: 0 0 16px 0; font-family: {fontFamily}; font-size: {fontSize}pt; {alignStyle} color: {color};\">");

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
                    string fontFamily = run.FontFamily?.Source ?? "Arial";
                    double fontSize = run.FontSize > 0 ? run.FontSize : 14;
                    string color = run.Foreground is SolidColorBrush brush ? ColorToHex(brush.Color) : "#ffffff";

                    htmlBody.Append($"<span style=\"font-family: {fontFamily}; font-size: {fontSize}pt; color: {color};");
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
                string tag = list.MarkerStyle switch
                {
                    TextMarkerStyle.Decimal => "ol",
                    TextMarkerStyle.Disc => "ul style=\"list-style-type: disc;\"",
                    TextMarkerStyle.Circle => "ul style=\"list-style-type: circle;\"",
                    TextMarkerStyle.Square => "ul style=\"list-style-type: square;\"",
                    _ => "ul"
                };

                string alignStyle = GetTextAlignmentStyle(list.ListItems.FirstOrDefault()?.Blocks.FirstOrDefault()?.TextAlignment ?? TextAlignment.Left);
                htmlBody.Append($"<{tag} style=\"{alignStyle}\">");

                foreach (ListItem listItem in list.ListItems)
                {
                    htmlBody.Append("<li>");
                    foreach (Block itemBlock in listItem.Blocks)
                    {
                        if (itemBlock is Paragraph para)
                        {
                            string fontFamily = para.FontFamily?.Source ?? "Arial";
                            double fontSize = para.FontSize > 0 ? para.FontSize : 14;
                            string color = para.Foreground is SolidColorBrush brush ? ColorToHex(brush.Color) : "#ffffff";
                            htmlBody.Append($"<span style=\"font-family: {fontFamily}; font-size: {fontSize}pt; color: {color};\">");

                            foreach (Inline inline in para.Inlines)
                            {
                                ProcessInline(inline);
                            }

                            htmlBody.Append("</span>");
                        }
                        else if (itemBlock is List nestedList)
                        {
                            ProcessList(nestedList);
                        }
                    }
                    htmlBody.Append("</li>");
                }

                htmlBody.Append($"</{tag.Split(' ')[0]}>");
            }

            string GetTextAlignmentStyle(TextAlignment alignment) =>
                alignment switch
                {
                    TextAlignment.Center => "text-align: center;",
                    TextAlignment.Right => "text-align: right;",
                    TextAlignment.Justify => "text-align: justify;",
                    _ => "text-align: left;"
                };

            string ColorToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            ProcessBlocks(richTextBox.Document.Blocks);

            if (!string.IsNullOrEmpty(backgroundCid))
            {
                htmlBody.Append("</td></tr></table>");
            }

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
                string json = JsonSerializer.Serialize(TemplateCategories, options);
                File.WriteAllText(TemplatesFilePath, json, System.Text.Encoding.UTF8);
                System.Diagnostics.Debug.WriteLine($"Templates saved to {TemplatesFilePath}");
            }
            catch (Exception ex)
            {
                LogError("Ошибка при сохранении JSON (templates.json)", ex);
                ShowDetailedError("Ошибка сохранения шаблонов", ex);
            }
        }

        private void SetBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp",
                Title = "Выберите фоновое изображение"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                backgroundImagePath = openFileDialog.FileName;
                MessageBox.Show("Фоновое изображение успешно выбрано. Оно будет применено при отправке письма.",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var addCategoryWindow = new MetroWindow
            {
                Title = "Добавление категории",
                TitleCharacterCasing = CharacterCasing.Normal,
                Width = 350,
                Height = 165,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png")),
                TitleForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                // Устанавливаем текущую тему сразу при создании
                Background = new SolidColorBrush(SettingsWindow.CurrentThemeColor)
            };


            SettingsWindow.ThemeChanged += UpdateWindowTheme;
            addCategoryWindow.Closed += (s, e) => SettingsWindow.ThemeChanged -= UpdateWindowTheme;

            void UpdateWindowTheme()
            {
                addCategoryWindow.Dispatcher.Invoke(() =>
                {
                    addCategoryWindow.Background = new SolidColorBrush(SettingsWindow.CurrentThemeColor);
                });
            }

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

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

            var title = new TextBlock
            {
                Text = "Введите название новой категории:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(title);

            var inputTextBox = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(5),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15),
                Template = CreateRoundedTextBoxTemplate()
            };
            stackPanel.Children.Add(inputTextBox);

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
                Margin = new Thickness(0, 0, 15, 0),
                Style = CreateActionButtonStyle()
            };

            var cancelButton = new Button
            {
                Content = "Отменить",
                Width = 125,
                Height = 35,
                Style = CreateActionButtonStyle()
            };

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);

            saveButton.Click += (s, args) =>
            {
                string categoryName = inputTextBox.Text.Trim();

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
                addCategoryWindow.Close();
            };

            cancelButton.Click += (s, args) =>
            {
                addCategoryWindow.Close();
            };

            addCategoryWindow.Content = stackPanel;
            addCategoryWindow.ShowDialog();

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
                    System.Diagnostics.Debug.WriteLine($"Selected template: {templateWindow.SelectedTemplate.Name}, Content length: {templateWindow.SelectedTemplate.Content?.Length ?? 0}");

                    MessageRichTextBox.Document.Blocks.Clear();

                    if (!string.IsNullOrWhiteSpace(templateWindow.SelectedTemplate.Content))
                    {
                        var flowDoc = RichTextSerializationHelper.DeserializeFlowDocument(templateWindow.SelectedTemplate.Content);
                        MessageRichTextBox.Document = flowDoc;

                        if (flowDoc.FontFamily != null)
                        {
                            MessageRichTextBox.Document.FontFamily = flowDoc.FontFamily;
                            currentFontFamily = flowDoc.FontFamily;
                            System.Diagnostics.Debug.WriteLine($"Applied FontFamily: {flowDoc.FontFamily}");
                        }
                        if (flowDoc.FontSize > 0)
                        {
                            MessageRichTextBox.Document.FontSize = flowDoc.FontSize;
                            currentFontSize = flowDoc.FontSize;
                            System.Diagnostics.Debug.WriteLine($"Applied FontSize: {flowDoc.FontSize}");
                        }
                        if (flowDoc.TextAlignment != TextAlignment.Left)
                        {
                            MessageRichTextBox.Document.TextAlignment = flowDoc.TextAlignment;
                            System.Diagnostics.Debug.WriteLine($"Applied TextAlignment: {flowDoc.TextAlignment}");
                        }
                        if (flowDoc.Foreground is SolidColorBrush foreground)
                        {
                            MessageRichTextBox.Document.Foreground = foreground;
                            currentForeground = foreground;
                            System.Diagnostics.Debug.WriteLine($"Applied Foreground: {foreground.Color}");
                        }

                        var flowDocText = new TextRange(flowDoc.ContentStart, flowDoc.ContentEnd).Text;
                        System.Diagnostics.Debug.WriteLine($"Loaded document text: {flowDocText}");

                        if (string.IsNullOrWhiteSpace(flowDocText))
                        {
                            MessageBox.Show("Шаблон пустой или содержит некорректный XAML.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            var paragraph = new Paragraph(new Run(""));
                            paragraph.FontFamily = new FontFamily("Times New Roman");
                            paragraph.FontSize = 12;
                            paragraph.Foreground = Brushes.Black;
                            MessageRichTextBox.Document.Blocks.Add(paragraph);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Template content is empty or whitespace.");
                        MessageBox.Show("Выбранный шаблон пустой.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        var paragraph = new Paragraph(new Run(""));
                        paragraph.FontFamily = new FontFamily("Times New Roman");
                        paragraph.FontSize = 12;
                        paragraph.Foreground = Brushes.Black;
                        MessageRichTextBox.Document.Blocks.Add(paragraph);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No template selected or dialog cancelled.");
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
        private bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = ping.Send("8.8.8.8", 1000); // Проверяем доступность Google DNS
                    return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Проверка подключения к интернету
            if (!IsInternetAvailable())
            {
                MessageBox.Show("Отсутствует подключение к интернету. Пожалуйста, проверьте соединение и попробуйте снова.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Проверка ввода сообщения
            string message = new TextRange(MessageRichTextBox.Document.ContentStart, MessageRichTextBox.Document.ContentEnd).Text.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show("Введите сообщение!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Проверка выбранных получателей
            var recipients = RecipientList.ItemsSource as IEnumerable<string>;
            if (recipients == null || !recipients.Any())
            {
                MessageBox.Show("Выберите хотя бы одного получателя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 4. Проверка отправителя
            if (selectedSender == null)
            {
                MessageBox.Show("Выберите отправителя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 5. Проверка темы
            string subject = SubjectTextBox.Text;
            bool isSubjectEmpty = subject == SubjectPrefix;

            if (isSubjectEmpty)
            {
                var result = MessageBox.Show("В этом сообщении не указана тема. Хотите отправить его?",
                                             "Подтверждение отправки",
                                             MessageBoxButton.OKCancel,
                                             MessageBoxImage.Question);

                if (result != MessageBoxResult.OK)
                {
                    return;
                }

                subject = "";
            }
            else if (subject.StartsWith(SubjectPrefix))
            {
                subject = subject.Substring(SubjectPrefix.Length).Trim();
            }

            try
            {
                foreach (var recipient in recipients)
                {
                    string recipientEmail = recipient.Split(new[] { " - " }, StringSplitOptions.None).Last().Trim();
                    SendEmail(recipientEmail, message, subject);
                }
                MessageBox.Show("Сообщения успешно отправлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetForm(); // Сбрасываем форму после успешной отправки
            }
            catch (Exception ex)
            {
                LogError("Ошибка отправки письма", ex);
                ShowDetailedError("Ошибка отправки письма", ex);
            }
        }



        private void ResetForm()
        {
            // Очистка RichTextBox
            MessageRichTextBox.Document.Blocks.Clear();
            var paragraph = new Paragraph(new Run(""))
            {
                FontFamily = currentFontFamily,
                FontSize = currentFontSize,
                Foreground = currentForeground
            };
            MessageRichTextBox.Document.Blocks.Add(paragraph);

            // Сброс темы
            SubjectTextBox.Text = DefaultSubject;
            SubjectTextBox.Foreground = Brushes.Gray;

            // Очистка списка получателей
            RecipientList.ItemsSource = null;

            // Очистка списка прикреплённых файлов
            attachedFiles.Clear();
            UpdateAttachedFilesList();

            // Сброс фонового изображения
            backgroundImagePath = null;

            // Сброс фокуса
            MessageRichTextBox.Focus();
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
                    mail.From = new MailAddress(selectedSender.Email, "Alesya");
                    mail.To.Add(recipientEmail);
                    mail.Subject = subject;

                    var (htmlBody, embeddedImages) = ConvertRichTextBoxToHtml(MessageRichTextBox, backgroundImagePath);
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
            catch (SmtpException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
            {
                LogError("Сетевая ошибка при отправке email", ex);
                throw new Exception("Не удалось отправить письмо: проверьте подключение к интернету.", ex);
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
            string pdfPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "help.pdf");

            if (File.Exists(pdfPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = pdfPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии файла справки:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Файл справки не найден.", "Справка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
                Background = new SolidColorBrush(SettingsWindow.CurrentThemeColor),
                ResizeMode = ResizeMode.NoResize,
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png")),
                TitleForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1)
            };


            SettingsWindow.ThemeChanged += UpdateWindowTheme;
            senderSelectionWindow.Closed += (s, e) => SettingsWindow.ThemeChanged -= UpdateWindowTheme;

            void UpdateWindowTheme()
            {
                senderSelectionWindow.Dispatcher.Invoke(() =>
                {
                    senderSelectionWindow.Background = new SolidColorBrush(SettingsWindow.CurrentThemeColor);
                });
            }

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

            object fontFamilyObj = selection.GetPropertyValue(TextElement.FontFamilyProperty);
            object fontSizeObj = selection.GetPropertyValue(TextElement.FontSizeProperty);
            object foregroundObj = selection.GetPropertyValue(TextElement.ForegroundProperty);

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
            }

            if (foregroundObj != DependencyProperty.UnsetValue && foregroundObj is SolidColorBrush brush)
            {
                currentForeground = brush;
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


        private void TemplateNameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TemplateNameTextBox.Text == TemplateNamePrefix)
            {
                TemplateNameTextBox.Foreground = Brushes.Black;
                TemplateNameTextBox.CaretIndex = TemplateNameTextBox.Text.Length;
            }
        }

        private void TemplateNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!TemplateNameTextBox.Text.StartsWith(TemplateNamePrefix))
            {
                string userText = TemplateNameTextBox.Text.Length >= TemplateNamePrefix.Length
                    ? TemplateNameTextBox.Text.Substring(TemplateNamePrefix.Length)
                    : "";
                TemplateNameTextBox.Text = TemplateNamePrefix + userText;
                TemplateNameTextBox.CaretIndex = TemplateNameTextBox.Text.Length;
            }

            if (TemplateNameTextBox.Text.Length < TemplateNamePrefix.Length)
            {
                TemplateNameTextBox.Text = TemplateNamePrefix;
                TemplateNameTextBox.CaretIndex = TemplateNameTextBox.Text.Length;
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




        private void AlignLeft_Click(object sender, RoutedEventArgs e)
        {
            SetTextAlignment(TextAlignment.Left);
        }

        private void AlignCenter_Click(object sender, RoutedEventArgs e)
        {
            SetTextAlignment(TextAlignment.Center);
        }

        private void AlignRight_Click(object sender, RoutedEventArgs e)
        {
            SetTextAlignment(TextAlignment.Right);
        }

        private void AlignJustify_Click(object sender, RoutedEventArgs e)
        {
            SetTextAlignment(TextAlignment.Justify);
        }

        #region Цвет текста

        private string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private Style CreateTabControlStyle()
        {
            var style = new Style(typeof(TabControl));

            // Основные свойства TabControl
            style.Setters.Add(new Setter(TabControl.BackgroundProperty, Brushes.White));
            style.Setters.Add(new Setter(TabControl.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
            style.Setters.Add(new Setter(TabControl.BorderThicknessProperty, new Thickness(1)));

            // Шаблон TabControl
            var template = new ControlTemplate(typeof(TabControl));
            var grid = new FrameworkElementFactory(typeof(Grid));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(TabControl.SelectedContentProperty));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Stretch);

            var tabPanel = new FrameworkElementFactory(typeof(TabPanel));
            tabPanel.SetValue(TabPanel.BackgroundProperty, Brushes.White);
            tabPanel.SetValue(TabPanel.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            tabPanel.SetValue(TabPanel.IsItemsHostProperty, true);

            grid.AppendChild(tabPanel);
            grid.AppendChild(contentPresenter);

            template.VisualTree = grid;
            style.Setters.Add(new Setter(TabControl.TemplateProperty, template));

            // Стиль для TabItem
            var tabItemStyle = new Style(typeof(TabItem));
            tabItemStyle.Setters.Add(new Setter(TabItem.BackgroundProperty, Brushes.White));
            tabItemStyle.Setters.Add(new Setter(TabItem.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
            tabItemStyle.Setters.Add(new Setter(TabItem.FontSizeProperty, 14.0));
            tabItemStyle.Setters.Add(new Setter(TabItem.PaddingProperty, new Thickness(10, 5, 10, 5)));
            tabItemStyle.Setters.Add(new Setter(TabItem.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
            tabItemStyle.Setters.Add(new Setter(TabItem.BorderThicknessProperty, new Thickness(1)));

            var tabItemTemplate = new ControlTemplate(typeof(TabItem));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TabItem.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TabItem.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TabItem.BorderThicknessProperty));

            var contentPresenterTab = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterTab.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(HeaderedContentControl.HeaderProperty));
            contentPresenterTab.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterTab.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(contentPresenterTab);
            tabItemTemplate.VisualTree = border;

            var selectedTrigger = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(TabItem.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E6F8"))));
            tabItemTemplate.Triggers.Add(selectedTrigger);

            tabItemStyle.Setters.Add(new Setter(TabItem.TemplateProperty, tabItemTemplate));
            style.Resources.Add(typeof(TabItem), tabItemStyle);

            return style;
        }
        private void LoadCustomColors()
        {
            try
            {
                if (File.Exists(CustomColorsFilePath))
                {
                    string json = File.ReadAllText(CustomColorsFilePath, Encoding.UTF8);
                    var colorList = JsonSerializer.Deserialize<List<string>>(json);
                    if (colorList != null)
                    {
                        customColors.Clear();
                        foreach (var hex in colorList)
                        {
                            customColors.Add(new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)));
                        }
                        System.Diagnostics.Debug.WriteLine($"Loaded {customColors.Count} custom colors.");
                    }
                }
                else
                {
                    customColors.Clear();
                    customColors.Add(new SolidColorBrush(Color.FromRgb(100, 150, 200)));
                    customColors.Add(new SolidColorBrush(Color.FromRgb(200, 100, 150)));
                    SaveCustomColors();
                    System.Diagnostics.Debug.WriteLine("Created default custom colors.");
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке пользовательских цветов", ex);
                customColors.Clear();
                System.Diagnostics.Debug.WriteLine("Custom colors reset to empty due to error.");
            }
        }

        private void SaveCustomColors()
        {
            try
            {
                var colorList = customColors
                    .Select(brush => ColorToHex(brush.Color))
                    .ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(colorList, options);
                File.WriteAllText(CustomColorsFilePath, json, Encoding.UTF8);
                System.Diagnostics.Debug.WriteLine($"Saved {colorList.Count} custom colors to {CustomColorsFilePath}");
            }
            catch (Exception ex)
            {
                LogError("Ошибка при сохранении пользовательских цветов", ex);
            }
        }

        // Полная переработанная версия ColorButton_Click с улучшениями: больше цветов, центрированные кнопки, автообновление, сортировка по оттенкам, удаление, скролл, прижатие кнопок
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCustomColors();

            var colorPickerWindow = new MetroWindow
            {
                Title = "Выбрать цвет текста",
                Width = 280,
                Height = 353,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(SettingsWindow.CurrentThemeColor),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png")),
                TitleForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                GlowBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                ShowTitleBar = true,
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            // --- Позиционирование окна относительно MainWindow с учетом границ экрана ---
            Window mainWindow = Application.Current.MainWindow;

            double pickerWidth = colorPickerWindow.Width;
            double pickerHeight = colorPickerWindow.Height;

            double mainLeft = mainWindow.Left;
            double mainTop = mainWindow.Top;
            double mainWidth = mainWindow.ActualWidth;
            double mainHeight = mainWindow.ActualHeight;

            // Предварительное положение в правом нижнем углу MainWindow
            double left = mainLeft + mainWidth - pickerWidth - 10;
            double top = mainTop + mainHeight - pickerHeight - 10;

            // Область рабочего стола
            Rect workArea = SystemParameters.WorkArea;

            // Коррекция, если окно выходит за экран
            if (left + pickerWidth > workArea.Right)
                left = workArea.Right - pickerWidth - 10;
            if (top + pickerHeight > workArea.Bottom)
                top = workArea.Bottom - pickerHeight - 10;
            if (left < workArea.Left)
                left = workArea.Left + 10;
            if (top < workArea.Top)
                top = workArea.Top + 10;

            colorPickerWindow.Left = left;
            colorPickerWindow.Top = top;


            // --- Остальной код интерфейса ---
            SettingsWindow.ThemeChanged += UpdateWindowTheme;
            colorPickerWindow.Closed += (s, args) => SettingsWindow.ThemeChanged -= UpdateWindowTheme;

            void UpdateWindowTheme()
            {
                colorPickerWindow.Dispatcher.Invoke(() =>
                {
                    colorPickerWindow.Background = new SolidColorBrush(SettingsWindow.CurrentThemeColor);
                });
            }

            // --- Layout ---
            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var mainStackPanel = new StackPanel { Margin = new Thickness(5) };
            Grid.SetRow(mainStackPanel, 0);
            rootGrid.Children.Add(mainStackPanel);

            var tabControl = new TabControl
            {
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 12
            };

            // --- Стандартные цвета ---
            var standardTab = new TabItem { Header = "Стандартные" };

            var standardWrap = new UniformGrid
            {
                Columns = 12,
                Rows = 6,
                Margin = new Thickness(0)
            };

            var columnSortedColors = new List<List<Color>>
{
    new() { Colors.Black, Colors.DimGray, Colors.Gray, Colors.LightGray, Colors.White, Colors.Transparent },
    new() { Colors.DarkRed, Colors.Red, Colors.IndianRed, Colors.Salmon, Colors.LightCoral, Colors.MistyRose },
    new() { Colors.OrangeRed, Colors.Orange, Colors.DarkOrange, Colors.Coral, Colors.Tomato, Colors.PeachPuff },
    new() { Colors.Goldenrod, Colors.Gold, Colors.Khaki, Colors.Yellow, Colors.LightYellow, Colors.LemonChiffon },
    new() { Colors.DarkGreen, Colors.Green, Colors.ForestGreen, Colors.LimeGreen, Colors.LawnGreen, Colors.PaleGreen },
    new() { Colors.Teal, Colors.MediumTurquoise, Colors.Turquoise, Colors.Aquamarine, Colors.MintCream, Colors.LightCyan },
    new() { Colors.DeepSkyBlue, Colors.SkyBlue, Colors.LightSkyBlue, Colors.PowderBlue, Colors.LightBlue, Colors.AliceBlue },
    new() { Colors.Navy, Colors.MidnightBlue, Colors.Blue, Colors.RoyalBlue, Colors.SteelBlue, Colors.CornflowerBlue },
    new() { Colors.Indigo, Colors.MediumPurple, Colors.SlateBlue, Colors.BlueViolet, Colors.MediumOrchid, Colors.Thistle },
    new() { Colors.HotPink, Colors.DeepPink, Colors.Pink, Colors.LightPink, Colors.LavenderBlush, Colors.Fuchsia },
    new() { Colors.SaddleBrown, Colors.Sienna, Colors.Chocolate, Colors.Peru, Colors.Tan, Colors.BurlyWood },
    new() { Colors.Olive, Colors.DarkOliveGreen, Colors.Maroon, Colors.Silver, Colors.Gainsboro, Colors.Beige }
};

            Border selectedBorder = null;

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    if (j >= columnSortedColors.Count || i >= columnSortedColors[j].Count)
                        continue;

                    var color = columnSortedColors[j][i];

                    var rect = new Rectangle
                    {
                        Width = 16,
                        Height = 16,
                        Fill = new SolidColorBrush(color),
                        Stroke = Brushes.DarkSlateBlue,
                        StrokeThickness = 1,
                        Margin = new Thickness(0),
                        Cursor = Cursors.Hand
                    };

                    var border = new Border
                    {
                        BorderThickness = new Thickness(1),
                        BorderBrush = Brushes.Transparent,
                        Child = rect
                    };

                    rect.MouseDown += (s, args) =>
                    {
                        selectedColor = (SolidColorBrush)rect.Fill;
                        if (selectedBorder != null)
                            selectedBorder.BorderBrush = Brushes.Transparent;

                        selectedBorder = border;
                        selectedBorder.BorderBrush = Brushes.DarkOrange;
                    };

                    standardWrap.Children.Add(border);
                }
            }

            standardTab.Content = new ScrollViewer
            {
                Content = standardWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 170
            };
            tabControl.Items.Add(standardTab);

            // --- Пользовательские цвета ---
            var customTab = new TabItem { Header = "Пользовательские" };
            var customStack = new StackPanel { Margin = new Thickness(2) };

            var canvas = new Xceed.Wpf.Toolkit.ColorCanvas
            {
                Width = 300,
                Height = 150,
                Margin = new Thickness(0, 0, 0, 5),
                SelectedColor = Colors.Black
            };
            customStack.Children.Add(canvas);

            var btnWrap = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var addBtn = new Button
            {
                Content = "+",
                Width = 60,
                Height = 25,
                FontSize = 12,
                Style = CreateActionButtonStyle(),
                Margin = new Thickness(2)
            };

            var removeBtn = new Button
            {
                Content = "-",
                Width = 60,
                Height = 25,
                FontSize = 12,
                Style = CreateActionButtonStyle(),
                Margin = new Thickness(2)
            };

            btnWrap.Children.Add(addBtn);
            btnWrap.Children.Add(removeBtn);
            customStack.Children.Add(btnWrap);

            var customColorPanel = new WrapPanel();
            var customScroll = new ScrollViewer
            {
                Content = customColorPanel,
                Height = 60,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            customStack.Children.Add(customScroll);

            void RefreshCustomColorPanel()
            {
                customColorPanel.Children.Clear();
                foreach (var brush in customColors.ToList())
                {
                    var rect = new Rectangle
                    {
                        Width = 12,
                        Height = 12,
                        Fill = brush,
                        Stroke = Brushes.DarkSlateBlue,
                        StrokeThickness = 1,
                        Margin = new Thickness(2),
                        Cursor = Cursors.Hand
                    };
                    rect.MouseDown += (s, args) =>
                    {
                        selectedColor = (SolidColorBrush)rect.Fill;
                        canvas.SelectedColor = selectedColor.Color;
                    };
                    customColorPanel.Children.Add(rect);
                }
            }

            RefreshCustomColorPanel();

            addBtn.Click += (s, args) =>
            {
                if (canvas.SelectedColor.HasValue)
                {
                    var newBrush = new SolidColorBrush(canvas.SelectedColor.Value);
                    if (!customColors.Any(c => c.Color == newBrush.Color))
                    {
                        customColors.Add(newBrush);
                        SaveCustomColors();
                        RefreshCustomColorPanel();
                    }
                }
            };

            removeBtn.Click += (s, args) =>
            {
                if (selectedColor != null && customColors.Contains(selectedColor))
                {
                    customColors.Remove(selectedColor);
                    SaveCustomColors();
                    RefreshCustomColorPanel();
                    selectedColor = null;
                }
            };

            customTab.Content = customStack;
            tabControl.Items.Add(customTab);
            mainStackPanel.Children.Add(tabControl);

            // --- Кнопки OK / Отмена ---
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Bottom
            };

            var apply = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 5, 0),
                Style = CreateActionButtonStyle(),
                FontSize = 12
            };

            var cancel = new Button
            {
                Content = "Отмена",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5, 0, 5, 0),
                Style = CreateActionButtonStyle(),
                FontSize = 12
            };

            apply.Click += (s, args) =>
            {
                if (selectedColor != null)
                {
                    currentForeground = selectedColor;
                    ApplyFormattingToSelection();
                    colorPickerWindow.Close();
                }
                else
                {
                    MessageBox.Show("Выберите цвет!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            cancel.Click += (s, args) => colorPickerWindow.Close();

            btnPanel.Children.Add(apply);
            btnPanel.Children.Add(cancel);
            Grid.SetRow(btnPanel, 1);
            rootGrid.Children.Add(btnPanel);

            colorPickerWindow.Content = rootGrid;
            colorPickerWindow.ShowDialog();
        }


        private Style CreateActionButtonStyle()
        {
            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
            style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 12.0)); // Уменьшили шрифт
            style.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Arial"))); // Упростили шрифт
            style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Normal)); // Убрали жирность
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(5, 0, 5, 0))); // Уменьшили отступы
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
            style.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
            style.Setters.Add(new Setter(Control.MinHeightProperty, 25.0)); // Уменьшили минимальную высоту
            return style;
        }

        #endregion

        private void ApplyFormattingToSelection()
        {
            var selection = MessageRichTextBox.Selection;
            if (!selection.IsEmpty)
            {
                if (currentFontFamily != null)
                    selection.ApplyPropertyValue(TextElement.FontFamilyProperty, currentFontFamily);
                if (currentFontSize > 0)
                    selection.ApplyPropertyValue(TextElement.FontSizeProperty, currentFontSize);
                if (currentForeground != null)
                    selection.ApplyPropertyValue(TextElement.ForegroundProperty, currentForeground);
            }
            else
            {
                var paragraph = MessageRichTextBox.CaretPosition.Paragraph ?? new Paragraph();
                paragraph.FontFamily = currentFontFamily;
                paragraph.FontSize = currentFontSize;
                paragraph.Foreground = currentForeground;
                if (!MessageRichTextBox.Document.Blocks.Contains(paragraph))
                    MessageRichTextBox.Document.Blocks.Add(paragraph);
                var run = new Run
                {
                    FontFamily = currentFontFamily,
                    FontSize = currentFontSize,
                    Foreground = currentForeground
                };
                MessageRichTextBox.CaretPosition.InsertTextInRun("");
                MessageRichTextBox.CaretPosition.Paragraph.Inlines.Add(run);
            }
        }

        private void SetTextAlignment(TextAlignment alignment)
        {
            var richTextBox = MessageRichTextBox;
            var selection = richTextBox.Selection;

            // Если есть выделение, применяем выравнивание ко всем абзацам в выделении
            if (!selection.IsEmpty)
            {
                TextPointer start = selection.Start;
                TextPointer end = selection.End;

                // Находим все абзацы в выделении
                List<Paragraph> paragraphsToAlign = new List<Paragraph>();
                Block currentBlock = start.Paragraph;

                while (currentBlock != null && (currentBlock.ContentStart.CompareTo(end) <= 0 || currentBlock == end.Paragraph))
                {
                    if (currentBlock is Paragraph paragraph)
                    {
                        paragraphsToAlign.Add(paragraph);
                    }
                    else if (currentBlock is List list)
                    {
                        // Для списков применяем выравнивание к каждому ListItem
                        foreach (ListItem listItem in list.ListItems)
                        {
                            foreach (Block block in listItem.Blocks)
                            {
                                if (block is Paragraph listItemParagraph)
                                {
                                    paragraphsToAlign.Add(listItemParagraph);
                                }
                            }
                        }
                    }
                    currentBlock = currentBlock.NextBlock;
                }

                // Применяем выравнивание к найденным абзацам
                foreach (var paragraph in paragraphsToAlign)
                {
                    paragraph.TextAlignment = alignment;
                }
            }
            else
            {
                // Если выделения нет, применяем выравнивание к текущему абзацу
                TextPointer caretPosition = richTextBox.CaretPosition;
                Paragraph currentParagraph = caretPosition.Paragraph;

                if (currentParagraph != null)
                {
                    currentParagraph.TextAlignment = alignment;
                }
                else
                {
                    // Если абзаца нет (например, документ пустой), создаём новый с заданным выравниванием
                    currentParagraph = new Paragraph();
                    currentParagraph.TextAlignment = alignment;
                    richTextBox.Document.Blocks.Add(currentParagraph);
                    richTextBox.CaretPosition = currentParagraph.ContentStart;
                }

                // Применяем выравнивание к текущему пустому выделению, чтобы новый текст наследовал выравнивание
                selection.Select(caretPosition, caretPosition);
                selection.ApplyPropertyValue(Paragraph.TextAlignmentProperty, alignment);
            }

            richTextBox.Focus();
        }

        #region редактирование



        public void EnterTemplateEditMode(Template template, TemplateManagerWindow managerWindow)
        {
            isTemplateEditMode = true;
            editingTemplate = template;
            templateManagerWindow = managerWindow;

            // Скрываем обычный интерфейс
            RegularInterfacePanel.Children.OfType<UIElement>().ToList().ForEach(child =>
            {
                if (child != RegularInterfacePanel.Children.OfType<Border>().FirstOrDefault(b => b.Child is Grid grid && grid.Children.Contains(MessageRichTextBox)))
                {
                    child.Visibility = Visibility.Collapsed;
                }
            });

            // Скрываем SubjectTextBox и показываем TemplateNameTextBox
            SubjectTextBox.Visibility = Visibility.Collapsed;
            TemplateNameTextBox.Visibility = Visibility.Visible;

            // Устанавливаем название шаблона с префиксом
            const string TemplateNamePrefix = "Название: ";
            TemplateNameTextBox.Text = TemplateNamePrefix + template.Name; // Добавляем префикс
            TemplateNameTextBox.Foreground = Brushes.Black; // Устанавливаем чёрный цвет текста

            // Показываем кнопки редактирования
            TemplateEditButtonsPanel.Visibility = Visibility.Visible;

            // Загружаем содержимое шаблона
            MessageRichTextBox.Document.Blocks.Clear();
            if (!string.IsNullOrWhiteSpace(template.Content))
            {
                var flowDoc = RichTextSerializationHelper.DeserializeFlowDocument(template.Content);
                MessageRichTextBox.Document = flowDoc;

                if (flowDoc.FontFamily != null)
                {
                    currentFontFamily = flowDoc.FontFamily;
                    FontFamilyComboBox.SelectedItem = FontFamilyComboBox.Items
                        .Cast<ComboBoxItem>()
                        .FirstOrDefault(item => item.Content.ToString() == currentFontFamily.Source);
                }
                if (flowDoc.FontSize > 0)
                {
                    currentFontSize = flowDoc.FontSize;
                    FontSizeComboBox.SelectedItem = FontSizeComboBox.Items
                        .Cast<ComboBoxItem>()
                        .FirstOrDefault(item => item.Content.ToString() == currentFontSize.ToString());
                }
                if (flowDoc.Foreground is SolidColorBrush foreground)
                {
                    currentForeground = foreground;
                }
            }
            else
            {
                var paragraph = new Paragraph(new Run(""))
                {
                    FontFamily = currentFontFamily,
                    FontSize = currentFontSize,
                    Foreground = currentForeground
                };
                MessageRichTextBox.Document.Blocks.Add(paragraph);
            }

            templateManagerWindow.DialogResult = false;
        }

        private void ExitTemplateEditMode(bool saveChanges)
        {
            if (saveChanges && editingTemplate != null)
            {
                // Сериализуем документ с форматированием
                editingTemplate.Content = RichTextSerializationHelper.SerializeFlowDocument(MessageRichTextBox.Document);
                SaveTemplates(); // Сохраняем в templates.json
            }

            // Сбрасываем состояние
            isTemplateEditMode = false;
            editingTemplate = null;

            // Восстанавливаем обычный интерфейс
            RegularInterfacePanel.Children.OfType<UIElement>().ToList().ForEach(child =>
            {
                child.Visibility = Visibility.Visible;
            });

            SubjectTextBox.Visibility = Visibility.Visible;
            TemplateNameTextBox.Visibility = Visibility.Collapsed;
            TemplateEditButtonsPanel.Visibility = Visibility.Collapsed;

            // Очищаем RichTextBox
            MessageRichTextBox.Document.Blocks.Clear();
            var paragraph = new Paragraph(new Run(""))
            {
                FontFamily = currentFontFamily,
                FontSize = currentFontSize,
                Foreground = currentForeground
            };
            MessageRichTextBox.Document.Blocks.Add(paragraph);

            // НЕ открываем TemplateManagerWindow автоматически
            templateManagerWindow = null; // Сбрасываем ссылку
        }

        // Вспомогательный метод для загрузки шаблона
        private void LoadSelectedTemplate(Template template)
        {
            MessageRichTextBox.Document.Blocks.Clear();
            if (!string.IsNullOrWhiteSpace(template.Content))
            {
                var flowDoc = RichTextSerializationHelper.DeserializeFlowDocument(template.Content);
                MessageRichTextBox.Document = flowDoc;

                if (flowDoc.FontFamily != null)
                {
                    MessageRichTextBox.Document.FontFamily = flowDoc.FontFamily;
                    currentFontFamily = flowDoc.FontFamily;
                }
                if (flowDoc.FontSize > 0)
                {
                    MessageRichTextBox.Document.FontSize = flowDoc.FontSize;
                    currentFontSize = flowDoc.FontSize;
                }
                if (flowDoc.TextAlignment != TextAlignment.Left)
                {
                    MessageRichTextBox.Document.TextAlignment = flowDoc.TextAlignment;
                }
                if (flowDoc.Foreground is SolidColorBrush foreground)
                {
                    MessageRichTextBox.Document.Foreground = foreground;
                    currentForeground = foreground;
                }

                var flowDocText = new TextRange(flowDoc.ContentStart, flowDoc.ContentEnd).Text;
                if (string.IsNullOrWhiteSpace(flowDocText))
                {
                    MessageBox.Show("Шаблон пустой или содержит некорректный XAML.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ResetRichTextBox();
                }
            }
            else
            {
                MessageBox.Show("Выбранный шаблон пустой.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                ResetRichTextBox();
            }
        }

        private void ResetRichTextBox()
        {
            var paragraph = new Paragraph(new Run(""));
            paragraph.FontFamily = new FontFamily("Times New Roman");
            paragraph.FontSize = 12;
            paragraph.Foreground = Brushes.Black;
            MessageRichTextBox.Document.Blocks.Add(paragraph);
        }


        private void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            var text = new TextRange(MessageRichTextBox.Document.ContentStart, MessageRichTextBox.Document.ContentEnd).Text;
            if (string.IsNullOrWhiteSpace(text) && MessageRichTextBox.Document.Blocks.Count == 0)
            {
                MessageBox.Show("Документ пустой. Введите содержимое шаблона.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var content = RichTextSerializationHelper.SerializeFlowDocument(MessageRichTextBox.Document);

            // Извлекаем имя шаблона без префикса
            const string TemplateNamePrefix = "Название: ";
            string templateNameRaw = TemplateNameTextBox.Text.Trim();
            if (!templateNameRaw.StartsWith(TemplateNamePrefix))
            {
                MessageBox.Show("Название должно начинаться с \"Название: \".", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string nameOnly = templateNameRaw.Substring(TemplateNamePrefix.Length).Trim();

            if (string.IsNullOrWhiteSpace(nameOnly))
            {
                MessageBox.Show("Введите корректное название шаблона.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (isTemplateEditMode && editingTemplate != null)
            {
                var selectedCategory = TemplateCategories.FirstOrDefault(c => c.Templates.Contains(editingTemplate));
                if (selectedCategory == null)
                {
                    LogToFile("❌ Категория для редактируемого шаблона не найдена");
                    MessageBox.Show("Категория шаблона не найдена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (nameOnly != editingTemplate.Name)
                {
                    if (selectedCategory.Templates.Any(t => t.Name.Equals(nameOnly, StringComparison.OrdinalIgnoreCase)))
                    {
                        LogToFile($"❗️ Шаблон с названием '{nameOnly}' уже существует в категории '{selectedCategory.Name}'");
                        MessageBox.Show("Шаблон с таким названием уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    LogToFile($"🔄 Изменяем название шаблона с '{editingTemplate.Name}' на '{nameOnly}'");
                    editingTemplate.Name = nameOnly;
                }

                System.Diagnostics.Debug.WriteLine("Шаблон редактирован (Успех)");
                editingTemplate.Content = content;
                SaveTemplates();
                MessageBox.Show("Шаблон успешно отредактирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                ExitTemplateEditMode(saveChanges: true);
            }
            else
            {
                var selectedCategory = TemplateCategories.FirstOrDefault(c => c.Name == templateManagerWindow?.Category?.Name);
                if (selectedCategory == null)
                {
                    LogToFile("❌ selectedCategory оказался null");
                    MessageBox.Show("Категория шаблона не найдена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (selectedCategory.Templates.Any(t => t.Name.Equals(nameOnly, StringComparison.OrdinalIgnoreCase)))
                {
                    LogToFile($"❗️ Шаблон с названием '{nameOnly}' уже существует в категории '{selectedCategory.Name}'");
                    MessageBox.Show("Шаблон с таким названием уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newTemplate = new Template
                {
                    Name = nameOnly,
                    Content = content
                };

                selectedCategory.Templates.Add(newTemplate);

                LogToFile($"Добавляется шаблон '{nameOnly}' в категорию: {selectedCategory.Name}");
                SaveTemplates();
                MessageBox.Show("Новый шаблон успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                if (templateManagerWindow != null && !templateManagerWindow.IsVisible)
                {
                    templateManagerWindow.RefreshTemplateList();
                }

                ExitTemplateAddMode();
            }

            // Сброс полей
            TemplateNameTextBox.Text = TemplateNamePrefix;
            TemplateNameTextBox.Foreground = Brushes.Gray;
            TemplateNameTextBox.Visibility = Visibility.Collapsed;
            TemplateEditButtonsPanel.Visibility = Visibility.Collapsed;
            SubjectTextBox.Visibility = Visibility.Visible;
        }




        private void CancelTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (isTemplateEditMode)
            {
                ExitTemplateEditMode(saveChanges: false);
            }
            else
            {
                ExitTemplateAddMode();
            }
        }


        private void TemplateNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TemplateNameTextBox.Text))
            {
                TemplateNameTextBox.Text = "Название шаблона";
                TemplateNameTextBox.Foreground = Brushes.Gray;
            }
        }

        public void EnterTemplateAddMode(TemplateManagerWindow managerWindow, TemplateCategory category)
        {
            isTemplateEditMode = false;
            editingTemplate = null;
            templateManagerWindow = managerWindow;

            // Скрываем обычный интерфейс (как в редактировании)
            RegularInterfacePanel.Children.OfType<UIElement>().ToList().ForEach(child =>
            {
                if (child != RegularInterfacePanel.Children.OfType<Border>().FirstOrDefault(b => b.Child is Grid grid && grid.Children.Contains(MessageRichTextBox)))
                {
                    child.Visibility = Visibility.Collapsed;
                }
            });

            SubjectTextBox.Visibility = Visibility.Collapsed;
            TemplateNameTextBox.Visibility = Visibility.Visible;
            TemplateEditButtonsPanel.Visibility = Visibility.Visible;

            // Очищаем редактор
            MessageRichTextBox.Document.Blocks.Clear();
            var paragraph = new Paragraph(new Run(""))
            {
                FontFamily = currentFontFamily,
                FontSize = currentFontSize,
                Foreground = currentForeground
            };
            MessageRichTextBox.Document.Blocks.Add(paragraph);

            // Устанавливаем категорию через метод
            templateManagerWindow.SetCategory(category);

            // ⬅️ Закрываем TemplateManagerWindow
            templateManagerWindow.DialogResult = false;
        }



        private void ExitTemplateAddMode()
        {
            isTemplateEditMode = false;
            editingTemplate = null;

            // Восстанавливаем обычный интерфейс
            RegularInterfacePanel.Children.OfType<UIElement>().ToList().ForEach(child =>
            {
                child.Visibility = Visibility.Visible;
            });

            SubjectTextBox.Visibility = Visibility.Visible;
            TemplateNameTextBox.Visibility = Visibility.Collapsed;
            TemplateEditButtonsPanel.Visibility = Visibility.Collapsed;

            // Очищаем редактор
            MessageRichTextBox.Document.Blocks.Clear();
            var paragraph = new Paragraph(new Run(""))
            {
                FontFamily = currentFontFamily,
                FontSize = currentFontSize,
                Foreground = currentForeground
            };
            MessageRichTextBox.Document.Blocks.Add(paragraph);

            // НЕ открываем TemplateManagerWindow автоматически
            templateManagerWindow = null; // Сбрасываем ссылку
        }


        #endregion


        private void LogToFile(string message)
        {
            try
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(logPath, logEntry);
            }
            catch
            {
                // Если логирование не удалось, игнорируем — чтобы не мешать работе приложения
            }
        }

    }
}

