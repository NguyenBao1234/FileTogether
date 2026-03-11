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
using MaterialDesignThemes.Wpf;
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
        
        string defaultFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        TxtSharedFolder.Text = "E:\\Applications and Programs\\JetBrains Rider\\Project\\FileTogether\\TestSharedFolder";
            //System.IO.Path.Combine(defaultFolder, "FTPServerFiles");
        TxtUserFolder.Text = System.IO.Path.Combine(defaultFolder, "FTPServerUser");
        AppendLog("[SERVER LOG]");
            
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
    
    
    private void BtnBrowseUser_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new FolderBrowserDialog();
        dialog.SelectedPath = TxtUserFolder.Text;

            
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtUserFolder.Text = dialog.SelectedPath;
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
           
            _server.OnClientCountChanged += (amount) => Dispatcher.Invoke(() =>
            {
                if (amount == 0) TxtClientCount.Text = amount.ToString();// in Stop server case
                else
                {
                    int current = int.Parse(TxtClientCount.Text);
                    TxtClientCount.Text = (current + amount).ToString();
                }
                
            });
                
            _server.Start();
                
            // Update UI
            TxtStatus.Text = "Running";
            TxtStatus.Foreground = System.Windows.Media.Brushes.Green;
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            TxtPort.IsEnabled = false;
            TxtSharedFolder.IsEnabled = false;
            TxtUserFolder.IsEnabled = false;
            BtnBrowse.IsEnabled = false;
            BtnBrowseUser.IsEnabled = false;
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
        TxtUserFolder.IsEnabled = true;
        BtnBrowse.IsEnabled = true;
        BtnBrowseUser.IsEnabled = true;
        TxtClientCount.Text = "0";
    }

    private void DarkThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        PaletteHelper paletteHelper = new PaletteHelper();
        Theme theme = paletteHelper.GetTheme();

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