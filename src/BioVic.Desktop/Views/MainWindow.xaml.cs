using System.Windows;
using System.Windows.Input;
using BioVic.Application.DTOs;
using BioVic.Desktop.Services;
using BioVic.Desktop.ViewModels;

namespace BioVic.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly BioVicApiClient _client;
    private readonly MainWindowViewModel _viewModel;
    private readonly SignedUpdateService _updateService;

    public MainWindow(BioVicApiClient client, MainWindowViewModel viewModel, SignedUpdateService updateService)
    {
        InitializeComponent();
        _client = client;
        _viewModel = viewModel;
        _updateService = updateService;
        DataContext = viewModel;
        Loaded += MainWindow_Loaded;
        viewModel.RequestSetup += ViewModel_RequestSetup;
        viewModel.RequestLogin += ViewModel_RequestLogin;
        viewModel.RequestNewEquipment += ViewModel_RequestNewEquipment;
        viewModel.RequestDetails += ViewModel_RequestDetails;
        viewModel.RequestModule += ViewModel_RequestModule;
        viewModel.RequestAdmin += ViewModel_RequestAdmin;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        await _updateService.CheckAndOfferAsync(this);
    }

    private void ViewModel_RequestSetup(object? sender, EventArgs e)
    {
        var window = new SetupWindow(_client) { Owner = this };
        if (window.ShowDialog() == true && window.Session is not null) _viewModel.CompleteAuthentication(window.Session);
    }

    private async void ViewModel_RequestLogin(object? sender, EventArgs e)
    {
        var window = new LoginWindow(_client) { Owner = this };
        if (window.ShowDialog() == true && window.Session is not null)
        {
            _viewModel.CompleteAuthentication(window.Session);
            await _viewModel.RefreshAsync();
        }
    }

    private async void ViewModel_RequestNewEquipment(object? sender, EventArgs e)
    {
        var window = new EquipmentEditorWindow(_client, _viewModel.IsMaster) { Owner = this };
        if (window.ShowDialog() == true) await _viewModel.RefreshAsync();
    }

    private void ViewModel_RequestDetails(object? sender, EquipmentSearchItemDto equipment)
    {
        var window = new EquipmentDetailsWindow(_client, equipment.Id, _viewModel.CanEdit, _viewModel.IsMaster) { Owner = this };
        window.ShowDialog();
    }

    private void ViewModel_RequestModule(string module)
    {
        Window window = module switch
        {
            "Mantenimientos" => new MaintenanceModuleWindow(_client, _viewModel.CanEdit, _viewModel.IsMaster),
            "Calibraciones" => new CalibrationModuleWindow(_client, _viewModel.CanEdit, _viewModel.IsMaster),
            "Tecnovigilancia" => new TechnoVigilanceModuleWindow(_client, _viewModel.CanEdit, _viewModel.IsMaster),
            _ => new ManagementModuleWindow(_client, "Inventario", _viewModel.CanEdit, _viewModel.IsMaster)
        };
        window.Owner = this; window.ShowDialog();
    }

    private void ViewModel_RequestAdmin(object? sender, EventArgs e)
    {
        new AdminWindow(_client) { Owner = this }.ShowDialog();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel.SearchCommand.CanExecute(null)) _viewModel.SearchCommand.Execute(null);
    }
}
