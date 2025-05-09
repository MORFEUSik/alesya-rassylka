﻿using System.Windows;
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
            TemplateContentTextBox.Text = Template.Content;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string newName = TemplateNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("Название шаблона не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Template.Name = newName;
            Template.Content = TemplateContentTextBox.Text;
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