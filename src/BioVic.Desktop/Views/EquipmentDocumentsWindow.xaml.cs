using System.IO;
using System.Windows;
using BioVic.Application.DTOs;
using BioVic.Desktop.Services;
using Microsoft.Win32;

namespace BioVic.Desktop.Views;

public partial class EquipmentDocumentsWindow : Window
{
    private readonly BioVicApiClient _client;
    private readonly Guid _equipmentId;
    private readonly bool _canEdit;
    private readonly bool _isMaster;
    private EquipmentDocumentDto? Selected => DocumentsGrid.SelectedItem as EquipmentDocumentDto;

    public EquipmentDocumentsWindow(BioVicApiClient client, Guid equipmentId, string equipmentName,
        bool canEdit, bool isMaster)
    {
        InitializeComponent();
        _client = client;
        _equipmentId = equipmentId;
        _canEdit = canEdit;
        _isMaster = isMaster;
        EquipmentNameText.Text = equipmentName;
        UploadButton.Visibility = NewVersionButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        IncludeArchivedBox.Visibility = ReasonLabel.Visibility = ReasonBox.Visibility =
            ArchiveButton.Visibility = RestoreButton.Visibility = isMaster ? Visibility.Visible : Visibility.Collapsed;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var items = await _client.GetEquipmentDocumentsAsync(_equipmentId, _isMaster && IncludeArchivedBox.IsChecked == true);
            DocumentsGrid.ItemsSource = items;
            StatusText.Text = $"{items.Count} documento(s)";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private async void IncludeArchived_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) await LoadAsync(); }

    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        if (!_canEdit) return;
        await UploadAsync(null);
    }

    private async void NewVersion_Click(object sender, RoutedEventArgs e)
    {
        if (!_canEdit || Selected is null || Selected.IsDeleted)
        {
            MessageBox.Show(this, "Seleccione un documento activo para crear una nueva versión.", "BioVic");
            return;
        }
        await UploadAsync(Selected);
    }

    private async Task UploadAsync(EquipmentDocumentDto? previous)
    {
        var fileDialog = new OpenFileDialog
        {
            Filter = "Documentos e imágenes|*.pdf;*.png;*.jpg;*.jpeg;*.webp|PDF|*.pdf|Imágenes|*.png;*.jpg;*.jpeg;*.webp"
        };
        if (fileDialog.ShowDialog(this) != true) return;
        var editor = new EquipmentDocumentEditorWindow(previous?.DocumentType) { Owner = this };
        if (editor.ShowDialog() != true) return;
        try
        {
            StatusText.Text = "Cargando y verificando archivo…";
            await _client.UploadEquipmentDocumentAsync(_equipmentId, fileDialog.FileName, editor.DocumentType,
                editor.Description, editor.DocumentDateUtc, editor.ExpiresAtUtc, editor.ApprovedForAiKnowledge,
                previous?.Id);
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void Download_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is null || Selected.IsDeleted)
        {
            MessageBox.Show(this, "Seleccione un documento activo.", "BioVic");
            return;
        }
        var dialog = new SaveFileDialog { FileName = Selected.OriginalName, Filter = "Todos los archivos|*.*" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            await File.WriteAllBytesAsync(dialog.FileName,
                await _client.DownloadEquipmentDocumentAsync(_equipmentId, Selected.Id));
            MessageBox.Show(this, "Documento descargado y verificado correctamente.", "BioVic");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void Archive_Click(object sender, RoutedEventArgs e)
    {
        if (!_isMaster || Selected is null || Selected.IsDeleted)
        {
            MessageBox.Show(this, "Seleccione un documento activo.", "BioVic");
            return;
        }
        if (string.IsNullOrWhiteSpace(ReasonBox.Text)) { MessageBox.Show(this, "Indique el motivo.", "BioVic"); return; }
        try { await _client.ArchiveEquipmentDocumentAsync(_equipmentId, Selected.Id, ReasonBox.Text); ReasonBox.Clear(); await LoadAsync(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (!_isMaster || Selected is null || !Selected.IsDeleted)
        {
            MessageBox.Show(this, "Seleccione un documento archivado.", "BioVic");
            return;
        }
        if (string.IsNullOrWhiteSpace(ReasonBox.Text)) { MessageBox.Show(this, "Indique el motivo.", "BioVic"); return; }
        try { await _client.RestoreEquipmentDocumentAsync(_equipmentId, Selected.Id, ReasonBox.Text); ReasonBox.Clear(); await LoadAsync(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "BioVic", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
