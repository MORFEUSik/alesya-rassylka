using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using MahApps.Metro.Controls;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;

namespace alesya_rassylka
{
    public partial class TemplateManagerWindow : MetroWindow
    {
        private TemplateCategory category;
        private Action saveChanges;
        public Template SelectedTemplate { get; private set; }

        public TemplateManagerWindow(TemplateCategory category, Action saveChanges)
        {
            InitializeComponent();
            this.category = category;
            this.saveChanges = saveChanges;

            CategoryNameTextBox.Text = category.Name;
            TemplatesListBox.ItemsSource = category.Templates;
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
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Word Documents (*.docx)|*.docx",
                Title = "Выберите документ Word"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Проверяем расширение файла
                    if (System.IO.Path.GetExtension(openFileDialog.FileName).ToLower() == ".doc")
                    {
                        MessageBox.Show("Формат .doc не поддерживается. Пожалуйста, конвертируйте файл в .docx.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string filePath = openFileDialog.FileName;
                    string content = "";

                    // Открываем документ Word с помощью OpenXml
                    using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
                    {
                        // Извлекаем текст из параграфов
                        var body = wordDoc.MainDocumentPart.Document.Body;
                        content = string.Join("\n", body.Descendants<Paragraph>()
                            .Select(p => p.InnerText)
                            .Where(t => !string.IsNullOrEmpty(t)));
                    }

                    // Добавляем содержимое в шаблон
                    var newTemplate = new Template { Name = "Новый шаблон из Word", Content = content };
                    category.Templates.Add(newTemplate);
                    TemplatesListBox.ItemsSource = null;
                    TemplatesListBox.ItemsSource = category.Templates;

                    // Вызываем делегат для сохранения изменений
                    saveChanges?.Invoke();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке документа Word: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditTemplate_Click(object sender, RoutedEventArgs e)
        {
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
        }

        private void SelectTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Template template)
            {
                SelectedTemplate = template;
                DialogResult = true;
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}