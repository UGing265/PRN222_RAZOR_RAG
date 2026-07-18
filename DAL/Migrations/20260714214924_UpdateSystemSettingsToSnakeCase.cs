using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSystemSettingsToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SystemSettings",
                table: "SystemSettings");

            migrationBuilder.RenameTable(
                name: "SystemSettings",
                newName: "system_settings");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "system_settings",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "system_settings",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Key",
                table: "system_settings",
                newName: "key");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "system_settings",
                newName: "updated_at");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "system_settings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddPrimaryKey(
                name: "system_settings_pkey",
                table: "system_settings",
                column: "key");

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(7619));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(7620));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(7638));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(7640));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(7641));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(7663));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(7665));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(7666));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(5870));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(5874));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(5877));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "created_at",
                value: new DateTime(2026, 7, 14, 21, 49, 23, 804, DateTimeKind.Utc).AddTicks(5878));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "system_settings_pkey",
                table: "system_settings");

            migrationBuilder.RenameTable(
                name: "system_settings",
                newName: "SystemSettings");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "SystemSettings",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "SystemSettings",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "key",
                table: "SystemSettings",
                newName: "Key");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "SystemSettings",
                newName: "UpdatedAt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "SystemSettings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SystemSettings",
                table: "SystemSettings",
                column: "Key");

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(6006));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(6008));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(6010));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(6012));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(6013));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(6039));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(6086));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(6087));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(3715));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(3719));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(3721));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "created_at",
                value: new DateTime(2026, 7, 12, 17, 49, 14, 834, DateTimeKind.Utc).AddTicks(3722));
        }
    }
}
