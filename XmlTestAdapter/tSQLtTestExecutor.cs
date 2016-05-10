using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace tSQLtTestAdapter
{
    [ExtensionUri(Constants.ExecutorUriString)]
    public class tSQLtTestExecutor : ITestExecutor
    {
        public void RunTests(IEnumerable<string> sources, IRunContext runContext,
            IFrameworkHandle frameworkHandle)
        {
            IEnumerable<TestCase> tests = tSQLtTestDiscoverer.GetTests(sources, null);
            RunTests(tests, runContext, frameworkHandle);
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            m_cancelled = false;

            foreach (TestCase test in tests)
            {
                if (m_cancelled)
                    break;

                var testResult = new TestResult(test);
                testResult.ErrorMessage= "slslsks   s s s s s s s s";
                testResult.ErrorStackTrace = new StackTrace().ToString();
                
                testResult.Outcome = TestOutcome.Failed;
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

    }
}
