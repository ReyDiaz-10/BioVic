using System.Windows; using BioVic.Desktop.Services;
namespace BioVic.Desktop.Views;
public partial class CalibrationModuleWindow : EquipmentModuleWindowBase
{
 public CalibrationModuleWindow(BioVicApiClient client,bool canEdit,bool isMaster=false):base(client,canEdit,isMaster){InitializeComponent();EquipmentGrid=GridControl;}
 private void Details_Click(object sender,RoutedEventArgs e)=>OpenDetails();
 private async void Add_Click(object sender,RoutedEventArgs e){if(!CanEdit){MessageBox.Show(this,"Se requieren permisos de Editor.","BioVic");return;}if(RequireSelection(out var item)&&new CalibrationWindow(Client,item.Id){Owner=this}.ShowDialog()==true)await RefreshAsync();}
}
