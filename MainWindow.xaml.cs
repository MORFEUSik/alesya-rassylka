using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Windows;


namespace alesya_rassylka
{
    public partial class MainWindow : Window
    {
        private List<Customer> customers;
        private const string JsonFilePath = "customers.json";
        private const string LogFilePath = "error.log"; // Файл для логов

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
                    customers = JsonSerializer.Deserialize<List<Customer>>(json);
                    RecipientsComboBox.ItemsSource = customers;
                    RecipientsComboBox.DisplayMemberPath = "Name"; // Отображаем только имена
                }
                else
                {
                    throw new FileNotFoundException("Файл customers.json не найден!");
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при загрузке JSON", ex);
                ShowDetailedError("Ошибка загрузки данных", ex);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecipientsComboBox.SelectedItem is Customer selectedCustomer && !string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                try
                {
                    SendEmail(selectedCustomer.Email, MessageTextBox.Text);
                    MessageBox.Show("Сообщение успешно отправлено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LogError("Ошибка при отправке письма", ex);
                    ShowDetailedError("Ошибка отправки письма", ex);
                }
            }
            else
            {
                MessageBox.Show("Выберите получателя и введите сообщение!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Timeout = 30000; // Увеличенный таймаут
                    smtp.Host = "smtp.gmail.com"; // Альтернативный SMTP Google


                    smtp.Send(mail);
                }
            }
            catch (SmtpException smtpEx)
            {
                LogError("SMTP ошибка", smtpEx);
                if (smtpEx.InnerException != null)
                {
                    LogError("Вложенная ошибка SMTP", smtpEx.InnerException);
                }
                throw new Exception($"SMTP ошибка ({smtpEx.StatusCode}): {smtpEx.Message}", smtpEx);
            }
            catch (Exception ex)
            {
                LogError("Ошибка при отправке email", ex);
                if (ex.InnerException != null)
                {
                    LogError("Вложенная ошибка", ex.InnerException);
                }
                throw new Exception("Ошибка при отправке email", ex);
            }
        }



        private void LogError(string context, Exception ex)
        {
            try
            {
                string logMessage = $"{DateTime.Now}: [{context}] {ex.GetType()} - {ex.Message}\nStackTrace: {ex.StackTrace}\n";
                File.AppendAllText(LogFilePath, logMessage);
            }
            catch
            {
                MessageBox.Show("Ошибка записи в лог-файл!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowDetailedError(string title, Exception ex)
        {
            MessageBox.Show($"Тип ошибки: {ex.GetType()}\nСообщение: {ex.Message}\nСтек вызовов:\n{ex.StackTrace}",
                title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public class Customer
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
