using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using Microsoft.VisualBasic;
using System.Windows.Controls.Primitives;

namespace alesya_rassylka
{
    public partial class SelectRecipientWindow : Window
    {
        private List<Recipient> allRecipients;
        private ICollectionView filteredRecipients;
        public List<Recipient> SelectedRecipients { get; private set; } = new();
        private readonly Action SaveCallback;
        private string rightClickedCategory;
        private ObservableCollection<string> selectedCategories = new ObservableCollection<string>();

        public ObservableCollection<string> Categories { get; set; }

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
        }

        private bool FilterRecipients(object obj)
        {
            if (obj is Recipient recipient)
            {
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

        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            selectedCategories.Clear();
            foreach (var item in listBox.SelectedItems.Cast<string>())
            {
                selectedCategories.Add(item);
            }
            filteredRecipients.Refresh();

            var textBlock = (TextBlock)CategoryComboBox.Template.FindName("SelectedCategoriesText", CategoryComboBox);
            if (selectedCategories.Any())
                textBlock.Text = string.Join(", ", selectedCategories);
            else
                textBlock.Text = "Выберите категории...";
        }

        private void CategoryListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Блокируем дальнейшую обработку
            var listBox = sender as ListBox;
            var item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            rightClickedCategory = item?.Content as string;
            System.Diagnostics.Debug.WriteLine($"Right-clicked category: {rightClickedCategory}");

            if (listBox?.ContextMenu != null && !string.IsNullOrEmpty(rightClickedCategory))
            {
                listBox.ContextMenu.DataContext = rightClickedCategory;
                listBox.ContextMenu.PlacementTarget = item != null ? item : listBox;
                listBox.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                listBox.ContextMenu.IsOpen = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ContextMenu not opened: ListBox or category missing.");
            }
        }

        private void MenuItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Останавливаем распространение события от MenuItem
            var popup = (Popup)CategoryComboBox.Template.FindName("Popup", CategoryComboBox);
            if (popup != null)
            {
                popup.IsOpen = true; // Убеждаемся, что Popup остается открытым
                System.Diagnostics.Debug.WriteLine("Popup forced to stay open.");
            }
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

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            string newCategory = Interaction.InputBox("Введите новую категорию:");
            if (!string.IsNullOrWhiteSpace(newCategory) && !Categories.Contains(newCategory))
            {
                Categories.Add(newCategory);
                SaveCallback();
                System.Diagnostics.Debug.WriteLine($"Added category: {newCategory}");
            }
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Attempting to delete: {rightClickedCategory}");
            if (!string.IsNullOrWhiteSpace(rightClickedCategory) && Categories.Contains(rightClickedCategory))
            {
                Categories.Remove(rightClickedCategory);
                foreach (var r in allRecipients)
                {
                    r.Categories.Remove(rightClickedCategory);
                }
                filteredRecipients.Refresh();
                SaveCallback();
                System.Diagnostics.Debug.WriteLine($"Deleted category: {rightClickedCategory}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Delete failed: Category not found or null.");
            }
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Attempting to edit: {rightClickedCategory}");
            if (!string.IsNullOrWhiteSpace(rightClickedCategory))
            {
                string newCategory = Interaction.InputBox("Редактировать категорию:", "Редактирование", rightClickedCategory);
                if (!string.IsNullOrWhiteSpace(newCategory) && newCategory != rightClickedCategory && !Categories.Contains(newCategory))
                {
                    int index = Categories.IndexOf(rightClickedCategory);
                    if (index >= 0)
                    {
                        Categories[index] = newCategory;
                        foreach (var r in allRecipients)
                        {
                            if (r.Categories.Contains(rightClickedCategory))
                            {
                                r.Categories.Remove(rightClickedCategory);
                                r.Categories.Add(newCategory);
                            }
                        }
                        filteredRecipients.Refresh();
                        SaveCallback();
                        System.Diagnostics.Debug.WriteLine($"Edited {rightClickedCategory} to {newCategory}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Edit failed: New category invalid or already exists.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Edit failed: No category selected.");
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