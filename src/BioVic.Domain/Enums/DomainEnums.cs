namespace BioVic.Domain.Enums;

public enum UserRole
{
    Searcher = 0,
    Editor = 1,
    MasterAdministrator = 2
}

public enum EquipmentStatus
{
    Operational = 0,
    OperationalWithObservations = 1,
    InMaintenance = 2,
    MaintenancePending = 3,
    OutOfService = 4,
    InRepair = 5,
    AwaitingSparePart = 6,
    InCalibration = 7,
    Quarantine = 8,
    Retired = 9,
    Decommissioned = 10
}

public enum Criticality
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum MaintenanceType
{
    Preventive = 0,
    Corrective = 1,
    Predictive = 2,
    Inspection = 3,
    Verification = 4,
    Other = 5
}

public enum MaintenanceResult
{
    Successful = 0,
    SuccessfulWithObservations = 1,
    Unsuccessful = 2,
    Pending = 3
}

public enum CalibrationResult
{
    Compliant = 0,
    NonCompliant = 1,
    Conditional = 2,
    Pending = 3
}

public enum IncidentStatus
{
    Open = 0,
    Investigating = 1,
    Reported = 2,
    Closed = 3
}

public enum AlertSeverity
{
    Information = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
