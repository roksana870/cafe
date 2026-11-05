using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace mur
{
    public partial class OrdersWindow : Window
    {
        public OrdersWindow(int userId)
        {
            InitializeComponent();
            LoadOrders();
        }

        // Загрузка заказов
        private void LoadOrders()
        {
            try
            {
                using (SqlConnection connection = Database.GetConnection())
                {
                    connection.Open();

                    string query = @"
SELECT
    o.Id AS OrderId,
    u.Login AS ClientName,
    o.OrderDate,
    o.StatusId,
    CASE
        WHEN o.StatusId = 1 THEN N'Новый'
        WHEN o.StatusId = 2 THEN N'Готовится'
        WHEN o.StatusId = 3 THEN N'Готов'
        WHEN o.StatusId = 4 THEN N'Выдан'
        ELSE N'Неизвестно'
    END AS Status,
    ISNULL((
        SELECT SUM(oi.Quantity * oi.Price)
        FROM dbo.OrderItems oi
        WHERE oi.OrderId = o.Id
    ), 0) AS TotalAmount
FROM dbo.Orders o
INNER JOIN dbo.Users u ON u.Id = o.UserId
ORDER BY o.OrderDate DESC;";

                    SqlDataAdapter adapter =
                        new SqlDataAdapter(query, connection);

                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    OrdersDataGrid.ItemsSource = table.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заказов:\n" + ex.Message);
            }
        }

        // Получение выбранного заказа
        private DataRowView GetSelectedOrder()
        {
            DataRowView row = OrdersDataGrid.SelectedItem as DataRowView;

            if (row == null)
            {
                MessageBox.Show("Сначала выберите заказ.");
                return null;
            }

            return row;
        }

        // Изменение статуса
        private void ChangeStatus(int statusId)
        {
            DataRowView row = GetSelectedOrder();

            if (row == null)
                return;

            int orderId = Convert.ToInt32(row["OrderId"]);

            try
            {
                using (SqlConnection connection = Database.GetConnection())
                {
                    connection.Open();

                    string query = @"
UPDATE dbo.Orders
SET StatusId = @StatusId
WHERE Id = @OrderId;";

                    using (SqlCommand command =
                        new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@StatusId", SqlDbType.Int).Value = statusId;
                        command.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;

                        command.ExecuteNonQuery();
                    }
                }

                LoadOrders();

                MessageBox.Show("Статус заказа изменён.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка изменения статуса:\n" + ex.Message);
            }
        }

        // Готовится
        private void PreparingButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeStatus(2);
        }

        // Готов
        private void ReadyButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeStatus(3);
        }

        // Выдан
        private void IssuedButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeStatus(4);
        }

        // Удаление выданного заказа
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView row = GetSelectedOrder();

            if (row == null)
                return;

            int orderId = Convert.ToInt32(row["OrderId"]);
            int statusId = Convert.ToInt32(row["StatusId"]);

            if (statusId != 4)
            {
                MessageBox.Show("Можно удалять только выданные заказы.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить заказ №" + orderId + "?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            using (SqlConnection connection = Database.GetConnection())
            {
                connection.Open();

                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    string deleteItems = @"
DELETE FROM dbo.OrderItems
WHERE OrderId = @OrderId;";

                    using (SqlCommand command =
                        new SqlCommand(deleteItems, connection, transaction))
                    {
                        command.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;
                        command.ExecuteNonQuery();
                    }

                    string deleteOrder = @"
DELETE FROM dbo.Orders
WHERE Id = @OrderId;";

                    using (SqlCommand command =
                        new SqlCommand(deleteOrder, connection, transaction))
                    {
                        command.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    OrderItemsDataGrid.ItemsSource = null;

                    LoadOrders();

                    MessageBox.Show("Заказ успешно удалён.");
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

                    MessageBox.Show("Ошибка удаления заказа:\n" + ex.Message);
                }
            }
        }

        // Выбор заказа и просмотр его товаров
        private void OrdersDataGrid_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            DataRowView row = OrdersDataGrid.SelectedItem as DataRowView;

            if (row == null)
            {
                OrderItemsDataGrid.ItemsSource = null;
                return;
            }

            int orderId = Convert.ToInt32(row["OrderId"]);

            try
            {
                using (SqlConnection connection = Database.GetConnection())
                {
                    connection.Open();

                    string query = @"
SELECT
    p.Name AS [Товар],
    oi.Quantity AS [Количество],
    oi.Price AS [Цена],
    oi.Quantity * oi.Price AS [Итого]
FROM dbo.OrderItems oi
INNER JOIN dbo.Products p ON p.Id = oi.ProductId
WHERE oi.OrderId = @OrderId;";

                    using (SqlCommand command =
                        new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;

                        DataTable table = new DataTable();

                        using (SqlDataAdapter adapter =
                            new SqlDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }

                        OrderItemsDataGrid.ItemsSource = table.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки товаров:\n" + ex.Message);
            }
        }

        // Обновление
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            OrderItemsDataGrid.ItemsSource = null;
            LoadOrders();
        }

        // Закрытие
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
