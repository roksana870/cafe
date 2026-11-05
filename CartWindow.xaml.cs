using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace mur
{
    public partial class CartWindow : Window
    {
        private ObservableCollection<CartItem> cart;
        private int userId;

        public CartWindow(ObservableCollection<CartItem> userCart, int currentUserId)
        {
            InitializeComponent();

            cart = userCart;
            userId = currentUserId;

            CartDataGrid.ItemsSource = cart;
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            decimal total = cart.Sum(x => x.Price * x.Quantity);
            TotalTextBlock.Text = total.ToString("F2");
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            CartItem selectedItem = CartDataGrid.SelectedItem as CartItem;

            if (selectedItem == null)
            {
                MessageBox.Show("Выберите товар для удаления.");
                return;
            }

            cart.Remove(selectedItem);
            UpdateTotal();
        }

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (userId == 0)
            {
                MessageBox.Show("Для оформления заказа необходимо войти в систему.");
                return;
            }

            if (cart.Count == 0)
            {
                MessageBox.Show("Корзина пуста.");
                return;
            }

            decimal total = cart.Sum(x => x.Price * x.Quantity);

            using (SqlConnection connection = Database.GetConnection())
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int orderId;

                        string orderQuery = @"
INSERT INTO dbo.Orders (UserId, OrderDate, StatusId)
OUTPUT INSERTED.Id
VALUES (@UserId, GETDATE(), 1);";

                        using (SqlCommand command = new SqlCommand(orderQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserId", userId);
                            orderId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        foreach (CartItem item in cart)
                        {
                            string stockQuery = @"
UPDATE dbo.Products
SET Stock = Stock - @Quantity
WHERE Id = @ProductId
AND Stock >= @Quantity
AND Available = 1;";

                            using (SqlCommand command = new SqlCommand(stockQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                command.Parameters.AddWithValue("@ProductId", item.ProductId);

                                int changedRows = command.ExecuteNonQuery();

                                if (changedRows == 0)
                                {
                                    throw new Exception("Недостаточно товара на складе: " + item.Name);
                                }
                            }

                            string itemQuery = @"
INSERT INTO dbo.OrderItems
(OrderId, ProductId, Quantity, Price)
VALUES
(@OrderId, @ProductId, @Quantity, @Price);";

                            using (SqlCommand command = new SqlCommand(itemQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@OrderId", orderId);
                                command.Parameters.AddWithValue("@ProductId", item.ProductId);
                                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                command.Parameters.AddWithValue("@Price", item.Price);

                                command.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();

                        MessageBox.Show(
                            "Заказ №" + orderId + " успешно оформлен!\nСумма: " + total.ToString("F2"),
                            "Заказ оформлен");

                        cart.Clear();
                        UpdateTotal();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                        }

                        MessageBox.Show(
                            "Не удалось оформить заказ:\n" + ex.Message,
                            "Ошибка");
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}