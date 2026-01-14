using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Task = System.Threading.Tasks.Task;

namespace SSDTExtensions
{
    internal sealed class UpdateRunSettingsCommand
    {
        public const int CommandId = 0x0103;
        public static readonly Guid CommandSet = new Guid("a5e8c7d3-6b4f-4a9c-8e3d-2f1b5c7d9e4a");

        private readonly AsyncPackage package;

        private UpdateRunSettingsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static UpdateRunSettingsCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new UpdateRunSettingsCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Get the extension installation directory
                string extensionDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string testAdapterPath = Path.Combine(extensionDir, "TestAdapter");

                // Get or create tSQLt.runsettings
                string runSettingsPath = GetOrCreateRunSettingsFile(testAdapterPath);

                if (!string.IsNullOrEmpty(runSettingsPath))
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        $"Updated tSQLt.runsettings with TestAdapterPath:\n{testAdapterPath}",
                        "SSDT Extensions",
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error updating RunSettings: {ex.Message}",
                    "SSDT Extensions",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private string GetOrCreateRunSettingsFile(string testAdapterPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            if (dte?.Solution == null)
                return null;

            string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
            if (string.IsNullOrEmpty(solutionDir))
                return null;

            string runSettingsPath = Path.Combine(solutionDir, "tSQLt.runsettings");

            // If file exists, update it
            if (File.Exists(runSettingsPath))
            {
                UpdateTestAdapterPath(runSettingsPath, testAdapterPath);
                return runSettingsPath;
            }

            // Otherwise create new file with template
            CreateRunSettingsFile(runSettingsPath, testAdapterPath, dte);
            return runSettingsPath;
        }

        private void UpdateTestAdapterPath(string runSettingsPath, string testAdapterPath)
        {
            XDocument doc = XDocument.Load(runSettingsPath);
            var runConfig = doc.Root?.Element("RunConfiguration");

            if (runConfig == null)
            {
                runConfig = new XElement("RunConfiguration");
                doc.Root.AddFirst(runConfig);
            }

            var testAdaptersPathElement = runConfig.Element("TestAdaptersPaths");
            if (testAdaptersPathElement == null)
            {
                testAdaptersPathElement = new XElement("TestAdaptersPaths");
                runConfig.Add(testAdaptersPathElement);
            }

            testAdaptersPathElement.Value = testAdapterPath;
            doc.Save(runSettingsPath);
        }

        private void CreateRunSettingsFile(string runSettingsPath, string testAdapterPath, DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string defaultContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <RunConfiguration>
    <TestAdaptersPaths>{testAdapterPath}</TestAdaptersPaths>
  </RunConfiguration>
  <tSQLt>
    <!-- Optional: Uncomment and set to the root folder containing tests to speed up test discovery -->
    <!-- Can be absolute path or relative to solution directory (e.g., DatabaseProject\Tests) -->
    <!-- <TestFolder>DatabaseProject\Tests</TestFolder> -->
    <!-- Optional: Controls whether table output is captured in test results (defaults to true if not specified) -->
    <!-- <CaptureTestOutput>false</CaptureTestOutput> -->
    <DatabaseConnectionString>Data Source=localhost;Initial Catalog=YourDatabase;Integrated Security=True;TrustServerCertificate=True;</DatabaseConnectionString>
  </tSQLt>
</RunSettings>";

            File.WriteAllText(runSettingsPath, defaultContent);

            // Add to Solution Items
            try
            {
                Project solutionItemsProject = null;
                foreach (Project proj in dte.Solution.Projects)
                {
                    if (proj.Name == "Solution Items" || proj.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                    {
                        solutionItemsProject = proj;
                        break;
                    }
                }

                if (solutionItemsProject == null)
                {
                    var solution2 = dte.Solution as Solution2;
                    if (solution2 != null)
                    {
                        solutionItemsProject = solution2.AddSolutionFolder("Solution Items");
                    }
                }

                if (solutionItemsProject != null && File.Exists(runSettingsPath))
                {
                    solutionItemsProject.ProjectItems.AddFromFile(runSettingsPath);
                }
            }
            catch
            {
                // If we can't add to solution, file still exists
            }
        }
    }
}
