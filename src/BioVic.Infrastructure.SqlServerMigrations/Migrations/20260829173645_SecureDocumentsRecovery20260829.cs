using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioVic.Infrastructure.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class SecureDocumentsRecovery20260829 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "EquipmentDocuments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "application/octet-stream");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "EquipmentDocuments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DocumentDateUtc",
                table: "EquipmentDocuments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExpiresAtUtc",
                table: "EquipmentDocuments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                table: "EquipmentDocuments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersedesDocumentId",
                table: "EquipmentDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDocuments_SupersedesDocumentId",
                table: "EquipmentDocuments",
                column: "SupersedesDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EquipmentDocuments_SupersedesDocumentId",
                table: "EquipmentDocuments");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "EquipmentDocuments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "EquipmentDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentDateUtc",
                table: "EquipmentDocuments");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "EquipmentDocuments");

            migrationBuilder.DropColumn(
                name: "IsCurrent",
                table: "EquipmentDocuments");

            migrationBuilder.DropColumn(
                name: "SupersedesDocumentId",
                table: "EquipmentDocuments");
        }
    }
}
