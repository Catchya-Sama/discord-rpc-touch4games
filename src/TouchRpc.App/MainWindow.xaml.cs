using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;

namespace TouchRpc.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await _viewModel.InitializeAsync();

    private async void Window_Closing(object? sender, CancelEventArgs e) => await _viewModel.DisposeAsync();

    private void Detect_Click(object sender, RoutedEventArgs e) => _viewModel.DetectProcesses();

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
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
}