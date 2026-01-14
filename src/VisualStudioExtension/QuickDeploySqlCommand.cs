using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Task = System.Threading.Tasks.Task;

namespace SSDTExtensions
{
    /// <summary>
    /// Command handler for Quick Deploy Sql
    /// </summary>
    internal sealed class QuickDeploySqlCommand
    {
        /// <summary>
        /// Enable diagnostic logging to the Build output window.
        /// Set to true to troubleshoot menu visibility or command execution issues.
        /// When enabled, you'll see "QuickDeploySql:" messages in View > Output > Build.
        /// </summary>
        private const bool EnableDiagnosticLogging = false;

        /// <summary>
        /// Command ID for Build menu.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command ID for Solution Explorer context menu.
        /// </summary>
        public const int ContextCommandId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a5e8c7d3-6b4f-4a9c-8e3d-2f1b5c7d9e4a");

        /// <summary>
        /// Session flag to track if user selected "don't ask again"
        /// </summary>
        private static bool dontAskAgainForSession = false;

        /// <summary>
        /// Stores the last approved connection string to detect changes
        /// </summary>
        private static string lastApprovedConnectionString = null;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickDeploySqlCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private QuickDeploySqlCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            // Register Build menu command
            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(menuItem);

            // Register Solution Explorer context menu command
            var contextCommandID = new CommandID(CommandSet, ContextCommandId);
            var contextMenuItem = new OleMenuCommand(this.Execute, contextCommandID);
            contextMenuItem.BeforeQueryStatus += OnBeforeQueryStatusContext;
            commandService.AddCommand(contextMenuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static QuickDeploySqlCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in QuickDeploySqlCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new QuickDeploySqlCommand(package, commandService);
        }

        /// <summary>
        /// Checks if the current solution contains an SSDT project (.sqlproj).
        /// </summary>
        /// <returns>True if an SSDT project exists in the solution.</returns>
        private bool SolutionContainsSsdtProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.Solution == null)
                {
                    WriteToOutputWindow("QuickDeploySql: DTE or Solution is null");
                    return false;
                }

                WriteToOutputWindow($"QuickDeploySql: Solution name: {dte.Solution.FullName}");
                WriteToOutputWindow($"QuickDeploySql: Projects count: {dte.Solution.Projects.Count}");

                // Check all projects in the solution (including nested ones)
                foreach (EnvDTE.Project project in dte.Solution.Projects)
                {
                    if (project != null)
                    {
                        WriteToOutputWindow($"QuickDeploySql: Checking project: {project.Name} (Kind: {project.Kind})");

                        if (CheckProjectAndChildren(project))
                            return true;
                    }
                }

