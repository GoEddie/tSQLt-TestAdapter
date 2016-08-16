using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using tSQLt.Client.Net;

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
            
            var doc = XDocument.Parse(runContext.RunSettings.SettingsXml);

            var connectionString = GetConnectionString(doc);
            
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

        private static string GetConnectionString(XDocument doc)
        {
            var current = doc.Element("RunSettings");
            if (current == null)
            {
                throw new InvalidOperationException("You must supply a runSettings and with a connectionString");
            }

            current = current.Element("TestRunParameters");

            if (current == null)
            {
                throw new InvalidOperationException(
                    "You must supply a runSettings with a TestRunParameters section with a connectionString");
            }

            foreach (var element in current.Elements())
            {
                if (element.HasAttributes && element.Attribute("name") != null && element.Attribute("name").Value == "TestDatabaseConnectionString")
                {
                    if (element.Attribute("value") == null)
                    {
                        throw new InvalidOperationException(
                            "You must supply a runSettings with a TestRunParameters section with a connectionString - it looks like you have the element but are missing the attribute \"value\"");

                    }

                    return element.Attribute("value").Value;

                }
            }

            throw new InvalidOperationException(
                "You must supply a runSettings with a TestRunParameters section with a connectionString - nope not found :(");

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
