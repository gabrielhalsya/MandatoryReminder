namespace MandatoryReminder;

public partial class App : System.Windows.Application
{
    private TrayApp? _trayApp;

    private void Application_Startup(object sender, System.Windows.StartupEventArgs e)
    {
        _trayApp = new TrayApp();
        _trayApp.Initialize();
    }

    private void Application_Exit(object sender, System.Windows.ExitEventArgs e)
    {
        _trayApp?.Dispose();
    }
}
