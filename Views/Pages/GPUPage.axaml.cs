using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PmGui.Resources.Localization;
using PmGui.ViewModels;
using PmGui.Managers;
using System;
using PmGui.ViewModels.Pages;
using System.Reflection;

namespace PmGui.Views.Pages
{
    public partial class GPUPage : UserControl
    {
        public GPUPage()
        {
            InitializeComponent();
            
            // Subscribe to localization changes
            LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
        }

        private void OnLocalizationChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Refresh view model when language changes
            if (DataContext is GPUPageViewModel viewModel)
            {
                viewModel.RefreshTranslations();
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            // Initialize view model if not already set through parent window
            if (DataContext == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.DataContext is MainWindowViewModel mainWindowVm)
                {
                    DataContext = mainWindowVm.GPUPageViewModel;
                }
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            
            // Unsubscribe from localization changes
            LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
        }
    }
}
