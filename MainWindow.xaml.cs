using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace alesya_rassylka
{
    public partial class MainWindow : Window
    {
        private List<Customer> customers;
        private const string JsonFilePath = "customers.json";
        private const string LogFilePath = "error.log";

        public MainWindow()
        {
            InitializeComponent();
            LoadCustomers();
        }

        private void LoadCustomers()
        {
            try
            {
                if (File.Exists(JsonFilePath))
                {
                    string json = File.ReadAllText(JsonFilePath);
                    customers = JsonSerializer.Deserialize<List<Customer>>(json) ?? new List<Customer>();
                    UpdateRecipientsListBox();
                    InitializeCategoryComboBox(); // Инициализация ComboBox после загрузки данных
                }
                else
                {
                    customers = new List<Customer>();
                    SaveCustomers();
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке JSON", ex);
                ShowDetailedError("Ошибка загрузки данных", ex);
            }
        }

        private void SaveCustomers()
        {
            try
            {
                string json = JsonSerializer.Serialize(customers, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(JsonFilePath, json);
                UpdateRecipientsListBox();
            }
            catch (Exception ex)
            {
                LogError("Ошибка при сохранении JSON", ex);
                ShowDetailedError("Ошибка сохранения данных", ex);
            }
        }

        private void InitializeCategoryComboBox()
        {
            // Получаем уникальные категории из списка customers
            var uniqueCategories = customers
                .Select(c => c.ProductCategory)
                .Distinct()
                .ToList();

            // Добавляем "Все" в начало списка
            uniqueCategories.Insert(0, "Все");

            // Устанавливаем источник данных для ComboBox
            CategoryComboBox.ItemsSource = uniqueCategories;
            CategoryComboBox.SelectedIndex = 0;
        }

        private void UpdateRecipientsListBox()
        {
            RecipientsListBox.ItemsSource = null;
            RecipientsListBox.ItemsSource = customers;
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedCategory = CategoryComboBox.SelectedItem as string;
            if (selectedCategory == "Все")
            {
                UpdateRecipientsListBox();
            }
            else
            {
                var filteredCustomers = customers.Where(c => c.ProductCategory == selectedCategory).ToList();
                RecipientsListBox.ItemsSource = null;
                RecipientsListBox.ItemsSource = filteredCustomers;
            }
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NewNameTextBox.Text.Trim();
            string email = NewEmailTextBox.Text.Trim();
            string productCategory = (NewProductCategoryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(productCategory))
            {
                MessageBox.Show("Введите имя, email и выберите категорию!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (customers.Any(c => c.Email == email))
            {
                MessageBox.Show("Этот email уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            customers.Add(new Customer { Name = name, Email = email, ProductCategory = productCategory });
            SaveCustomers();
            NewNameTextBox.Clear();
            NewEmailTextBox.Clear();
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecipientsListBox.SelectedItem is Customer selectedCustomer)
            {
                string newName = NewNameTextBox.Text.Trim();
                string newEmail = NewEmailTextBox.Text.Trim();
                string newProductCategory = (NewProductCategoryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (string.IsNullOrWhiteSpace(newName) || string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(newProductCategory))
                {
                    MessageBox.Show("Введите имя, email и выберите категорию!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                selectedCustomer.Name = newName;
                selectedCustomer.Email = newEmail;
                selectedCustomer.ProductCategory = newProductCategory;
                SaveCustomers();
            }
            else
            {
                MessageBox.Show("Выберите пользователя для редактирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecipientsListBox.SelectedItem is Customer selectedCustomer)
            {
                customers.Remove(selectedCustomer);
                SaveCustomers();
            }
            else
            {
                MessageBox.Show("Выберите пользователя для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCustomers = RecipientsListBox.SelectedItems.Cast<Customer>().ToList();

            if (!selectedCustomers.Any() || string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                MessageBox.Show("Выберите получателей и введите сообщение!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var customer in selectedCustomers)
            {
                try
                {
                    SendEmail(customer.Email, MessageTextBox.Text);
                }
                catch (Exception ex)
                {
                    LogError($"Ошибка отправки письма {customer.Email}", ex);
                }
            }

            MessageBox.Show("Сообщения успешно отправлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SendEmail(string recipientEmail, string message)
        {
            string senderEmail = "kikbika@gmail.com";
            string appPassword = "zbobujojpchynxwd";

            try
            {
                using (MailMessage mail = new MailMessage())
                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    mail.From = new MailAddress(senderEmail);
                    mail.To.Add(recipientEmail);
                    mail.Subject = "Сообщение от компании";
                    mail.Body = message;
                    mail.IsBodyHtml = false;

                    smtp.Credentials = new NetworkCredential(senderEmail, appPassword);
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при отправке email", ex);
                throw;
            }
        }

        private void LogError(string context, Exception ex)
        {
            string logMessage = $"{DateTime.Now}: [{context}] {ex.Message}\n";
            File.AppendAllText(LogFilePath, logMessage);
        }

        private void ShowDetailedError(string title, Exception ex)
        {
            MessageBox.Show($"{ex.Message}\n{ex.StackTrace}", title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public class Customer
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string ProductCategory { get; set; } // Группа техники
    }
}