using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace mur
{
    public static class Database
    {
        private static readonly string connectionString =
            @"Server=localhost;Database=cafe;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        public static bool TestConnection()
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static DataTable GetData(string query)
        {
            DataTable table = new DataTable();

            using (SqlConnection connection = GetConnection())
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                adapter.Fill(table);
            }

            return table;
        }
    }
}