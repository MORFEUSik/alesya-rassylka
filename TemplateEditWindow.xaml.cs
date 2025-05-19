using System.Windows;
using System.Windows.Documents;
using MahApps.Metro.Controls;

namespace alesya_rassylka
{
    public partial class TemplateEditWindow : MetroWindow
    {
        public Template Template { get; private set; }

        public TemplateEditWindow(Template template)
        {
            InitializeComponent();
            Template = template ?? new Template { Name = "Новый шаблон", Content = "" };

            TemplateNameTextBox.Text = Template.Name;

            // Загрузка содержимого в RichTextBox
            if (!string.IsNullOrEmpty(Template.Content))
            {
                TemplateContentRichTextBox.Document.Blocks.Clear();
                TemplateContentRichTextBox.Document.Blocks.Add(new Paragraph(new Run(Template.Content)));
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string newName = TemplateNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("Название шаблона не может быть пустым!", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Template.Name = newName;

            // Получение текста из RichTextBox
            TextRange textRange = new TextRange(
                TemplateContentRichTextBox.Document.ContentStart,
                TemplateContentRichTextBox.Document.ContentEnd
            );
            Template.Content = textRange.Text;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}