using System.Collections.ObjectModel;
using BioVic.Application.DTOs;
using BioVic.Desktop.Commands;
using BioVic.Desktop.Services;
using BioVic.Domain.Enums;
using System.Net.Http;

namespace BioVic.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly BioVicApiClient _client;
    private string _searchText = string.Empty;
    private string _statusMessage = "Conectando con BioVic…";
    private string? _suggestion;
    private bool _isBusy;
    private bool _isAuthenticated;
    private bool _canEdit;
    private bool _isMaster;
    private string _currentUser = "Sesión requerida";

    public MainWindowViewModel(BioVicApiClient client)
    {
        _client = client;
        SearchCommand = new AsyncRelayCommand(SearchAsync, () => IsAuthenticated && !IsBusy);
        LoginCommand = new RelayCommand(_ => RequestLogin?.Invoke(this, EventArgs.Empty));
        LogoutCommand = new AsyncRelayCommand(LogoutAsync, () => IsAuthenticated && !IsBusy);
        NewEquipmentCommand = new RelayCommand(_ => RequestNewEquipment?.Invoke(this, EventArgs.Empty), _ => CanEdit);
        OpenDetailsCommand = new RelayCommand(item =>
        {
            if (item is EquipmentSearchItemDto equipment) RequestDetails?.Invoke(this, equipment);
        });
        UseSuggestionCommand = new AsyncRelayCommand(async () =>
        {
            if (!string.IsNullOrWhiteSpace(Suggestion)) SearchText = Suggestion;
            await SearchAsync();
        }, () => !string.IsNullOrWhiteSpace(Suggestion) && !IsBusy);
        GoToInventoryCommand = new RelayCommand(_ => RequestModule?.Invoke("Inventario"), _ => IsAuthenticated);
        GoToMaintenancesCommand = new RelayCommand(_ => RequestModule?.Invoke("Mantenimientos"), _ => IsAuthenticated);
        GoToCalibrationsCommand = new RelayCommand(_ => RequestModule?.Invoke("Calibraciones"), _ => IsAuthenticated);
        GoToVigilanceCommand = new RelayCommand(_ => RequestModule?.Invoke("Tecnovigilancia"), _ => IsAuthenticated);
        GoToDashboardCommand = new RelayCommand(_ => RequestAdmin?.Invoke(this, EventArgs.Empty), _ => IsMaster);
    }

    public ObservableCollection<EquipmentSearchItemDto> Results { get; } = [];
    public AsyncRelayCommand SearchCommand { get; }
    public RelayCommand LoginCommand { get; }
    public AsyncRelayCommand LogoutCommand { get; }
    public RelayCommand NewEquipmentCommand { get; }
    public RelayCommand OpenDetailsCommand { get; }
    public AsyncRelayCommand UseSuggestionCommand { get; }
    public RelayCommand GoToInventoryCommand { get; }
    public RelayCommand GoToMaintenancesCommand { get; }
    public RelayCommand GoToCalibrationsCommand { get; }
    public RelayCommand GoToVigilanceCommand { get; }
    public RelayCommand GoToDashboardCommand { get; }

    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public string? Suggestion
    {
        get => _suggestion;
        set
        {
            if (SetProperty(ref _suggestion, value)) UseSuggestionCommand.RaiseCanExecuteChanged();
        }
    }
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            SearchCommand.RaiseCanExecuteChanged();
            LogoutCommand.RaiseCanExecuteChanged();
            UseSuggestionCommand.RaiseCanExecuteChanged();
        }
    }
    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set
        {
            if (!SetProperty(ref _isAuthenticated, value)) return;
            SearchCommand.RaiseCanExecuteChanged();
            GoToInventoryCommand.RaiseCanExecuteChanged();
            GoToMaintenancesCommand.RaiseCanExecuteChanged();
            GoToCalibrationsCommand.RaiseCanExecuteChanged();
            GoToVigilanceCommand.RaiseCanExecuteChanged();
        }
    }
    public bool CanEdit
    {
        get => _canEdit;
        private set
        {
            if (SetProperty(ref _canEdit, value)) NewEquipmentCommand.RaiseCanExecuteChanged();
        }
    }
    public bool IsMaster
    {
        get => _isMaster;
        private set
        {
            if (SetProperty(ref _isMaster, value)) GoToDashboardCommand.RaiseCanExecuteChanged();
        }
    }
    public string CurrentUser { get => _currentUser; private set => SetProperty(ref _currentUser, value); }
    public bool HasSuggestion => !string.IsNullOrWhiteSpace(Suggestion);

    public event EventHandler? RequestSetup;
    public event EventHandler? RequestLogin;
    public event EventHandler? RequestNewEquipment;
    public event EventHandler<EquipmentSearchItemDto>? RequestDetails;
    public event Action<string>? RequestModule;
    public event EventHandler? RequestAdmin;

    public async Task InitializeAsync()
    {
        try
        {
            var setup = await _client.GetSetupStatusAsync();
            if (setup.RequiresMasterAdministrator) RequestSetup?.Invoke(this, EventArgs.Empty);
            await SearchAsync();
        }
        catch (HttpRequestException)
        {
            StatusMessage = $"No fue posible conectar con el servidor de BioVic en {_client.BaseUrl}. Verifique la conexión a Internet.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    public void CompleteAuthentication(AuthSessionDto session)
    {
        IsAuthenticated = true;
        CanEdit = session.Role is UserRole.Editor or UserRole.MasterAdministrator;
        IsMaster = session.Role == UserRole.MasterAdministrator;
        CurrentUser = $"{session.DisplayName} · {RoleLabel(session.Role)}";
        StatusMessage = "Sesión iniciada correctamente.";
    }

    public async Task RefreshAsync() => await SearchAsync();

    private async Task SearchAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _client.SearchEquipmentAsync(SearchText);
            Results.Clear();
            foreach (var equipment in result.Items) Results.Add(equipment);
            Suggestion = result.Suggestion;
            StatusMessage = result.TotalCount == 0
                ? "No encontramos equipos con ese criterio."
                : $"{result.TotalCount} equipo(s) encontrado(s).";
            OnPropertyChanged(nameof(HasSuggestion));
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally { IsBusy = false; }
    }

    private async Task LogoutAsync()
    {
        IsBusy = true;
        try { await _client.LogoutAsync(); }
        catch (Exception exception) { StatusMessage = exception.Message; }
        finally
        {
            IsAuthenticated = false;
            CanEdit = false;
            IsMaster = false;
            CurrentUser = "Sesión requerida";
            Results.Clear();
            Suggestion = null;
            StatusMessage = "Inicie sesión para consultar información.";
            IsBusy = false;
        }
        RequestLogin?.Invoke(this, EventArgs.Empty);
    }

    private static string RoleLabel(UserRole role) => role switch
    {
        UserRole.MasterAdministrator => "Administrador Maestro",
        UserRole.Editor => "Editor",
        _ => "Consulta"
    };
}
