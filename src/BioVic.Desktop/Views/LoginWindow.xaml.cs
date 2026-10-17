using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using BioVic.Application.DTOs;
using BioVic.Desktop.Services;

namespace BioVic.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly BioVicApiClient _client;
    private bool _isSigningIn;

    public AuthSessionDto? Session { get; private set; }

    public LoginWindow(BioVicApiClient client)
    {
        InitializeComponent();
        _client = client;
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e) => await SignInAsync();

    private async Task SignInAsync()
    {
        if (_isSigningIn)
            return;

        var username = UsernameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(PasswordBox.Password))
        {
            ErrorText.Text = "Escribe tu usuario y contraseña.";
            return;
        }

        _isSigningIn = true;
        SetBusyState(true);
        ErrorText.Text = string.Empty;

        try
        {
            Session = await _client.LoginAsync(new(username, PasswordBox.Password, Environment.MachineName));
            StatusText.Text = "Sesión iniciada. Abriendo BioVic...";
            DialogResult = true;
        }
        catch (BioVicApiException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            ErrorText.Text = "El usuario o la contraseña no son válidos.";
        }
        catch (BioVicApiException exception)
        {
            ErrorText.Text = exception.Message;
        }
        catch (TaskCanceledException)
        {
            ErrorText.Text = "El servidor tardó demasiado en responder. Intenta iniciar sesión nuevamente.";
        }
        catch (HttpRequestException)
        {
            ErrorText.Text = "No fue posible conectar con el servidor. Revisa tu conexión a Internet e inténtalo nuevamente.";
        }
        catch (Exception)
        {
            ErrorText.Text = "No fue posible iniciar sesión. Inténtalo nuevamente o contacta al administrador.";
        }
        finally
        {
            _isSigningIn = false;
            if (DialogResult != true)
                SetBusyState(false);
        }
    }

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        await SignInAsync();
    }

    private void SetBusyState(bool isBusy)
    {
        LoginButton.IsEnabled = !isBusy;
        LoginButton.Content = isBusy ? "Verificando..." : "Iniciar sesión";
        UsernameBox.IsEnabled = !isBusy;
        PasswordBox.IsEnabled = !isBusy;
        LoginProgress.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        StatusText.Text = isBusy
            ? "Validando credenciales con el servidor. Esto puede tardar unos segundos..."
            : string.Empty;
    }
}
