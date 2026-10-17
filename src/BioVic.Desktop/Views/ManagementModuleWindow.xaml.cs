using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BioVic.Application.DTOs;
using BioVic.Desktop.Services;

namespace BioVic.Desktop.Views;

public partial class ManagementModuleWindow : Window
{
    private readonly BioVicApiClient _client;
    private readonly bool _canEdit;
    private readonly bool _isMaster;
    private readonly ObservableCollection<EquipmentSearchItemDto> _items = [];
    private List<EquipmentSearchItemDto> _all = [];

    public ManagementModuleWindow(BioVicApiClient client, string module, bool canEdit, bool isMaster = false)
    {
        InitializeComponent();
        _client = client;
        _canEdit = canEdit;
        _isMaster = isMaster;
        TitleText.Text = module;
        Title = $"BioVic · {module}";
        DescriptionText.Text = module switch
        {
            "Inventario" => "Listado general de equipos. Puede filtrar y ordenar por nombre, modelo o próxima fecha de mantenimiento.",
            "Mantenimientos" => "Seleccione un equipo para consultar su historial y documentar actividades realizadas, hallazgos, pendientes y próxima intervención.",
            "Calibraciones" => "Seleccione el equipo para consultar su trazabilidad metrológica, resultados, laboratorio, certificado y próxima calibración.",
            _ => "Seleccione un equipo para consultar y documentar fallas, incidentes, acciones correctivas y seguimiento de tecnovigilancia."
        };
        EquipmentGrid.ItemsSource = _items;
        EditButton.Visibility = DeleteButton.Visibility = DeleteReasonBox.Visibility = isMaster ? Visibility.Visible : Visibility.Collapsed;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try { _all = (await _client.SearchEquipmentAsync(null, 1, 100)).Items.ToList(); Apply(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void Apply()
    {
        var filter = FilterBox.Text.Trim();
        IEnumerable<EquipmentSearchItemDto> query = _all.Where(x => filter.Length == 0 ||
            x.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
            (x.Model?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
            x.InternalCode.Contains(filter, StringComparison.CurrentCultureIgnoreCase));
        var tag = (SortBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        query = tag switch
        {
            "NameDesc" => query.OrderByDescending(x => x.Name),
            "ModelAsc" => query.OrderBy(x => x.Model).ThenBy(x => x.Name),
            "MaintenanceAsc" => query.OrderBy(x => x.NextMaintenanceUtc is null).ThenBy(x => x.NextMaintenanceUtc),
            "MaintenanceDesc" => query.OrderByDescending(x => x.NextMaintenanceUtc),
            "LastMaintenanceDesc" => query.OrderByDescending(x => x.LastMaintenanceUtc),
            "LastMaintenanceAsc" => query.OrderBy(x => x.LastMaintenanceUtc is null).ThenBy(x => x.LastMaintenanceUtc),
            _ => query.OrderBy(x => x.Name)
        };
        _items.Clear(); foreach (var item in query) _items.Add(item);
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) Apply(); }
    private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) Apply(); }
    private void OpenEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (EquipmentGrid.SelectedItem is not EquipmentSearchItemDto selected)
        { MessageBox.Show(this, "Seleccione un equipo.", "BioVic"); return; }
        new EquipmentDetailsWindow(_client, selected.Id, _canEdit, _isMaster) { Owner = this }.ShowDialog();
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async void EditEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (!_isMaster || EquipmentGrid.SelectedItem is not EquipmentSearchItemDto selected) { MessageBox.Show(this, "Seleccione un equipo.", "BioVic"); return; }
        try { var details = await _client.GetEquipmentAsync(selected.Id); if (new EquipmentEditorWindow(_client, true, details) { Owner = this }.ShowDialog() == true) await LoadAsync(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic"); }
    }

    private async void DeleteEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (!_isMaster || EquipmentGrid.SelectedItem is not EquipmentSearchItemDto selected) { MessageBox.Show(this, "Seleccione un equipo.", "BioVic"); return; }
        if (string.IsNullOrWhiteSpace(DeleteReasonBox.Text)) { MessageBox.Show(this, "Indique el motivo de la baja.", "BioVic"); return; }
        if (MessageBox.Show(this, $"¿Dar de baja {selected.Name}? El historial se conservará.", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { await _client.DeleteEquipmentAsync(selected.Id, DeleteReasonBox.Text); DeleteReasonBox.Clear(); await LoadAsync(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic"); }
    }
}
