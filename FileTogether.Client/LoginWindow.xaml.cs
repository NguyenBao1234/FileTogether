using System.Windows;
using System.Windows.Input;

namespace FileTogether.Client;

public partial class LoginWindow : Window
{
    public FTPClient Client { get; private set; }
    public bool LoginSuccessful { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        PerformLogin();
    }
    
    private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PerformLogin();
        }
    }

    private void PerformLogin()
    {
        string serverIP = txtServerIP.Text.Trim();
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Password;
            
        if (string.IsNullOrEmpty(username))
        {
            txtStatus.Text = "Please enter username";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            return;
        }
            
        if (string.IsNullOrEmpty(password))
        {
            txtStatus.Text = "Please enter password";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            return;
        }
            
        if (!int.TryParse(txtPort.Text, out int port))
        {
            txtStatus.Text = "Invalid port number";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            return;
        }
            
        // Disable controls
        btnLogin.IsEnabled = false;
        txtStatus.Text = "Connecting...";
        txtStatus.Foreground = System.Windows.Media.Brushes.Blue;
        
        try
        {
            Client = new FTPClient();
                
            // Kết nối đến server
            bool connected = Client.Connect(serverIP, port);
                
            if (!connected)
            {
                txtStatus.Text = "Failed to connect to server";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                btnLogin.IsEnabled = true;
                return;
            }
                
            txtStatus.Text = "Authenticating...";
                
            // Login
            var loginResponse = Client.Login(username, password);
                
            if (loginResponse.Success)
            {
                LoginSuccessful = true;
                txtStatus.Text = "Login successful!";
                txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    
                // Đóng window sau 500ms
                Task.Delay(500).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => DialogResult = true);
                });
            }
            else
            {
                txtStatus.Text = loginResponse.Message;
                txtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                Client.Disconnect();
                Client = null;
                btnLogin.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Error: {ex.Message}";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            Client?.Disconnect();
            Client = null;
            btnLogin.IsEnabled = true;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        var registerWindow = new RegisterWindow();
        registerWindow.Owner = this;
        
        bool? result = registerWindow.ShowDialog();
        Console.WriteLine("Register Window = " + result);

        if (result != true) return;
        txtStatus.Text = "Registration successful! Please login.";
        txtStatus.Foreground = System.Windows.Media.Brushes.Green;
    }
}