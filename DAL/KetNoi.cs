using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DAL
{
    public interface IDbConnectionFactory
    {
        SqlConnection CreateConnection();
    }

    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string? connectionString = null)
        {
            _connectionString = string.IsNullOrWhiteSpace(connectionString)
                ? KetNoi.ConnectionString
                : connectionString;
            KetNoi.ConnectionString = _connectionString;
        }

        public SqlConnection CreateConnection() => new SqlConnection(_connectionString);
    }

    public class KetNoi
    {
        public static string ConnectionString { get; set; } =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=.\\SQLEXPRESS;Database=QuanLyHocTrucTuyen;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}
