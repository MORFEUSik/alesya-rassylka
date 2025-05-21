using System.Windows;
using System.Windows.Documents;
using MahApps.Metro.Controls;
using System.IO;
using System.Text;


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
                if (!string.IsNullOrWhiteSpace(Template.Content))
                {
                    TemplateContentRichTextBox.Document = RichTextSerializationHelper.DeserializeFlowDocument(Template.Content);
                }
                else
                {
                    TemplateContentRichTextBox.Document.Blocks.Clear();
                }

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

            Template.Content = RichTextSerializationHelper.SerializeFlowDocument(TemplateContentRichTextBox.Document);
            

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