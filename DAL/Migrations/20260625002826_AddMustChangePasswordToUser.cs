using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMustChangePasswordToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "must_change_password",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_changed_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "retrieved_chunk_ids",
                table: "chat_messages",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(4445));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(4447));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(4449));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(4451));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(4452));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(4478));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(4480));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(4481));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(1862));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(1868));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(1869));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "created_at",
                value: new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(1872));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "must_change_password",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_changed_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "retrieved_chunk_ids",
                table: "chat_messages");

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7372));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7374));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7375));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7376));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7378));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7399));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7401));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7402));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(5129));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(5132));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(5134));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "created_at",
                value: new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(5135));
        }
    }
}
