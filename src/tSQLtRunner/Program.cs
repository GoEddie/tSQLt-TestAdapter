using System.Data;
using System.Data.SqlClient;
using System.Xml.Linq;

namespace tSQLtRunner;

class Program
{
    static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            string connectionString = args[0];
            string? testName = args.Length > 1 ? args[1] : null;

            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($"tSQLt Test Runner");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            if (string.IsNullOrEmpty(testName))
            {
                Console.WriteLine("Running ALL tests...");
            }
            else
            {
                Console.WriteLine($"Running test: {testName}");
            }
            Console.WriteLine();

            var results = RunTests(connectionString, testName);
            DisplayResults(results);

            return results.AllPassed ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: tSQLtRunner <connection-string> [test-name]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  connection-string  SQL Server connection string (required)");
        Console.WriteLine("  test-name          Specific test to run in format [Schema].[TestName] (optional)");
        Console.WriteLine("                     If not provided, all tests will be run");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  tSQLtRunner \"Server=.;Database=MyDb;Integrated Security=true\"");
        Console.WriteLine("  tSQLtRunner \"Server=.;Database=MyDb;Integrated Security=true\" \"[MyTests].[test should validate input]\"");
        Console.WriteLine();
        Console.WriteLine("Exit Codes:");
        Console.WriteLine("  0 = All tests passed");
        Console.WriteLine("  1 = One or more tests failed or error occurred");
    }

    static TestResults RunTests(string connectionString, string? testName)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // Build the tSQLt command
        string sqlCommand = string.IsNullOrEmpty(testName)
            ? "EXEC tSQLt.RunAll"
            : $"EXEC tSQLt.Run '{testName}'";

        Console.WriteLine($"Executing: {sqlCommand}");
        Console.WriteLine();

        using var command = new SqlCommand(sqlCommand, connection);
        command.CommandTimeout = 300; // 5 minutes

        var results = new TestResults();

        using var reader = command.ExecuteReader();

        // Process all result sets
        do
        {
            // Check if this looks like tSQLt XML results
            bool isXmlResults = false;
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i) == "Result Set" || reader.GetName(i) == "ResultSet")
                {
                    isXmlResults = true;
                    break;
                }
            }

            if (isXmlResults && reader.Read())
            {
                // This is the tSQLt XML results
                string? xml = reader.GetString(0);
                if (!string.IsNullOrEmpty(xml))
                {
                    ParseXmlResults(xml, results);
                }
            }
            else
            {
                // This might be intermediate output from the test
                var outputLines = new List<string>();
                var columnNames = new List<string>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columnNames.Add(reader.GetName(i));
                }

                if (columnNames.Count > 0)
                {
                    outputLines.Add(string.Join(" | ", columnNames));
                    outputLines.Add(new string('-', outputLines[0].Length));
                }

                while (reader.Read())
                {
                    var values = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        values.Add(reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString() ?? "");
                    }
                    outputLines.Add(string.Join(" | ", values));
                }

                if (outputLines.Count > 2) // More than just headers
                {
                    results.IntermediateOutput.AddRange(outputLines);
                }
            }
        } while (reader.NextResult());

        return results;
    }

    static void ParseXmlResults(string xml, TestResults results)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var testSuites = doc.Root;

            if (testSuites == null)
                return;

            foreach (var testSuite in testSuites.Elements("testsuite"))
            {
                string suiteName = testSuite.Attribute("name")?.Value ?? "Unknown";
                int tests = int.Parse(testSuite.Attribute("tests")?.Value ?? "0");
                int failures = int.Parse(testSuite.Attribute("failures")?.Value ?? "0");
                int errors = int.Parse(testSuite.Attribute("errors")?.Value ?? "0");

                results.TotalTests += tests;
                results.TotalFailures += failures;
                results.TotalErrors += errors;

                foreach (var testCase in testSuite.Elements("testcase"))
                {
                    string className = testCase.Attribute("classname")?.Value ?? "";
                    string testName = testCase.Attribute("name")?.Value ?? "";

                    var failure = testCase.Element("failure");
                    var error = testCase.Element("error");

                    var test = new TestResult
                    {
                        Name = $"{className}.{testName}",
                        Passed = failure == null && error == null,
                        Message = failure?.Attribute("message")?.Value ?? error?.Attribute("message")?.Value
                    };

                    results.Tests.Add(test);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to parse XML results: {ex.Message}");
        }
    }

    static void DisplayResults(TestResults results)
    {
        // Display any intermediate output
        if (results.IntermediateOutput.Count > 0)
        {
            Console.WriteLine("Test Output:");
            Console.WriteLine("-".PadRight(80, '-'));
            foreach (var line in results.IntermediateOutput)
            {
                Console.WriteLine(line);
            }
            Console.WriteLine();
        }

        // Display test results
        Console.WriteLine("Test Results:");
        Console.WriteLine("-".PadRight(80, '-'));

        foreach (var test in results.Tests)
        {
            if (test.Passed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[PASS] ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[FAIL] ");
            }
            Console.ResetColor();
            Console.WriteLine(test.Name);

            if (!test.Passed && !string.IsNullOrEmpty(test.Message))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"       {test.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine($"Summary: {results.TotalTests} tests, {results.TotalFailures} failures, {results.TotalErrors} errors");

        if (results.AllPassed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ALL TESTS PASSED");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"TESTS FAILED ({results.TotalFailures + results.TotalErrors} failed)");
        }
        Console.ResetColor();
        Console.WriteLine("=".PadRight(80, '='));
    }
}

class TestResults
{
    public List<TestResult> Tests { get; } = new();
    public List<string> IntermediateOutput { get; } = new();
    public int TotalTests { get; set; }
    public int TotalFailures { get; set; }
    public int TotalErrors { get; set; }
    public bool AllPassed => TotalFailures == 0 && TotalErrors == 0 && TotalTests > 0;
}

class TestResult
{
    public required string Name { get; set; }
    public bool Passed { get; set; }
    public string? Message { get; set; }
}
