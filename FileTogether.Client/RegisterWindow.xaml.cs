using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FileTogether.Client;

public partial class RegisterWindow : Window
{
    public RegisterWindow()
    {
        InitializeComponent();
    }

    private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateInput();
    }

    private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ValidateInput();
    }

    private void TxtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ValidateInput();
    }

    private void TxtConfirmPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) PerformRegister();
    }

    private void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        PerformRegister();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void PerformRegister()
    {
        string serverIP = txtServerIP.Text.Trim();
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Password;
        string confirmPassword = txtConfirmPassword.Password;
        
        if (!ValidateInput()) return;
        
        if (!int.TryParse(txtPort.Text, out int port))
        {
            txtStatus.Text = "Invalid port number";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            return;
        }
            
        // Disable controls
        btnRegister.IsEnabled = false;
        txtStatus.Text = "Connecting to server...";
        txtStatus.Foreground = System.Windows.Media.Brushes.Blue;
        
        try
        {
            var client = new FTPClient();
            bool connected = client.Connect(serverIP, port);
                
            if (!connected)
            {
                txtStatus.Text = "Failed to connect to server";
                txtStatus.Foreground = Brushes.Red;
                btnRegister.IsEnabled = true;
                return;
            }
                
            txtStatus.Text = "Registering...";
                
            // Register
            var registerResponse = client.Register(username, password);
                
            client.Disconnect();
                
            if (registerResponse.Success)
            {
                MessageBox.Show($"{registerResponse.Message}\n\nYou can login with your new account now.",
                    "Registration Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                    
                DialogResult = true;
            }
            else
            {
                txtStatus.Text = registerResponse.Message;
                txtStatus.Foreground = Brushes.Red;
                btnRegister.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Error: {ex.Message}";
            txtStatus.Foreground = Brushes.Red;
            btnRegister.IsEnabled = true;
        }

    }
    
    
    private bool ValidateInput()
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Password;
        string confirmPassword = txtConfirmPassword.Password;
            
        // Username validation
        if (string.IsNullOrWhiteSpace(username))
        {
            txtStatus.Text = "";
            return false;
        }
            
        if (username.Length < 3)
        {
            txtStatus.Text = "Username must be at least 3 characters";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            return false;
        }
            
        if (username.Length > 20)
        {
            txtStatus.Text = "Username must not exceed 20 characters";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            return false;
        }
            
        // Password validation
        if (!string.IsNullOrEmpty(password) && password.Length < 6)
        {
            txtStatus.Text = "Password must be at least 6 characters";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            return false;
        }
            
        // Confirm password validation
        if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(confirmPassword))
        {
            if (password != confirmPassword)
            {
                txtStatus.Text = "Passwords do not match";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                return false;
            }
        }
            
        txtStatus.Text = "";
        return true;
    }
    
}