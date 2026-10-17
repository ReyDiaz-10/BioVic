using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioVic.Infrastructure.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    SnoozedUntilUtc = table.Column<long>(type: "bigint", nullable: true),
                    ResolvedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimestampUtc = table.Column<long>(type: "bigint", nullable: false),
                    TimestampLocal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComputerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecordHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentResponsibleHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousResponsible = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewResponsible = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentResponsibleHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Institution = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Site = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Building = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Floor = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Service = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Room = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SpecificLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SearchTextNormalized = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegulatoryRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ArticleOrSection = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveFromUtc = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulatoryRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    NormalizedUsername = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    HasAllLocations = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutEndUtc = table.Column<long>(type: "bigint", nullable: true),
                    LastLoginAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    PasswordChangedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InternalCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    EquipmentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    InventoryNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssetCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegulatoryRegistration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegulatoryExpiryUtc = table.Column<long>(type: "bigint", nullable: true),
                    RiskClassification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegulatoryCondition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsControlledTechnology = table.Column<bool>(type: "bit", nullable: true),
                    RegulatoryNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcquisitionDateUtc = table.Column<long>(type: "bigint", nullable: true),
                    ReceptionDateUtc = table.Column<long>(type: "bigint", nullable: true),
                    InstallationDateUtc = table.Column<long>(type: "bigint", nullable: true),
                    CommissioningDateUtc = table.Column<long>(type: "bigint", nullable: true),
                    Supplier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcquisitionCost = table.Column<double>(type: "float", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcquisitionMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ownership = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WarrantyStartUtc = table.Column<long>(type: "bigint", nullable: true),
                    WarrantyEndUtc = table.Column<long>(type: "bigint", nullable: true),
                    EstimatedUsefulLifeMonths = table.Column<int>(type: "int", nullable: true),
                    ExpectedReplacementUtc = table.Column<long>(type: "bigint", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsibleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ResponsiblePosition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsibleArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsiblePhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsibleEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Voltage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Power = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Current = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Battery = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Connections = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoftwareName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoftwareVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MacAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TechnicalCharacteristics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Accessories = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Consumables = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomPropertiesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Criticality = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequiresCalibration = table.Column<bool>(type: "bit", nullable: false),
                    NextMaintenanceUtc = table.Column<long>(type: "bigint", nullable: true),
                    NextCalibrationUtc = table.Column<long>(type: "bigint", nullable: true),
                    PublicOperationalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SearchTextNormalized = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    ExpiresAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    RevokedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    ComputerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
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
                name: "UserLocationPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLocationPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLocationPermissions_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserLocationPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Calibrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    NextCalibrationUtc = table.Column<long>(type: "bigint", nullable: true),
                    Laboratory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertificateNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Traceability = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ApprovedForAiKnowledge = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NewLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveredBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    NewStatus = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ChangedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Service = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Impact = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EquipmentStopped = table.Column<bool>(type: "bit", nullable: false),
                    ImmediateAction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Solution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DowntimeMinutes = table.Column<int>(type: "int", nullable: true),
                    ClosedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WorkOrderNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    PerformedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Responsible = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TechnicalDiagnosis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionsTaken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SparePartsSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cost = table.Column<double>(type: "float", nullable: true),
                    DowntimeMinutes = table.Column<int>(type: "int", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResultingStatus = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    NextMaintenanceUtc = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Consequence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActionsTaken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WasReportedExternally = table.Column<bool>(type: "bit", nullable: false),
                    ExternalReportNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalReportDateUtc = table.Column<long>(type: "bigint", nullable: true),
                    ReportedEntity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Conclusions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedMaintenanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                name: "IX_AdminNotifications_EquipmentId",
                table: "AdminNotifications",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_ResolvedAtUtc_SnoozedUntilUtc",
                table: "AdminNotifications",
                columns: new[] { "ResolvedAtUtc", "SnoozedUntilUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_SourceKey",
                table: "AdminNotifications",
                column: "SourceKey",
                unique: true);

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
                name: "IX_UserLocationPermissions_LocationId",
                table: "UserLocationPermissions",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLocationPermissions_UserId_LocationId",
                table: "UserLocationPermissions",
                columns: new[] { "UserId", "LocationId" },
                unique: true);

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
                filter: "[Role] = N'MasterAdministrator' AND [IsDeleted] = CAST(0 AS bit)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminNotifications");

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
                name: "UserLocationPermissions");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Locations");
        }
    }
}
