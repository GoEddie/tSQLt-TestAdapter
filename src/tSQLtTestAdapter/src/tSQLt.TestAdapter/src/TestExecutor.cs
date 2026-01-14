using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using tSQLt.TestAdapter.Client;

namespace tSQLt.TestAdapter
{
    [ExtensionUri(Constants.ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, "=== tSQLt Test Execution Started ===");

            // Get the database connection string from runsettings
            var connectionString = GetConnectionString(runContext, frameworkHandle);
            if (connectionString == null)
            {
                return; // Error already logged in GetConnectionString
            }

            frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"Using connection string: {MaskConnectionString(connectionString)}");

            // Get the CaptureTestOutput setting (defaults to true)
            var captureTestOutput = RunSettingsHelper.GetTSQLtSettingBool(runContext?.RunSettings, "CaptureTestOutput", true);

            // Create tSQLt test runner
            tSQLtTestRunner runner;
            try
            {
                runner = new tSQLtTestRunner(connectionString);
                frameworkHandle?.SendMessage(TestMessageLevel.Informational, "tSQLt test runner initialized");
            }
            catch (Exception ex)
            {
                frameworkHandle?.SendMessage(TestMessageLevel.Error, $"Failed to initialize tSQLt test runner: {ex.Message}");
                return;
            }

            // Execute each test
            var testList = tests.ToList();
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"Running {testList.Count} test(s)");

            foreach (var testCase in testList)
            {
                RunSingleTest(testCase, runner, frameworkHandle, captureTestOutput);
            }

            frameworkHandle?.SendMessage(TestMessageLevel.Informational, "=== tSQLt Test Execution Completed ===");
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            frameworkHandle?.SendMessage(TestMessageLevel.Informational, "=== tSQLt Test Execution Started (from sources) ===");

            // Get the database connection string from runsettings
            var connectionString = GetConnectionString(runContext, frameworkHandle);
            if (connectionString == null)
            {
                return; // Error already logged in GetConnectionString
            }

            frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"Using connection string: {MaskConnectionString(connectionString)}");

            // TODO: Implement test execution from sources
            frameworkHandle?.SendMessage(TestMessageLevel.Warning, "Test execution from sources not yet implemented");
        }

        public void Cancel()
        {
            // TODO: Implement cancellation logic
        }

        /// <summary>
        /// Runs a single test and records the result
        /// </summary>
        private void RunSingleTest(TestCase testCase, tSQLtTestRunner runner, IFrameworkHandle frameworkHandle, bool captureTestOutput = true)
        {
            frameworkHandle?.RecordStart(testCase);
            var startTime = DateTimeOffset.Now;

            try
            {
                // Parse the test name: "tSQLt.SchemaName.ProcedureName"
                var parts = testCase.FullyQualifiedName.Split('.');
                if (parts.Length != 3)
                {
                    var errorResult = new TestResult(testCase)
                    {
                        Outcome = TestOutcome.Failed,
                        ErrorMessage = $"Invalid test name format: {testCase.FullyQualifiedName}. Expected format: tSQLt.SchemaName.ProcedureName",
                        Duration = DateTimeOffset.Now - startTime
                    };
                    frameworkHandle?.RecordResult(errorResult);
                    frameworkHandle?.RecordEnd(testCase, TestOutcome.Failed);
                    return;
                }

                var schemaName = parts[1];
                var procedureName = parts[2];

                frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"Executing: [{schemaName}].[{procedureName}]");

                // Run the test - use capture mode if enabled, otherwise use standard mode
                TestSuites results;
                List<Client.Gateways.ResultSetTable> earlierResultSets = null;

                if (captureTestOutput)
                {
                    var runResult = runner.RunWithCapture(schemaName, procedureName);
                    results = runResult.TestResults;
                    earlierResultSets = runResult.EarlierResultSets;
                }
                else
                {
                    // Use standard mode without capturing output
                    results = runner.Run(schemaName, procedureName);
                }

                // Process results
                var testResult = new TestResult(testCase)
                {
                    StartTime = startTime,
                    EndTime = DateTimeOffset.Now
                };

                testResult.Duration = testResult.EndTime - testResult.StartTime;

                // Format and display earlier recordsets if any
                if (earlierResultSets != null && earlierResultSets.Count > 0)
                {
                    var outputMessage = new System.Text.StringBuilder();
                    outputMessage.AppendLine();
                    outputMessage.AppendLine("=== Test Output ===");
                    outputMessage.AppendLine();

                    for (int i = 0; i < earlierResultSets.Count; i++)
                    {
                        var resultSet = earlierResultSets[i];
                        if (i > 0)
                        {
                            outputMessage.AppendLine();
                        }
                        outputMessage.AppendLine($"Result Set {i + 1}:");
                        outputMessage.Append(resultSet.FormatAsTable());
                        outputMessage.AppendLine();
                    }

                    testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, outputMessage.ToString()));
                }

                if (results.Passed())
                {
                    testResult.Outcome = TestOutcome.Passed;
                    frameworkHandle?.SendMessage(TestMessageLevel.Informational, $"  ✓ PASSED: [{schemaName}].[{procedureName}]");
                }
                else
                {
                    testResult.Outcome = TestOutcome.Failed;
                    testResult.ErrorMessage = results.FailureMessages();

                    frameworkHandle?.SendMessage(TestMessageLevel.Error, $"  ✗ FAILED: [{schemaName}].[{procedureName}]");
                    frameworkHandle?.SendMessage(TestMessageLevel.Error, $"    {testResult.ErrorMessage}");
                }

                frameworkHandle?.RecordResult(testResult);
                frameworkHandle?.RecordEnd(testCase, testResult.Outcome);
            }
            catch (Exception ex)
            {
                var errorResult = new TestResult(testCase)
                {
                    Outcome = TestOutcome.Failed,
                    ErrorMessage = $"Exception running test: {ex.Message}",
                    ErrorStackTrace = ex.StackTrace,
                    Duration = DateTimeOffset.Now - startTime
                };

                frameworkHandle?.SendMessage(TestMessageLevel.Error, $"  ✗ ERROR: {testCase.FullyQualifiedName}");
                frameworkHandle?.SendMessage(TestMessageLevel.Error, $"    {ex.Message}");

                frameworkHandle?.RecordResult(errorResult);
                frameworkHandle?.RecordEnd(testCase, TestOutcome.Failed);
            }
        }

        /// <summary>
        /// Gets the database connection string from runsettings
        /// </summary>
        private string GetConnectionString(IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var connectionString = RunSettingsHelper.GetTSQLtSetting(runContext?.RunSettings, "DatabaseConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                frameworkHandle?.SendMessage(TestMessageLevel.Error,
                    "DatabaseConnectionString not found in runsettings. Please add <DatabaseConnectionString> under <RunSettings><tSQLt>.");
                frameworkHandle?.SendMessage(TestMessageLevel.Error,
                    "Example: <RunSettings><tSQLt><DatabaseConnectionString>Server=.;Database=MyDb;Integrated Security=true</DatabaseConnectionString></tSQLt></RunSettings>");
                return null;
            }

            return connectionString;
        }

        /// <summary>
        /// Masks the password in a connection string for logging
        /// </summary>
        private string MaskConnectionString(string connectionString)
        {
            // Simple masking - replace password value with ****
            var maskedCs = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(Password|Pwd)\s*=\s*[^;]*",
                "$1=****",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return maskedCs;
        }
    }
}