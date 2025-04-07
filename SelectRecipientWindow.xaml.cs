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

namespace alesya_rassylka
{
    public partial class SelectRecipientWindow : Window
    {
        private List<Recipient> allRecipients;
        private ICollectionView filteredRecipients;
        public List<Recipient> SelectedRecipients { get; private set; } = new();
        private readonly Action SaveCallback;
        private string rightClickedCategory;
        private Recipient rightClickedRecipient; // Для хранения получателя, на которого нажали правой кнопкой
        private ObservableCollection<string> selectedCategories = new ObservableCollection<string>();

        public ObservableCollection<string> Categories { get; set; }
        public ObservableCollection<string> FilteredCategories { get; set; }

        public SelectRecipientWindow(DataStore dataStore, Action saveCallback)
        {
            InitializeComponent();

            SaveCallback = saveCallback;
            allRecipients = dataStore.Recipients;
            Categories = dataStore.Categories;
            FilteredCategories = new ObservableCollection<string>(Categories);

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

        private void CategorySearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = CategorySearchTextBox.Text?.ToLower() ?? "";
            FilteredCategories.Clear();
            foreach (var category in Categories.Where(c => c.ToLower().Contains(searchText)))
            {
                FilteredCategories.Add(category);
            }
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
        }

        private void CategoryListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
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

        private void RecipientsListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Предотвращаем изменение выделения правой кнопкой
            var listBox = sender as ListBox;
            var item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            rightClickedRecipient = item?.Content as Recipient; // Сохраняем получателя, на которого нажали правой кнопкой
            System.Diagnostics.Debug.WriteLine($"Right-clicked recipient: {rightClickedRecipient?.Name}");

            if (listBox?.ContextMenu != null && rightClickedRecipient != null)
            {
                listBox.ContextMenu.DataContext = rightClickedRecipient;
                listBox.ContextMenu.PlacementTarget = item != null ? item : listBox;
                listBox.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                listBox.ContextMenu.IsOpen = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ContextMenu not opened: ListBox or recipient missing.");
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
                FilteredCategories.Add(newCategory);
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
                FilteredCategories.Remove(rightClickedCategory);
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
                        FilteredCategories[FilteredCategories.IndexOf(rightClickedCategory)] = newCategory;
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
            // Ввод имени
            string name = Interaction.InputBox("Введите имя получателя:", "Добавление получателя");
            if (string.IsNullOrWhiteSpace(name))
            {
                System.Windows.MessageBox.Show("Имя не может быть пустым.", "Ошибка");
                return;
            }

            // Ввод email
            string email = Interaction.InputBox("Введите email получателя:", "Добавление получателя");
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                System.Windows.MessageBox.Show("Некорректный email.", "Ошибка");
                return;
            }

            // Проверка на уникальность email
            if (allRecipients.Any(r => r.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                System.Windows.MessageBox.Show("Получатель с таким email уже существует.", "Ошибка");
                return;
            }

            // Выбор категорий
            var categorySelectionWindow = new Window
            {
                Title = "Выбор категорий",
                Width = 300,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            var categoryListBox = new ListBox
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = Categories,
                Height = 300
            };

            var confirmButton = new Button
            {
                Content = "Подтвердить",
                Width = 100,
                Margin = new Thickness(0, 10, 0, 0)
            };

            List<string> selectedCategoriesForNewRecipient = new List<string>();
            confirmButton.Click += (s, args) =>
            {
                selectedCategoriesForNewRecipient = categoryListBox.SelectedItems.Cast<string>().ToList();
                categorySelectionWindow.DialogResult = true;
                categorySelectionWindow.Close();
            };

            stackPanel.Children.Add(categoryListBox);
            stackPanel.Children.Add(confirmButton);
            categorySelectionWindow.Content = stackPanel;

            if (categorySelectionWindow.ShowDialog() != true)
            {
                return; // Пользователь отменил выбор категорий
            }

            // Создаем нового получателя
            var newRecipient = new Recipient
            {
                Name = name,
                Email = email,
                Categories = selectedCategoriesForNewRecipient
            };

            allRecipients.Add(newRecipient);
            filteredRecipients.Refresh();
            SaveCallback();
            System.Diagnostics.Debug.WriteLine($"Added recipient: {name}, {email}");
        }

