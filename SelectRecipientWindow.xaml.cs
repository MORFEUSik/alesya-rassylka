using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;

namespace alesya_rassylka
{
    public partial class SelectRecipientWindow : Window
    {
        private List<Customer> allRecipients;
        private ICollectionView filteredRecipients;
        public List<Customer> SelectedRecipients { get; private set; } = new();

        public SelectRecipientWindow(List<Customer> recipients, List<string> categories)
        {
            InitializeComponent();

            allRecipients = recipients;

            // Заполняем категории
            CategoryComboBox.ItemsSource = categories;

            // Отображаем список получателей
            filteredRecipients = CollectionViewSource.GetDefaultView(allRecipients);
            filteredRecipients.Filter = FilterRecipients;
            RecipientsListBox.ItemsSource = filteredRecipients;
        }

        private bool FilterRecipients(object obj)
        {
            if (obj is Customer customer)
            {
                string selectedCategory = CategoryComboBox.SelectedItem as string;
                string searchText = SearchTextBox.Text?.ToLower();

                bool matchesCategory = string.IsNullOrEmpty(selectedCategory) || customer.ProductCategory == selectedCategory;
                bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                                     customer.Name.ToLower().Contains(searchText) ||
                                     customer.Email.ToLower().Contains(searchText);

                return matchesCategory && matchesSearch;
            }
            return false;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            filteredRecipients.Refresh();
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            filteredRecipients.Refresh();
        }

        private void ConfirmSelection_Click(object sender, RoutedEventArgs e)
        {
            SelectedRecipients = RecipientsListBox.SelectedItems.Cast<Customer>().ToList();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // Добавление категории
        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            string newCategory = Microsoft.VisualBasic.Interaction.InputBox("Введите новую категорию:");
            if (!string.IsNullOrWhiteSpace(newCategory) && !CategoryComboBox.Items.Contains(newCategory))
            {
                CategoryComboBox.Items.Add(newCategory);
            }
        }

        // Удаление категории
        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem != null)
            {
                CategoryComboBox.Items.Remove(CategoryComboBox.SelectedItem);
            }
        }

        // Редактирование категории
        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem != null)
            {
                string newCategory = Microsoft.VisualBasic.Interaction.InputBox("Редактировать категорию:", "", CategoryComboBox.SelectedItem.ToString());
                if (!string.IsNullOrWhiteSpace(newCategory))
                {
                    CategoryComboBox.Items[CategoryComboBox.SelectedIndex] = newCategory;
                }
            }
        }
    }
}
