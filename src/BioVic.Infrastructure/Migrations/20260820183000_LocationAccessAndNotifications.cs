using BioVic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioVic.Infrastructure.Migrations;

[DbContext(typeof(BioVicDbContext))]
[Migration("20260820183000_LocationAccessAndNotifications")]
public sealed class LocationAccessAndNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "HasAllLocations",
            table: "Users",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "AdminNotifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SourceKey = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                NotificationType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                Severity = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                EquipmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                LocationId = table.Column<Guid>(type: "TEXT", nullable: true),
                DueAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                SnoozedUntilUtc = table.Column<long>(type: "INTEGER", nullable: true),
                ResolvedAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                UpdatedAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AdminNotifications", item => item.Id));

        migrationBuilder.CreateTable(
            name: "UserLocationPermissions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                UpdatedAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserLocationPermissions", item => item.Id);
                table.ForeignKey("FK_UserLocationPermissions_Locations_LocationId", item => item.LocationId,
                    principalTable: "Locations", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_UserLocationPermissions_Users_UserId", item => item.UserId,
                    principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_AdminNotifications_EquipmentId", "AdminNotifications", "EquipmentId");
        migrationBuilder.CreateIndex("IX_AdminNotifications_ResolvedAtUtc_SnoozedUntilUtc", "AdminNotifications", new[] { "ResolvedAtUtc", "SnoozedUntilUtc" });
        migrationBuilder.CreateIndex("IX_AdminNotifications_SourceKey", "AdminNotifications", "SourceKey", unique: true);
        migrationBuilder.CreateIndex("IX_UserLocationPermissions_LocationId", "UserLocationPermissions", "LocationId");
        migrationBuilder.CreateIndex("IX_UserLocationPermissions_UserId_LocationId", "UserLocationPermissions", new[] { "UserId", "LocationId" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminNotifications");
        migrationBuilder.DropTable(name: "UserLocationPermissions");
        migrationBuilder.DropColumn(name: "HasAllLocations", table: "Users");
    }
}
