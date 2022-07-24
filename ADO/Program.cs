using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Npgsql;
using ADO.Components;
using ADO.Model;


namespace ADO
{
    class Program
    {
        static void Main(string[] args)
        {
            // smart exception handling
            TransactionalWithReachableVars();
            // work with DataReader
            QueryWithDataReader();
            QueryDataReaderIntoListWordyWithoutNullCheck();
            QueryDataReaderIntoListStillWordyWithoutNullCheck();
            QueryDataReaderIntoListWithExtensionMethodWithCheckNull();
            QueryDataReaderForMultipleDatasets();
            // work with DataAdapter and DataTable
            QueryDataAdapterIntoDataTable();
            // work with DataAdapter and DataSet
            Dictionary<string, List<string>> keyValuePairs = QueryDataAdapterIntoDataSets();
            foreach (KeyValuePair<string, List<string>> pair in keyValuePairs)
            {
                foreach (string s in pair.Value)
                {
                    Console.WriteLine(s);
                }
            }
            Console.WriteLine("***** EOF *****");
            // manual work with DataTable
            BuildingDataTablesDemo();
            BuildDataTableFromExisting();
        }

        static void TransactionalWithReachableVars()
        {
            string connectionString = ConnectionHelper.ConnectionString;
            NpgsqlConnection connection = null;
            NpgsqlTransaction transaction = null;
            NpgsqlCommand cmd = null;

            try
            {
                connection = new(connectionString);
                Console.WriteLine($"{connection.GetType().Name}");
                connection.Open();
                transaction = connection.BeginTransaction();

                string sqlScalar = "SELECT COUNT(*) FROM customers";
                cmd = new(sqlScalar, connection);

                long count = (long)cmd.ExecuteScalar();
                Console.WriteLine("*** Results ***");
                Console.WriteLine($"SELECT COUNT: {count}");

                cmd.CommandText = $"INSERT INTO customers(name, location, email)" +
                    $" VALUES ('customer#{count + 1}', 'location#{count + 1}', 'email{count + 1}')";
                Console.WriteLine(cmd.CommandText);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "SELECT COUNT(*) FROM customers WHERE name LIKE @name";
                cmd.Parameters.Add(new NpgsqlParameter("@name", "customer%"));
                long count2 = (long)cmd.ExecuteScalar();
                Console.WriteLine($"SELECT COUNT LIKE: {count2}");

                cmd.CommandText = "INSERT INTO customers(name, location, email)" +
                    " VALUES (@name, @location, @email)";
                cmd.Parameters.Remove("@name");
                cmd.Parameters.Add(new NpgsqlParameter("@name", "customer###" + (count + 2).ToString()));
                cmd.Parameters.Add(new NpgsqlParameter("@location", "location###" + (count + 2).ToString()));
                cmd.Parameters.Add(new NpgsqlParameter("@email", "email###" + (count + 2).ToString()));
                cmd.ExecuteNonQuery();

                transaction.Commit();

                Console.WriteLine("***** SUMMARY *****");
                string summary = ConnectionHelper.GetConnectionInformation(connection);
                Console.WriteLine(summary);

                Console.WriteLine("***** EOF *****");
            }
            // any block for log/dump with all objects being reachable
            catch (PostgresException e)
            {
                transaction.Rollback();

                StringBuilder sb = new(1024);
                sb.AppendLine(e.Message);
                for (int i = 0; i < e.Data.Count; i++)
                {
                    sb.AppendLine($"{i}\t{e.Data[i]}");
                }

                Console.WriteLine(sb.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
                if (transaction != null)
                {
                    transaction.Dispose();
                }
                if (cmd != null)
                {
                    cmd.Dispose();
                }
            }
        }

        static void QueryWithDataReader()
        {
            string connectionString = ConnectionHelper.ConnectionString;
            StringBuilder sb = new(1024);

            using (NpgsqlConnection connection = new(connectionString))
            {
                string sql = "SELECT * FROM customers";
                using NpgsqlCommand cmd = new(sql, connection);
                connection.Open();
                using (NpgsqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (dr.Read())
                    {
                        sb.AppendLine($"{dr["id"]}\t{dr["name"]}\t{dr["location"]}\t{dr["email"]}");
                    }
                }
            }

            Console.WriteLine(sb.ToString());
            Console.WriteLine("***** EOF *****");
        }

        static void QueryDataReaderIntoListWordyWithoutNullCheck()
        {
            string connectionString = ConnectionHelper.ConnectionString;
            List<Customer> list = new();

            using (NpgsqlConnection connection = new(connectionString))
            {
                string sql = "SELECT * FROM customers";
                using NpgsqlCommand cmd = new(sql, connection);
                connection.Open();
                using (NpgsqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (dr.Read())
                    {
                        list.Add(
                            new Customer
                            {
                                Id = dr.GetInt32(dr.GetOrdinal("id")),
                                Name = dr.GetString(dr.GetOrdinal("name")),
                                Location = dr.GetString(dr.GetOrdinal("location")),
                                Email = dr.GetString(dr.GetOrdinal("email"))
                            });
                    }
                }
            }

            foreach (Customer customer in list.Reverse<Customer>())
            {
                Console.WriteLine(customer.ToString());
            }
            Console.WriteLine("***** EOF *****");
        }

        static void QueryDataReaderIntoListStillWordyWithoutNullCheck()
        {
            string connectionString = ConnectionHelper.ConnectionString;
            List<Customer> list = new();

            using (NpgsqlConnection connection = new(connectionString))
            {
                string sql = "SELECT * FROM customers";
                using NpgsqlCommand cmd = new(sql, connection);
                connection.Open();
                using (NpgsqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (dr.Read())
                    {
                        list.Add(
                            new Customer
                            {
                                Id = dr.GetFieldValue<int>(dr.GetOrdinal("id")),
                                Name = dr.GetFieldValue<string>(dr.GetOrdinal("name")),
                                Location = dr.GetFieldValue<string>(dr.GetOrdinal("location")),
                                Email = dr.GetFieldValue<string>(dr.GetOrdinal("email"))
                            });
                    }
                }
            }

            Random r = new();
            var shuffled = list.OrderBy(x => r.Next());

            foreach (Customer customer in shuffled.Reverse())
            {
                Console.WriteLine(customer.ToString());
            }
            Console.WriteLine("***** EOF *****");
        }

        static void QueryDataReaderIntoListWithExtensionMethodWithCheckNull()
        {
            string connectionString = ConnectionHelper.ConnectionString;
            List<Customer> list = new();

            using (NpgsqlConnection connection = new(connectionString))
            {
                string sql = "SELECT * FROM customers";
                using NpgsqlCommand cmd = new(sql, connection);
                connection.Open();
                using (NpgsqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (dr.Read())
                    {
                        list.Add(
                            new Customer
                            {
                                Id = dr.GetColumnValue<int>("id"),
                                Name = dr.GetColumnValue<string>("name"),
                                Location = dr.GetColumnValue<string>("location"),
                                Email = dr.GetColumnValue<string>("email")
                            });
                    }
                }
            }

            Random r = new();
            var shuffled = list.OrderBy(x => r.Next());

            foreach (Customer customer in shuffled.Reverse())
            {
                Console.WriteLine(customer.ToString());
            }
            Console.WriteLine("***** EOF *****");
        }

        static void QueryDataReaderForMultipleDatasets()
        {
            string connectionString = ConnectionHelper.ConnectionString;
            using (NpgsqlConnection connection = new(connectionString))
            {
                string sql = "SELECT id, name FROM customers";
                sql += "; SELECT location, email FROM customers";
                connection.Open();
                using NpgsqlCommand cmd = new(sql, connection);
                using (NpgsqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (dr.Read())
                    {
                        Console.WriteLine($"{dr["id"]}\t{dr["name"]}");
                    }

                    dr.NextResult();

                    while (dr.Read())
                    {
                        Console.WriteLine($"{dr["location"]}\t{dr["email"]}");
                    }
                }
            }
            Console.WriteLine("***** EOF *****");
        }

        static void QueryDataAdapterIntoDataTable()
        {
            string connectionString = ConnectionHelper.ConnectionString;
            NpgsqlConnection connection = null;
            NpgsqlCommand command = null;
            NpgsqlDataAdapter dataAdapter = null;
            DataTable dataTable = null;
            string sql = "SELECT * FROM customers";

            using (connection = new(connectionString))
            {
                using (command = new(sql, connection))
                {
                    using (dataAdapter = new(command))
                    {
                        dataTable = new();
                        dataAdapter.Fill(dataTable);
                    }
                }
            }

            DataTableHelper.ProcessDataTable(dataTable);
            Console.WriteLine("***** EOF *****");
        }

        static Dictionary<string, List<string>> QueryDataAdapterIntoDataSets()
        {
            string connectionString = ConnectionHelper.ConnectionString;
            NpgsqlConnection connection = null;
            NpgsqlCommand command = null;
            NpgsqlDataAdapter dataAdapter = null;
            DataSet dataSet = null;

            string sql = "SELECT id FROM customers";
            sql += ";" + "SELECT name FROM customers";
            sql += ";" + "SELECT location FROM customers";
            sql += ";" + "SELECT email FROM customers";

            using (connection = new(connectionString))
            {
                using (command = new(sql, connection))
                {
                    using (dataAdapter = new(command))
                    {
                        dataSet = new();
                        dataAdapter.Fill(dataSet);
                    }
                }
            }

            List<string> ids = (from row in dataSet.Tables[0].AsEnumerable()
                                select (row.Field<int>("id")).ToString()).ToList();
            List<string> names = (from row in dataSet.Tables[1].AsEnumerable()
                                  select row.Field<string>("name")).ToList();
            List<string> locations = (from row in dataSet.Tables[2].AsEnumerable()
                                      select row.Field<string>("location")).ToList();
            List<string> emails = (from row in dataSet.Tables[3].AsEnumerable()
                                   select row.Field<string>("email")).ToList();

            Dictionary<string, List<string>> result = new();
            result["id"] = ids;
            result["name"] = names;
            result["location"] = locations;
            result["email"] = emails;

            return result;
        }

        static void BuildingDataTablesDemo()
        {
            DataTable exampleDataTable = DataTableHelper.BuildDataTableSample();
            Console.WriteLine($"Columns: {exampleDataTable.Columns.Count}; Rows: {exampleDataTable.Rows.Count}");

            DataTable emptyClone = exampleDataTable.Clone(); // schema only
            Console.WriteLine($"Columns: {emptyClone.Columns.Count}; Rows: {emptyClone.Rows.Count}");

            DataTable deepCopy = exampleDataTable.Copy(); // schema+data
            Console.WriteLine($"Columns: {deepCopy.Columns.Count}; Rows: {deepCopy.Rows.Count}");
            Console.WriteLine("***** EOF *****");
        }

        static void BuildDataTableFromExisting()
        {
            string connectionString = ConnectionHelper.ConnectionString;
            NpgsqlConnection connection = null;
            NpgsqlCommand command = null;
            NpgsqlDataAdapter dataAdapter = null;
            DataTable dataTable = null;
            string sql = "SELECT * FROM customers";

            using (connection = new(connectionString))
            {
                using (command = new(sql, connection))
                {
                    using (dataAdapter = new(command))
                    {
                        dataTable = new();
                        dataAdapter.Fill(dataTable);
                    }
                }
            }

            /**
            DataTable newDataTable = dataTable.Clone();
            DataRow[] rows = dataTable.Select("id > 10 AND id < 31");
            /**
            foreach (DataRow row in rows)
            {
                // newDataTable.Rows.Add(row.ItemArray); // same as ImportRow 
                newDataTable.ImportRow(row);
            }
            // wrapper method for loop
            newDataTable = rows.CopyToDataTable(); // same as loop above
            newDataTable.AcceptChanges() // not needed actually since CopyToDataTable() calls it anyway
            */

            // shortcut for commented code
            DataTable newDataTable = dataTable.Select("id > 10 AND id < 20").CopyToDataTable();

            DataTableHelper.ProcessDataTable(newDataTable);
            Console.WriteLine("***** EOF *****");
        }
    }
}
