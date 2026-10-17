using System.Windows; using BioVic.Desktop.Services;
namespace BioVic.Desktop.Views;
public partial class TechnoVigilanceModuleWindow : EquipmentModuleWindowBase
{
 public TechnoVigilanceModuleWindow(BioVicApiClient client,bool canEdit,bool isMaster=false):base(client,canEdit,isMaster){InitializeComponent();EquipmentGrid=GridControl;}
 private void Details_Click(object sender,RoutedEventArgs e)=>OpenDetails();
 private async void Failure_Click(object sender,RoutedEventArgs e){if(!CanEdit){MessageBox.Show(this,"Se requieren permisos de Editor.","BioVic");return;}if(RequireSelection(out var item)&&new FailureWindow(Client,item.Id){Owner=this}.ShowDialog()==true)await RefreshAsync();}
 private async void Incident_Click(object sender,RoutedEventArgs e){if(!CanEdit){MessageBox.Show(this,"Se requieren permisos de Editor.","BioVic");return;}if(RequireSelection(out var item)&&new IncidentWindow(Client,item.Id){Owner=this}.ShowDialog()==true)await RefreshAsync();}
}
