using System.Windows;
using BioVic.Application.DTOs;
using BioVic.Desktop.Services;
using BioVic.Domain.Enums;
using BioVic.Desktop.Converters;

namespace BioVic.Desktop.Views;

public partial class EquipmentEditorWindow : Window
{
    private readonly BioVicApiClient _client;
    private readonly EquipmentDetailsDto? _existing;

    public EquipmentEditorWindow(BioVicApiClient client, bool isMaster)
    {
        InitializeComponent();
        _client = client;
        CreateLocationButton.Visibility = isMaster ? Visibility.Visible : Visibility.Collapsed;
        ConfigureEnums();
        Loaded += EquipmentEditorWindow_Loaded;
    }

    public EquipmentEditorWindow(BioVicApiClient client, bool isMaster, EquipmentDetailsDto existing) : this(client, isMaster)
    {
        _existing = existing; Title = "Editar equipo · BioVic"; HeadingText.Text = "Editar ficha del equipo";
        SaveButton.Content = "Guardar modificaciones"; ChangeReasonPanel.Visibility = Visibility.Visible;
    }

    private async void EquipmentEditorWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try { await LoadLocationsAsync(); if (_existing is not null) FillExisting(_existing); }
        catch (Exception exception) { ErrorText.Text = exception.Message; }
    }

    private async Task LoadLocationsAsync(Guid? selectId = null)
    {
        LocationBox.ItemsSource = await _client.GetLocationsAsync();
        if (selectId is not null) LocationBox.SelectedValue = selectId.Value;
    }

    private async void CreateLocationButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new LocationEditorWindow(_client) { Owner = this };
        if (window.ShowDialog() == true && window.Location is not null)
            await LoadLocationsAsync(window.Location.Id);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        ErrorText.Text = string.Empty;
        try
        {
            if (LocationBox.SelectedValue is not Guid selectedLocationId)
                throw new InvalidOperationException("Seleccione la ubicación del equipo.");
            Guid? locationId = selectedLocationId;
            var status = StatusBox.SelectedValue is EquipmentStatus selectedStatus ? selectedStatus : EquipmentStatus.Operational;
            var criticality = CriticalityBox.SelectedValue is Criticality selectedCriticality ? selectedCriticality : Criticality.Medium;
            if (_existing is null) await _client.CreateEquipmentAsync(new(
                CodeBox.Text, NameBox.Text, TypeBox.Text, CategoryBox.Text, null, ManufacturerBox.Text,
                BrandBox.Text, ModelBox.Text, SerialBox.Text, InventoryBox.Text, null, locationId,
                ResponsibleBox.Text, PositionBox.Text,
                status, criticality,
                RequiresCalibrationBox.IsChecked == true,
                ToUtc(NextMaintenancePicker.SelectedDate), ToUtc(NextCalibrationPicker.SelectedDate), NotesBox.Text));
            else await _client.UpdateEquipmentAsync(_existing.Id, new(CodeBox.Text, NameBox.Text, TypeBox.Text, CategoryBox.Text,
                _existing.Description, ManufacturerBox.Text, BrandBox.Text, ModelBox.Text, SerialBox.Text, InventoryBox.Text,
                _existing.AssetCode, locationId, ResponsibleBox.Text, PositionBox.Text, status, criticality,
                RequiresCalibrationBox.IsChecked == true, ToUtc(NextMaintenancePicker.SelectedDate), ToUtc(NextCalibrationPicker.SelectedDate),
                NotesBox.Text, ChangeReasonBox.Text));
            DialogResult = true;
        }
        catch (Exception exception) { ErrorText.Text = exception.Message; }
        finally { SaveButton.IsEnabled = true; }
    }

    private static DateTimeOffset? ToUtc(DateTime? value) => value is null ? null : new DateTimeOffset(value.Value).ToUniversalTime();
    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void ConfigureEnums()
    {
        StatusBox.ItemsSource = SpanishLabels.Options<EquipmentStatus>(); StatusBox.DisplayMemberPath = "Text"; StatusBox.SelectedValuePath = "Value"; StatusBox.SelectedValue = EquipmentStatus.Operational;
        CriticalityBox.ItemsSource = SpanishLabels.Options<Criticality>(); CriticalityBox.DisplayMemberPath = "Text"; CriticalityBox.SelectedValuePath = "Value"; CriticalityBox.SelectedValue = Criticality.Medium;
    }

    private void FillExisting(EquipmentDetailsDto e)
    {
        CodeBox.Text=e.InternalCode; NameBox.Text=e.Name; TypeBox.Text=e.EquipmentType; CategoryBox.Text=e.Category; ManufacturerBox.Text=e.Manufacturer;
        BrandBox.Text=e.Brand; ModelBox.Text=e.Model; SerialBox.Text=e.SerialNumber; InventoryBox.Text=e.InventoryNumber; ResponsibleBox.Text=e.ResponsibleName;
        PositionBox.Text=e.ResponsiblePosition; StatusBox.SelectedValue=e.Status; CriticalityBox.SelectedValue=e.Criticality; RequiresCalibrationBox.IsChecked=e.RequiresCalibration;
        NextMaintenancePicker.SelectedDate=e.NextMaintenanceUtc?.LocalDateTime; NextCalibrationPicker.SelectedDate=e.NextCalibrationUtc?.LocalDateTime; NotesBox.Text=e.PublicOperationalNotes;
        LocationBox.SelectedValue = e.LocationId;
    }
}
