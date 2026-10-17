using System.Windows;
using System.Windows.Controls;
using BioVic.Application.DTOs;
using BioVic.Desktop.Services;
using BioVic.Domain.Enums;

namespace BioVic.Desktop.Views;

public partial class AdminWindow : Window
{
    private readonly BioVicApiClient _client;
    private List<LocationDto> _locations = [];
    private List<LocationPermissionChoice> _createLocationChoices = [];
    private List<LocationPermissionChoice> _editLocationChoices = [];
    private MasterProfileDto? _masterProfile;
    private UserSummaryDto? SelectedUser => UsersGrid.SelectedItem as UserSummaryDto;
    private AdminNotificationDto? SelectedNotification => NotificationsGrid.SelectedItem as AdminNotificationDto;
    private DeletedItemDto? SelectedDeletedItem => RecoveryGrid.SelectedItem as DeletedItemDto;

    public AdminWindow(BioVicApiClient client)
    {
        InitializeComponent();
        _client = client;
        CreateRoleBox.ItemsSource = new[] { UserRole.Searcher, UserRole.Editor };
        EditRoleBox.ItemsSource = new[] { UserRole.Searcher, UserRole.Editor };
        CreateRoleBox.SelectedItem = UserRole.Searcher;
        Loaded += async (_, _) => await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        try
        {
            await LoadLocationsAsync();
            await Task.WhenAll(LoadDashboardAsync(), LoadUsersAsync(), LoadAuditAsync(), LoadNotificationsAsync(),
                LoadMasterProfileAsync(), LoadRecoveryAsync());
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async Task LoadDashboardAsync()
    {
        var d = await _client.GetDashboardAsync();
        DashboardText.Text = $"Equipos: {d.TotalEquipment}\nOperativos: {d.Operational} ({d.OperationalPercentage:0.0} %)\nFuera de servicio: {d.OutOfService}\nEn mantenimiento: {d.InMaintenance}\nMantenimientos vencidos: {d.OverdueMaintenance}\nMantenimientos próximos: {d.UpcomingMaintenance}\nCalibraciones vencidas: {d.OverdueCalibrations}\nIncidentes abiertos: {d.OpenIncidents}";
    }
    private async Task LoadUsersAsync() => UsersGrid.ItemsSource = await _client.GetUsersAsync();
    private async Task LoadLocationsAsync()
    {
        _locations = (await _client.GetLocationsAsync()).ToList();
        LocationsGrid.ItemsSource = _locations;
        _createLocationChoices = _locations.Select(item => new LocationPermissionChoice(item)).ToList();
        CreateLocationsList.ItemsSource = _createLocationChoices;
        RefreshEditLocationChoices(SelectedUser);
    }

    private async Task LoadNotificationsAsync() => NotificationsGrid.ItemsSource = await _client.GetAdminNotificationsAsync();
    private async Task LoadMasterProfileAsync()
    {
        _masterProfile = await _client.GetMasterProfileAsync();
        MasterFirstNameBox.Text = _masterProfile.FirstName;
        MasterLastNameBox.Text = _masterProfile.LastName;
        MasterUsernameBox.Text = _masterProfile.Username;
        MasterEmailBox.Text = _masterProfile.Email ?? string.Empty;
    }

    private async void SaveMasterProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_masterProfile is null)
        {
            MessageBox.Show(this, "No fue posible cargar la cuenta maestra.", "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SaveMasterProfileButton.IsEnabled = false;
        MasterProfileStatusText.Text = "Guardando los datos de la cuenta...";
        try
        {
            _masterProfile = await _client.UpdateMasterProfileAsync(new(
                MasterFirstNameBox.Text,
                MasterLastNameBox.Text,
                MasterUsernameBox.Text,
                MasterEmailBox.Text,
                MasterCurrentPasswordBox.Password,
                MasterNewPasswordBox.Password,
                MasterConfirmPasswordBox.Password));

            MasterCurrentPasswordBox.Clear();
            MasterNewPasswordBox.Clear();
            MasterConfirmPasswordBox.Clear();
            MasterProfileStatusText.Text = $"Cuenta actualizada correctamente. En el próximo ingreso utiliza el usuario '{_masterProfile.Username}'.";
            await LoadUsersAsync();
            await LoadAuditAsync();
        }
        catch (Exception ex)
        {
            MasterProfileStatusText.Text = string.Empty;
            MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            SaveMasterProfileButton.IsEnabled = true;
        }
    }

    private async Task LoadAuditAsync()
    {
        var result = await _client.GetAuditAsync(); AuditGrid.ItemsSource = result.Items;
        AuditStatusText.Text = $"{result.TotalCount} movimiento(s) registrados";
    }

    private async Task LoadRecoveryAsync()
    {
        var result = await _client.GetRecoverySummaryAsync();
        var items = result.Equipment.Concat(result.Users).Concat(result.Locations)
            .OrderByDescending(item => item.DeletedAtUtc).ToList();
        RecoveryGrid.ItemsSource = items;
        RecoveryStatusText.Text = items.Count == 0
            ? "No hay elementos eliminados lógicamente."
            : $"{items.Count} elemento(s) disponibles para restaurar.";
    }

    private async void RefreshRecovery_Click(object sender, RoutedEventArgs e)
    {
        try { await LoadRecoveryAsync(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic"); }
    }

    private async void RestoreDeletedItem_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedDeletedItem is null) { MessageBox.Show(this, "Seleccione un elemento.", "BioVic"); return; }
        if (string.IsNullOrWhiteSpace(RecoveryReasonBox.Text)) { MessageBox.Show(this, "Indique el motivo de restauración.", "BioVic"); return; }
        if (MessageBox.Show(this, $"¿Restaurar {SelectedDeletedItem.DisplayName}?", "BioVic",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            await _client.RestoreDeletedItemAsync(SelectedDeletedItem, RecoveryReasonBox.Text);
            RecoveryReasonBox.Clear();
            await LoadLocationsAsync();
            await Task.WhenAll(LoadUsersAsync(), LoadRecoveryAsync(), LoadAuditAsync(), LoadDashboardAsync());
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void CreateUser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _client.CreateUserAsync(new(FirstNameBox.Text, LastNameBox.Text, UsernameBox.Text, PasswordBox.Password,
                EmailBox.Text, (UserRole)(CreateRoleBox.SelectedItem ?? UserRole.Searcher),
                CreateAllLocationsBox.IsChecked == true,
                _createLocationChoices.Where(item => item.IsSelected).Select(item => item.Location.Id).ToList()));
            FirstNameBox.Clear(); LastNameBox.Clear(); UsernameBox.Clear(); EmailBox.Clear(); PasswordBox.Clear();
            CreateAllLocationsBox.IsChecked = false;
            await LoadLocationsAsync(); await LoadUsersAsync(); await LoadAuditAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedUser is null) return;
        EditRoleBox.SelectedItem = SelectedUser.Role;
        EnabledBox.IsChecked = SelectedUser.IsEnabled;
        EditAllLocationsBox.IsChecked = SelectedUser.HasAllLocations;
        RefreshEditLocationChoices(SelectedUser);
    }

    private async void SaveAccess_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedUser is null) { MessageBox.Show(this, "Seleccione un usuario.", "BioVic"); return; }
        try
        {
            await _client.UpdateUserAccessAsync(SelectedUser.Id, new((UserRole)(EditRoleBox.SelectedItem ?? SelectedUser.Role),
                EnabledBox.IsChecked == true, EditAllLocationsBox.IsChecked == true,
                _editLocationChoices.Where(item => item.IsSelected).Select(item => item.Location.Id).ToList()));
            await LoadUsersAsync(); await LoadAuditAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void DeleteUser_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedUser is null) { MessageBox.Show(this, "Seleccione un usuario.", "BioVic"); return; }
        if (MessageBox.Show(this, $"¿Eliminar el usuario {SelectedUser.Username}? El registro histórico se conservará.", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { await _client.DeleteUserAsync(SelectedUser.Id, DeleteReasonBox.Text); DeleteReasonBox.Clear(); await LoadUsersAsync(); await LoadAuditAsync(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void RefreshDashboard_Click(object sender, RoutedEventArgs e) { try { await LoadDashboardAsync(); } catch (Exception ex) { MessageBox.Show(this, ex.Message); } }
    private async void RefreshAudit_Click(object sender, RoutedEventArgs e) { try { await LoadAuditAsync(); } catch (Exception ex) { MessageBox.Show(this, ex.Message); } }
    private async void VerifyAudit_Click(object sender, RoutedEventArgs e)
    {
        try { var result = await _client.VerifyAuditAsync(); AuditStatusText.Text = result.Message; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message); }
    }

    private void RefreshEditLocationChoices(UserSummaryDto? user)
    {
        var selected = user?.LocationIds.ToHashSet() ?? [];
        _editLocationChoices = _locations.Select(item => new LocationPermissionChoice(item) { IsSelected = selected.Contains(item.Id) }).ToList();
        EditLocationsList.ItemsSource = _editLocationChoices;
    }

    private async void NewLocation_Click(object sender, RoutedEventArgs e)
    {
        if (new LocationEditorWindow(_client) { Owner = this }.ShowDialog() == true)
        { await LoadLocationsAsync(); await LoadAuditAsync(); }
    }

    private async void EditLocation_Click(object sender, RoutedEventArgs e)
    {
        if (LocationsGrid.SelectedItem is not LocationDto location) { MessageBox.Show(this, "Seleccione una ubicación.", "BioVic"); return; }
        if (new LocationEditorWindow(_client, location) { Owner = this }.ShowDialog() == true)
        { await LoadLocationsAsync(); await LoadAuditAsync(); }
    }

    private async void DeleteLocation_Click(object sender, RoutedEventArgs e)
    {
        if (LocationsGrid.SelectedItem is not LocationDto location) { MessageBox.Show(this, "Seleccione una ubicación.", "BioVic"); return; }
        if (string.IsNullOrWhiteSpace(LocationDeleteReasonBox.Text)) { MessageBox.Show(this, "Indique el motivo de eliminación.", "BioVic"); return; }
        if (MessageBox.Show(this, $"¿Eliminar la ubicación {location.DisplayName}? Los equipos asociados quedarán como 'Ubicación no disponible' y se crearán alertas para reasignarlos.",
            "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            await _client.DeleteLocationAsync(location.Id, LocationDeleteReasonBox.Text);
            LocationDeleteReasonBox.Clear();
            await LoadLocationsAsync(); await LoadNotificationsAsync(); await LoadAuditAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void RefreshNotifications_Click(object sender, RoutedEventArgs e) { try { await LoadNotificationsAsync(); } catch (Exception ex) { MessageBox.Show(this, ex.Message); } }
    private async void SnoozeOneDay_Click(object sender, RoutedEventArgs e) => await SnoozeSelectedAsync(TimeSpan.FromDays(1));
    private async void SnoozeSevenDays_Click(object sender, RoutedEventArgs e) => await SnoozeSelectedAsync(TimeSpan.FromDays(7));

    private async Task SnoozeSelectedAsync(TimeSpan duration)
    {
        if (SelectedNotification is null) { MessageBox.Show(this, "Seleccione un mensaje.", "BioVic"); return; }
        try { await _client.SnoozeNotificationAsync(SelectedNotification.Id, DateTimeOffset.UtcNow.Add(duration)); await LoadNotificationsAsync(); await LoadAuditAsync(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic"); }
    }

    private async void ResolveNotification_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedNotification is null) { MessageBox.Show(this, "Seleccione un mensaje.", "BioVic"); return; }
        try { await _client.ResolveNotificationAsync(SelectedNotification.Id); await LoadNotificationsAsync(); await LoadAuditAsync(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic"); }
    }

    private async void OpenNotificationEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedNotification?.EquipmentId is not Guid equipmentId) { MessageBox.Show(this, "El mensaje no está asociado a un equipo.", "BioVic"); return; }
        try
        {
            var equipment = await _client.GetEquipmentAsync(equipmentId);
            if (new EquipmentEditorWindow(_client, true, equipment) { Owner = this }.ShowDialog() == true)
            {
                await LoadNotificationsAsync();
                await LoadDashboardAsync();
                await LoadAuditAsync();
            }
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private sealed class LocationPermissionChoice(LocationDto location)
    {
        public LocationDto Location { get; } = location;
        public string DisplayName => location.DisplayName;
        public bool IsSelected { get; set; }
    }
}
