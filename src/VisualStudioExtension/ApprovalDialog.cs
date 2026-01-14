using System.Windows;
using System.Windows.Controls;

namespace SSDTExtensions
{
    /// <summary>
    /// Simple approval dialog for confirming deployment
    /// </summary>
    public class ApprovalDialog : Window
    {
        private CheckBox dontAskAgainCheckBox;
        public bool DontAskAgain { get; private set; }
        public bool Approved { get; private set; }

        public ApprovalDialog(string connectionString)
        {
            Title = "Quick Deploy SQL - Approval Required";
            Width = 500;
            Height = 250;
            MinWidth = 400;
            MinHeight = 200;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;

            var grid = new Grid();
            grid.Margin = new Thickness(10);

            // Define rows
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Label
            var label = new TextBlock
            {
                Text = "Deploy to the following database?",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            // Connection string display
            var connectionStringTextBox = new TextBox
            {
                Text = connectionString,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(5),
                Background = System.Windows.Media.Brushes.LightGray
            };
            Grid.SetRow(connectionStringTextBox, 1);
            grid.Children.Add(connectionStringTextBox);

            // Don't ask again checkbox
            dontAskAgainCheckBox = new CheckBox
            {
                Content = "Don't ask again for this session",
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(dontAskAgainCheckBox, 2);
            grid.Children.Add(dontAskAgainCheckBox);

            // Buttons panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 4);

            var approveButton = new Button
            {
                Content = "Approve",
                Width = 80,
                Height = 25,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            approveButton.Click += (s, e) =>
            {
                Approved = true;
                DontAskAgain = dontAskAgainCheckBox.IsChecked == true;
                DialogResult = true;
                Close();
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 25,
                IsCancel = true
            };
            cancelButton.Click += (s, e) =>
            {
                Approved = false;
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(approveButton);
            buttonPanel.Children.Add(cancelButton);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}
