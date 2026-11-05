using Microsoft.Data.SqlClient;
using System;
using System.Windows;

namespace mur
{
    public partial class MainWindow : Window
    {
        public int UserId { get; private set; } = 0;
        public string UserLogin { get; private set; } = "";
        public string RoleName { get; private set; } = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            ErrorTextBlock.Text = "";

            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password))
            {
                ErrorTextBlock.Text = "Введите логин и пароль.";
                return;
            }

            try
            {
                using (SqlConnection connection = Database.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            Users.Id,
                            Users.Login,
                            Roles.Name AS RoleName
                        FROM Users
                        INNER JOIN Roles ON Users.RoleId = Roles.Id
                        WHERE Users.Login = @Login
                          AND Users.PasswordHash = @Password";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        command.Parameters.AddWithValue("@Password", password);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                UserId = Convert.ToInt32(reader["Id"]);
                                UserLogin = reader["Login"].ToString();
                                RoleName = reader["RoleName"].ToString();

                                DialogResult = true;
                                Close();
                            }
                            else
                            {
                                ErrorTextBlock.Text = "Неверный логин или пароль.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка подключения к базе данных:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}