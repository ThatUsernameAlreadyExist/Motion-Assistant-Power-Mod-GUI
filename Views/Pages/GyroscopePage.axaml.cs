using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using PmGui.ViewModels;
using PmGui.Managers;
using System.Reflection;

namespace PmGui.Views.Pages
{
    public partial class GyroscopePage : UserControl
    {
        public GyroscopePage()
        {
            InitializeComponent();
        }

        private void OnGyroscopeSensitivityDragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Event fired: {MethodBase.GetCurrentMethod()?.Name}");

            if (sender is Slider slider)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow?.DataContext is MainWindowViewModel mainWindowVm)
                    {
                        var gyroVm = mainWindowVm.GyroscopePageViewModel;
                        if (gyroVm != null)
                        {
                            GlobalAppManager.Instance.SendCmdGyroscopeSensitivity(slider.Value);
                        }
                    }
                }
            }
        }
    }
}
