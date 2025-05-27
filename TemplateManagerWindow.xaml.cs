using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using MahApps.Metro.Controls;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;
using System.Windows.Documents;
using System.Text;
using Xceed.Words.NET;
using Paragraph = System.Windows.Documents.Paragraph; // Псевдоним для ясности
using Run = System.Windows.Documents.Run; // Псевдоним для ясности

// Псевдонимы для избежания конфликтов
using WpfParagraph = System.Windows.Documents.Paragraph;
using WpfRun = System.Windows.Documents.Run;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WordRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using WordStyle = DocumentFormat.OpenXml.Wordprocessing.Style;
using System.Windows.Media;
using DocumentFormat.OpenXml.Spreadsheet;
using HTMLConverter;

namespace alesya_rassylka
{
    public partial class TemplateManagerWindow : MetroWindow
    {
        private TemplateCategory category;
        private Action saveChanges;
        public Template SelectedTemplate { get; set; }

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

        public void SetCategory(TemplateCategory newCategory)
        {
            category = newCategory;
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
            if (Owner is MainWindow mainWindow)
            {
                // ❗ Передаём null вместо шаблона
                mainWindow.EnterTemplateAddMode(this, category);
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
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Word Documents (*.docx)|*.docx",
                Title = "Выберите документ Word"
            };

            if (openFileDialog.ShowDialog() != true) return;

            try
            {
                var flowDoc = new System.Windows.Documents.FlowDocument();

                using (WordprocessingDocument doc = WordprocessingDocument.Open(openFileDialog.FileName, false))
                {
                    var body = doc.MainDocumentPart?.Document.Body;
                    if (body == null) return;

                    // Используем WordParagraph для работы с OpenXML
                    foreach (var paragraph in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                    {
                        var wpfParagraph = new System.Windows.Documents.Paragraph();

                        foreach (var run in paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>())
                        {
                            var wpfRun = new System.Windows.Documents.Run(run.InnerText);

                            if (run.RunProperties != null)
                            {
                                // Жирный текст
                                if (run.RunProperties.Bold != null)
                                    wpfRun.FontWeight = FontWeights.Bold;

                                // Курсив
                                if (run.RunProperties.Italic != null)
                                    wpfRun.FontStyle = FontStyles.Italic;

                                // Размер шрифта
                                if (run.RunProperties.FontSize != null && !string.IsNullOrEmpty(run.RunProperties.FontSize.Val))
                                {
                                    if (double.TryParse(run.RunProperties.FontSize.Val, out double fontSize))
                                    {
                                        wpfRun.FontSize = fontSize / 2; // OpenXML использует половинные единицы
                                    }
                                }

                                // Цвет текста
                                if (run.RunProperties.Color != null && run.RunProperties.Color.Val != null)
                                {
                                    string colorStr = run.RunProperties.Color.Val.Value;
                                    if (colorStr.Length == 6)
                                    {
                                        var color = System.Windows.Media.Color.FromRgb(
                                            Convert.ToByte(colorStr.Substring(0, 2), 16),
                                            Convert.ToByte(colorStr.Substring(2, 2), 16),
                                            Convert.ToByte(colorStr.Substring(4, 2), 16));
                                        wpfRun.Foreground = new SolidColorBrush(color);
                                    }
                                }

                            }

                            wpfParagraph.Inlines.Add(wpfRun);
                        }

                        flowDoc.Blocks.Add(wpfParagraph);
                    }
                }

                string serialized = RichTextSerializationHelper.SerializeFlowDocument(flowDoc);

                var newTemplate = new Template
                {
                    Name = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName),
                    Content = serialized
                };

                category.Templates.Add(newTemplate);
                TemplatesListBox.ItemsSource = null;
                TemplatesListBox.ItemsSource = category.Templates;
                saveChanges?.Invoke();

                MessageBox.Show("Шаблон успешно импортирован!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при импорте: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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