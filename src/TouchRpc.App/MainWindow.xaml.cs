using System.ComponentModel;
using System.Drawing;
using System.Windows;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace TouchRpc.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();
    private readonly Forms.NotifyIcon _trayIcon;
    private bool _exitRequested;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        var trayMenu = new Forms.ContextMenuStrip();
        trayMenu.Items.Add("Open TOUCH RPC", null, (_, _) => ShowFromTray());
        trayMenu.Items.Add("Exit", null, async (_, _) => await ExitAsync());
        _trayIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = trayMenu,
            Icon = SystemIcons.Application,
            Text = "TOUCH RPC",
            Visible = true,
        };
        _trayIcon.DoubleClick += (_, _) => ShowFromTray();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await _viewModel.InitializeAsync();

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_exitRequested) return;

        e.Cancel = true;
        if (_viewModel.MinimizeToTray)
        {
            Hide();
            await _viewModel.SaveSettingsAsync();
            return;
        }

        await ShutdownAsync();
    }

    private void Detect_Click(object sender, RoutedEventArgs e) => _viewModel.DetectProcesses();

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select TOUCH output_log.txt",
            Filter = "TOUCH log (output_log.txt)|output_log.txt|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dialog.ShowDialog(this) == true) _viewModel.LogPath = dialog.FileName;
    }

    private async void Start_Click(object sender, RoutedEventArgs e) => await _viewModel.StartAsync();

    private void Stop_Click(object sender, RoutedEventArgs e) => _viewModel.Stop();

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private async Task ExitAsync()
    {
        if (!Dispatcher.CheckAccess())
        {
            await await Dispatcher.InvokeAsync(ExitAsync);
            return;
        }

        await ShutdownAsync();
    }

    private async Task ShutdownAsync()
    {
        if (_exitRequested) return;

        _exitRequested = true;
        await _viewModel.SaveSettingsAsync();
        await _viewModel.DisposeAsync();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Close();
    }
}