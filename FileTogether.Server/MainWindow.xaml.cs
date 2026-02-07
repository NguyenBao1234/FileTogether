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
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;

namespace FileTogether.Server;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private FTPServer _server;

    public MainWindow()
    {
        InitializeComponent();
        
        string defaultFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "FTPServerFiles");
            TxtSharedFolder.Text = defaultFolder;
            
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        dialog.SelectedPath = TxtSharedFolder.Text;

            
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtSharedFolder.Text = dialog.SelectedPath;
        }
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int port = int.Parse(TxtPort.Text);
            string sharedFolder = TxtSharedFolder.Text;
                
            if (string.IsNullOrWhiteSpace(sharedFolder))
            {
                MessageBox.Show("Please select a shared folder", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
                
            _server = new FTPServer(port, sharedFolder);
                
            // Subscribe events
            _server.OnLog += (msg) => Dispatcher.Invoke(() => AppendLog(msg));
            _server.OnClientCountChanged += (count) => Dispatcher.Invoke(() => TxtClientCount.Text = count.ToString());
                
            _server.Start();
                
            // Update UI
            TxtStatus.Text = "Running";
            TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            TxtPort.IsEnabled = false;
            TxtSharedFolder.IsEnabled = false;
            BtnBrowse.IsEnabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    //Append into log box UI
    private void AppendLog(string message)
    {
        TxtBoxLogs.AppendText(message + Environment.NewLine);
        TxtBoxLogs.ScrollToEnd();
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _server?.Stop();
            
        // Update UI
        TxtStatus.Text = "Stopped";
        TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
        BtnStart.IsEnabled = true;
        BtnStop.IsEnabled = false;
        TxtPort.IsEnabled = true;
        TxtSharedFolder.IsEnabled = true;
        BtnBrowse.IsEnabled = true;
        TxtClientCount.Text = "0";
    }
}