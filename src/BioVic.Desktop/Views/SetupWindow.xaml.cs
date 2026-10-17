using System.Windows;
using BioVic.Application.DTOs;
using BioVic.Desktop.Services;

namespace BioVic.Desktop.Views;

public partial class SetupWindow : Window
{
    private readonly BioVicApiClient _client;
    public AuthSessionDto? Session { get; private set; }

    public SetupWindow(BioVicApiClient client)
    {
        InitializeComponent();
        _client = client;
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        CreateButton.IsEnabled = false;
        ErrorText.Text = string.Empty;
        try
        {
            Session = await _client.CreateMasterAsync(new(
                FirstNameBox.Text, LastNameBox.Text, UsernameBox.Text, PasswordBox.Password,
                ConfirmPasswordBox.Password, EmailBox.Text));
            DialogResult = true;
        }
        catch (Exception exception) { ErrorText.Text = exception.Message; }
        finally { CreateButton.IsEnabled = true; }
    }
}
