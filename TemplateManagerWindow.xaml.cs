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

namespace alesya_rassylka
{
    public partial class TemplateManagerWindow : MetroWindow
    {
        private TemplateCategory category;
        private Action saveChanges;
        public Template SelectedTemplate { get; private set; }

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
            var editWindow = new TemplateEditWindow(null)
            {
                Owner = this
            };
            if (editWindow.ShowDialog() == true)
            {
                category.Templates.Add(editWindow.Template);
                TemplatesListBox.ItemsSource = null;
                TemplatesListBox.ItemsSource = category.Templates;
                saveChanges?.Invoke();
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
                StringBuilder htmlContent = new StringBuilder();

                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
                {
                    var body = wordDoc.MainDocumentPart?.Document?.Body;
                    if (body == null)
                        throw new InvalidOperationException("Word document body is missing.");

                    foreach (var element in body.Elements())
                    {
                        if (element is WordParagraph p)
                        {
                            // Извлекаем свойства параграфа
                            string fontFamily = "Arial";
                            double fontSize = 12;
                            string textAlignment = "Left";
                            string fontWeight = "Normal";
                            string fontStyle = "Normal";
                            string textDecorations = "";

                            var paraProps = p.ParagraphProperties;
                            if (paraProps != null)
                            {
                                // Выравнивание
                                if (paraProps.Justification?.Val != null)
                                {
                                    var justification = paraProps.Justification.Val.Value;
                                    if (justification == JustificationValues.Center)
                                        textAlignment = "Center";
                                    else if (justification == JustificationValues.Right)
                                        textAlignment = "Right";
                                    else if (justification == JustificationValues.Both)
                                        textAlignment = "Justify";
                                    else
                                        textAlignment = "Left";
                                }

                                // Свойства стиля параграфа
                                var styleId = paraProps.ParagraphStyleId?.Val?.Value;
                                if (styleId != null)
                                {
                                    var style = wordDoc.MainDocumentPart?.StyleDefinitionsPart?.Styles
                                        ?.Elements<WordStyle>()
                                        ?.FirstOrDefault(s => s.StyleId == styleId && s.Type == StyleValues.Paragraph);
                                    if (style?.StyleRunProperties != null)
                                    {
                                        var runProps = style.StyleRunProperties;
                                        if (runProps.FontSize != null && double.TryParse(runProps.FontSize.Val, out double size))
                                            fontSize = size / 2; // Word использует half-points
                                        if (runProps.RunFonts != null)
                                            fontFamily = runProps.RunFonts.Ascii ?? "Arial";
                                        if (runProps.Bold != null)
                                            fontWeight = "Bold";
                                        if (runProps.Italic != null)
                                            fontStyle = "Italic";
                                        if (runProps.Underline != null)
                                            textDecorations = "Underline";
                                    }
                                }
                            }

                            // Проверяем, является ли параграф частью списка
                            var numberingProps = paraProps?.NumberingProperties;
                            bool isListItem = numberingProps != null;
                            string listStyle = "Disc";
                            if (isListItem && numberingProps.NumberingLevelReference != null)
                            {
                                var numberingId = numberingProps.NumberingId?.Val?.Value;
                                var level = numberingProps.NumberingLevelReference?.Val?.Value ?? 0;
                                var numberingPart = wordDoc.MainDocumentPart?.NumberingDefinitionsPart;
                                if (numberingPart != null && numberingId.HasValue)
                                {
                                    var numbering = numberingPart.Numbering.Elements<NumberingInstance>()
                                        ?.FirstOrDefault(n => n.NumberID?.Value == numberingId);
                                    var levelFormat = numbering?.Elements<AbstractNum>()
                                        ?.SelectMany(an => an.Elements<Level>())
                                        ?.FirstOrDefault(l => l.LevelIndex?.Value == level)?.NumberingFormat?.Val?.Value;
                                    if (levelFormat == NumberFormatValues.Decimal)
                                        listStyle = "Decimal";
                                }
                            }

                            // Извлекаем текст и форматирование Run
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

                                if (runProps != null)
                                {
                                    if (runProps.FontSize != null && double.TryParse(runProps.FontSize.Val, out double size))
                                        runFontSize = size / 2;
                                    if (runProps.RunFonts != null)
                                        runFontFamily = runProps.RunFonts.Ascii ?? fontFamily;
                                    if (runProps.Bold != null)
                                        runFontWeight = "Bold";
                                    if (runProps.Italic != null)
                                        runFontStyle = "Italic";
                                    if (runProps.Underline != null)
                                        runTextDecorations = "Underline";
                                }

                                string escapedText = System.Security.SecurityElement.Escape(runText);
                                string runHtml = escapedText;
                                if (runFontWeight == "Bold")
                                    runHtml = $"<b>{runHtml}</b>";
                                if (runFontStyle == "Italic")
                                    runHtml = $"<i>{runHtml}</i>";
                                if (runTextDecorations == "Underline")
                                    runHtml = $"<u>{runHtml}</u>";

                                paraText.Append(runHtml);
                            }

                            if (paraText.Length == 0)
                                continue;

                            string paraHtml;
                            if (isListItem)
                            {
                                // Исправленная интерполяция для списков
                                string listTag = listStyle.ToLower() == "decimal" ? "ol" : "ul";
                                paraHtml = $"<{listTag}><li>{paraText}</li></{listTag}>";
                            }
                            else
                            {
                                paraHtml = $"<p data-font-family=\"{fontFamily}\" data-font-size=\"{fontSize}\" data-text-align=\"{textAlignment}\" data-font-weight=\"{fontWeight}\" data-font-style=\"{fontStyle}\" data-text-decorations=\"{textDecorations}\">{paraText}</p>";
                            }

                            htmlContent.Append(paraHtml);
                        }
                    }

                    if (htmlContent.Length == 0)
                        throw new InvalidOperationException("No valid content found in Word document.");

                    string xamlText = HtmlToXamlConverter.ConvertHtmlToXaml(htmlContent.ToString(), false);
                    System.Diagnostics.Debug.WriteLine($"XAML content: {xamlText}");
                    if (string.IsNullOrWhiteSpace(xamlText))
                        throw new InvalidOperationException("XAML content is empty after conversion.");

                    var flowDoc = RichTextSerializationHelper.DeserializeFlowDocument(xamlText);
                    var flowDocText = new TextRange(flowDoc.ContentStart, flowDoc.ContentEnd).Text;
                    System.Diagnostics.Debug.WriteLine($"FlowDocument text: {flowDocText}");
                    if (string.IsNullOrWhiteSpace(flowDocText))
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed XAML: {xamlText}");
                        throw new InvalidOperationException("FlowDocument is empty after deserialization.");
                    }

