using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using MahApps.Metro.Controls;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;
using System.Windows.Documents;
using System.Text;

// Псевдонимы для избежания конфликтов
using WpfParagraph = System.Windows.Documents.Paragraph;
using WpfRun = System.Windows.Documents.Run;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WordRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using WordStyle = DocumentFormat.OpenXml.Wordprocessing.Style;
using System.Windows.Media;
using DocumentFormat.OpenXml.Spreadsheet;

namespace alesya_rassylka
{
    public partial class TemplateManagerWindow : MetroWindow
    {
        private TemplateCategory category;
        private Action saveChanges;
        public Template SelectedTemplate { get; private set; }

        public TemplateCategory Category => category;

        public TemplateManagerWindow(TemplateCategory category, Action saveChanges)
        {
            System.Diagnostics.Debug.WriteLine("Initializing TemplateManagerWindow");
            InitializeComponent();
            this.category = category ?? throw new ArgumentNullException(nameof(category));
            this.saveChanges = saveChanges;

            CategoryNameTextBox.Text = category.Name;
            TemplatesListBox.ItemsSource = category.Templates;
            System.Diagnostics.Debug.WriteLine("TemplateManagerWindow initialized successfully");

            // Применяем текущую тему
            this.Background = new SolidColorBrush(SettingsWindow.CurrentThemeColor);

            // Подписываемся на изменения темы
            SettingsWindow.ThemeChanged += () =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.Background = new SolidColorBrush(SettingsWindow.CurrentThemeColor);
                });
            };

        }

        private void SaveCategoryName_Click(object sender, RoutedEventArgs e)
        {
            string newName = CategoryNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("Название категории не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            category.Name = newName;
            saveChanges?.Invoke();
            if (Owner is MainWindow mainWindow)
            {
                mainWindow.RefreshTemplateCategories();
            }
            MessageBox.Show("Название категории обновлено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddTemplate_Click(object sender, RoutedEventArgs e)
        {
            var newTemplate = new Template { Name = "Новый шаблон", Content = "" };
            if (Owner is MainWindow mainWindow)
            {
                mainWindow.EnterTemplateEditMode(newTemplate, this);
                // После редактирования шаблон добавляется, если сохранён
                if (!string.IsNullOrWhiteSpace(newTemplate.Content))
                {
                    category.Templates.Add(newTemplate);
                    TemplatesListBox.ItemsSource = null;
                    TemplatesListBox.ItemsSource = category.Templates;
                    saveChanges?.Invoke();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Owner is not MainWindow");
                MessageBox.Show("Ошибка: главное окно не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTemplate_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"EditTemplate_Click called. Sender type: {sender.GetType().Name}");
            if (sender is Button button && button.Tag is Template template)
            {
                if (Owner is MainWindow mainWindow)
                {
                    mainWindow.EnterTemplateEditMode(template, this);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Owner is not MainWindow");
                    MessageBox.Show("Ошибка: главное окно не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected sender type in EditTemplate_Click: {sender.GetType().Name}");
            }
        }

        private void AddTemplateFromWord_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Word Documents (*.docx)|*.docx",
                Title = "Выберите документ Word"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            try
            {
                string filePath = openFileDialog.FileName;
                StringBuilder htmlContent = new StringBuilder("<body>");

                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
                {
                    var body = wordDoc.MainDocumentPart?.Document?.Body;
                    if (body == null)
                        throw new InvalidOperationException("Тело документа Word отсутствует.");

                    bool isListOpen = false;
                    string currentListTag = null;
                    string currentListStyle = null;

                    foreach (var element in body.Elements())
                    {
                        if (element is WordParagraph p)
                        {
                            // Извлекаем свойства параграфа
                            string fontFamily = "Arial";
                            double fontSize = 12;
                            string textAlignment = "left";
                            string fontWeight = "normal";
                            string fontStyle = "normal";
                            string textDecorations = "";
                            string textColor = "#000000"; // По умолчанию чёрный

                            var paraProps = p.ParagraphProperties;
                            if (paraProps != null)
                            {
                                // Выравнивание
                                if (paraProps.Justification?.Val != null)
                                {
                                    string justification = paraProps.Justification.Val.Value.ToString().ToLower();
                                    if (justification == "center")
                                        textAlignment = "center";
                                    else if (justification == "right")
                                        textAlignment = "right";
                                    else if (justification == "both")
                                        textAlignment = "justify";
                                    else
                                        textAlignment = "left";
                                }

                                // Свойства стиля параграфа
                                var styleId = paraProps.ParagraphStyleId?.Val?.Value;
                                if (!string.IsNullOrEmpty(styleId))
                                {
                                    var style = wordDoc.MainDocumentPart?.StyleDefinitionsPart?.Styles
                                        ?.Elements<WordStyle>()
                                        ?.FirstOrDefault(s => s.StyleId == styleId && s.Type == StyleValues.Paragraph);
                                    if (style?.StyleRunProperties != null)
                                    {
                                        var runProps = style.StyleRunProperties;
                                        if (runProps.FontSize != null && double.TryParse(runProps.FontSize.Val?.Value, out double size))
                                            fontSize = size / 2; // Word использует half-points
                                        if (runProps.RunFonts?.Ascii != null)
                                            fontFamily = runProps.RunFonts.Ascii;
                                        if (runProps.Bold != null)
                                            fontWeight = "bold";
                                        if (runProps.Italic != null)
                                            fontStyle = "italic";
                                        if (runProps.Underline != null)
                                            textDecorations = "underline";
                                        if (runProps.Color != null && !string.IsNullOrEmpty(runProps.Color.Val?.Value))
                                            textColor = "#" + runProps.Color.Val;
                                    }
                                }
                            }

                            // Проверяем, является ли параграф частью списка
                            var numberingProps = paraProps?.NumberingProperties;
                            bool isListItem = numberingProps != null && numberingProps.NumberingLevelReference != null;
                            string listStyle = "disc";
                            if (isListItem)
                            {
                                var numberingId = numberingProps.NumberingId?.Val?.Value;
                                var level = numberingProps.NumberingLevelReference?.Val?.Value ?? 0;
                                var numberingPart = wordDoc.MainDocumentPart?.NumberingDefinitionsPart;
                                if (numberingPart != null && numberingId.HasValue)
                                {
                                    var numbering = numberingPart.Numbering?.Elements<NumberingInstance>()
                                        ?.FirstOrDefault(n => n.NumberID?.Value == numberingId);
                                    var levelFormat = numbering?.AbstractNumId?.Val != null
                                        ? wordDoc.MainDocumentPart?.NumberingDefinitionsPart?.Numbering
                                            ?.Elements<AbstractNum>()
                                            ?.FirstOrDefault(an => an.AbstractNumberId?.Value == numbering.AbstractNumId?.Val?.Value)
                                            ?.Elements<Level>()
                                            ?.FirstOrDefault(l => l.LevelIndex?.Value == level)?.NumberingFormat?.Val?.Value
                                        : null;
                                    if (levelFormat == NumberFormatValues.Decimal)
                                        listStyle = "decimal";
                                }
                            }

                            // Закрываем предыдущий список, если текущий параграф не является элементом списка
                            if (!isListItem && isListOpen)
                            {
                                htmlContent.Append($"</{currentListTag}>");
                                isListOpen = false;
                                currentListTag = null;
                                currentListStyle = null;
                            }

                            // Извлекаем текст и форматирование
                            StringBuilder paraText = new StringBuilder();
                            foreach (var run in p.Elements<WordRun>())
                            {
                                string runText = run.InnerText;
                                if (string.IsNullOrWhiteSpace(runText))
                                    continue;

                                var runProps = run.RunProperties;
                                string runFontFamily = fontFamily;
                                double runFontSize = fontSize;
                                string runFontWeight = fontWeight;
                                string runFontStyle = fontStyle;
                                string runTextDecorations = textDecorations;
                                string runTextColor = textColor;

                                if (runProps != null)
                                {
                                    if (runProps.FontSize != null && double.TryParse(runProps.FontSize.Val?.Value, out double size))
                                        runFontSize = size / 2;
                                    if (runProps.RunFonts?.Ascii != null)
                                        runFontFamily = runProps.RunFonts.Ascii;
                                    if (runProps.Bold != null)
                                        runFontWeight = "bold";
                                    if (runProps.Italic != null)
                                        runFontStyle = "italic";
                                    if (runProps.Underline != null)
                                        runTextDecorations = "underline";
                                    if (runProps.Color != null && !string.IsNullOrEmpty(runProps.Color.Val?.Value))
                                        runTextColor = "#" + runProps.Color.Val;
                                }

                                string escapedText = System.Security.SecurityElement.Escape(runText);
                                string style = $"font-family:'{runFontFamily}';font-size:{runFontSize}pt;" +
                                               $"font-weight:{runFontWeight};font-style:{runFontStyle};" +
                                               $"color:{runTextColor};";
                                if (runTextDecorations == "underline")
                                    style += "text-decoration:underline;";
                                string runHtml = $"<span style=\"{style}\">{escapedText}</span>";
                                paraText.Append(runHtml);
                            }

                            if (paraText.Length == 0)
                                continue;

                            // Формируем HTML
                            string paraStyle = $"text-align:{textAlignment};font-family:'{fontFamily}';font-size:{fontSize}pt;color:{textColor};";
                            if (isListItem)
                            {
                                if (!isListOpen || currentListStyle != listStyle)
                                {
                                    if (isListOpen)
                                        htmlContent.Append($"</{currentListTag}>");
                                    currentListTag = listStyle.ToLower() == "decimal" ? "ol" : "ul";
                                    currentListStyle = listStyle;
                                    htmlContent.Append($"<{currentListTag} style=\"list-style-type:{listStyle};\">");
                                    isListOpen = true;
                                }
                                htmlContent.Append($"<li style=\"{paraStyle}\">{paraText}</li>");
                            }
                            else
                            {
                                htmlContent.Append($"<p style=\"{paraStyle}\">{paraText}</p>");
                            }
                        }
                    }

                    // Закрываем открытый список, если он есть
                    if (isListOpen)
                        htmlContent.Append($"</{currentListTag}>");

                    htmlContent.Append("</body>");
                    if (htmlContent.Length <= "<body></body>".Length)
                        throw new InvalidOperationException("В документе Word нет содержимого.");

                    string htmlString = htmlContent.ToString();
                    System.Diagnostics.Debug.WriteLine($"HTML content: {htmlString}");

                    // Конвертируем HTML в XAML
                    string xamlText = HtmlToXamlConverter.ConvertHtmlToXaml(htmlString, false);
                    System.Diagnostics.Debug.WriteLine($"XAML content: {xamlText}");
                    if (string.IsNullOrWhiteSpace(xamlText))
                        throw new InvalidOperationException("Конвертация в XAML не удалась.");

                    // Десериализуем XAML в FlowDocument
                    var flowDoc = RichTextSerializationHelper.DeserializeFlowDocument(xamlText);
                    var flowDocText = new TextRange(flowDoc.ContentStart, flowDoc.ContentEnd).Text;
                    System.Diagnostics.Debug.WriteLine($"FlowDocument text: {flowDocText}");
                    if (string.IsNullOrWhiteSpace(flowDocText))
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed XAML: {xamlText}");
                        throw new InvalidOperationException("FlowDocument пуст после десериализации.");
                    }

                    // Сериализуем FlowDocument для сохранения
                    string serialized = RichTextSerializationHelper.SerializeFlowDocument(flowDoc);
                    System.Diagnostics.Debug.WriteLine($"Serialized XAML content: {serialized}");
                    if (string.IsNullOrWhiteSpace(serialized))
                        throw new InvalidOperationException("Сериализованный XAML пуст.");

                    // Создаём новый шаблон
                    var newTemplate = new Template
                    {
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        Content = serialized
                    };

                    // Добавляем шаблон в категорию
                    category.Templates.Add(newTemplate);
                    TemplatesListBox.ItemsSource = null;
                    TemplatesListBox.ItemsSource = category.Templates;
                    saveChanges?.Invoke();
                    System.Diagnostics.Debug.WriteLine($"Шаблон добавлен: {newTemplate.Name}, длина содержимого: {newTemplate.Content.Length}");
                    MessageBox.Show("Шаблон успешно импортирован из Word!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в AddTemplateFromWord: {ex}");
                MessageBox.Show($"Ошибка при загрузке документа Word: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        

        private void SelectTemplate_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"SelectTemplate_Click called. Sender type: {sender.GetType().Name}");
            if (sender is Button button && button.Tag is Template template)
            {
                System.Diagnostics.Debug.WriteLine($"Selected template: {template.Name}, Content length: {template.Content?.Length ?? 0}");
                SelectedTemplate = template;
                DialogResult = true;
                Close();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected sender type in SelectTemplate_Click: {sender.GetType().Name}");
            }
        }

        private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteTemplate_Click called. Sender type: {sender.GetType().Name}");
            if (sender is Button button && button.Tag is Template template)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить шаблон '{template.Name}'?",
                                            "Подтверждение удаления",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    category.Templates.Remove(template);
                    TemplatesListBox.ItemsSource = null;
                    TemplatesListBox.ItemsSource = category.Templates;
                    saveChanges?.Invoke();
                    MessageBox.Show("Шаблон успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected sender type in DeleteTemplate_Click: {sender.GetType().Name}");
            }
        }

        public void RefreshTemplateList()
        {
            TemplatesListBox.ItemsSource = null;
            TemplatesListBox.ItemsSource = Category.Templates;
        }

    }
}