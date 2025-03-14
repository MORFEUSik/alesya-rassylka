using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace alesya_rassylka
{
    public partial class SelectRecipientWindow : Window
    {
        public List<string> SelectedRecipients { get; private set; }

        public SelectRecipientWindow(List<Customer> customers)
        {
            InitializeComponent();

            // Преобразуем в список с возможностью выбора
            RecipientListBox.ItemsSource = customers.Select(c => new SelectableCustomer
            {
                DisplayName = $"{c.Name} - {c.Email}",
                IsSelected = false
            }).ToList();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Сохраняем выбранных получателей
            SelectedRecipients = RecipientListBox.Items
                .Cast<SelectableCustomer>()
                .Where(c => c.IsSelected)
                .Select(c => c.DisplayName)
                .ToList();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class SelectableCustomer
    {
        public string DisplayName { get; set; }
        public bool IsSelected { get; set; }
    }


}
