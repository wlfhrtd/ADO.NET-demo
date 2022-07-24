using System;
using System.Data;
using System.Text;


namespace ADO.Components
{
    public static class DataTableHelper
    {
        public static void ProcessDataTable(DataTable dataTable)
        {
            StringBuilder stringBuilder = new(1024);

            string header = "#";
            foreach (DataColumn col in dataTable.Columns)
            {
                header += $"\t{col.ColumnName}";
            }
            stringBuilder.AppendLine(header);

            int index = 1;
            foreach (DataRow row in dataTable.Rows)
            {
                stringBuilder.Append(index);
                foreach (DataColumn column in dataTable.Columns)
                {
                    stringBuilder.Append("\t" + row[column.ColumnName].ToString());
                }

                stringBuilder.AppendLine();
                index++;
            }

            Console.WriteLine(stringBuilder.ToString());
        }

        public static DataTable BuildDataTableSample()
        {
            DataTable dataTable = new();
            // inline syntax
            dataTable.Columns.Add("id", typeof(int));
            // OOP syntax
            DataColumn dataColumn = new()
            {
                DataType = typeof(string),
                ColumnName = "FirstName",
                Caption = "First Name",
                ReadOnly = false,
            };
            dataTable.Columns.Add(dataColumn); 

            dataTable.Columns.Add(new DataColumn
            {
                DataType = typeof(string),
                ColumnName = "LastName",
                Caption = "Last Name",
                ReadOnly = false,
            });
            // inline syntax
            dataTable.Rows.Add(1, "Andy", "Anderson");
            // key-array-access syntax
            DataRow dataRow = dataTable.NewRow();
            dataRow["id"] = 2;
            dataRow["FirstName"] = "Robert";
            dataRow["LastName"] = "O'Connel";
            dataTable.Rows.Add(dataRow);

            dataTable.AcceptChanges();

            return dataTable;
        }
    }
}
