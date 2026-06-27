using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAcademicTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_documents_academic_term",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "fk_subjects_academic_term",
                table: "subjects");

            migrationBuilder.DropTable(
                name: "academic_terms");

            migrationBuilder.DropIndex(
                name: "IX_subjects_academic_term_id",
                table: "subjects");

            migrationBuilder.DropIndex(
                name: "IX_documents_academic_term_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "academic_term_id",
                table: "subjects");

            migrationBuilder.DropColumn(
                name: "academic_term_id",
                table: "documents");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "academic_term_id",
                table: "subjects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "academic_term_id",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "academic_terms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    term_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("academic_terms_pkey", x => x.id);
                });

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
                columns: new[] { "academic_term_id", "created_at" },
                values: new object[] { null, new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(1862) });

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "academic_term_id", "created_at" },
                values: new object[] { null, new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(1868) });

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                columns: new[] { "academic_term_id", "created_at" },
                values: new object[] { null, new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(1869) });

            migrationBuilder.UpdateData(
                table: "subjects",
                keyColumn: "id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"),
                columns: new[] { "academic_term_id", "created_at" },
                values: new object[] { null, new DateTime(2026, 6, 25, 0, 28, 26, 179, DateTimeKind.Utc).AddTicks(1872) });

            migrationBuilder.CreateIndex(
                name: "IX_subjects_academic_term_id",
                table: "subjects",
                column: "academic_term_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_academic_term_id",
                table: "documents",
                column: "academic_term_id");

            migrationBuilder.CreateIndex(
                name: "academic_terms_name_key",
                table: "academic_terms",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_documents_academic_term",
                table: "documents",
                column: "academic_term_id",
                principalTable: "academic_terms",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_subjects_academic_term",
                table: "subjects",
                column: "academic_term_id",
                principalTable: "academic_terms",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
