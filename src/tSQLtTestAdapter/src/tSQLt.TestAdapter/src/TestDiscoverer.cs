using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Dac.Extensions;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using tSQLt.TestAdapter.Dacpac;


namespace tSQLt.TestAdapter
{
    [DefaultExecutorUri(Constants.ExecutorUriString)]
    [FileExtension(Constants.FileExtension)]
    [FileExtension(".dacpac")]
    [FileExtension(".dll")]
    [FileExtension("*")]
    public class TestDiscoverer : ITestDiscoverer
    {
       /// <summary>
       /// sources will be the name of the dll's built with the project so we will need to get the dacpac name and explore that
       /// </summary>
       /// <param name="sources"></param>
       /// <param name="discoveryContext"></param>
       /// <param name="logger"></param>
       /// <param name="discoverySink"></param>
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            logger.SendMessage(TestMessageLevel.Informational, "=== tSQLt Test Discovery Started ===");
           
            // Get configuration
            var config = GetConfig(sources, discoveryContext, logger);

            // Build source file cache
            var cache = new TestSourceFileCache(logger);
            if (config.TestFolders != null && config.TestFolders.Any())
            {
                cache.BuildCache(config.TestFolders);
            }
            else
            {
                logger.SendMessage(TestMessageLevel.Warning, "No test folders found - source file locations will not be available");
            }

            // Discover tests from DACPAC files
            logger.SendMessage(TestMessageLevel.Informational, "Starting test case discovery from DACPAC files...");

            try
            {
                foreach (var dacpacPath in config.DacpacSources)
                {
                    logger.SendMessage(TestMessageLevel.Informational, $"Processing DACPAC: {dacpacPath}");

                    // Get DACPAC name without extension for use in test names
                    var dacpacName = System.IO.Path.GetFileNameWithoutExtension(dacpacPath);
                    logger.SendMessage(TestMessageLevel.Informational, $"DACPAC name: {dacpacName}");

                    // Open the DACPAC model
                    var model = new TSqlModel(dacpacPath);

                    // Get all schemas
                    var schemas = model.GetObjects(DacQueryScopes.UserDefined, new[] { Schema.TypeClass }).ToList();
                    logger.SendMessage(TestMessageLevel.Informational, $"Found {schemas.Count} schema(s) in DACPAC");

                    // Get all extended properties
                    var extendedProperties = model.GetObjects(DacQueryScopes.UserDefined, ExtendedProperty.TypeClass).ToList();
                    logger.SendMessage(TestMessageLevel.Informational, $"Found {extendedProperties.Count} extended property(ies)");

                    // Process each schema
                    foreach (var schema in schemas)
                    {
                        var schemaName = schema.Name.GetName();

                        // Check if this schema has the tSQLt.TestClass extended property
                        foreach (var property in extendedProperties)
                        {
                            if (property.GetReferenced(ExtendedProperty.Host).Any(p => p.Name.GetName() == schemaName))
                            {
                                if (string.Equals(property.Name.GetName(), "tSQLt.TestClass", StringComparison.OrdinalIgnoreCase))
                                {
                                    logger.SendMessage(TestMessageLevel.Informational, $"Found test class schema: [{schemaName}]");

                                    // Get all procedures that reference this schema
                                    var procs = schema.GetReferencing(DacQueryScopes.UserDefined);

                                    foreach (var proc in procs)
                                    {
                                        var procName = proc.Name.GetName();

                                        // Check if procedure name starts with "test"
                                        if (procName.StartsWith("test", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var fullTestName = $"tSQLt.{schemaName}.{procName}";
                                            logger.SendMessage(TestMessageLevel.Informational, $"  Found test: {fullTestName}");

                                            // Look up source location in cache
                                            var location = cache.GetLocation(schemaName, procName);

                                            string codeFilePath;
                                            int lineNumber;

                                            if (location != null)
                                            {
                                                codeFilePath = location.FilePath;
                                                lineNumber = location.LineNumber;
                                                logger.SendMessage(TestMessageLevel.Informational,
                                                    $"    Source: {codeFilePath}:{lineNumber}");
                                            }
                                            else
                                            {
                                                codeFilePath = dacpacPath; // Fallback to DACPAC path
                                                lineNumber = 1;
                                                logger.SendMessage(TestMessageLevel.Warning,
                                                    $"    No source file found for [{schemaName}].[{procName}]");
                                            }

                                            var testCase = new TestCase(fullTestName, Constants.ExecutorUri, sources.First())
                                            {
                                                CodeFilePath = codeFilePath,
                                                LineNumber = lineNumber
                                            };

                                            discoverySink.SendTestCase(testCase);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                logger.SendMessage(TestMessageLevel.Informational, "Test discovery completed successfully");
            }
            catch (Exception ex)
            {
                logger.SendMessage(TestMessageLevel.Error, $"Error during test discovery: {ex.Message}");
                logger.SendMessage(TestMessageLevel.Error, $"Stack trace: {ex.StackTrace}");
            }

            logger.SendMessage(TestMessageLevel.Informational, "=== tSQLt Test Discovery Finished ===");
        }

        /// <summary>
        /// Gets the discovery configuration by processing sources and runsettings
        /// </summary>
        private DiscoveryConfiguration GetConfig(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger)
        {
            logger.SendMessage(TestMessageLevel.Informational, $"Received {sources.Count()} source(s) for discovery");

            foreach (var source in sources)
            {
                logger.SendMessage(TestMessageLevel.Informational, $"  Source: {source}");
            }

            // Convert DLL paths to DACPAC paths
            logger.SendMessage(TestMessageLevel.Informational, "Converting DLL paths to DACPAC paths...");
            var dacpacSources = sources.Select(s =>
            {
                var originalPath = s;

                // Replace .dll with .dacpac
                var dacpacPath = s.Replace(".dll", ".dacpac");

                // Handle obj vs bin folder - DACPAC is always in bin folder
                // Replace \obj\ with \bin\ (case-insensitive to handle both Debug and Release)
                dacpacPath = System.Text.RegularExpressions.Regex.Replace(
                    dacpacPath,
                    @"\\obj\\",
                    @"\bin\",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (originalPath != dacpacPath)
                {
                    logger.SendMessage(TestMessageLevel.Informational, $"  Converted: {originalPath}");
                    logger.SendMessage(TestMessageLevel.Informational, $"         -> {dacpacPath}");
                }

                return dacpacPath;
            }).ToList();

            logger.SendMessage(TestMessageLevel.Informational, $"Found {dacpacSources.Count} DACPAC source(s):");
            foreach (var dacpac in dacpacSources)
            {
                logger.SendMessage(TestMessageLevel.Informational, $"  DACPAC: {dacpac}");
            }

            // Check for LaunchDebugger setting
            if (RunSettingsHelper.GetTSQLtSettingBool(discoveryContext.RunSettings, "LaunchDebugger"))
            {
                logger.SendMessage(TestMessageLevel.Informational, "LaunchDebugger is set to true - launching debugger...");
                Debugger.Launch();
            }

            // Find the root test folders
            var testFolders = TestFolderResolver.GetTestFolders(discoveryContext.RunSettings, dacpacSources, logger);

            return new DiscoveryConfiguration
            {
                DacpacSources = dacpacSources,
                TestFolders = testFolders
            };
        }
    }
}
