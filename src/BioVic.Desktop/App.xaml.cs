using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using BioVic.Desktop.Services;
using BioVic.Desktop.ViewModels;
using BioVic.Desktop.Views;
using BioVic.Application.DTOs;

namespace BioVic.Desktop;

public partial class App : System.Windows.Application
{
    private BioVicApiClient? _client;
    private SignedUpdateService? _updateService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Durante el arranque se cierran el splash y las ventanas modales antes de
        // crear MainWindow. Evita que WPF termine el proceso al cerrar la única
        // ventana visible.
        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show("ERROR TÉCNICO:\n" + args.Exception.ToString(),
                "BioVic - Error Real", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // 1. CREAR Y MOSTRAR EL SPLASH SCREEN (La animación) INMEDIATAMENTE
        var splash = new SplashScreenWindow();
        splash.Show();

        // Forzar a Windows a pintar la animación en pantalla al instante
        await Task.Delay(50);
        Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

        var settings = ClientConfiguration.Load();
        var apiUrl = settings.ApiUrl;
        _client = new BioVicApiClient(apiUrl);
        _updateService = new SignedUpdateService(settings);

        // 2. Conectarse a la API en segundo plano mientras el icono se anima
        bool connected = false;
        SetupStatusDto? setupStatus = null;
        int retries = 5;

        while (retries > 0 && !connected)
        {
            try
            {
                splash.UpdateStatus($"Conectando con el servidor... ({retries})");
                setupStatus = await _client.GetSetupStatusAsync();
                connected = true;
            }
            catch
            {
                retries--;
                await Task.Delay(1000); // Esperar 1 segundo antes de reintentar
            }
        }

        // 3. Pequeña pausa estética de 1.5 segundos para que alcances a ver la animación fluida
        await Task.Delay(1500);

        if (!connected)
        {
            splash.Close();
            MessageBox.Show($"No fue posible conectar con el servidor de BioVic en:\n{apiUrl}\n\n" +
                "Compruebe la conexión a Internet e inténtelo nuevamente. Si el problema continúa, contacte al administrador.",
                "BioVic - Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // 4. ¡Conexión exitosa! Abrir la interfaz principal y cerrar la animación
        splash.UpdateStatus("¡Conexión exitosa! Abriendo sistema...");
        await Task.Delay(500);

        splash.Close();
        AuthSessionDto? session;
        if (setupStatus?.RequiresMasterAdministrator == true)
        {
            var setupWindow = new SetupWindow(_client);
            if (setupWindow.ShowDialog() != true || setupWindow.Session is null) { Shutdown(); return; }
            session = setupWindow.Session;
        }
        else
        {
            var loginWindow = new LoginWindow(_client);
            if (loginWindow.ShowDialog() != true || loginWindow.Session is null) { Shutdown(); return; }
            session = loginWindow.Session;
        }

        var viewModel = new MainWindowViewModel(_client);
        viewModel.CompleteAuthentication(session);
        var mainWindow = new MainWindow(_client, viewModel, _updateService);
        MainWindow = mainWindow;

        mainWindow.Show(); // Abre tu hermosa interfaz de inventario
        ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _client?.Dispose();
        _updateService?.Dispose();
        base.OnExit(e);
    }
}
