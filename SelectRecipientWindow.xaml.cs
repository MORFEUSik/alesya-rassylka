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
            System.Diagnostics.Debug.WriteLine($"Щелчок правой кнопкой по категории: {rightClickedCategory}");

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
                System.Diagnostics.Debug.WriteLine("Контекстное меню не открыто: отсутствует ListBox или категория.");

            }
        }

        private void RecipientsListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var listBox = sender as ListBox;
            var item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            rightClickedRecipient = item?.Content as Recipient;
            System.Diagnostics.Debug.WriteLine($"Щелчок правой кнопкой по получателю: {rightClickedRecipient?.Name}");


            if (listBox?.ContextMenu != null && rightClickedRecipient != null)
            {
                listBox.ContextMenu.DataContext = rightClickedRecipient;
                listBox.ContextMenu.PlacementTarget = item != null ? item : listBox;
                listBox.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                listBox.ContextMenu.IsOpen = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Контекстное меню не открыто: отсутствует ListBox или получатель.");

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
                TitleCharacterCasing = CharacterCasing.Normal,
                Width = 350,
                Height = 165,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE3EB")),
                ResizeMode = ResizeMode.NoResize,
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png")),
                TitleForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1)
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            var title = new TextBlock
            {
                Text = "Введите название новой категории:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(title);

            var inputTextBox = new TextBox
            {
                
                Height = 30,
                FontSize = 14,
                Padding = new Thickness(5),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15),
                Template = CreateRoundedTextBoxTemplate()
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Style CreateActionButtonStyle()
            {
                var style = new Style(typeof(Button));
                style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
                style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
                style.Setters.Add(new Setter(Control.FontSizeProperty, 16.0));
                style.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Arial Black")));
                style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
                style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
                style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
                style.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
                style.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
                style.Setters.Add(new Setter(Control.MinHeightProperty, 30.0));
                style.Setters.Add(new Setter(Control.TemplateProperty, CreateButtonTemplate()));
                return style;
            }

            ControlTemplate CreateButtonTemplate()
            {
                var template = new ControlTemplate(typeof(Button));
                var border = new FrameworkElementFactory(typeof(Border));
                border.Name = "border";
                border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
                border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
                border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
                border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

                var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                border.AppendChild(contentPresenter);

                template.VisualTree = border;

                var mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                mouseOverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E6F8")), "border"));
                template.Triggers.Add(mouseOverTrigger);

                var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
                pressedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0D0F0")), "border"));
                template.Triggers.Add(pressedTrigger);

                return template;
            }

            ControlTemplate CreateRoundedTextBoxTemplate()
            {
                var template = new ControlTemplate(typeof(TextBox));
                var border = new FrameworkElementFactory(typeof(Border));
                border.Name = "Border";
                border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
                border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
                border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

                var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
                scrollViewer.Name = "PART_ContentHost";
                scrollViewer.SetValue(ScrollViewer.MarginProperty, new Thickness(0));
                border.AppendChild(scrollViewer);

                template.VisualTree = border;
                return template;
            }

            var saveButton = new Button
            {
                Content = "Применить",
                Width = 125,
                Height = 35,
                Margin = new Thickness(0, 0, 15, 0),
                Style = CreateActionButtonStyle()
            };

            var cancelButton = new Button
            {
                Content = "Отменить",
                Width = 125,
                Height = 35,
                Style = CreateActionButtonStyle()
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
            System.Diagnostics.Debug.WriteLine($"Попытка удалить: {rightClickedCategory}");
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
                System.Diagnostics.Debug.WriteLine($"Удалена категория: {rightClickedCategory}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Ошибка удаления: Категория не найдена или пуста.");
            }
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(rightClickedCategory))
            {
                System.Diagnostics.Debug.WriteLine("Редактирование не выполнено: категория не выбрана.");
                return;
            }

            var editCategoryWindow = new MetroWindow
            {
                Title = "Редактирование категории",
                TitleCharacterCasing = CharacterCasing.Normal,
                Width = 350,
                Height = 165,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE3EB")),
                ResizeMode = ResizeMode.NoResize,
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png")),
                TitleForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1)
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            var title = new TextBlock
            {
                Text = "Введите новое название категории:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(title);

            var inputTextBox = new TextBox
            {
                Text = rightClickedCategory,
                Height = 30,
                FontSize = 14,
                Padding = new Thickness(5),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 15),
                Template = CreateRoundedTextBoxTemplate()
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var saveButton = new Button
            {
                Content = "Применить",
                Width = 125,
                Height = 35,
                Margin = new Thickness(0, 0, 15, 0),
                Style = CreateActionButtonStyle()
            };

            var cancelButton = new Button
            {
                Content = "Отменить",
                Width = 125,
                Height = 35,
                Style = CreateActionButtonStyle()
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
                        System.Diagnostics.Debug.WriteLine($"Категория изменена: {rightClickedCategory} → {newCategory}");
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

            // Локальные методы
            Style CreateActionButtonStyle()
            {
                var style = new Style(typeof(Button));
                style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
                style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
                style.Setters.Add(new Setter(Control.FontSizeProperty, 16.0));
                style.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Arial Black")));
                style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
                style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
                style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
                style.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
                style.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
                style.Setters.Add(new Setter(Control.MinHeightProperty, 30.0));
                style.Setters.Add(new Setter(Control.TemplateProperty, CreateButtonTemplate()));
                return style;
            }

            ControlTemplate CreateButtonTemplate()
            {
                var template = new ControlTemplate(typeof(Button));
                var border = new FrameworkElementFactory(typeof(Border));
                border.Name = "border";
                border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
                border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
                border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
                border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

                var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                border.AppendChild(contentPresenter);

                template.VisualTree = border;

                var mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                mouseOverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E6F8")), "border"));
                template.Triggers.Add(mouseOverTrigger);

                var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
                pressedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0D0F0")), "border"));
                template.Triggers.Add(pressedTrigger);

                return template;
            }

            ControlTemplate CreateRoundedTextBoxTemplate()
            {
                var template = new ControlTemplate(typeof(TextBox));
                var border = new FrameworkElementFactory(typeof(Border));
                border.Name = "Border";
                border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
                border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
                border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

                var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
                scrollViewer.Name = "PART_ContentHost";
                scrollViewer.SetValue(ScrollViewer.MarginProperty, new Thickness(0));
                border.AppendChild(scrollViewer);

                template.VisualTree = border;
                return template;
            }
        }

        private void AddRecipient_Click(object sender, RoutedEventArgs e)
        {
            var addRecipientWindow = new MetroWindow
            {
                Title = "Добавление получателя",
                Width = 350,
                Height = 445,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE3EB")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png")),
                TitleForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                TitleCharacterCasing = CharacterCasing.Normal
            };

            Style CreateActionButtonStyle()
            {
                var style = new Style(typeof(Button));
                style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
                style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
                style.Setters.Add(new Setter(Control.FontSizeProperty, 16.0));
                style.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Arial Black")));
                style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
                style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
                style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
                style.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
                style.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
                style.Setters.Add(new Setter(Control.MinHeightProperty, 30.0));
                style.Setters.Add(new Setter(Control.TemplateProperty, CreateButtonTemplate()));
                return style;
            }

            ControlTemplate CreateButtonTemplate()
            {
                var template = new ControlTemplate(typeof(Button));
                var border = new FrameworkElementFactory(typeof(Border));
                border.Name = "border";
                border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
                border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
                border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
                border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

                var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
                border.AppendChild(contentPresenter);

                template.VisualTree = border;

                var mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                mouseOverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E6F8")), "border"));
                template.Triggers.Add(mouseOverTrigger);

                var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
                pressedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0D0F0")), "border"));
                template.Triggers.Add(pressedTrigger);

                return template;
            }

            ControlTemplate CreateRoundedTextBoxTemplate()
            {
                var template = new ControlTemplate(typeof(TextBox));
                var border = new FrameworkElementFactory(typeof(Border));
                border.Name = "Border";
                border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
                border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
                border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

                var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
                scrollViewer.Name = "PART_ContentHost";
                scrollViewer.SetValue(ScrollViewer.MarginProperty, new Thickness(0));
                border.AppendChild(scrollViewer);

                template.VisualTree = border;
                return template;
            }

            // Создаём стиль ListBox с закруглёнными углами
            Style listBoxStyle = new Style(typeof(ListBox));
            listBoxStyle.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
            listBoxStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            listBoxStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));

            ControlTemplate listBoxTemplate = new ControlTemplate(typeof(ListBox));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            FrameworkElementFactory scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            scrollViewer.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            scrollViewer.SetValue(FrameworkElement.MarginProperty, new Thickness(5));

            FrameworkElementFactory itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            scrollViewer.AppendChild(itemsPresenter);
            border.AppendChild(scrollViewer);
            listBoxTemplate.VisualTree = border;

            listBoxStyle.Setters.Add(new Setter(Control.TemplateProperty, listBoxTemplate));

            // Стиль для ListBoxItem с подсветкой (обновленный)
            Style listBoxItemStyle = new Style(typeof(ListBoxItem));
            listBoxItemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(5)));
            listBoxItemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            listBoxItemStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            listBoxItemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            listBoxItemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));

            ControlTemplate listBoxItemTemplate = new ControlTemplate(typeof(ListBoxItem));
            FrameworkElementFactory itemBorder = new FrameworkElementFactory(typeof(Border));
            itemBorder.Name = "Bd";
            itemBorder.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            itemBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            itemBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            itemBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));

            FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
            contentPresenter.SetValue(FrameworkElement.VerticalAlignmentProperty, new TemplateBindingExtension(Control.VerticalContentAlignmentProperty));
            itemBorder.AppendChild(contentPresenter);

            listBoxItemTemplate.VisualTree = itemBorder;

            // Обновленные триггеры для выделения и наведения мыши
            Trigger isSelectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            isSelectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0D0F0")), "Bd"));
            isSelectedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black)); // Черный текст при выделении

            Trigger isMouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            isMouseOverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E6F8")), "Bd"));

            listBoxItemTemplate.Triggers.Add(isSelectedTrigger);
            listBoxItemTemplate.Triggers.Add(isMouseOverTrigger);

            listBoxItemStyle.Setters.Add(new Setter(Control.TemplateProperty, listBoxItemTemplate));
            listBoxStyle.Setters.Add(new Setter(ListBox.ItemContainerStyleProperty, listBoxItemStyle));

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            var title = new TextBlock
            {
                Text = "Добавление нового получателя:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))
            };
            stackPanel.Children.Add(title);

            var nameLabel = new TextBlock
            {
                Text = "Имя:",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))
            };
            var nameTextBox = new TextBox
            {
                Height = 30,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                Padding = new Thickness(5),
                Template = CreateRoundedTextBoxTemplate()
            };
            stackPanel.Children.Add(nameLabel);
            stackPanel.Children.Add(nameTextBox);

            var emailLabel = new TextBlock
            {
                Text = "Email:",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))
            };
            var emailTextBox = new TextBox
            {
                Height = 30,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                Padding = new Thickness(5),
                Template = CreateRoundedTextBoxTemplate()
            };
            stackPanel.Children.Add(emailLabel);
            stackPanel.Children.Add(emailTextBox);

            var categoryTitle = new TextBlock
            {
                Text = "Выбор категории:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))
            };
            stackPanel.Children.Add(categoryTitle);

            var categoryListBox = new ListBox
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = Categories,
                Height = 150,
                Margin = new Thickness(0, 0, 0, 15),
                Style = listBoxStyle, // применяем стиль с закруглёнными углами
                FontSize = 14
            };
            stackPanel.Children.Add(categoryListBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var saveButton = new Button
            {
                Content = "Применить",
                Width = 125,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Style = CreateActionButtonStyle()
            };

            var cancelButton = new Button
            {
                Content = "Отменить",
                Width = 125,
                Height = 35,
                Style = CreateActionButtonStyle()
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
                System.Diagnostics.Debug.WriteLine($"Получатель добавлен: {name}, {email}");
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

            if (!selectedRecipients.Any() && rightClickedRecipient != null)
            {
                selectedRecipients = new List<Recipient> { rightClickedRecipient };
            }

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
                    System.Diagnostics.Debug.WriteLine($"Получатель(ли) удален(ы): {recipient.Name}, {recipient.Email}");
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
                Width = 350,
                Height = 445,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE3EB")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Icon = new BitmapImage(new Uri("pack://application:,,,/icons8-почта-100.png")),
                TitleForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                TitleCharacterCasing = CharacterCasing.Normal
            };

            // Создаём стиль ListBox с закруглёнными углами
            Style listBoxStyle = new Style(typeof(ListBox));
            listBoxStyle.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
            listBoxStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            listBoxStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));

            ControlTemplate listBoxTemplate = new ControlTemplate(typeof(ListBox));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            FrameworkElementFactory scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            scrollViewer.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            scrollViewer.SetValue(FrameworkElement.MarginProperty, new Thickness(5));

            FrameworkElementFactory itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            scrollViewer.AppendChild(itemsPresenter);
            border.AppendChild(scrollViewer);
            listBoxTemplate.VisualTree = border;

            listBoxStyle.Setters.Add(new Setter(Control.TemplateProperty, listBoxTemplate));

            // Стиль для ListBoxItem с подсветкой
            Style listBoxItemStyle = new Style(typeof(ListBoxItem));
            listBoxItemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(5)));
            listBoxItemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            listBoxItemStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            listBoxItemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            listBoxItemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));

            ControlTemplate listBoxItemTemplate = new ControlTemplate(typeof(ListBoxItem));
            FrameworkElementFactory itemBorder = new FrameworkElementFactory(typeof(Border));
            itemBorder.Name = "Bd";
            itemBorder.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            itemBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            itemBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            itemBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));

            FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
            contentPresenter.SetValue(FrameworkElement.VerticalAlignmentProperty, new TemplateBindingExtension(Control.VerticalContentAlignmentProperty));
            itemBorder.AppendChild(contentPresenter);

            listBoxItemTemplate.VisualTree = itemBorder;

            // Триггеры для выделения и наведения мыши
            Trigger isSelectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            isSelectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0D0F0")), "Bd"));
            isSelectedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black));

            Trigger isMouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            isMouseOverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E6F8")), "Bd"));

            listBoxItemTemplate.Triggers.Add(isSelectedTrigger);
            listBoxItemTemplate.Triggers.Add(isMouseOverTrigger);

            listBoxItemStyle.Setters.Add(new Setter(Control.TemplateProperty, listBoxItemTemplate));
            listBoxStyle.Setters.Add(new Setter(ListBox.ItemContainerStyleProperty, listBoxItemStyle));

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            var title = new TextBlock
            {
                Text = "Редактирование получателя:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))
            };
            stackPanel.Children.Add(title);

            var nameLabel = new TextBlock
            {
                Text = "Имя:",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))
            };
            var nameTextBox = new TextBox
            {
                Text = recipient.Name,
                Height = 30,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                Padding = new Thickness(5),
                Template = CreateRoundedTextBoxTemplate()
            };
            stackPanel.Children.Add(nameLabel);
            stackPanel.Children.Add(nameTextBox);

            var emailLabel = new TextBlock
            {
                Text = "Email:",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))
            };
            var emailTextBox = new TextBox
            {
                Text = recipient.Email,
                Height = 30,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74")),
                Padding = new Thickness(5),
                Template = CreateRoundedTextBoxTemplate()
            };
            stackPanel.Children.Add(emailLabel);
            stackPanel.Children.Add(emailTextBox);

            var categoryTitle = new TextBlock
            {
                Text = "Выбор категории:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))
            };
            stackPanel.Children.Add(categoryTitle);

            var categoryListBox = new ListBox
            {
                SelectionMode = SelectionMode.Multiple,
                ItemsSource = Categories,
                Height = 150,
                Margin = new Thickness(0, 0, 0, 15),
                Style = listBoxStyle,
                FontSize = 14
            };

            // Выделяем категории получателя
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
                Content = "Применить",
                Width = 125,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Style = CreateActionButtonStyle()
            };

            var cancelButton = new Button
            {
                Content = "Отменить",
                Width = 125,
                Height = 35,
                Style = CreateActionButtonStyle()
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

        // Методы для создания стилей (те же, что и в AddRecipient_Click)
        Style CreateActionButtonStyle()
        {
            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
            style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 16.0));
            style.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Arial Black")));
            style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172A74"))));
            style.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
            style.Setters.Add(new Setter(Control.MinHeightProperty, 30.0));
            style.Setters.Add(new Setter(Control.TemplateProperty, CreateButtonTemplate()));
            return style;
        }

        ControlTemplate CreateButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(contentPresenter);

            template.VisualTree = border;

            var mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E6F8")), "border"));
            template.Triggers.Add(mouseOverTrigger);

            var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C0D0F0")), "border"));
            template.Triggers.Add(pressedTrigger);

            return template;
        }

        ControlTemplate CreateRoundedTextBoxTemplate()
        {
            var template = new ControlTemplate(typeof(TextBox));
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "Border";
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));

            var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.Name = "PART_ContentHost";
            scrollViewer.SetValue(ScrollViewer.MarginProperty, new Thickness(0));
            border.AppendChild(scrollViewer);

            template.VisualTree = border;
            return template;
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