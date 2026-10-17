using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioVic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimestampUtc = table.Column<long>(nullable: false),
                    TimestampLocal = table.Column<string>(nullable: false),
                    UserId = table.Column<Guid>(nullable: true),
                    Username = table.Column<string>(nullable: false),
                    Role = table.Column<string>(maxLength: 40, nullable: true),
                    Action = table.Column<string>(maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(maxLength: 120, nullable: false),
                    EntityId = table.Column<string>(nullable: true),
                    EquipmentId = table.Column<Guid>(nullable: true),
                    Description = table.Column<string>(nullable: false),
                    OldValue = table.Column<string>(nullable: true),
                    NewValue = table.Column<string>(nullable: true),
                    ComputerName = table.Column<string>(nullable: false),
                    SessionId = table.Column<string>(nullable: false),
                    IpAddress = table.Column<string>(nullable: false),
                    ApplicationVersion = table.Column<string>(nullable: false),
                    PreviousHash = table.Column<string>(maxLength: 64, nullable: false),
                    RecordHash = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentResponsibleHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EquipmentId = table.Column<Guid>(nullable: false),
                    PreviousResponsible = table.Column<string>(nullable: true),
                    NewResponsible = table.Column<string>(nullable: false),
                    Position = table.Column<string>(nullable: true),
                    Area = table.Column<string>(nullable: true),
                    AssignedAtUtc = table.Column<long>(nullable: false),
                    ChangedByUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentResponsibleHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Institution = table.Column<string>(maxLength: 200, nullable: false),
                    Site = table.Column<string>(maxLength: 150, nullable: false),
                    Building = table.Column<string>(maxLength: 150, nullable: true),
                    Floor = table.Column<string>(maxLength: 80, nullable: true),
                    Service = table.Column<string>(maxLength: 150, nullable: false),
                    Area = table.Column<string>(maxLength: 150, nullable: true),
                    Room = table.Column<string>(maxLength: 100, nullable: true),
                    SpecificLocation = table.Column<string>(maxLength: 200, nullable: true),
                    SearchTextNormalized = table.Column<string>(maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<long>(nullable: false),
                    CreatedByUserId = table.Column<Guid>(nullable: true),
                    UpdatedAtUtc = table.Column<long>(nullable: true),
                    UpdatedByUserId = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false),
                    DeletedAtUtc = table.Column<long>(nullable: true),
                    DeletedByUserId = table.Column<Guid>(nullable: true),
                    DeletionReason = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegulatoryRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    StandardName = table.Column<string>(maxLength: 250, nullable: false),
                    ArticleOrSection = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: false),
                    EffectiveFromUtc = table.Column<long>(nullable: true),
                    Status = table.Column<string>(nullable: false),
                    ReferenceUrl = table.Column<string>(nullable: true),
                    Notes = table.Column<string>(nullable: true),
                    CreatedAtUtc = table.Column<long>(nullable: false),
                    CreatedByUserId = table.Column<Guid>(nullable: true),
                    UpdatedAtUtc = table.Column<long>(nullable: true),
                    UpdatedByUserId = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false),
                    DeletedAtUtc = table.Column<long>(nullable: true),
                    DeletedByUserId = table.Column<Guid>(nullable: true),
                    DeletionReason = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulatoryRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    FirstName = table.Column<string>(maxLength: 100, nullable: false),
                    LastName = table.Column<string>(maxLength: 100, nullable: false),
                    Username = table.Column<string>(maxLength: 80, nullable: false),
                    NormalizedUsername = table.Column<string>(maxLength: 80, nullable: false),
                    Email = table.Column<string>(maxLength: 254, nullable: true),
                    PasswordHash = table.Column<string>(maxLength: 600, nullable: false),
                    Role = table.Column<string>(maxLength: 40, nullable: false),
                    IsEnabled = table.Column<bool>(nullable: false),
                    FailedLoginAttempts = table.Column<int>(nullable: false),
                    LockoutEndUtc = table.Column<long>(nullable: true),
                    LastLoginAtUtc = table.Column<long>(nullable: true),
                    PasswordChangedAtUtc = table.Column<long>(nullable: true),
                    CreatedAtUtc = table.Column<long>(nullable: false),
                    CreatedByUserId = table.Column<Guid>(nullable: true),
                    UpdatedAtUtc = table.Column<long>(nullable: true),
                    UpdatedByUserId = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false),
                    DeletedAtUtc = table.Column<long>(nullable: true),
                    DeletedByUserId = table.Column<Guid>(nullable: true),
                    DeletionReason = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PublicId = table.Column<Guid>(nullable: false),
                    InternalCode = table.Column<string>(maxLength: 100, nullable: false),
                    Name = table.Column<string>(maxLength: 250, nullable: false),
                    EquipmentType = table.Column<string>(nullable: true),
                    Category = table.Column<string>(maxLength: 150, nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Manufacturer = table.Column<string>(maxLength: 200, nullable: true),
                    Brand = table.Column<string>(maxLength: 150, nullable: true),
                    Model = table.Column<string>(maxLength: 150, nullable: true),
                    SerialNumber = table.Column<string>(maxLength: 150, nullable: true),
                    InventoryNumber = table.Column<string>(nullable: true),
                    AssetCode = table.Column<string>(nullable: true),
                    RegulatoryRegistration = table.Column<string>(nullable: true),
                    RegulatoryExpiryUtc = table.Column<long>(nullable: true),
                    RiskClassification = table.Column<string>(nullable: true),
                    RegulatoryCondition = table.Column<string>(nullable: true),
                    IsControlledTechnology = table.Column<bool>(nullable: true),
                    RegulatoryNotes = table.Column<string>(nullable: true),
                    AcquisitionDateUtc = table.Column<long>(nullable: true),
                    ReceptionDateUtc = table.Column<long>(nullable: true),
                    InstallationDateUtc = table.Column<long>(nullable: true),
                    CommissioningDateUtc = table.Column<long>(nullable: true),
                    Supplier = table.Column<string>(nullable: true),
                    AcquisitionCost = table.Column<double>(nullable: true),
                    InvoiceNumber = table.Column<string>(nullable: true),
                    ContractNumber = table.Column<string>(nullable: true),
                    AcquisitionMethod = table.Column<string>(nullable: true),
                    Ownership = table.Column<string>(nullable: true),
                    WarrantyStartUtc = table.Column<long>(nullable: true),
                    WarrantyEndUtc = table.Column<long>(nullable: true),
                    EstimatedUsefulLifeMonths = table.Column<int>(nullable: true),
                    ExpectedReplacementUtc = table.Column<long>(nullable: true),
                    LocationId = table.Column<Guid>(nullable: true),
                    ResponsibleName = table.Column<string>(maxLength: 200, nullable: true),
                    ResponsiblePosition = table.Column<string>(nullable: true),
                    ResponsibleArea = table.Column<string>(nullable: true),
                    ResponsiblePhone = table.Column<string>(nullable: true),
                    ResponsibleEmail = table.Column<string>(nullable: true),
                    Voltage = table.Column<string>(nullable: true),
                    Frequency = table.Column<string>(nullable: true),
                    Power = table.Column<string>(nullable: true),
                    Current = table.Column<string>(nullable: true),
                    Battery = table.Column<string>(nullable: true),
                    Connections = table.Column<string>(nullable: true),
                    SoftwareName = table.Column<string>(nullable: true),
                    SoftwareVersion = table.Column<string>(nullable: true),
                    FirmwareVersion = table.Column<string>(nullable: true),
                    IpAddress = table.Column<string>(nullable: true),
                    MacAddress = table.Column<string>(nullable: true),
                    TechnicalCharacteristics = table.Column<string>(nullable: true),
                    Accessories = table.Column<string>(nullable: true),
                    Consumables = table.Column<string>(nullable: true),
                    CustomPropertiesJson = table.Column<string>(nullable: true),
                    Status = table.Column<string>(maxLength: 60, nullable: false),
                    Criticality = table.Column<string>(maxLength: 30, nullable: false),
                    RequiresCalibration = table.Column<bool>(nullable: false),
                    NextMaintenanceUtc = table.Column<long>(nullable: true),
                    NextCalibrationUtc = table.Column<long>(nullable: true),
                    PublicOperationalNotes = table.Column<string>(nullable: true),
                    SearchTextNormalized = table.Column<string>(maxLength: 3000, nullable: false),
                    CreatedAtUtc = table.Column<long>(nullable: false),
                    CreatedByUserId = table.Column<Guid>(nullable: true),
                    UpdatedAtUtc = table.Column<long>(nullable: true),
                    UpdatedByUserId = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false),
                    DeletedAtUtc = table.Column<long>(nullable: true),
                    DeletedByUserId = table.Column<Guid>(nullable: true),
                    DeletionReason = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipment_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false),
                    TokenHash = table.Column<string>(maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<long>(nullable: false),
                    ExpiresAtUtc = table.Column<long>(nullable: false),
                    RevokedAtUtc = table.Column<long>(nullable: true),
                    ComputerName = table.Column<string>(maxLength: 255, nullable: true),
                    IpAddress = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Calibrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EquipmentId = table.Column<Guid>(nullable: false),
                    PerformedAtUtc = table.Column<long>(nullable: false),
                    NextCalibrationUtc = table.Column<long>(nullable: true),
                    Laboratory = table.Column<string>(nullable: false),
                    CertificateNumber = table.Column<string>(nullable: true),
                    Result = table.Column<string>(maxLength: 40, nullable: false),
                    Traceability = table.Column<string>(nullable: true),
                    Notes = table.Column<string>(nullable: true),
                    CertificateDocumentId = table.Column<Guid>(nullable: true),
                    CreatedAtUtc = table.Column<long>(nullable: false),
                    CreatedByUserId = table.Column<Guid>(nullable: true),
                    UpdatedAtUtc = table.Column<long>(nullable: true),
                    UpdatedByUserId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calibrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calibrations_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EquipmentId = table.Column<Guid>(nullable: false),
                    OriginalName = table.Column<string>(nullable: false),
                    DocumentType = table.Column<string>(maxLength: 80, nullable: false),
                    RelativePath = table.Column<string>(nullable: false),
                    Sha256 = table.Column<string>(maxLength: 64, nullable: false),
                    SizeBytes = table.Column<long>(nullable: false),
                    Version = table.Column<int>(nullable: false),
                    ApprovedForAiKnowledge = table.Column<bool>(nullable: false),
                    CreatedAtUtc = table.Column<long>(nullable: false),
                    CreatedByUserId = table.Column<Guid>(nullable: true),
                    UpdatedAtUtc = table.Column<long>(nullable: true),
                    UpdatedByUserId = table.Column<Guid>(nullable: true),
                    IsDeleted = table.Column<bool>(nullable: false),
                    DeletedAtUtc = table.Column<long>(nullable: true),
                    DeletedByUserId = table.Column<Guid>(nullable: true),
                    DeletionReason = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentDocuments_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentLocationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EquipmentId = table.Column<Guid>(nullable: false),
                    PreviousLocationId = table.Column<Guid>(nullable: true),
                    NewLocationId = table.Column<Guid>(nullable: true),
                    ChangedAtUtc = table.Column<long>(nullable: false),
                    Reason = table.Column<string>(nullable: false),
                    DeliveredBy = table.Column<string>(nullable: true),
                    ReceivedBy = table.Column<string>(nullable: true),
                    ChangedByUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentLocationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentLocationHistory_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EquipmentId = table.Column<Guid>(nullable: false),
                    PreviousStatus = table.Column<string>(maxLength: 60, nullable: false),
                    NewStatus = table.Column<string>(maxLength: 60, nullable: false),
                    ChangedAtUtc = table.Column<long>(nullable: false),
                    Reason = table.Column<string>(nullable: false),
                    ChangedByUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentStatusHistory_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Failures",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EquipmentId = table.Column<Guid>(nullable: false),
                    ReportedAtUtc = table.Column<long>(nullable: false),
                    Description = table.Column<string>(nullable: false),
                    ReportedBy = table.Column<string>(nullable: false),
                    Service = table.Column<string>(nullable: true),
                    Impact = table.Column<string>(nullable: true),
                    EquipmentStopped = table.Column<bool>(nullable: false),
                    ImmediateAction = table.Column<string>(nullable: true),
                    Diagnosis = table.Column<string>(nullable: true),
                    Solution = table.Column<string>(nullable: true),
                    DowntimeMinutes = table.Column<int>(nullable: true),
                    ClosedAtUtc = table.Column<long>(nullable: true),
                    CreatedAtUtc = table.Column<long>(nullable: false),
                    CreatedByUserId = table.Column<Guid>(nullable: true),
                    UpdatedAtUtc = table.Column<long>(nullable: true),
                    UpdatedByUserId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Failures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Failures_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EquipmentId = table.Column<Guid>(nullable: false),
                    Type = table.Column<string>(maxLength: 40, nullable: false),
                    WorkOrderNumber = table.Column<string>(nullable: true),
                    ScheduledAtUtc = table.Column<long>(nullable: true),
                    PerformedAtUtc = table.Column<long>(nullable: false),
                    PerformedBy = table.Column<string>(nullable: false),
                    Responsible = table.Column<string>(nullable: true),
                    Activities = table.Column<string>(nullable: false),
                    Findings = table.Column<string>(nullable: true),
                    TechnicalDiagnosis = table.Column<string>(nullable: true),
                    ActionsTaken = table.Column<string>(nullable: true),
                    SparePartsSummary = table.Column<string>(nullable: true),
                    Cost = table.Column<double>(nullable: true),
                    DowntimeMinutes = table.Column<int>(nullable: true),
                    Result = table.Column<string>(maxLength: 50, nullable: false),
                    ResultingStatus = table.Column<string>(maxLength: 60, nullable: true),
                    NextMaintenanceUtc = table.Column<long>(nullable: true),
                    Notes = table.Column<string>(nullable: true),
                    SignatureName = table.Column<string>(nullable: true),
                    CreatedAtUtc = table.Column<long>(nullable: false),
                    CreatedByUserId = table.Column<Guid>(nullable: true),
                    UpdatedAtUtc = table.Column<long>(nullable: true),
                    UpdatedByUserId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TechnoVigilanceIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    EquipmentId = table.Column<Guid>(nullable: false),
                    OccurredAtUtc = table.Column<long>(nullable: false),
                    EventType = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: false),
                    Consequence = table.Column<string>(nullable: true),
                    ReportedBy = table.Column<string>(nullable: false),
                    Status = table.Column<string>(maxLength: 40, nullable: false),
                    ActionsTaken = table.Column<string>(nullable: true),
                    WasReportedExternally = table.Column<bool>(nullable: false),
                    ExternalReportNumber = table.Column<string>(nullable: true),
                    ExternalReportDateUtc = table.Column<long>(nullable: true),
                    ReportedEntity = table.Column<string>(nullable: true),
                    Conclusions = table.Column<string>(nullable: true),
                    RelatedMaintenanceId = table.Column<Guid>(nullable: true),
                    CreatedAtUtc = table.Column<long>(nullable: false),
                    CreatedByUserId = table.Column<Guid>(nullable: true),
                    UpdatedAtUtc = table.Column<long>(nullable: true),
                    UpdatedByUserId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnoVigilanceIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnoVigilanceIncidents_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EquipmentId",
                table: "AuditLogs",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TimestampUtc",
                table: "AuditLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Calibrations_EquipmentId_PerformedAtUtc",
                table: "Calibrations",
                columns: new[] { "EquipmentId", "PerformedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Brand",
                table: "Equipment",
                column: "Brand");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Category",
                table: "Equipment",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Criticality",
                table: "Equipment",
                column: "Criticality");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_InternalCode",
                table: "Equipment",
                column: "InternalCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_LocationId",
                table: "Equipment",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Manufacturer",
                table: "Equipment",
                column: "Manufacturer");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Model",
                table: "Equipment",
                column: "Model");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Name",
                table: "Equipment",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_NextCalibrationUtc",
                table: "Equipment",
                column: "NextCalibrationUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_NextMaintenanceUtc",
                table: "Equipment",
                column: "NextMaintenanceUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_PublicId",
                table: "Equipment",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_ResponsibleName",
                table: "Equipment",
                column: "ResponsibleName");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_SerialNumber",
                table: "Equipment",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Status",
                table: "Equipment",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocuments_EquipmentId_DocumentType",
                table: "EquipmentDocuments",
                columns: new[] { "EquipmentId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentLocationHistory_EquipmentId_ChangedAtUtc",
                table: "EquipmentLocationHistory",
                columns: new[] { "EquipmentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentResponsibleHistory_EquipmentId_AssignedAtUtc",
                table: "EquipmentResponsibleHistory",
                columns: new[] { "EquipmentId", "AssignedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentStatusHistory_EquipmentId_ChangedAtUtc",
                table: "EquipmentStatusHistory",
                columns: new[] { "EquipmentId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Failures_EquipmentId_ReportedAtUtc",
                table: "Failures",
                columns: new[] { "EquipmentId", "ReportedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Service",
                table: "Locations",
                column: "Service");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_EquipmentId_PerformedAtUtc",
                table: "MaintenanceRecords",
                columns: new[] { "EquipmentId", "PerformedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryRequirements_StandardName",
                table: "RegulatoryRequirements",
                column: "StandardName");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ExpiresAtUtc",
                table: "Sessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TokenHash",
                table: "Sessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnoVigilanceIncidents_EquipmentId_OccurredAtUtc",
                table: "TechnoVigilanceIncidents",
                columns: new[] { "EquipmentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedUsername",
                table: "Users",
                column: "NormalizedUsername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role",
                unique: true,
                filter: ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase)
                    ? "[Role] = N'MasterAdministrator' AND [IsDeleted] = CAST(0 AS bit)"
                    : "\"Role\" = 'MasterAdministrator' AND \"IsDeleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Calibrations");

            migrationBuilder.DropTable(
                name: "EquipmentDocuments");

            migrationBuilder.DropTable(
                name: "EquipmentLocationHistory");

            migrationBuilder.DropTable(
                name: "EquipmentResponsibleHistory");

            migrationBuilder.DropTable(
                name: "EquipmentStatusHistory");

            migrationBuilder.DropTable(
                name: "Failures");

            migrationBuilder.DropTable(
                name: "MaintenanceRecords");

            migrationBuilder.DropTable(
                name: "RegulatoryRequirements");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "TechnoVigilanceIncidents");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Locations");
        }
    }
}
