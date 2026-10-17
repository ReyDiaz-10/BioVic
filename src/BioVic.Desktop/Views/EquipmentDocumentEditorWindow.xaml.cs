using System.Windows;
using BioVic.Application.DTOs;

namespace BioVic.Desktop.Views;

public partial class EquipmentDocumentEditorWindow : Window
{
    public string DocumentType => DocumentTypeBox.SelectedItem?.ToString() ?? EquipmentDocumentTypes.Other;
    public string? Description => string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();
    public DateTimeOffset? DocumentDateUtc => AsUtc(DocumentDatePicker.SelectedDate);
    public DateTimeOffset? ExpiresAtUtc => AsUtc(ExpiresAtPicker.SelectedDate);
    public bool ApprovedForAiKnowledge => ApprovedForAiBox.IsChecked == true;

    public EquipmentDocumentEditorWindow(string? selectedType)
    {
        InitializeComponent();
        DocumentTypeBox.ItemsSource = EquipmentDocumentTypes.All;
        DocumentTypeBox.SelectedItem = EquipmentDocumentTypes.All.Contains(selectedType ?? string.Empty)
            ? selectedType : EquipmentDocumentTypes.Other;
        DocumentDatePicker.SelectedDate = DateTime.Today;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (ExpiresAtPicker.SelectedDate is DateTime expiry && DocumentDatePicker.SelectedDate is DateTime documentDate
            && expiry <= documentDate)
        {
            MessageBox.Show(this, "El vencimiento debe ser posterior a la fecha del documento.", "BioVic");
            return;
        }
        DialogResult = true;
    }

    private static DateTimeOffset? AsUtc(DateTime? date) => date is null
        ? null : new DateTimeOffset(DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Local)).ToUniversalTime();
}
