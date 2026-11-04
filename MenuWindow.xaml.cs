using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace mur
{
    public partial class MenuWindow : Window
    {
        private int userId = 0;
        private string userLogin = "Гость";
        private string roleName = "Гость";

        public MenuWindow()
        {
            InitializeComponent();
            UpdateUserText();
            LoadProducts();
        }

        private void UpdateUserText()
        {
            UserTextBlock.Text = userId == 0
                ? "Гость"
                : userLogin + " (" + roleName + ")";
        }

        private void LoadProducts()
        {
            try
            {
                using (SqlConnection connection = Database.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            Products.Id,
                            Products.Name,
                            Categories.Name AS CategoryName,
                            Products.Price,
                            Products.Stock,
                            Products.Portion
                        FROM Products
                        INNER JOIN Categories
                            ON Products.CategoryId = Categories.Id
                        WHERE Products.Available = 1
                          AND Products.Stock > 0
                        ORDER BY Products.Name";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);

                        ProductsDataGrid.ItemsSource = table.DefaultView;
                        StatusTextBlock.Text = "Товаров в меню: " + table.Rows.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить товары:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (ProductsDataGrid.ItemsSource is DataView view)
            {
                string search = SearchTextBox.Text.Trim()
                    .Replace("'", "''");

                view.RowFilter = string.IsNullOrWhiteSpace(search)
                    ? ""
                    : $"Convert(Name, 'System.String') LIKE '%{search}%'";
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();

            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                userId = loginWindow.UserId;
                userLogin = loginWindow.UserLogin;
                roleName = loginWindow.RoleName;

                UpdateUserText();

                MessageBox.Show(
                    "Вы вошли в систему!",
                    "Кафе 34",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            if (userId == 0)
            {
                MessageBox.Show(
                    "Для оформления заказа сначала войдите в систему.",
                    "Авторизация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            MessageBox.Show(
                "Корзину подключим следующим шагом.",
                "Кафе 34",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}