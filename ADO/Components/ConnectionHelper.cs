using Npgsql;
using System.Text;


namespace ADO.Components
{
    public static class ConnectionHelper
    {
        public static string ConnectionString;


        static ConnectionHelper()
        {
            ConnectionString = (new NpgsqlConnectionStringBuilder()
            {
                Host = "localhost",
                Username = "postgres",
                Password = "P@ssw0rd",
                Database = "SimpleADONET",
                Timeout = 30,
            }).ConnectionString;
        }


        public static string GetConnectionInformation(NpgsqlConnection connection)
        {
            StringBuilder sb = new StringBuilder(1024);
            sb.AppendLine("Connection String: " + connection.ConnectionString);
            sb.AppendLine("State: " + connection.State.ToString());
            sb.AppendLine("Connection Timeout: " + connection.ConnectionTimeout.ToString());
            sb.AppendLine("Database: " + connection.Database);
            sb.AppendLine("Data Source: " + connection.DataSource);
            sb.AppendLine("Server Version: " + connection.ServerVersion);

            return sb.ToString();
        }
    }
}
