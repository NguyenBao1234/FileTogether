using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using FileTogether.Core;
using Microsoft.Win32;

namespace FileTogether.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    FTPClient _client;
    List<FileInfo>? _currentFiles;
    public MainWindow()
    {
        InitializeComponent();
        _client = new FTPClient();
        _client.OnLog += (msg) => Dispatcher.Invoke( () => AppendLogUI(msg));
        _client.OnConnectionChanged += (bConnect)  => Dispatcher.Invoke( () => UpdateConnecctionStateUI(bConnect));
    }

    private void UpdateConnecctionStateUI(bool bConnect)
    {
        TxtConnectionStatus.Text = bConnect ? "Connected" : "Disconnected";
        TxtConnectionStatus.Foreground = bConnect ? Brushes.Green : Brushes.Red;
        BtnConnect.IsEnabled = !bConnect;
        BtnDisconnect.IsEnabled = bConnect;
        TxtServerIP.IsEnabled = !bConnect;
        TxtPort.IsEnabled = !bConnect;
        BtnRefresh.IsEnabled = bConnect;
        BtnDownload.IsEnabled = bConnect;
        BtnUpload.IsEnabled = bConnect;
        BtnDelete.IsEnabled = bConnect;
        
    }

    private void AppendLogUI(string message)
    {
        TxtBoxLogs.AppendText(message + Environment.NewLine);
        TxtBoxLogs.ScrollToEnd();
    }

    private void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        var serverIP = TxtServerIP.Text.Trim();
        if (!int.TryParse(TxtPort.Text, out int port) || string.IsNullOrEmpty(serverIP))
        {
            MessageBox.Show("Invalid port number or server ip", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        Console.WriteLine("Starting connection...");
        bool bConnectSuccess = _client.Connect(serverIP, port);
        if (bConnectSuccess)
        {
            //RefreshFileList();
            Console.WriteLine("Successfully connected to server");
        }
        else
        {
            Console.WriteLine("Failed to connect to server");
            MessageBox.Show("Failed to connect to server", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
    {
        _client.Disconnect();
        FileDG.ItemsSource = null;
        _currentFiles = null;
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshFileList();
    }

    private void BtnDownload_Click(object sender, RoutedEventArgs e)
    {
        if (FileDG.SelectedItem is FileDisplayInfo selectedItem)
        {
            FileInfo selectedFile = selectedItem.OriginalFile;
            
            var dialog = new SaveFileDialog
            {
                FileName = selectedFile.FileName,
                Filter = "All Files (*.*)|*.*"
            };
            
            if (dialog.ShowDialog() == true)
            {
                var progress = new Progress<int>(percent =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value = percent;
                        TxtProgress.Text = $"{percent}%";
                    });
                });
                    
                bool success = _client.DownloadFile(selectedFile.FileName, dialog.FileName, progress);
                    
                if (success)
                {
                    MessageBox.Show($"Downloaded successfully to:\n{dialog.FileName}", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Download failed", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                    
                // Reset progress bar
                ProgressBar.Value = 0;
                TxtProgress.Text = "0%";
            }
        }
        else
        {
            MessageBox.Show("Please select a file to download", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnUpload_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "All Files (*.*)|*.*",
            Multiselect = false
        };
        
        if (dialog.ShowDialog() == true)
        {
            var progress = new Progress<int>(percent =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = percent;
                    TxtProgress.Text = $"{percent}%";
                });
            });
            
            bool success = _client.UploadFile(dialog.FileName, progress);
            
            if (success)
            {
                MessageBox.Show("Uploaded successfully", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshFileList();
            }
            else
            {
                MessageBox.Show("Upload failed", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            // Reset progress bar
            ProgressBar.Value = 0;
            TxtProgress.Text = "0%";
        }
        
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (FileDG.SelectedItem is FileDisplayInfo selectedItem)
        {
            FileInfo selectedFile = selectedItem.OriginalFile;
        
            var decision = MessageBox.Show(
                $"Are you sure you want to delete '{selectedFile.FileName}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (decision != MessageBoxResult.Yes) return;
            bool success = _client.DeleteFile(selectedFile.FileName);
                    
            if (success)
            {
                MessageBox.Show("Deleted successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshFileList();
            }
            else
            {
                MessageBox.Show("Delete failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show("Please select a file to delete", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private void RefreshFileList()
    {
        if (!_client.IsConnected)
        {
            MessageBox.Show("Not connected to server", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
            
        _currentFiles = _client.GetFileList();
            
        if (_currentFiles != null)
        {
            // Bind to DataGrid with FormattedSize property
            var displayFiles = _currentFiles.Select(f => new FileDisplayInfo(f) ).ToList();
                
            FileDG.ItemsSource = displayFiles;//Data grid
        }

    }
}