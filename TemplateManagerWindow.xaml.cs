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
            System.Diagnostics.Debug.WriteLine("Initializing TemplateManagerWindow");
            InitializeComponent();
            this.category = category ?? throw new ArgumentNullException(nameof(category));
            this.saveChanges = saveChanges;

            CategoryNameTextBox.Text = category.Name;
            TemplatesListBox.ItemsSource = category.Templates;
            System.Diagnostics.Debug.WriteLine("TemplateManagerWindow initialized successfully");
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
                    if (System.IO.Path.GetExtension(openFileDialog.FileName).ToLower() == ".doc")
                    {
                        MessageBox.Show("Формат .doc не поддерживается. Пожалуйста, конвертируйте файл в .docx.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string filePath = openFileDialog.FileName;
                    string content = "";

                    using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
                    {
                        var body = wordDoc.MainDocumentPart.Document.Body;
                        content = string.Join("\n", body.Descendants<Paragraph>()
                            .Select(p => p.InnerText)
                            .Where(t => !string.IsNullOrEmpty(t)));
                    }

                    var newTemplate = new Template { Name = "Новый шаблон из Word", Content = content };
                    category.Templates.Add(newTemplate);
                    TemplatesListBox.ItemsSource = null;
                    TemplatesListBox.ItemsSource = category.Templates;

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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}