using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using PmGui.ViewModels;
using PmGui.ViewModels.Pages;
using System.Threading.Tasks;

namespace PmGui.Views.Pages
{
    public partial class ProcessProfilesPage : UserControl
    {
        public ProcessProfilesPage()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            // Set up view model event handler for input dialog
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.DataContext is MainWindowViewModel mainWindowVm)
                {
                    mainWindowVm.ProcessProfilesPageViewModel.RequestProfileName = async () =>
                    {
                        return await ShowInputDialogAsync(mainWindow);
                    };
                }
            }
        }

        private async Task<string> ShowInputDialogAsync(Window owner)
        {
            var localization = PmGui.Resources.Localization.LocalizationManager.Instance;
            
            var dialog = new Window
            {
                Title = localization["AddProfile"],
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 15
            };

            var label = new TextBlock
            {
                Text = localization["EnterProfileName"],
                FontSize = 14
            };

            var textBox = new TextBox
            {
                Watermark = localization["ProfileName"]
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            string result = null;
            var okButton = new Button
            {
                Content = localization["OK"],
                MinWidth = 80
            };
            okButton.Click += (s, e) =>
            {
                result = textBox.Text;
                dialog.Close();
            };

            var cancelButton = new Button
            {
                Content = localization["Cancel"],
                MinWidth = 80
            };
            cancelButton.Click += (s, e) =>
            {
                result = null;
                dialog.Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(label);
            panel.Children.Add(textBox);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;

            await dialog.ShowDialog(owner);

            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
    }
}
