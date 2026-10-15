using System.Drawing;
using System.ComponentModel;
using System.Windows;
using Forms = System.Windows.Forms;

namespace TouchRpc.App;

public partial class MainWindow : Window
{
    private readonly TrayRuntime _runtime = new();
    private readonly Forms.NotifyIcon _trayIcon;
    private bool _initialized;
    private bool _exiting;

    public MainWindow()
    {
        InitializeComponent();
        var trayMenu = new Forms.ContextMenuStrip();
        trayMenu.Items.Add("Exit", null, async (_, _) => await ExitAsync());
        _trayIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = trayMenu,
            Icon = SystemIcons.Application,
            Text = "TOUCH RPC · Starting",
            Visible = true,
        };
        _trayIcon.MouseClick += TrayIcon_MouseClick;
        _runtime.StatusChanged += Runtime_StatusChanged;
        Dispatcher.BeginInvoke(async () => await InitializeAsync());
    }

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        await _runtime.InitializeAsync();
        EnabledSwitch.IsChecked = _runtime.IsEnabled;
        Hide();
    }

    private void TrayIcon_MouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button != Forms.MouseButtons.Left) return;
        Dispatcher.Invoke(() =>
        {
            if (IsVisible)
            {
                Hide();
                return;
            }

            var workArea = SystemParameters.WorkArea;
            Left = Math.Min(workArea.Right - Width, Forms.Cursor.Position.X - Width / 2);
            Top = workArea.Bottom - Height - 8;
            Show();
            Activate();
        });
    }

    private async void EnabledSwitch_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        await _runtime.SetEnabledAsync(EnabledSwitch.IsChecked == true);
    }

    private void Window_Deactivated(object sender, EventArgs e) => Hide();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_exiting) return;
        e.Cancel = true;
        Hide();
    }

    private void Runtime_StatusChanged(object? sender, string status)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var tooltip = $"TOUCH RPC · {status}";
            _trayIcon.Text = tooltip.Length <= 63 ? tooltip : tooltip[..63];
        });
    }

    private async Task ExitAsync()
    {
        if (_exiting) return;
        _exiting = true;
        await _runtime.DisposeAsync();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}