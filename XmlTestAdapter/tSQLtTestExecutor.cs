using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using tSQLt.Client.Net;
using tSQLtTestAdapter.Helpers;

namespace tSQLtTestAdapter
{
    [ExtensionUri(Constants.ExecutorUriString)]
    public class tSQLtTestExecutor : ITestExecutor
    {
        public void RunTests(IEnumerable<string> sources, IRunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
           
            IEnumerable<TestCase> tests = XmlTestDiscoverer.GetTests(sources, null);
            RunTests(tests, runContext, frameworkHandle);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            m_cancelled = false;
            
            var connectionString =new RunSettings(runContext.RunSettings).GetSetting("TestDatabaseConnectionString");
            
            foreach (TestCase test in tests)
            {
                if (m_cancelled)
                    break;
               
                var testResult = new TestResult(test);
                var testSession = new tSQLtTestRunner(connectionString);
                var result = testSession.Run(test.DisplayName.Split('.')[0], test.DisplayName.Split('.')[1]);

                testResult.Outcome = result.Passed() ? TestOutcome.Passed : TestOutcome.Failed;
                testResult.ErrorMessage += result.FailureMessages();

                frameworkHandle.RecordResult(testResult);
            }

        }

        

        public void Cancel()
        {
            m_cancelled = true;
        }

        public static readonly Uri ExecutorUri = new Uri(Constants.ExecutorUriString);
        private bool m_cancelled;
    }

    public static class Constants
    {
        public const string ExecutorUriString = "executor://tSQLtTestExecutor/v1";
        public const string FileExtension = ".sql";

    }
}
