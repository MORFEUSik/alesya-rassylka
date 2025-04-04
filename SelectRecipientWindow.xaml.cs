using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using Xceed.Wpf.Toolkit;
using System.Windows.Input;

namespace alesya_rassylka
{
    public partial class SelectRecipientWindow : Window
    {

        private List<Recipient> allRecipients;
        private ICollectionView filteredRecipients;
        public List<Recipient> SelectedRecipients { get; private set; } = new();

        private readonly Action SaveCallback;

        public List<string> Categories { get; set; }

        public SelectRecipientWindow(DataStore dataStore, Action saveCallback)
        {
            InitializeComponent();

            SaveCallback = saveCallback;
            allRecipients = dataStore.Recipients;
            Categories = dataStore.Categories;

            DataContext = this;

            filteredRecipients = CollectionViewSource.GetDefaultView(allRecipients);
            filteredRecipients.Filter = FilterRecipients;
            RecipientsListBox.ItemsSource = filteredRecipients;
            CategoryComboBox.ItemSelectionChanged += (s, e) => filteredRecipients.Refresh();
        }

        private bool FilterRecipients(object obj)
        {
            if (obj is Recipient recipient)
            {
                var selectedCategories = CategoryComboBox.SelectedItems.Cast<string>().ToList();
                var searchText = SearchTextBox.Text?.ToLower() ?? "";

                bool matchesCategory = selectedCategories.Count == 0 || recipient.Categories.Any(c => selectedCategories.Contains(c));
                bool matchesSearch = recipient.Name.ToLower().Contains(searchText) || recipient.Email.ToLower().Contains(searchText);

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
            SelectedRecipients = RecipientsListBox.SelectedItems.Cast<Recipient>().ToList();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CategoryComboBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Блокируем закрытие выпадающего списка по правому клику
            e.Handled = true;
        }

        private void CategoryComboBox_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var comboBox = sender as FrameworkElement;
            if (comboBox?.ContextMenu != null)
            {
                comboBox.ContextMenu.PlacementTarget = comboBox;
                comboBox.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                comboBox.ContextMenu.IsOpen = true;
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            string newCategory = Microsoft.VisualBasic.Interaction.InputBox("Введите новую категорию:");
            if (!string.IsNullOrWhiteSpace(newCategory) && !Categories.Contains(newCategory))
            {
                Categories.Add(newCategory);
                CategoryComboBox.Items.Refresh();
                SaveCallback();
            }
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem is string selected)
            {
                Categories.Remove(selected);
                CategoryComboBox.Items.Refresh();
                SaveCallback();
            }
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem is string selectedCategory)
            {
                string newCategory = Microsoft.VisualBasic.Interaction.InputBox("Редактировать категорию:", "", selectedCategory);
                if (!string.IsNullOrWhiteSpace(newCategory))
                {
                    int index = Categories.IndexOf(selectedCategory);
                    Categories[index] = newCategory;

                    foreach (var r in allRecipients)
                    {
                        if (r.Categories.Contains(selectedCategory))
                        {
                            r.Categories.Remove(selectedCategory);
                            r.Categories.Add(newCategory);
                        }
                    }

                    CategoryComboBox.Items.Refresh();
                    filteredRecipients.Refresh();
                    SaveCallback();
                }
            }
        }

        private void AddRecipient_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Добавление получателя пока не реализовано");
        }
        private void DeleteRecipient_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Удаление получателя пока не реализовано");
        }
        private void EditRecipient_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Редактирование получателя пока не реализовано");
        }
    }
}