        private void DeleteRecipient_Click(object sender, RoutedEventArgs e)
        {
            var selectedRecipients = RecipientsListBox.SelectedItems.Cast<Recipient>().ToList();

            // Если никто не выделен, но есть rightClickedRecipient, добавляем его в список для удаления
            if (!selectedRecipients.Any() && rightClickedRecipient != null)
            {
                selectedRecipients = new List<Recipient> { rightClickedRecipient };
            }

            // Если после этого список всё еще пуст, показываем ошибку
            if (!selectedRecipients.Any())
            {
                System.Windows.MessageBox.Show("Выберите хотя бы одного получателя для удаления.", "Ошибка");
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Вы уверены, что хотите удалить {selectedRecipients.Count} получателя(ей)?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var recipient in selectedRecipients)
                {
                    allRecipients.Remove(recipient);
                    System.Diagnostics.Debug.WriteLine($"Deleted recipient: {recipient.Name}, {recipient.Email}");
                }
                filteredRecipients.Refresh();
                SaveCallback();
            }
        }

        private void EditRecipient_Click(object sender, RoutedEventArgs e)
        {
            if (rightClickedRecipient == null)
            {
                System.Windows.MessageBox.Show("Выберите получателя для редактирования.", "Ошибка");
                return;
            }

            var recipient = rightClickedRecipient;

            // Ввод нового имени
            string newName = Interaction.InputBox("Введите новое имя получателя:", "Редактирование получателя", recipient.Name);
            if (string.IsNullOrWhiteSpace(newName))
            {
                System.Windows.MessageBox.Show("Имя не может быть пустым.", "Ошибка");
                return;
            }

            // Ввод нового email
            string newEmail = Interaction.InputBox("Введите новый email получателя:", "Редактирование получателя", recipient.Email);
            if (string.IsNullOrWhiteSpace(newEmail) || !newEmail.Contains("@"))
            {
                System.Windows.MessageBox.Show("Некорректный email.", "Ошибка");
                return;
            }

            // Проверка на уникальность email (кроме текущего получателя)
            if (allRecipients.Any(r => r != recipient && r.Email.Equals(newEmail, StringComparison.OrdinalIgnoreCase)))
            {
                System.Windows.MessageBox.Show("Получатель с таким email уже существует.", "Ошибка");
                return;
            }

            // Выбор категорий
            var categorySelectionWindow = new Window
            {
                Title = "Выбор категорий",
                Width = 300,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            var categoryListBox = new ListBox
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = Categories,
                Height = 300
            };

            // Отмечаем текущие категории получателя
            foreach (var category in Categories)
            {
                if (recipient.Categories.Contains(category))
                {
                    categoryListBox.SelectedItems.Add(category);
                }
            }

            var confirmButton = new Button
            {
                Content = "Подтвердить",
                Width = 100,
                Margin = new Thickness(0, 10, 0, 0)
            };

            List<string> newSelectedCategories = new List<string>();
            confirmButton.Click += (s, args) =>
            {
                newSelectedCategories = categoryListBox.SelectedItems.Cast<string>().ToList();
                categorySelectionWindow.DialogResult = true;
                categorySelectionWindow.Close();
            };

            stackPanel.Children.Add(categoryListBox);
            stackPanel.Children.Add(confirmButton);
            categorySelectionWindow.Content = stackPanel;

            if (categorySelectionWindow.ShowDialog() != true)
            {
                return; // Пользователь отменил редактирование категорий
            }

            // Обновляем данные получателя
            recipient.Name = newName;
            recipient.Email = newEmail;
            recipient.Categories = newSelectedCategories;

            filteredRecipients.Refresh();
            SaveCallback();
            System.Diagnostics.Debug.WriteLine($"Edited recipient: {newName}, {newEmail}");
        }

        private void ToggleSelection_Click(object sender, RoutedEventArgs e)
        {
            // Получаем всех отфильтрованных получателей
            var recipientsToToggle = filteredRecipients.Cast<Recipient>().ToList();

            if (!recipientsToToggle.Any())
            {
                System.Windows.MessageBox.Show("Нет получателей для выделения.", "Ошибка");
                return;
            }

            // Проверяем, все ли отфильтрованные получатели уже выделены
            bool allSelected = recipientsToToggle.All(r => RecipientsListBox.SelectedItems.Contains(r));

            if (allSelected)
            {
                // Если все уже выделены, снимаем выделение
                foreach (var recipient in recipientsToToggle)
                {
                    RecipientsListBox.SelectedItems.Remove(recipient);
                }
            }
            else
            {
                // Если не все выделены, выделяем всех
                foreach (var recipient in recipientsToToggle)
                {
                    if (!RecipientsListBox.SelectedItems.Contains(recipient))
                    {
                        RecipientsListBox.SelectedItems.Add(recipient);
                    }
                }
            }
        }
    }
}