                    string serialized = RichTextSerializationHelper.SerializeFlowDocument(flowDoc);
                    System.Diagnostics.Debug.WriteLine($"Serialized XAML content: {serialized}");
                    if (string.IsNullOrWhiteSpace(serialized))
                        throw new InvalidOperationException("Serialized XAML content is empty.");

                    var newTemplate = new Template
                    {
                        Name = "Новый шаблон из Word",
                        Content = serialized
                    };

                    category.Templates.Add(newTemplate);
                    TemplatesListBox.ItemsSource = null;
                    TemplatesListBox.ItemsSource = category.Templates;
                    saveChanges?.Invoke();
                    System.Diagnostics.Debug.WriteLine($"Template added: {newTemplate.Name}, Content length: {newTemplate.Content.Length}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddTemplateFromWord: {ex}");
                MessageBox.Show($"Ошибка при загрузке документа Word: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTemplate_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"EditTemplate_Click called. Sender type: {sender.GetType().Name}");
            if (sender is Button button && button.Tag is Template template)
            {
                var editWindow = new TemplateEditWindow(template)
                {
                    Owner = this
                };
                if (editWindow.ShowDialog() == true)
                {
                    TemplatesListBox.ItemsSource = null;
                    TemplatesListBox.ItemsSource = category.Templates;
                    saveChanges?.Invoke();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected sender type in EditTemplate_Click: {sender.GetType().Name}");
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
    }
}