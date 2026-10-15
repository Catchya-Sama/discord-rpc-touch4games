using System.Threading;
using System.Windows;

namespace TouchRpc.App;

public partial class App : System.Windows.Application
{
    private Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, "Local\\TouchRpc.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);
        MainWindow = new MainWindow();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
