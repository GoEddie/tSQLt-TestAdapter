using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace SSDTExtensions
{
    internal sealed class DownloadTestAdapterCommand
    {
        public const int CommandId = 0x0102;
        public static readonly Guid CommandSet = new Guid("a5e8c7d3-6b4f-4a9c-8e3d-2f1b5c7d9e4a");

        private readonly AsyncPackage package;
        private const string PackageName = "tSQLtVisualStudio2022.TestAdapter";

        private DownloadTestAdapterCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static DownloadTestAdapterCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new DownloadTestAdapterCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Determine default extension directory for initial folder selection
            string extensionDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Prompt user for target folder on UI thread
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Choose folder to download the test adapter to.";
                dlg.SelectedPath = extensionDir;
                var dr = dlg.ShowDialog();
                if (dr != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.SelectedPath))
                {
                    return; // user cancelled
                }

                string userChosenDir = dlg.SelectedPath;

                // Run download asynchronously to avoid freezing UI
                this.package.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        // Get the latest version from NuGet
                        string latestVersion = await GetLatestVersionAsync();

                        // Create versioned subdirectory
                        string testAdapterDir = Path.Combine(userChosenDir, "TestAdapter", latestVersion);

                        // Create TestAdapter directory if it doesn't exist
                        if (!Directory.Exists(testAdapterDir))
                        {
                            Directory.CreateDirectory(testAdapterDir);
                        }

                        // Download and extract package
                        await DownloadAndExtractPackageAsync(testAdapterDir, latestVersion);

                        // Show success on UI thread with an explicit "Show folder" button
                        await this.package.JoinableTaskFactory.SwitchToMainThreadAsync();

                        // Point to the build\net472 folder where the actual DLLs are
                        string buildFolder = Path.Combine(testAdapterDir, "build", "net472");

                        // Show a WPF dialog styled like ApprovalDialog so it matches other dialogs
                        var completionDialog = new DownloadCompleteDialog(buildFolder);
                        // Make sure the dialog is owned by the foreground window so it isn't hidden
                        new WindowInteropHelper(completionDialog).Owner = GetForegroundWindow();
                        completionDialog.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        // Show error on UI thread
                        await this.package.JoinableTaskFactory.SwitchToMainThreadAsync();
                        VsShellUtilities.ShowMessageBox(
                            this.package,
                            $"Error downloading test adapter: {ex.Message}",
                            "SSDT Extensions",
                            OLEMSGICON.OLEMSGICON_CRITICAL,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                }).FileAndForget("SSDTExtensions/DownloadTestAdapter");
            }
        }

        private async Task<string> GetLatestVersionAsync()
        {
            using (var httpClient = new HttpClient())
            {
                // Query NuGet v3 API for latest version
                string indexUrl = "https://api.nuget.org/v3/index.json";
                var indexResponse = await httpClient.GetStringAsync(indexUrl);

                // Parse JSON to find registration base URL
                using (JsonDocument indexDoc = JsonDocument.Parse(indexResponse))
                {
                    string registrationBase = null;

                    // Look for RegistrationsBaseUrl in resources
                    foreach (var resource in indexDoc.RootElement.GetProperty("resources").EnumerateArray())
                    {
                        if (resource.TryGetProperty("@type", out var typeElement))
                        {
                            var type = typeElement.GetString();
                            if (type == "RegistrationsBaseUrl/3.6.0" || type == "RegistrationsBaseUrl")
                            {
                                registrationBase = resource.GetProperty("@id").GetString();
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(registrationBase))
                    {
                        throw new Exception("Could not find registration base URL in NuGet index");
                    }

                    // Get package metadata
                    string metadataUrl = $"{registrationBase}{PackageName.ToLowerInvariant()}/index.json";
                    var metadataResponse = await httpClient.GetStringAsync(metadataUrl);

                    // Parse package metadata to find latest version
                    using (JsonDocument metadataDoc = JsonDocument.Parse(metadataResponse))
                    {
                        string latestVersion = null;

                        // Navigate through the catalog to find the latest version
                        foreach (var item in metadataDoc.RootElement.GetProperty("items").EnumerateArray())
                        {
                            if (item.TryGetProperty("items", out var catalogItems))
                            {
                                foreach (var catalogItem in catalogItems.EnumerateArray())
                                {
                                    var version = catalogItem.GetProperty("catalogEntry").GetProperty("version").GetString();
                                    latestVersion = version; // Last one will be the latest
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(latestVersion))
                        {
                            throw new Exception("Could not find version information for package");
                        }

                        return latestVersion;
                    }
                }
            }
        }

        private async Task DownloadAndExtractPackageAsync(string targetDirectory, string version)
        {
            using (var httpClient = new HttpClient())
            {
                // Download from nuget.org
                string downloadUrl = $"https://www.nuget.org/api/v2/package/{PackageName}/{version}";

                var response = await httpClient.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();

                // Save to temp file
                string tempNupkgPath = Path.Combine(Path.GetTempPath(), $"{PackageName}.{version}.nupkg");

                using (var fileStream = File.Create(tempNupkgPath))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                // Extract .nupkg (it's a ZIP file)
                string tempExtractDir = Path.Combine(Path.GetTempPath(), $"{PackageName}_extracted");
                if (Directory.Exists(tempExtractDir))
                {
                    Directory.Delete(tempExtractDir, true);
                }

                ZipFile.ExtractToDirectory(tempNupkgPath, tempExtractDir);

                // Copy contents to TestAdapter directory
                CopyDirectory(tempExtractDir, targetDirectory, true);

                // Cleanup
                File.Delete(tempNupkgPath);
                Directory.Delete(tempExtractDir, true);
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                return;

            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
    }
}
