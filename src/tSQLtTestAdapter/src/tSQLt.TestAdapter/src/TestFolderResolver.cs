using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace tSQLt.TestAdapter
{
    public static class TestFolderResolver
    {
        /// <summary>
        /// Determines the root test folders from runsettings or by deriving from DACPAC path
        /// </summary>
        /// <param name="runSettings">The run settings from discovery context</param>
        /// <param name="dacpacSources">List of DACPAC file paths</param>
        /// <param name="logger">Logger for diagnostic messages</param>
        /// <returns>List of test folder paths, or empty list if unable to determine</returns>
        public static List<string> GetTestFolders(IRunSettings runSettings, IEnumerable<string> dacpacSources, IMessageLogger logger)
        {
            logger.SendMessage(TestMessageLevel.Informational, "Determining root test folders...");

            // First priority: Check runsettings for TestFolder elements
            logger.SendMessage(TestMessageLevel.Informational, "Checking runsettings for TestFolder configuration...");
            var testFolders = RunSettingsHelper.GetTSQLtSettings(runSettings, "TestFolder");

            if (testFolders.Any())
            {
                logger.SendMessage(TestMessageLevel.Informational, $"✓ Found {testFolders.Count} test folder(s) from runsettings:");
                foreach (var folder in testFolders)
                {
                    logger.SendMessage(TestMessageLevel.Informational, $"    {folder}");
                }
                return testFolders;
            }

            logger.SendMessage(TestMessageLevel.Informational, "No TestFolder found in runsettings");

            // Fallback: Derive from dacpac path by removing bin\Debug or bin\Release
            if (dacpacSources != null && dacpacSources.Any())
            {
                logger.SendMessage(TestMessageLevel.Informational, "Using fallback: deriving test folder from DACPAC path...");
                var firstDacpac = dacpacSources.First();
                var directory = Path.GetDirectoryName(firstDacpac);
                logger.SendMessage(TestMessageLevel.Informational, $"  DACPAC directory: {directory}");

                // Remove bin\Debug or bin\Release (case-insensitive)
                var testFolder = Regex.Replace(directory, @"\\bin\\(Debug|Release)$", "", RegexOptions.IgnoreCase);
                logger.SendMessage(TestMessageLevel.Informational, $"✓ Test folder derived from DACPAC path: {testFolder}");
                return new List<string> { testFolder };
            }

            logger.SendMessage(TestMessageLevel.Warning, "Unable to determine test folder - no runsettings and no DACPAC sources");
            return new List<string>();
        }
    }
}
