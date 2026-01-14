using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Diagnostics;

namespace SSDTExtensions
{
    /// <summary>
    /// Simple download-complete dialog styled like ApprovalDialog
    /// </summary>
    public class DownloadCompleteDialog : Window
    {
        public DownloadCompleteDialog(string targetPath)
        {
            Title = "SSDT Extensions";
            Width = 520;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid
            {
                Margin = new Thickness(10)
            };

            // Define rows
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Label
            var label = new TextBlock
            {
                Text = "Download complete",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            // Path display
            var pathTextBox = new TextBox
            {
                Text = targetPath,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(5),
                Background = Brushes.LightGray
            };
            Grid.SetRow(pathTextBox, 1);
            grid.Children.Add(pathTextBox);

            // Informational text
            var infoText = new TextBlock
            {
                Text = "The test adapter was downloaded successfully. You can configure your runsettings to use it.",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(infoText, 2);
            grid.Children.Add(infoText);

            // Buttons panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 3);

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 25,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += (s, e) =>
            {
                DialogResult = true;
                Close();
            };

            var showButton = new Button
            {
                Content = "Show folder",
                Width = 110,
                Height = 25
            };
            showButton.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", targetPath) { UseShellExecute = true });
                }
                catch { }
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(showButton);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}
