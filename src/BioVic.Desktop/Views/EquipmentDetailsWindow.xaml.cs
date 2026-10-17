using System.Windows;
using BioVic.Application.DTOs;
using BioVic.Desktop.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace BioVic.Desktop.Views;

public partial class EquipmentDetailsWindow : Window
{
    private readonly BioVicApiClient _client;
    private readonly Guid _equipmentId;
    private EquipmentDetailsDto? _equipment;
    private readonly bool _canEdit;
    private readonly bool _isMaster;
    private bool _qrRemoteAccessReady;

    public EquipmentDetailsWindow(BioVicApiClient client, Guid equipmentId, bool canEdit, bool isMaster = false)
    {
        InitializeComponent();
        _client = client;
        _equipmentId = equipmentId;
        _canEdit = canEdit;
        _isMaster = isMaster;
        AddMaintenanceButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        AddCalibrationButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        QrActionsPanel.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        Loaded += EquipmentDetailsWindow_Loaded;
    }

    private async void EquipmentDetailsWindow_Loaded(object sender, RoutedEventArgs e) => await LoadEquipmentAsync();

    private async Task LoadEquipmentAsync()
    {
        LoadingText.Visibility = Visibility.Visible;
        DetailsTabs.Visibility = Visibility.Collapsed;
        try
        {
            _equipment = await _client.GetEquipmentAsync(_equipmentId);
            DataContext = _equipment;
            await LoadQrAccessInfoAsync();
            DetailsTabs.Visibility = Visibility.Visible;
            LoadingText.Visibility = Visibility.Collapsed;
        }
        catch (Exception exception) { LoadingText.Text = exception.Message; }
    }

    private async Task LoadQrAccessInfoAsync()
    {
        if (_equipment is null) return;
        try
        {
            var access = await _client.GetQrAccessInfoAsync(_equipment.Id);
            _qrRemoteAccessReady = access.IsRemoteAccessReady;
            QrLinkText.Text = access.Url;
            CopyQrLinkButton.IsEnabled = access.IsRemoteAccessReady;
            GenerateQrButton.IsEnabled = access.IsRemoteAccessReady;
            PrintQrButton.IsEnabled = access.IsRemoteAccessReady;

            if (!access.IsRemoteAccessReady)
            {
                QrStatusText.Text = "Configuración pendiente: " + string.Join(" ", access.Warnings);
                QrStatusText.Foreground = CreateBrush("#B42318");
            }
            else if (access.Warnings.Count > 0)
            {
                QrStatusText.Text = "QR listo para teléfonos de la red local. " + string.Join(" ", access.Warnings);
                QrStatusText.Foreground = CreateBrush("#8A5A00");
            }
            else
            {
                QrStatusText.Text = "QR listo y protegido mediante inicio de sesión.";
                QrStatusText.Foreground = CreateBrush("#12644F");
            }
        }
        catch (Exception exception)
        {
            _qrRemoteAccessReady = false;
            QrLinkText.Text = "No fue posible obtener el enlace QR.";
            QrStatusText.Text = exception.Message;
            QrStatusText.Foreground = CreateBrush("#B42318");
            CopyQrLinkButton.IsEnabled = false;
            GenerateQrButton.IsEnabled = false;
            PrintQrButton.IsEnabled = false;
        }
    }

    private static Brush CreateBrush(string color) => (Brush)new BrushConverter().ConvertFromString(color)!;

    private async void AddMaintenanceButton_Click(object sender, RoutedEventArgs e)
    {
        if (_equipment is null) return;
        var window = new MaintenanceWindow(_client, _equipment.Id) { Owner = this };
        if (window.ShowDialog() == true) await LoadEquipmentAsync();
    }

    private void DocumentsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_equipment is null) return;
        new EquipmentDocumentsWindow(_client, _equipment.Id, _equipment.Name, _canEdit, _isMaster)
            { Owner = this }.ShowDialog();
    }

    private void CopyQrLinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_qrRemoteAccessReady && !string.IsNullOrWhiteSpace(QrLinkText.Text)) Clipboard.SetText(QrLinkText.Text);
    }

    private async void AddCalibrationButton_Click(object sender, RoutedEventArgs e)
    { if (_equipment is not null && new CalibrationWindow(_client, _equipment.Id) { Owner = this }.ShowDialog() == true) await LoadEquipmentAsync(); }

    private async void GenerateQrButton_Click(object sender, RoutedEventArgs e)
    {
        if (_equipment is null || !_qrRemoteAccessReady) return; var dialog = new SaveFileDialog { Filter="Imagen PNG|*.png", FileName=$"BioVic-QR-{_equipment.InternalCode}.png" };
        if (dialog.ShowDialog(this)==true) { await File.WriteAllBytesAsync(dialog.FileName, await _client.GetQrAsync(_equipment.Id)); MessageBox.Show(this,"QR guardado correctamente.","BioVic"); }
    }
    private async void PrintQrButton_Click(object sender, RoutedEventArgs e)
    {
        if (_equipment is null || !_qrRemoteAccessReady) return; var path=Path.Combine(Path.GetTempPath(),$"BioVic-QR-{_equipment.Id:N}.png"); await File.WriteAllBytesAsync(path,await _client.GetQrAsync(_equipment.Id));
        try { Process.Start(new ProcessStartInfo(path){UseShellExecute=true,Verb="print"}); } catch { Process.Start(new ProcessStartInfo(path){UseShellExecute=true}); }
    }
    private async void DownloadPdfButton_Click(object sender, RoutedEventArgs e)
    {
        if (_equipment is null) return; var dialog=new SaveFileDialog{Filter="Documento PDF|*.pdf",FileName=$"Hoja-de-vida-{_equipment.InternalCode}.pdf"};
        if(dialog.ShowDialog(this)==true){await File.WriteAllBytesAsync(dialog.FileName,await _client.GetLifeSheetPdfAsync(_equipment.Id));MessageBox.Show(this,"PDF guardado correctamente.","BioVic");}
    }
    private async void ReviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_equipment is null) return;
        ReviewAnswerBox.Text = "Revisando…";
        try
        {
            var answer = await _client.AskAiAsync(_equipment.Id, "Ejecutar revisión automática local");
            ReviewAnswerBox.Text = answer.Answer;
        }
        catch (Exception exception)
        {
            ReviewAnswerBox.Text = exception.Message;
        }
    }}
