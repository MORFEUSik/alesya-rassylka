using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace alesya_rassylka
{
    public partial class SelectRecipientWindow : Window
    {
        private List<Recipient> allRecipients;
        private ICollectionView filteredRecipients;
        public List<Recipient> SelectedRecipients { get; private set; } = new();
        private readonly Action SaveCallback;
        private string rightClickedCategory;
        private List<string> selectedCategories = new();

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
            selectedCategories = listBox.SelectedItems.Cast<string>().ToList();
            filteredRecipients.Refresh();

            var textBlock = (TextBlock)CategoryComboBox.Template.FindName("SelectedCategoriesText", CategoryComboBox);
            if (selectedCategories.Any())
                textBlock.Text = string.Join(", ", selectedCategories);
            else
                textBlock.Text = "Выберите категории...";
        }

        private void CategoryListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Блокируем выбор при правом клике

            var element = e.OriginalSource as DependencyObject;
            while (element != null && !(element is CheckBox))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            if (element is CheckBox checkBox && checkBox.Content is string category)
            {
                rightClickedCategory = category;
                var listBox = sender as ListBox;
                if (listBox?.ContextMenu != null)
                {
                    listBox.ContextMenu.DataContext = category;
                    listBox.ContextMenu.PlacementTarget = listBox;
                    listBox.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    listBox.ContextMenu.IsOpen = true;
                }
            }
        }

        private void CategoryComboBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Предотвращаем лишние действия на ComboBox
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
            if (!string.IsNullOrWhiteSpace(rightClickedCategory) && Categories.Contains(rightClickedCategory))
            {
                Categories.Remove(rightClickedCategory);
                foreach (var r in allRecipients)
                {
                    r.Categories.Remove(rightClickedCategory);
                }
                CategoryComboBox.Items.Refresh();
                filteredRecipients.Refresh();
                SaveCallback();
            }
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(rightClickedCategory))
            {
                string newCategory = Microsoft.VisualBasic.Interaction.InputBox("Редактировать категорию:", "", rightClickedCategory);
                if (!string.IsNullOrWhiteSpace(newCategory) && newCategory != rightClickedCategory)
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
                        CategoryComboBox.Items.Refresh();
                        filteredRecipients.Refresh();
                        SaveCallback();
                    }
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