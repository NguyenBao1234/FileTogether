using System.ComponentModel;
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
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace FileTogether.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    FTPClient _client;
    List<ItemInfo>? _currentItems;
    public MainWindow()
    {
        InitializeComponent();
        ShowLoginWindow();
    }

    private void UpdateConnecctionStateUI(bool bConnect)
    {
        TxtConnectionStatus.Text = bConnect ? "Connected" : "Disconnected";
        TxtConnectionStatus.Foreground = bConnect ? Brushes.Green : Brushes.Red;

        TxtServerIP.IsEnabled = !bConnect;
        TxtPort.IsEnabled = !bConnect;
        BtnRefresh.IsEnabled = bConnect;

        
        txtCurrentUser.Text = _client.CurrentUser.Username;
        txtCurrentUser.Foreground = Brushes.Black;
            
        txtCurrentRole.Text = _client.CurrentUser.Role.ToString();
        txtCurrentRole.Foreground = GetRoleColor(_client.CurrentUser.Role);
            
        BtnLogout.IsEnabled = bConnect;
        BtnDownload.IsEnabled = bConnect;
        btnBack.IsEnabled = bConnect;
            
        // Chỉ enable upload/delete nếu có quyền
        if (!bConnect) return;
        BtnUpload.IsEnabled = _client.CurrentUser.Role >= UserRole.PowerUser;
        BtnDelete.IsEnabled = _client.CurrentUser.Role == UserRole.Admin;
        btnNewFolder.IsEnabled = _client.CurrentUser.Role >= UserRole.PowerUser;
    }

    private void AppendLogUI(string message)
    {
        TxtBoxLogs.AppendText(message + Environment.NewLine);
        TxtBoxLogs.ScrollToEnd();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshItemList();
    }

    private async void BtnDownload_Click(object sender, RoutedEventArgs e)
    {
        if (ItemDG.SelectedItem is ItemDisplayInfo selectedItem)
        {
            ItemInfo selectedFile = selectedItem.OriginalFile;
            
            var dialog = new SaveFileDialog
            {
                FileName = selectedFile.FileName,
                Filter = "All Files (*.*)|*.*"
            };
            
            if (dialog.ShowDialog() == true)
            {
                ProgressBar.Value = 0;
                TxtProgress.Text = "0%";
                TxtSpeed.Text = "0 KB/s";
                TxtETA.Text = "--";
                var progress = new Progress<TransferProgress>(transferProgress =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value = transferProgress.Percentage;
                        TxtProgress.Text = $"{transferProgress.Percentage}%";
                        TxtSpeed.Text = transferProgress.GetFormattedSpeed();
                        TxtETA.Text = transferProgress.GetFormattedETA();
                    });
                });

                bool success = await _client.DownloadFile(selectedFile.FileName, dialog.FileName, progress);
                    
                if (success)
                {
                    MessageBox.Show($"Downloaded successfully to:\n{dialog.FileName}", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Download failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private async void BtnUpload_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "All Files (*.*)|*.*",
            Multiselect = false
        };
        
        if (dialog.ShowDialog() == true)
        {
            var progress = new Progress<TransferProgress>(transferProgress =>
            {
                ProgressBar.Value = 0;
                TxtProgress.Text = "0%";
                TxtSpeed.Text = "0 KB/s";
                TxtETA.Text = "--";
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = transferProgress.Percentage;
                    TxtProgress.Text = $"{transferProgress.Percentage}%";
                    TxtSpeed.Text = transferProgress.GetFormattedSpeed();
                    TxtETA.Text = transferProgress.GetFormattedETA();
                });
            });
            
            bool success = await _client.UploadFile(dialog.FileName, progress);
            
            if (success)
            {
                MessageBox.Show("Uploaded successfully", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshItemList();
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
        if (ItemDG.SelectedItem is ItemDisplayInfo selectedItem)
        {
            ItemInfo selectedFile = selectedItem.OriginalFile;
        
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
                RefreshItemList();
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
    
    private void RefreshItemList()
    {
        Console.WriteLine("call Refresh file list");
        if (!_client.IsConnected)
        {
            MessageBox.Show("Not connected to server", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
            
        _currentItems = _client.GetItemList();
            
        if (_currentItems != null)
        {
            Console.WriteLine("items found");
            var displayItems = _currentItems.Select(f => new ItemDisplayInfo(f) ).ToList();
            
            // DEBUG: Kiểm tra Items
            Console.WriteLine($"ItemDG.Items.Count = {ItemDG.Items.Count}");
            Console.WriteLine($"ItemDG.ItemsSource is null? {ItemDG.ItemsSource == null}");

            
            ItemDG.ItemsSource = displayItems;//Data grid
            Console.WriteLine("set up display item finish ");
            Console.WriteLine($"after setup ItemDG.Items.Count = {ItemDG.Items.Count}");
            string path = _client.CurrentPath;
            txtCurrentPath.Text = string.IsNullOrEmpty(path) ? "/" : "/" + path.Replace("\\", "/");
        }
        else
        {
            MessageBox.Show("Session expire or lost connection","Response Request", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }

    private void DgFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemDG.SelectedItem is ItemDisplayInfo selectedItem)
        {
            if (selectedItem.OriginalFile.IsDirectory)
            {
                bool success = _client.ChangeCurrentDirectory(selectedItem.FileName);
                
                if(success) RefreshItemList();
                else 
                    MessageBox.Show("Change failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (_client == null || !_client.IsAuthenticated) return;
    
        if (string.IsNullOrEmpty(_client.CurrentPath))
        {
            MessageBox.Show("Already at root directory", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
    
        bool success = _client.ChangeCurrentDirectory("..");
    
        if (success) RefreshItemList();
        else
            MessageBox.Show("Failed to go back", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        
    }

    private void BtnNewFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_client == null || !_client.IsAuthenticated) return;
        var promptDialog = new PromptDialog("Enter folder name:", "New Folder");
        var result = promptDialog.ShowDialog();
        if (result == true)
        {
            var folderName = promptDialog.GetInputText();
            Console.WriteLine("new folder name request: " + folderName);
            if (string.IsNullOrEmpty(folderName)) return;
            
            var success = _client.CreateDirectory(folderName);
            if (success)
            {
                MessageBox.Show("Created successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshItemList();
            }
            else
            {
                MessageBox.Show("Create failed, session expired or lost connection", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
            MessageBox.Show("Unexpected Error",  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to logout?",
            "Confirm Logout",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
            
        if (result == MessageBoxResult.Yes)
        {
            _client?.Disconnect();
                
            // Hiển thị login window lại
            ShowLoginWindow();
        }
    }

    private void ShowLoginWindow()
    {
        var loginWindow = new LoginWindow();
        bool? result = loginWindow.ShowDialog();
            
        if (result == true && loginWindow.LoginSuccessful)
        {
            // Login thành công
            _client = loginWindow.Client;
            _client.OnLog += (msg) => Dispatcher.Invoke( () => AppendLogUI(msg));
            _client.OnConnectionChanged += (bConnect)  => Dispatcher.Invoke( () => UpdateConnecctionStateUI(bConnect));//call back if server off
            UpdateConnecctionStateUI(true);
            AppendLogUI("[CLIENT LOG]");
            // Load file list
            RefreshItemList();
        }
        else
        {
            // User cancel hoặc login fail → đóng app
            Application.Current.Shutdown();
        }
    }

    private Brush GetRoleColor(UserRole inUserRole)
    {
        return inUserRole switch
        {
            UserRole.Admin => Brushes.Green,
            UserRole.PowerUser => Brushes.Blue,
            _ => Brushes.Gray
        };
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Console.WriteLine("Close Application");
        _client?.Disconnect();
        base.OnClosing(e);
    }

    private void DarkThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        PaletteHelper paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        // Kiểm tra và đảo ngược Theme
        if (DarkThemeToggleButton.IsChecked == true)
        {
            theme.SetBaseTheme(BaseTheme.Dark);
        }
        else
        {
            theme.SetBaseTheme(BaseTheme.Light);
        }
        paletteHelper.SetTheme(theme);
    }
}