using System.Configuration;
using System.Data;
using System.Windows;

namespace DesktopImgFrame
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            FrameService.ServiceInstance = new();
            FrameService.ServiceInstance?.Start();
            new Window() { Visibility = Visibility.Hidden }.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            FrameService.ServiceInstance?.Stop();
        }
    }

}
