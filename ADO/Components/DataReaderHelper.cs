using Npgsql;
using System;


namespace ADO.Components
{
    public static class DataReaderHelper
    {
        public static T GetColumnValue<T>(this NpgsqlDataReader dr, string columnName)
            => dr[columnName].Equals(DBNull.Value) ? default : (T)dr[columnName];
    }
}
