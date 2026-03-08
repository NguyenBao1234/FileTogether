using System.Windows;

namespace FileTogether.Client;

public partial class PromptDialog : Window
{
    public PromptDialog(string message, string title)
    {
        InitializeComponent();

        Title = title;
        TxtMessage.Text = message;

        Loaded += (_, _) => TxtInput.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    public string GetInputText()
    {
        return TxtInput.Text;
    }
}