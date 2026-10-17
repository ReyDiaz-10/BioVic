using BioVic.Domain.Common;
using BioVic.Domain.Enums;

namespace BioVic.Domain.Entities;

public sealed class Equipment : SoftDeletableEntity
{
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string InternalCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? EquipmentType { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? InventoryNumber { get; set; }
    public string? AssetCode { get; set; }

    public string? RegulatoryRegistration { get; set; }
    public DateTimeOffset? RegulatoryExpiryUtc { get; set; }
    public string? RiskClassification { get; set; }
    public string? RegulatoryCondition { get; set; }
    public bool? IsControlledTechnology { get; set; }
    public string? RegulatoryNotes { get; set; }

    public DateTimeOffset? AcquisitionDateUtc { get; set; }
    public DateTimeOffset? ReceptionDateUtc { get; set; }
    public DateTimeOffset? InstallationDateUtc { get; set; }
    public DateTimeOffset? CommissioningDateUtc { get; set; }
    public string? Supplier { get; set; }
    public decimal? AcquisitionCost { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? ContractNumber { get; set; }
    public string? AcquisitionMethod { get; set; }
    public string? Ownership { get; set; }
    public DateTimeOffset? WarrantyStartUtc { get; set; }
    public DateTimeOffset? WarrantyEndUtc { get; set; }
    public int? EstimatedUsefulLifeMonths { get; set; }
    public DateTimeOffset? ExpectedReplacementUtc { get; set; }

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }
    public string? ResponsibleName { get; set; }
    public string? ResponsiblePosition { get; set; }
    public string? ResponsibleArea { get; set; }
    public string? ResponsiblePhone { get; set; }
    public string? ResponsibleEmail { get; set; }

    public string? Voltage { get; set; }
    public string? Frequency { get; set; }
    public string? Power { get; set; }
    public string? Current { get; set; }
    public string? Battery { get; set; }
    public string? Connections { get; set; }
    public string? SoftwareName { get; set; }
    public string? SoftwareVersion { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string? TechnicalCharacteristics { get; set; }
    public string? Accessories { get; set; }
    public string? Consumables { get; set; }
    public string? CustomPropertiesJson { get; set; }

    public EquipmentStatus Status { get; set; } = EquipmentStatus.Operational;
    public Criticality Criticality { get; set; } = Criticality.Medium;
    public bool RequiresCalibration { get; set; }
    public DateTimeOffset? NextMaintenanceUtc { get; set; }
    public DateTimeOffset? NextCalibrationUtc { get; set; }
    public string? PublicOperationalNotes { get; set; }
    public string SearchTextNormalized { get; set; } = string.Empty;

    public ICollection<MaintenanceRecord> Maintenances { get; set; } = new List<MaintenanceRecord>();
    public ICollection<Calibration> Calibrations { get; set; } = new List<Calibration>();
    public ICollection<FailureRecord> Failures { get; set; } = new List<FailureRecord>();
    public ICollection<TechnoVigilanceIncident> Incidents { get; set; } = new List<TechnoVigilanceIncident>();
    public ICollection<EquipmentDocument> Documents { get; set; } = new List<EquipmentDocument>();
    public ICollection<EquipmentLocationHistory> LocationHistory { get; set; } = new List<EquipmentLocationHistory>();
    public ICollection<EquipmentStatusHistory> StatusHistory { get; set; } = new List<EquipmentStatusHistory>();
}
