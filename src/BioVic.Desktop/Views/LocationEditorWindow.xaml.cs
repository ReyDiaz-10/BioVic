using System.Windows;
using BioVic.Application.DTOs;
using BioVic.Desktop.Services;

namespace BioVic.Desktop.Views;

public partial class LocationEditorWindow : Window
{
    private readonly BioVicApiClient _client;
    private readonly LocationDto? _existing;
    public LocationDto? Location { get; private set; }

    public LocationEditorWindow(BioVicApiClient client)
    {
        InitializeComponent();
        _client = client;
    }

    public LocationEditorWindow(BioVicApiClient client, LocationDto existing) : this(client)
    {
        _existing = existing;
        Title = "Editar ubicación · BioVic";
        HeadingText.Text = "Editar ubicación";
        SaveButton.Content = "Guardar cambios";
        ChangeReasonPanel.Visibility = Visibility.Visible;
        InstitutionBox.Text = existing.Institution; SiteBox.Text = existing.Site; BuildingBox.Text = existing.Building;
        FloorBox.Text = existing.Floor; ServiceBox.Text = existing.Service; AreaBox.Text = existing.Area;
        RoomBox.Text = existing.Room; SpecificBox.Text = existing.SpecificLocation;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        ErrorText.Text = string.Empty;
        try
        {
            Location = _existing is null
                ? await _client.CreateLocationAsync(new(InstitutionBox.Text, SiteBox.Text, BuildingBox.Text,
                    FloorBox.Text, ServiceBox.Text, AreaBox.Text, RoomBox.Text, SpecificBox.Text))
                : await _client.UpdateLocationAsync(_existing.Id, new(InstitutionBox.Text, SiteBox.Text, BuildingBox.Text,
                    FloorBox.Text, ServiceBox.Text, AreaBox.Text, RoomBox.Text, SpecificBox.Text, ChangeReasonBox.Text));
            DialogResult = true;
        }
        catch (Exception exception) { ErrorText.Text = exception.Message; }
        finally { SaveButton.IsEnabled = true; }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
