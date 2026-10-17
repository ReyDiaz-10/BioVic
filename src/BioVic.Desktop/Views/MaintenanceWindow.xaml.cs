using System.Globalization;
using System.Windows;
using BioVic.Application.DTOs;
using BioVic.Desktop.Services;
using BioVic.Domain.Enums;
using BioVic.Desktop.Converters;

namespace BioVic.Desktop.Views;

public partial class MaintenanceWindow : Window
{
    private readonly BioVicApiClient _client;
    private readonly Guid _equipmentId;

    public MaintenanceWindow(BioVicApiClient client, Guid equipmentId)
    {
        InitializeComponent();
        _client = client;
        _equipmentId = equipmentId;
        TypeBox.ItemsSource = SpanishLabels.Options<MaintenanceType>(); TypeBox.DisplayMemberPath="Text"; TypeBox.SelectedValuePath="Value"; TypeBox.SelectedValue=MaintenanceType.Preventive;
        ResultBox.ItemsSource = SpanishLabels.Options<MaintenanceResult>(); ResultBox.DisplayMemberPath="Text"; ResultBox.SelectedValuePath="Value"; ResultBox.SelectedValue=MaintenanceResult.Successful;
        ResultingStatusBox.ItemsSource = SpanishLabels.Options<EquipmentStatus>(); ResultingStatusBox.DisplayMemberPath="Text"; ResultingStatusBox.SelectedValuePath="Value"; ResultingStatusBox.SelectedValue=EquipmentStatus.Operational;
        PerformedPicker.SelectedDate = DateTime.Today;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        ErrorText.Text = string.Empty;
        try
        {
            decimal? cost = null;
            if (!string.IsNullOrWhiteSpace(CostBox.Text))
            {
                if (!decimal.TryParse(CostBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsedCost))
                    throw new BioVicApiException("El costo no tiene un formato válido.");
                cost = parsedCost;
            }
            int? downtime = null;
            if (!string.IsNullOrWhiteSpace(DowntimeBox.Text))
            {
                if (!int.TryParse(DowntimeBox.Text, out var parsedDowntime) || parsedDowntime < 0)
                    throw new BioVicApiException("El tiempo fuera de servicio debe ser un número positivo.");
                downtime = parsedDowntime;
            }

            await _client.AddMaintenanceAsync(_equipmentId, new(
                TypeBox.SelectedValue is MaintenanceType type ? type : MaintenanceType.Preventive,
                OrderBox.Text, ToUtc(PerformedPicker.SelectedDate) ?? DateTimeOffset.UtcNow,
                PerformedByBox.Text, ResponsibleBox.Text, ActivitiesBox.Text, FindingsBox.Text,
                DiagnosisBox.Text, ActionsBox.Text, SparePartsBox.Text, cost, downtime,
                ResultBox.SelectedValue is MaintenanceResult result ? result : MaintenanceResult.Pending,
                ChangeStatusBox.IsChecked == true && ResultingStatusBox.SelectedValue is EquipmentStatus status ? status : null,
                ToUtc(NextPicker.SelectedDate), NotesBox.Text));
            DialogResult = true;
        }
        catch (Exception exception) { ErrorText.Text = exception.Message; }
        finally { SaveButton.IsEnabled = true; }
    }

    private static DateTimeOffset? ToUtc(DateTime? value) => value is null ? null : new DateTimeOffset(value.Value).ToUniversalTime();
    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
