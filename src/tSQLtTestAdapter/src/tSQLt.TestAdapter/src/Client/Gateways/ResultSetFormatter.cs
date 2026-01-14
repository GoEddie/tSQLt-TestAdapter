using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace tSQLt.TestAdapter.Client.Gateways
{
    public class ResultSetTable
    {
        public List<string> ColumnNames { get; set; }
        public List<List<string>> Rows { get; set; }

        public ResultSetTable()
        {
            ColumnNames = new List<string>();
            Rows = new List<List<string>>();
        }

        /// <summary>
        /// Formats the result set as a nicely aligned table with headers
        /// </summary>
        public string FormatAsTable()
        {
            if (ColumnNames.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            // Calculate column widths
            var columnWidths = new int[ColumnNames.Count];
            for (int i = 0; i < ColumnNames.Count; i++)
            {
                columnWidths[i] = ColumnNames[i].Length;
            }

            foreach (var row in Rows)
            {
                for (int i = 0; i < row.Count && i < columnWidths.Length; i++)
                {
                    columnWidths[i] = Math.Max(columnWidths[i], row[i].Length);
                }
            }

            // Build header
            sb.Append("| ");
            for (int i = 0; i < ColumnNames.Count; i++)
            {
                sb.Append(ColumnNames[i].PadRight(columnWidths[i]));
                sb.Append(" | ");
            }
            sb.AppendLine();

            // Build separator
            sb.Append("|-");
            for (int i = 0; i < ColumnNames.Count; i++)
            {
                sb.Append(new string('-', columnWidths[i]));
                sb.Append("-|-");
            }
            sb.AppendLine();

            // Build rows
            foreach (var row in Rows)
            {
                sb.Append("| ");
                for (int i = 0; i < ColumnNames.Count; i++)
                {
                    var value = i < row.Count ? row[i] : string.Empty;
                    sb.Append(value.PadRight(columnWidths[i]));
                    sb.Append(" | ");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    public static class ResultSetFormatter
    {
        /// <summary>
        /// Reads a SqlDataReader and converts it to a ResultSetTable
        /// </summary>
        public static ResultSetTable ReadResultSet(SqlDataReader reader)
        {
            var table = new ResultSetTable();

            // Read column names
            for (int i = 0; i < reader.FieldCount; i++)
            {
                table.ColumnNames.Add(reader.GetName(i));
            }

            // Read rows
            while (reader.Read())
            {
                var row = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                    row.Add(value);
                }
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