                WriteToOutputWindow("QuickDeploySql: No .sqlproj found after checking all projects");
            }
            catch (Exception ex)
            {
                WriteToOutputWindow($"QuickDeploySql: Exception in SolutionContainsSsdtProject: {ex.Message}");
                return false;
            }

            return false;
        }

        /// <summary>
        /// Recursively checks a project and its children for SSDT projects.
        /// Handles solution folders.
        /// </summary>
        private bool CheckProjectAndChildren(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Check if this is an SSDT project
                if (!string.IsNullOrEmpty(project.FullName))
                {
                    string extension = Path.GetExtension(project.FullName);
                    WriteToOutputWindow($"QuickDeploySql:   Project FullName: {project.FullName} (Extension: {extension})");

                    if (string.Equals(extension, ".sqlproj", StringComparison.OrdinalIgnoreCase))
                    {
                        WriteToOutputWindow($"QuickDeploySql:   Found SSDT project: {project.FullName}");
                        return true;
                    }
                }

                // Check if this is a solution folder (Kind == vsProjectKindSolutionItems)
                if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                {
                    WriteToOutputWindow($"QuickDeploySql:   Project is a solution folder, checking children");

                    // Recursively check projects in the solution folder
                    if (project.ProjectItems != null)
                    {
                        foreach (EnvDTE.ProjectItem item in project.ProjectItems)
                        {
                            if (item.SubProject != null)
                            {
                                if (CheckProjectAndChildren(item.SubProject))
                                    return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToOutputWindow($"QuickDeploySql:   Exception checking project: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Called to update the command status before the menu is displayed.
        /// This is where we check if the active document is a .sql file.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var command = sender as OleMenuCommand;
            if (command == null)
            {
                WriteToOutputWindow("QuickDeploySql: OnBeforeQueryStatus - command is null");
                return;
            }

            // Default to not visible
            command.Visible = false;
            command.Enabled = false;

            // First check if solution contains an SSDT project
            if (!SolutionContainsSsdtProject())
            {
                WriteToOutputWindow("QuickDeploySql: No SSDT project found in solution");
                return;
            }

            try
            {
                // Get the DTE service to access the active document
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument != null)
                {
                    string filePath = dte.ActiveDocument.FullName;
                    string extension = Path.GetExtension(filePath);

                    WriteToOutputWindow($"QuickDeploySql: Checking active document: {filePath} (extension: {extension})");

                    // Show the command only if the active document is a .sql file
                    if (string.Equals(extension, ".sql", StringComparison.OrdinalIgnoreCase))
                    {
                        command.Visible = true;
                        command.Enabled = true;
                        WriteToOutputWindow("QuickDeploySql: Command made visible and enabled");
                    }
                }
                else
                {
                    WriteToOutputWindow("QuickDeploySql: No active document");
                }
            }
            catch (Exception ex)
            {
                // If anything goes wrong, just hide the command
                command.Visible = false;
                command.Enabled = false;
                WriteToOutputWindow($"QuickDeploySql: Error in OnBeforeQueryStatus: {ex.Message}");
            }
        }

        /// <summary>
        /// Called to update the command status for Solution Explorer context menu.
        /// This checks if the selected item in Solution Explorer is a .sql file.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnBeforeQueryStatusContext(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var command = sender as OleMenuCommand;
            if (command == null)
                return;

            // Default to not visible
            command.Visible = false;
            command.Enabled = false;

            // First check if solution contains an SSDT project
            if (!SolutionContainsSsdtProject())
                return;

            try
            {
                // Get the DTE service to access selected items in Solution Explorer
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.SelectedItems != null)
                {
                    foreach (EnvDTE.SelectedItem selectedItem in dte.SelectedItems)
                    {
                        if (selectedItem.ProjectItem != null)
                        {
                            // Get the file path of the selected item
                            string filePath = selectedItem.ProjectItem.FileNames[0];
                            string extension = Path.GetExtension(filePath);

                            // Show the command only if it's a .sql file
                            if (string.Equals(extension, ".sql", StringComparison.OrdinalIgnoreCase))
                            {
                                command.Visible = true;
                                command.Enabled = true;
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
                // If anything goes wrong, just hide the command
                command.Visible = false;
                command.Enabled = false;
            }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Call your Quick Deploy Sql method here
            QuickDeploySql();
        }

        /// <summary>
        /// Gets the path to the active runsettings file configured in Visual Studio.
        /// </summary>
        /// <returns>The path to the runsettings file, or null if not configured.</returns>
        private string GetRunSettingsFilePath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.Solution == null)
                    return null;

                string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
                var allRunSettingsFiles = new List<string>();

                // First, search all project directories explicitly
                foreach (EnvDTE.Project project in dte.Solution.Projects)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(project.FullName))
                        {
                            string projectDir = Path.GetDirectoryName(project.FullName);
                            if (Directory.Exists(projectDir))
                            {
                                var projectRunSettings = Directory.GetFiles(projectDir, "*.runsettings", SearchOption.TopDirectoryOnly);
                                allRunSettingsFiles.AddRange(projectRunSettings);
                            }
                        }
                    }
                    catch
                    {
                        // Skip projects we can't access
                    }
                }

                // Then search solution directory and all subdirectories
                if (!string.IsNullOrEmpty(solutionDir) && Directory.Exists(solutionDir))
                {
                    var solutionRunSettings = Directory.GetFiles(solutionDir, "*.runsettings", SearchOption.AllDirectories);
                    allRunSettingsFiles.AddRange(solutionRunSettings);
                }

                // Remove duplicates
                allRunSettingsFiles = allRunSettingsFiles.Distinct().ToList();

                if (allRunSettingsFiles.Count > 0)
                {
                    // Prioritize tSQLt.runsettings if it exists (case insensitive)
                    var tSqltRunSettings = allRunSettingsFiles.FirstOrDefault(f =>
                        string.Equals(Path.GetFileName(f), "tSQLt.runsettings", StringComparison.OrdinalIgnoreCase));

                    if (tSqltRunSettings != null)
                    {
                        return tSqltRunSettings;
                    }

                    // Otherwise return the first one found
                    return allRunSettingsFiles[0];
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Parses the runsettings file to extract the tSQLt DatabaseConnectionString.
        /// </summary>
        /// <param name="runSettingsPath">Path to the runsettings file.</param>
        /// <returns>The database connection string, or null if not found.</returns>
        private string GetConnectionStringFromRunSettings(string runSettingsPath)
        {
            if (string.IsNullOrEmpty(runSettingsPath) || !File.Exists(runSettingsPath))
                return null;

            try
            {
                XDocument doc = XDocument.Load(runSettingsPath);

                // Navigate to tSQLt/DatabaseConnectionString
                var connectionStringElement = doc.Root?
                    .Element("tSQLt")?
                    .Element("DatabaseConnectionString");

                return connectionStringElement?.Value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Splits SQL script into batches by splitting on GO statements
        /// </summary>
        /// <param name="sqlScript">The complete SQL script</param>
        /// <returns>List of SQL batches</returns>
        private List<string> SplitIntoBatches(string sqlScript)
        {
            var batches = new List<string>();

            // Split on GO keyword (case insensitive, must be on its own line)
            var regex = new Regex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var splits = regex.Split(sqlScript);

            foreach (var batch in splits)
            {
                var trimmedBatch = batch.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedBatch))
                {
                    batches.Add(trimmedBatch);
                }
            }

            return batches;
        }

        /// <summary>
        /// Converts CREATE PROCEDURE statements to CREATE OR ALTER using TSqlScriptDom
        /// </summary>
        /// <param name="batch">SQL batch to process</param>
        /// <returns>Modified SQL batch</returns>
        private string ConvertCreateToCreateOrAlter(string batch)
        {
            try
            {
                var parser = new TSql160Parser(true);
                IList<ParseError> errors;

                using (var reader = new StringReader(batch))
                {
                    var fragment = parser.Parse(reader, out errors);

                    if (errors.Count > 0)
                    {
                        // If there are parse errors, return the original batch
                        return batch;
                    }

                    // Replace CREATE with CREATE OR ALTER for various object types
                    var modifiedBatch = batch;

                    // Handle CREATE PROCEDURE
                    modifiedBatch = Regex.Replace(
                        modifiedBatch,
                        @"\bCREATE\s+PROCEDURE\b",
                        "CREATE OR ALTER PROCEDURE",
                        RegexOptions.IgnoreCase);

                    modifiedBatch = Regex.Replace(
                        modifiedBatch,
                        @"\bCREATE\s+PROC\b",
                        "CREATE OR ALTER PROC",
                        RegexOptions.IgnoreCase);

                    // Handle CREATE VIEW
                    modifiedBatch = Regex.Replace(
                        modifiedBatch,
                        @"\bCREATE\s+VIEW\b",
                        "CREATE OR ALTER VIEW",
                        RegexOptions.IgnoreCase);

                    // Handle CREATE FUNCTION
                    modifiedBatch = Regex.Replace(
                        modifiedBatch,
                        @"\bCREATE\s+FUNCTION\b",
                        "CREATE OR ALTER FUNCTION",
                        RegexOptions.IgnoreCase);

                    return modifiedBatch;
                }
            }
            catch
            {
                // If anything goes wrong, return the original batch
                return batch;
            }
        }

        /// <summary>
        /// Custom visitor to find CREATE PROCEDURE statements
        /// </summary>
        private class CreateProcedureVisitor : TSqlFragmentVisitor
        {
            public List<CreateProcedureStatement> CreateProcedureStatements { get; } = new List<CreateProcedureStatement>();

            public override void ExplicitVisit(CreateProcedureStatement node)
            {
                CreateProcedureStatements.Add(node);
                base.ExplicitVisit(node);
            }
        }

        /// <summary>
        /// Executes SQL batches against the database asynchronously
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="batches">SQL batches to execute</param>
        private async System.Threading.Tasks.Task ExecuteSqlBatchesAsync(string connectionString, List<string> batches)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var batch in batches)
                {
                    using (var command = new SqlCommand(batch, connection))
                    {
                        command.CommandTimeout = 30; // 30 seconds timeout per batch
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the path of the currently active SQL file
        /// </summary>
        /// <returns>Path to the active SQL file, or null if none</returns>
        private string GetActiveFilePath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument != null)
                {
                    return dte.ActiveDocument.FullName;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Creates a default tSQLt.runsettings file in the solution directory
        /// </summary>
        /// <returns>Path to the created file, or null if failed</returns>
        private string CreateDefaultRunSettingsFile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.Solution == null)
                    return null;

                string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
                if (string.IsNullOrEmpty(solutionDir))
                    return null;

                string runSettingsPath = Path.Combine(solutionDir, "tSQLt.runsettings");

                // Don't create if it already exists
                if (File.Exists(runSettingsPath))
                {
                    return runSettingsPath;
                }

                // Create default content
                string defaultContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RunSettings>
  <RunConfiguration>
    <!-- Uncomment the line below and update the path if needed -->
    <!-- <TestAdaptersPaths>C:\path\to\tSQLt.TestAdapter</TestAdaptersPaths> -->
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

                // Write the file to disk first
                File.WriteAllText(runSettingsPath, defaultContent, Encoding.UTF8);

                // Verify file was created
                if (!File.Exists(runSettingsPath))
                {
                    return null;
                }

                // Add to Solution Items folder
                try
                {
                    // Find Solution Items folder
                    EnvDTE.Project solutionItemsProject = null;
                    foreach (EnvDTE.Project proj in dte.Solution.Projects)
                    {
                        if (proj.Name == "Solution Items" || proj.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                        {
                            solutionItemsProject = proj;
                            break;
                        }
                    }

                    // If Solution Items doesn't exist, create it using Solution2 interface
                    if (solutionItemsProject == null)
                    {
                        var solution2 = dte.Solution as Solution2;
                        if (solution2 != null)
                        {
                            solutionItemsProject = solution2.AddSolutionFolder("Solution Items");
                        }
                    }

                    // Add the file to Solution Items (only if file exists on disk)
                    if (solutionItemsProject != null && File.Exists(runSettingsPath))
                    {
                        solutionItemsProject.ProjectItems.AddFromFile(runSettingsPath);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail - file still exists on disk
                    WriteToOutputWindow($"Warning: Created tSQLt.runsettings but couldn't add to solution: {ex.Message}");
                }

                return runSettingsPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Writes a message to the Visual Studio Build output window (only if diagnostic logging is enabled)
        /// </summary>
        /// <param name="message">Message to write</param>
        private void WriteToOutputWindow(string message)
        {
            if (!EnableDiagnosticLogging)
                return;

            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                if (outputWindow == null)
                    return;

                Guid buildPaneGuid = Microsoft.VisualStudio.VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid;
                IVsOutputWindowPane buildPane;

                outputWindow.GetPane(ref buildPaneGuid, out buildPane);
                if (buildPane != null)
                {
                    buildPane.Activate();
                    buildPane.OutputStringThreadSafe(message + Environment.NewLine);
                }
            }
            catch
            {
                // If we can't write to output window, just silently continue
            }
        }

        /// <summary>
        /// Writes a user-facing message to the Visual Studio Build output window (always enabled)
        /// </summary>
        /// <param name="message">Message to write</param>
        private void WriteUserMessage(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                if (outputWindow == null)
                    return;

                Guid buildPaneGuid = Microsoft.VisualStudio.VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid;
                IVsOutputWindowPane buildPane;

                outputWindow.GetPane(ref buildPaneGuid, out buildPane);
                if (buildPane != null)
                {
                    buildPane.Activate();
                    buildPane.OutputStringThreadSafe(message + Environment.NewLine);
                }
            }
            catch
            {
                // If we can't write to output window, just silently continue
            }
        }

        /// <summary>
        /// Your custom Quick Deploy Sql method
        /// </summary>
        private void QuickDeploySql()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Get the runsettings file and extract the connection string
                string runSettingsPath = GetRunSettingsFilePath();

                // If no runsettings file exists, offer to create one
                if (string.IsNullOrEmpty(runSettingsPath))
                {
                    var result = VsShellUtilities.ShowMessageBox(
                        this.package,
                        "No tSQLt.runsettings file found in the solution. Would you like to create one?",
                        "SSDT Extensions",
                        OLEMSGICON.OLEMSGICON_QUERY,
                        OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                    if (result == 6) // IDYES
                    {
                        runSettingsPath = CreateDefaultRunSettingsFile();
                        if (!string.IsNullOrEmpty(runSettingsPath))
                        {
                            VsShellUtilities.ShowMessageBox(
                                this.package,
                                $"Created tSQLt.runsettings at:\n{runSettingsPath}\n\nPlease update the DatabaseConnectionString in this file with your database connection details.",
                                "SSDT Extensions",
                                OLEMSGICON.OLEMSGICON_INFO,
                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        }
                    }
                    return;
                }

                string connectionString = GetConnectionStringFromRunSettings(runSettingsPath);

                if (string.IsNullOrEmpty(connectionString))
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "Could not find DatabaseConnectionString in runsettings file.\n\nPlease ensure the <tSQLt><DatabaseConnectionString> element is present.",
                        "SSDT Extensions",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                // Get the active document text from the editor (unsaved changes included)
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument == null)
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "No active SQL file found.",
                        "SSDT Extensions",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                var textDocument = dte.ActiveDocument.Object("TextDocument") as EnvDTE.TextDocument;
                if (textDocument == null)
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "Could not access the active document text.",
                        "SSDT Extensions",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                // Check if connection string has changed - if so, reset approval
                if (lastApprovedConnectionString != connectionString)
                {
                    dontAskAgainForSession = false;
                }

                // Show approval dialog if not already approved for this session
                if (!dontAskAgainForSession)
                {
                    var dialog = new ApprovalDialog(connectionString);
                    var result = dialog.ShowDialog();

                    if (result != true || !dialog.Approved)
                    {
                        // User cancelled
                        return;
                    }

                    if (dialog.DontAskAgain)
                    {
                        dontAskAgainForSession = true;
                        lastApprovedConnectionString = connectionString;
                    }
                }

                // Read the SQL script from the editor (includes unsaved changes)
                var startPoint = textDocument.StartPoint.CreateEditPoint();
                string sqlScript = startPoint.GetText(textDocument.EndPoint);

                // Split into batches
                var batches = SplitIntoBatches(sqlScript);

                // Convert CREATE PROCEDURE to CREATE OR ALTER in each batch
                var modifiedBatches = batches.Select(b => ConvertCreateToCreateOrAlter(b)).ToList();

                // Extract object names before deployment
                var extractor = new SqlObjectNameExtractor();
                var objectNames = extractor.ExtractObjectNames(batches);

                // Execute the batches asynchronously on background thread
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteSqlBatchesAsync(connectionString, modifiedBatches);

                        // Log success to Build output window (back on UI thread)
                        await this.package.JoinableTaskFactory.SwitchToMainThreadAsync();

                        if (objectNames.Count > 0)
                        {
                            string objectList = string.Join(", ", objectNames);
                            WriteUserMessage($"Quick Deploy SQL: Successfully deployed {objectNames.Count} object(s): {objectList}");
                        }
                        else
                        {
                            WriteUserMessage($"Quick Deploy SQL: Successfully deployed {modifiedBatches.Count} batch(es) to database.");
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        // Show error on UI thread
                        await this.package.JoinableTaskFactory.SwitchToMainThreadAsync();
                        VsShellUtilities.ShowMessageBox(
                            this.package,
                            $"SQL Error: {sqlEx.Message}",
                            "SSDT Extensions - Deployment Failed",
                            OLEMSGICON.OLEMSGICON_CRITICAL,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                    catch (Exception ex)
                    {
                        // Show error on UI thread
                        await this.package.JoinableTaskFactory.SwitchToMainThreadAsync();
                        VsShellUtilities.ShowMessageBox(
                            this.package,
                            $"Error: {ex.Message}",
                            "SSDT Extensions - Error",
                            OLEMSGICON.OLEMSGICON_CRITICAL,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                }).FileAndForget("SSDTExtensions/QuickDeploySql");
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error: {ex.Message}",
                    "SSDT Extensions - Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
