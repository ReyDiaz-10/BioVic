using BioVic.Application.DTOs;

namespace BioVic.Tests;

public sealed class EquipmentDocumentTypesTests
{
    [Fact]
    public void DocumentCatalogIncludesClinicalAndTechnicalEvidence()
    {
        Assert.Contains(EquipmentDocumentTypes.CalibrationCertificate, EquipmentDocumentTypes.All);
        Assert.Contains(EquipmentDocumentTypes.MaintenanceReport, EquipmentDocumentTypes.All);
        Assert.Contains(EquipmentDocumentTypes.Photo, EquipmentDocumentTypes.All);
        Assert.Equal(EquipmentDocumentTypes.All.Count, EquipmentDocumentTypes.All.Distinct().Count());
    }
}
