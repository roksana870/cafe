using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace mur
{
    public partial class CartWindow : Window
    {
        private ObservableCollection<CartItem> cart;
        private int currentUserId;

        public CartWindow()
        {
            InitializeComponent();

            cart = new ObservableCollection<CartItem>();
            currentUserId = 0;

            CartDataGrid.ItemsSource = cart;
            UpdateTotal();
        }

        public CartWindow(
            ObservableCollection<CartItem> userCart,
            int userId)
        {
            InitializeComponent();

            cart = userCart ?? new ObservableCollection<CartItem>();
            currentUserId = userId;

            CartDataGrid.ItemsSource = cart;
            UpdateTotal();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            CartItem selectedItem = CartDataGrid.SelectedItem as CartItem;

            if (selectedItem == null)
            {
                MessageBox.Show(
                    "Сначала выберите товар.",
                    "Корзина",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            cart.Remove(selectedItem);
            UpdateTotal();
        }

        private void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentUserId == 0)
            {
                MessageBox.Show(
                    "Для оформления заказа необходимо войти в систему.",
                    "Авторизация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (cart.Count == 0)
            {
                MessageBox.Show(
                    "Корзина пуста.",
                    "Оформление заказа",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            MessageBox.Show(
                "Товары выбраны. Следующим шагом подключим сохранение заказа в базе данных.",
                "Оформление заказа",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void UpdateTotal()
        {
            decimal total = cart.Sum(item => item.Total);
            TotalTextBlock.Text = $"Итого: {total:N2} ₽";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class CartItem
    {
        public int ProductId { get; set; }

        public string Name { get; set; } = "";

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public decimal Total
        {
            get { return Price * Quantity; }
        }
    }
}