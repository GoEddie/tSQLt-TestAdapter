using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace tSQLt.TestAdapter.Client.Gateways
{
    public interface ISqlServerGateway
    {
        string RunWithXmlResult(string query);
        TestExecutionResult RunWithXmlResultAndCapture(string query);
        void RunWithNoResult(string query);
        DataReaderResult RunWithDataReader(string query);
    }

    public struct DataReaderResult
    {
        public SqlConnection Connection;
        public SqlCommand Command;
        public SqlDataReader Reader;
    }

    public class TestExecutionResult
    {
        public string XmlResults { get; set; }
        public List<ResultSetTable> EarlierResultSets { get; set; }

        public TestExecutionResult()
        {
            EarlierResultSets = new List<ResultSetTable>();
        }
    }

    public class SqlServerGateway : ISqlServerGateway
    {
        private readonly string _connectionString;
        private readonly int _runTimeout;

        public SqlServerGateway(string connectionString, int runTimeout)
        {
            _connectionString = connectionString;
            _runTimeout = runTimeout;
        }
        public DataReaderResult RunWithDataReader(string query)
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = System.Data.CommandType.Text;

            var result = new DataReaderResult();
            result.Connection = connection;
            result.Command = command;
            result.Reader = command.ExecuteReader();
            return result;
        }


        public void RunWithNoResult(string query)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = query;
                    cmd.CommandTimeout = _runTimeout;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public string RunWithXmlResult(string query)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = query;
                    cmd.CommandTimeout = _runTimeout;

                    var reader = cmd.ExecuteReader();

                    var builder = new StringBuilder();

                    do
                    {
                        builder = new StringBuilder();  // if the test has a select in it it breaks this, we only ever want the last result set

                        while (reader.Read())
                        {
                            var part = reader[0] as string;
                            if (!String.IsNullOrEmpty(part))
                            {
                                builder.Append(part);
                            }
                        }
                    } while (reader.NextResult());

                    var results = builder.ToString();

                    if (results.Contains("testsuite"))
                    {
                        return results;
                    }

                    return null;


                }
            }
        }

        public TestExecutionResult RunWithXmlResultAndCapture(string query)
        {
            var result = new TestExecutionResult();

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = query;
                    cmd.CommandTimeout = _runTimeout;

                    var reader = cmd.ExecuteReader();

                    var builder = new StringBuilder();

                    do
                    {
                        // Check if this looks like the XML result set (single column)
                        bool isXmlResultSet = reader.FieldCount == 1;

                        if (isXmlResultSet)
                        {
                            // This might be the XML results, try reading it
                            var tempBuilder = new StringBuilder();
                            while (reader.Read())
                            {
                                var part = reader[0] as string;
                                if (!String.IsNullOrEmpty(part))
                                {
                                    tempBuilder.Append(part);
                                }
                            }

                            var tempResult = tempBuilder.ToString();
                            if (tempResult.Contains("testsuite"))
                            {
                                // This is the final XML result set
                                result.XmlResults = tempResult;
                            }
                            else if (tempResult.Length > 0)
                            {
                                // This is a single-column result set that's not XML
                                // We need to convert it to a table format
                                var table = new ResultSetTable();
                                table.ColumnNames.Add("(No column name)");
                                // Split the accumulated string back into rows if needed
                                // For now, just add as single row
                                var row = new List<string> { tempResult };
                                table.Rows.Add(row);
                                result.EarlierResultSets.Add(table);
                            }
                        }
                        else
                        {
                            // Multi-column result set, capture it
                            var table = ResultSetFormatter.ReadResultSet(reader);
                            if (table.ColumnNames.Count > 0)
                            {
                                result.EarlierResultSets.Add(table);
                            }
                        }

                    } while (reader.NextResult());

                    return result;
                }
            }
        }
    }
}
