using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using tSQLt.Client.Net;
using tSQLtTestAdapter.Helpers;

namespace tSQLtTestAdapter
{
    [ExtensionUri(Constants.ExecutorUriString)]
    public class tSQLtTestExecutor : ITestExecutor
    {
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var discoverer = new tSQLtTestDiscoverer();
            IEnumerable<TestCase> tests = discoverer.GetTests(sources, null, new RunSettings(runContext.RunSettings).GetSetting("IncludePath"));
            RunTests(tests, runContext, frameworkHandle);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {


            _cancelled = false;
            var settings = new RunSettings(runContext.RunSettings);

            var connectionString = settings.GetSetting("TestDatabaseConnectionString");
            var debug = settings.GetSetting("tSQLt-TestAdapter-Debug")?.ToLowerInvariant() == "true";


            if (String.IsNullOrEmpty(connectionString))
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, @"No connection string found. You need to specify a run setting with the name ""TestDatabaseConnectionString"". Create a .runsettings file a sample is: 

<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <!-- Parameters used by tests at runtime -->
  <TestRunParameters>
    <Parameter name=""TestDatabaseConnectionString"" value=""server=Servername;initial catalog=UnitTestDatabase;integrated security=sspi"" />
    <!-- If you have a large project then to speed up discovery, use this to limit which .sql files are parsed. If all your tests are in a subfolder called \UnitTests\ or \Our-UnitTests\ then set the value to UnitTests - it is a regex so U.i.T.s.s will also work.
    <!-- <Parameter name=""IncludePath"" value=""RegexToTestsToInclude"" /> -->
  </TestRunParameters>
</RunSettings>

If you are running tests in visual studio choose ""Test-->Test Settings-->Select Test Settings File--> Choose your .runsettings file"", if you are using the command line pass the runsettings files using /Settings:PathTo.runsettings

");
                return;
            }            


            foreach (TestCase test in tests)
            {
                if (_cancelled)
                    break;
               
                var testResult = new TestResult(test);
                var testSession = new tSQLtTestRunner(connectionString);

                Debug(debug, frameworkHandle, $"running test {test.DisplayName}");

                var result = Run(testSession, test);

                Debug(debug, frameworkHandle, $"running test {test.DisplayName}...done");

                if (null == result)
                {
                    continue;
                }

                testResult.Outcome = result.Passed() ? TestOutcome.Passed : TestOutcome.Failed;
                testResult.ErrorMessage += result.FailureMessages();
                
                Debug(debug, frameworkHandle, $"test passed: {result.Passed()} failureMessages: {result.FailureMessages()}, ErrorMessage: {testResult.ErrorMessage}");
                
                frameworkHandle.RecordResult(testResult);
            }

        }

        private static void Debug(bool debug, IFrameworkHandle frameworkHandle, string message)
        {
            if(debug)
                frameworkHandle.SendMessage(TestMessageLevel.Informational, message);
        }

        private static TestSuites Run(tSQLtTestRunner testSession, TestCase test)
        {
            if(test.DisplayName.Contains("."))
                return testSession.Run(test.DisplayName.Split('.')[0], test.DisplayName.Split('.')[1]);
            return null;
        }


        public void Cancel()
        {
            _cancelled = true;
        }

        public static readonly Uri ExecutorUri = new Uri(Constants.ExecutorUriString);
        private bool _cancelled;
    }

    public static class Constants
    {
        public const string ExecutorUriString = "executor://tSQLtTestExecutor/v1";
        public const string FileExtension = ".sql";

    }
}
