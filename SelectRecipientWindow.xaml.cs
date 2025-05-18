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
using MahApps.Metro.Controls;
using System.Windows.Media.Imaging;

namespace alesya_rassylka
{
    public partial class SelectRecipientWindow : MetroWindow
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
                if (item != null)
                {
                    listBox.ContextMenu.PlacementTarget = item;
                    item.IsSelected = true;
                }
                else
                {
                    listBox.ContextMenu.PlacementTarget = listBox;
                }
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
            e.Handled = true;
            var listBox = sender as ListBox;
            var item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            rightClickedRecipient = item?.Content as Recipient;
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
            var addCategoryWindow = new MetroWindow
            {
                Title = "Добавление категории",
                Width = 350,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop((Color)ColorConverter.ConvertFromString("#F5F6F5"), 0.0),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#E0E7E9"), 1.0)
                    }
                },
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                BorderThickness = new Thickness(1),
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png"))
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = "Введите название новой категории:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"))
            };
            stackPanel.Children.Add(title);

            var inputTextBox = new TextBox
            {
                Width = 250,
                Height = 30,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White
            };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var saveButton = new Button
            {
                Content = "Сохранить",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D3D3D3")),
                Foreground = Brushes.Black,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };

            bool confirmed = false;
            saveButton.Click += (s, args) =>
            {
                string newCategory = inputTextBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(newCategory) && !Categories.Contains(newCategory))
                {
                    Categories.Add(newCategory);
                    FilteredCategories.Add(newCategory);
                    SaveCallback();
                    System.Diagnostics.Debug.WriteLine($"Added category: {newCategory}");
                    confirmed = true;
                }
                addCategoryWindow.Close();
            };
            cancelButton.Click += (s, args) => addCategoryWindow.Close();

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(inputTextBox);
            stackPanel.Children.Add(buttonPanel);
            addCategoryWindow.Content = stackPanel;
            addCategoryWindow.ShowDialog();
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
            if (string.IsNullOrWhiteSpace(rightClickedCategory))
            {
                System.Diagnostics.Debug.WriteLine("Edit failed: No category selected.");
                return;
            }

            var editCategoryWindow = new MetroWindow
            {
                Title = "Редактирование категории",
                Width = 350,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop((Color)ColorConverter.ConvertFromString("#F5F6F5"), 0.0),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#E0E7E9"), 1.0)
                    }
                },
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                BorderThickness = new Thickness(1),
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png"))
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = "Введите новое название категории:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"))
            };
            stackPanel.Children.Add(title);

            var inputTextBox = new TextBox
            {
                Width = 250,
                Height = 30,
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                Text = rightClickedCategory,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White
            };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var saveButton = new Button
            {
                Content = "Сохранить",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D3D3D3")),
                Foreground = Brushes.Black,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };

            saveButton.Click += (s, args) =>
            {
                string newCategory = inputTextBox.Text.Trim();
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
                editCategoryWindow.Close();
            };
            cancelButton.Click += (s, args) => editCategoryWindow.Close();

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(inputTextBox);
            stackPanel.Children.Add(buttonPanel);
            editCategoryWindow.Content = stackPanel;
            editCategoryWindow.ShowDialog();
        }

        private void AddRecipient_Click(object sender, RoutedEventArgs e)
        {
            var addRecipientWindow = new MetroWindow
            {
                Title = "Добавление получателя",
                Width = 700,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop((Color)ColorConverter.ConvertFromString("#F5F6F5"), 0.0),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#E0E7E9"), 1.0)
                    }
                },
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                BorderThickness = new Thickness(1),
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png"))
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = "Добавление нового получателя",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"))
            };
            stackPanel.Children.Add(title);

            var nameLabel = new TextBlock { Text = "Имя:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5), Foreground = Brushes.Black };
            var nameTextBox = new TextBox { Width = 300, Height = 30, FontSize = 14, Margin = new Thickness(0, 0, 0, 15), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")), BorderThickness = new Thickness(1), Background = Brushes.White };
            stackPanel.Children.Add(nameLabel);
            stackPanel.Children.Add(nameTextBox);

            var emailLabel = new TextBlock { Text = "Email:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5), Foreground = Brushes.Black };
            var emailTextBox = new TextBox { Width = 300, Height = 30, FontSize = 14, Margin = new Thickness(0, 0, 0, 15), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")), BorderThickness = new Thickness(1), Background = Brushes.White };
            stackPanel.Children.Add(emailLabel);
            stackPanel.Children.Add(emailTextBox);

            var categoryTitle = new TextBlock
            {
                Text = "Выберите категории:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"))
            };
            stackPanel.Children.Add(categoryTitle);

            var categoryListBox = new ListBox
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = Categories,
                Height = 200,
                Margin = new Thickness(0, 0, 0, 15),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                FontSize = 14
            };
            stackPanel.Children.Add(categoryListBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var saveButton = new Button
            {
                Content = "Сохранить",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D3D3D3")),
                Foreground = Brushes.Black,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };

            saveButton.Click += (s, args) =>
            {
                string name = nameTextBox.Text.Trim();
                string email = emailTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    System.Windows.MessageBox.Show("Имя не может быть пустым.", "Ошибка");
                    return;
                }
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                {
                    System.Windows.MessageBox.Show("Некорректный email.", "Ошибка");
                    return;
                }
                if (allRecipients.Any(r => r.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                {
                    System.Windows.MessageBox.Show("Получатель с таким email уже существует.", "Ошибка");
                    return;
                }

                var selectedCategories = categoryListBox.SelectedItems.Cast<string>().ToList();
                var newRecipient = new Recipient { Name = name, Email = email, Categories = selectedCategories };
                allRecipients.Add(newRecipient);
                filteredRecipients.Refresh();
                SaveCallback();
                System.Diagnostics.Debug.WriteLine($"Added recipient: {name}, {email}");
                addRecipientWindow.Close();
            };
            cancelButton.Click += (s, args) => addRecipientWindow.Close();

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);
            addRecipientWindow.Content = stackPanel;
            addRecipientWindow.ShowDialog();
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

            var editRecipientWindow = new MetroWindow
            {
                Title = "Редактирование получателя",
                Width = 400,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop((Color)ColorConverter.ConvertFromString("#F5F6F5"), 0.0),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#E0E7E9"), 1.0)
                    }
                },
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                BorderThickness = new Thickness(1),
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png"))
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var title = new TextBlock
            {
                Text = "Редактирование получателя",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"))
            };
            stackPanel.Children.Add(title);

            var nameLabel = new TextBlock { Text = "Имя:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5), Foreground = Brushes.Black };
            var nameTextBox = new TextBox { Width = 300, Height = 30, FontSize = 14, Text = recipient.Name, Margin = new Thickness(0, 0, 0, 15), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")), BorderThickness = new Thickness(1), Background = Brushes.White };
            stackPanel.Children.Add(nameLabel);
            stackPanel.Children.Add(nameTextBox);

            var emailLabel = new TextBlock { Text = "Email:", FontSize = 14, Margin = new Thickness(0, 0, 0, 5), Foreground = Brushes.Black };
            var emailTextBox = new TextBox { Width = 300, Height = 30, FontSize = 14, Text = recipient.Email, Margin = new Thickness(0, 0, 0, 15), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")), BorderThickness = new Thickness(1), Background = Brushes.White };
            stackPanel.Children.Add(emailLabel);
            stackPanel.Children.Add(emailTextBox);

            var categoryTitle = new TextBlock
            {
                Text = "Выберите категории:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30"))
            };
            stackPanel.Children.Add(categoryTitle);

            var categoryListBox = new ListBox
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = Categories,
                Height = 200,
                Margin = new Thickness(0, 0, 0, 15),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                FontSize = 14
            };
            foreach (var category in Categories)
            {
                if (recipient.Categories.Contains(category))
                {
                    categoryListBox.SelectedItems.Add(category);
                }
            }
            stackPanel.Children.Add(categoryListBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var saveButton = new Button
            {
                Content = "Сохранить",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D3D3D3")),
                Foreground = Brushes.Black,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand
            };

            saveButton.Click += (s, args) =>
            {
                string newName = nameTextBox.Text.Trim();
                string newEmail = emailTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(newName))
                {
                    System.Windows.MessageBox.Show("Имя не может быть пустым.", "Ошибка");
                    return;
                }
                if (string.IsNullOrWhiteSpace(newEmail) || !newEmail.Contains("@"))
                {
                    System.Windows.MessageBox.Show("Некорректный email.", "Ошибка");
                    return;
                }
                if (allRecipients.Any(r => r != recipient && r.Email.Equals(newEmail, StringComparison.OrdinalIgnoreCase)))
                {
                    System.Windows.MessageBox.Show("Получатель с таким email уже существует.", "Ошибка");
                    return;
                }

                var newSelectedCategories = categoryListBox.SelectedItems.Cast<string>().ToList();
                recipient.Name = newName;
                recipient.Email = newEmail;
                recipient.Categories = newSelectedCategories;
                filteredRecipients.Refresh();
                SaveCallback();
                System.Diagnostics.Debug.WriteLine($"Edited recipient: {newName}, {newEmail}");
                editRecipientWindow.Close();
            };
            cancelButton.Click += (s, args) => editRecipientWindow.Close();

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);
            editRecipientWindow.Content = stackPanel;
            editRecipientWindow.ShowDialog();
        }

        private void ToggleSelection_Click(object sender, RoutedEventArgs e)
        {
            var recipientsToToggle = filteredRecipients.Cast<Recipient>().ToList();
            if (!recipientsToToggle.Any())
            {
                System.Windows.MessageBox.Show("Нет получателей для выделения.", "Ошибка");
                return;
            }

            bool allSelected = recipientsToToggle.All(r => RecipientsListBox.SelectedItems.Contains(r));
            if (allSelected)
            {
                foreach (var recipient in recipientsToToggle)
                {
                    RecipientsListBox.SelectedItems.Remove(recipient);
                }
            }
            else
            {
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