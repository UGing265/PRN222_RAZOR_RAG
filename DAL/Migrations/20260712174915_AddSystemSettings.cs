using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "latency_ms",
                table: "chat_messages",
                type: "integer",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "token_count",
                table: "chat_messages",
                type: "integer",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "token_usage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usage_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    chat_tokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    doc_tokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("token_usage_pkey", x => x.id);
                    table.ForeignKey(
                        name: "token_usage_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "idx_token_usage_usage_date",
                table: "token_usage",
                column: "usage_date");

            migrationBuilder.CreateIndex(
                name: "idx_token_usage_user_id",
                table: "token_usage",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "token_usage_user_date_key",
                table: "token_usage",
                columns: new[] { "user_id", "usage_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "token_usage");

            migrationBuilder.DropColumn(
                name: "latency_ms",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "token_count",
                table: "chat_messages");

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(7923));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(7924));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(7926));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(7927));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(7928));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(7951));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(7953));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(7963));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(6074));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(6077));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(6078));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "created_at",
                value: new DateTime(2026, 6, 27, 4, 54, 30, 28, DateTimeKind.Utc).AddTicks(6080));
        }
    }
}
