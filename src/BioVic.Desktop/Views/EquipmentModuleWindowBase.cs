using System.Windows;
using System.Windows.Controls;
using BioVic.Application.DTOs;
using BioVic.Desktop.Services;

namespace BioVic.Desktop.Views;

public class EquipmentModuleWindowBase : Window
{
    protected readonly BioVicApiClient Client;
    protected readonly bool CanEdit;
    protected readonly bool IsMaster;
    protected DataGrid EquipmentGrid = null!;

    protected EquipmentModuleWindowBase(BioVicApiClient client, bool canEdit, bool isMaster = false) { Client = client; CanEdit = canEdit; IsMaster = isMaster; Loaded += async (_, _) => await RefreshAsync(); }
    protected async Task RefreshAsync() { var result = await Client.SearchEquipmentAsync(null, 1, 100); EquipmentGrid.ItemsSource = result.Items.OrderBy(x => x.Name).ToList(); }
    protected EquipmentSearchItemDto? Selected => EquipmentGrid.SelectedItem as EquipmentSearchItemDto;
    protected bool RequireSelection(out EquipmentSearchItemDto selected)
    {
        if (Selected is { } item) { selected = item; return true; }
        MessageBox.Show(this, "Seleccione un equipo.", "BioVic"); selected = null!; return false;
    }
    protected void OpenDetails() { if (RequireSelection(out var item)) new EquipmentDetailsWindow(Client, item.Id, CanEdit, IsMaster) { Owner = this }.ShowDialog(); }
}
