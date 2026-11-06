using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.Metrics;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace mur
{
    public partial class MenuWindow : Window
    {
        private int userId = 0;
        private string userLogin = "Гость";
        private string roleName = "Гость";

        public ObservableCollection<CartItem> cartItems =
            new ObservableCollection<CartItem>();

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

            // Блокировка корзины для гостя
            CartButton.IsEnabled = userId != 0;

            // Меняем внешний вид кнопки
            CartButton.Opacity = userId == 0 ? 0.5 : 1.0;
        }

        // Загрузка товаров
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
                            Products.Portion,
                            Products.SourceId
                        FROM Products
                        INNER JOIN Categories
                            ON Products.CategoryId = Categories.Id
                        WHERE Products.Available = 1
                          AND Products.Stock > 0
                        ORDER BY Products.Name";

                    using (SqlCommand command =
                        new SqlCommand(query, connection))
                    using (SqlDataAdapter adapter =
                        new SqlDataAdapter(command))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);

                        table.Columns.Add("PhotoUri", typeof(Uri));
                        string photoDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos");
                        foreach (DataRow row in table.Rows)
                        {
                            string fileName = row.IsNull("SourceId")
                                ? "product-" + row["Id"] + ".jpg"
                                : row["SourceId"] + ".jpg";
                            string photoPath = Path.Combine(photoDirectory, fileName);
                            if (File.Exists(photoPath))
                                row["PhotoUri"] = new Uri(photoPath, UriKind.Absolute);
                        }

                        ProductsDataGrid.ItemsSource = table.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки товаров:\n" + ex.Message);
            }
        }

        // Поиск
        private void SearchTextBox_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            if (ProductsDataGrid.ItemsSource is DataView)
            {
                DataView view =
                    (DataView)ProductsDataGrid.ItemsSource;

                string search = SearchTextBox.Text.Trim()
                    .Replace("'", "''");

                view.RowFilter = string.IsNullOrWhiteSpace(search)
                    ? ""
                    : "Convert(Name, 'System.String') LIKE '%"
                      + search + "%'";
            }
        }

        // Авторизация
        private void LoginButton_Click(
            object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Owner = this;

            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                userId = loginWindow.UserId;
                userLogin = loginWindow.UserLogin;
                roleName = loginWindow.RoleName;

                UpdateUserText();

                MessageBox.Show("Вы успешно вошли в систему!");
            }
        }

        // Кнопка добавления
        private void AddToCartButton_Click(
            object sender, RoutedEventArgs e)
        {
            AddSelectedProductToCart();
        }

        // Добавление выбранного товара
        private void AddSelectedProductToCart()
        {
            // Проверяем авторизацию
            if (userId == 0)
            {
                MessageBox.Show(
                    "Чтобы добавить товар в корзину, необходимо войти в систему!",
                    "Требуется авторизация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            DataRowView row =
                ProductsDataGrid.SelectedItem as DataRowView;

            if (row == null)
            {
                MessageBox.Show("Сначала выберите товар.");
                return;
            }

            try
            {
                int productId = Convert.ToInt32(row["Id"]);
                string name = Convert.ToString(row["Name"]);
                decimal price = Convert.ToDecimal(row["Price"]);
                int stock = Convert.ToInt32(row["Stock"]);

                CartItem existingItem = cartItems.FirstOrDefault(
                    item => item.ProductId == productId);

                if (existingItem != null)
                {
                    if (existingItem.Quantity >= stock)
                    {
                        MessageBox.Show("Больше этого товара нет в наличии.");
                        return;
                    }

                    existingItem.Quantity++;
                }
                else
                {
                    cartItems.Add(new CartItem
                    {
                        ProductId = productId,
                        Name = name,
                        Price = price,
                        Quantity = 1
                    });
                }

                MessageBox.Show(
                    "Товар «" + name + "» добавлен в корзину!",
                    "Кафе 34",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления товара:\n" + ex.Message);
            }
        }


        // Двойной щелчок по товару
        private void ProductsDataGrid_MouseDoubleClick(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            AddSelectedProductToCart();
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            if (userId == 0)
            {
                MessageBox.Show(
                    "Для доступа к корзине необходимо авторизоваться!",
                    "Требуется авторизация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            CartWindow window = new CartWindow(cartItems, userId);
            window.Owner = this;
            window.ShowDialog();
            LoadProducts();
            SearchTextBox_TextChanged(SearchTextBox, null);
        }

        // Открытие заказов
        private void OrdersButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (userId == 0)
            {
                MessageBox.Show(
                    "Для просмотра заказов войдите в систему.");
                return;
            }

            OrdersWindow window = new OrdersWindow(userId);
            window.Owner = this;
            window.ShowDialog();
        }
    }
}
