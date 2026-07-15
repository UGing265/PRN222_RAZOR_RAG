using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageHourRemoveTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_tags");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropIndex(
                name: "token_usage_user_date_key",
                table: "token_usage");

            migrationBuilder.RenameColumn(
                name: "UsageHour",
                table: "token_usage",
                newName: "usage_hour");

            migrationBuilder.AlterColumn<byte>(
                name: "usage_hour",
                table: "token_usage",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "smallint");

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(8318));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(8320));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(8322));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(8323));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(8324));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(8346));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(8348));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(8350));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(6243));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(6246));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(6247));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 47, 1, 149, DateTimeKind.Utc).AddTicks(6249));

            migrationBuilder.CreateIndex(
                name: "token_usage_user_date_key",
                table: "token_usage",
                columns: new[] { "user_id", "usage_date", "usage_hour" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "token_usage_user_date_key",
                table: "token_usage");

            migrationBuilder.RenameColumn(
                name: "usage_hour",
                table: "token_usage",
                newName: "UsageHour");

            migrationBuilder.AlterColumn<byte>(
                name: "UsageHour",
                table: "token_usage",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldDefaultValue: (byte)0);

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tags_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_tags",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("document_tags_pkey", x => new { x.document_id, x.tag_id });
                    table.ForeignKey(
                        name: "document_tags_document_id_fkey",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "document_tags_tag_id_fkey",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(9179));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(9181));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(9182));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-444444444444"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(9184));

            migrationBuilder.UpdateData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(9185));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-111111111111"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(9207));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-222222222222"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(9209));

            migrationBuilder.UpdateData(
                table: "languages",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-333333333333"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(9211));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(7463));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(7466));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(7469));

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                column: "created_at",
                value: new DateTime(2026, 7, 15, 10, 40, 43, 153, DateTimeKind.Utc).AddTicks(7470));

            migrationBuilder.CreateIndex(
                name: "token_usage_user_date_key",
                table: "token_usage",
                columns: new[] { "user_id", "usage_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_document_tags_tag_id",
                table: "document_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "tags_name_key",
                table: "tags",
                column: "name",
                unique: true);
        }
    }
}
