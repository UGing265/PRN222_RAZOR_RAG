using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class MultiDocumentChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "academic_terms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    term_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("academic_terms_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("document_sources_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("document_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "languages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("languages_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("roles_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("tags_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    academic_term_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("subjects_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_subjects_academic_term",
                        column: x => x.academic_term_id,
                        principalTable: "academic_terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    role_id = table.Column<short>(type: "smallint", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    email_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    displayUsername = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                    table.ForeignKey(
                        name: "users_role_id_fkey",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_table = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("audit_logs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "audit_logs_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("chat_sessions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "chat_sessions_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    academic_term_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "'pending'::character varying"),
                    language_id = table.Column<Guid>(type: "uuid", nullable: true),
                    visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "'school_wide'::character varying"),
                    document_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    total_chunks = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_chapters = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    download_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    search_text = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    md5_hash = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("documents_pkey", x => x.id);
                    table.ForeignKey(
                        name: "documents_owner_user_id_fkey",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "documents_subject_id_fkey",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_documents_academic_term",
                        column: x => x.academic_term_id,
                        principalTable: "academic_terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_documents_document_source",
                        column: x => x.document_source_id,
                        principalTable: "document_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_documents_document_type",
                        column: x => x.document_type_id,
                        principalTable: "document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_documents_language",
                        column: x => x.language_id,
                        principalTable: "languages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_subjects",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_subjects_pkey", x => new { x.user_id, x.subject_id });
                    table.ForeignKey(
                        name: "user_subjects_subject_id_fkey",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "user_subjects_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("chat_messages_pkey", x => x.id);
                    table.ForeignKey(
                        name: "chat_messages_session_id_fkey",
                        column: x => x.session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_session_documents",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("chat_session_documents_pkey", x => new { x.session_id, x.document_id });
                    table.ForeignKey(
                        name: "chat_session_documents_document_id_fkey",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "chat_session_documents_session_id_fkey",
                        column: x => x.session_id,
                        principalTable: "chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_chapters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_chapter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    chapter_order = table.Column<int>(type: "integer", nullable: false),
                    start_page = table.Column<int>(type: "integer", nullable: true),
                    end_page = table.Column<int>(type: "integer", nullable: true),
                    start_chunk_index = table.Column<int>(type: "integer", nullable: true),
                    end_chunk_index = table.Column<int>(type: "integer", nullable: true),
                    is_ai_generated = table.Column<bool>(type: "boolean", nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("document_chapters_pkey", x => x.id);
                    table.ForeignKey(
                        name: "document_chapters_document_id_fkey",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "document_chapters_parent_chapter_id_fkey",
                        column: x => x.parent_chapter_id,
                        principalTable: "document_chapters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_path = table.Column<string>(type: "text", nullable: true),
                    s3_bucket = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    s3_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    file_url = table.Column<string>(type: "text", nullable: true),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checksum_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    extracted_text = table.Column<string>(type: "text", nullable: true),
                    extraction_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "'pending'::character varying"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("document_files_pkey", x => x.id);
                    table.ForeignKey(
                        name: "document_files_document_id_fkey",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "'pending'::character varying"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("document_reports_pkey", x => x.id);
                    table.ForeignKey(
                        name: "document_reports_document_id_fkey",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "document_reports_reporter_user_id_fkey",
                        column: x => x.reporter_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "upload_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_path = table.Column<string>(type: "text", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "'pending'::character varying"),
                    progress_percent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    message = table.Column<string>(type: "text", nullable: true),
                    is_notified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("upload_jobs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "upload_jobs_document_id_fkey",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "upload_jobs_owner_user_id_fkey",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_bookmarks",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_bookmarks_pkey", x => new { x.user_id, x.document_id });
                    table.ForeignKey(
                        name: "user_bookmarks_document_id_fkey",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "user_bookmarks_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chapter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chunk_order = table.Column<int>(type: "integer", nullable: false),
                    page_number = table.Column<int>(type: "integer", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    content_tokens = table.Column<int>(type: "integer", nullable: true),
                    chunk_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    embedding = table.Column<Vector>(type: "vector(3072)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("document_chunks_pkey", x => x.id);
                    table.ForeignKey(
                        name: "document_chunks_chapter_id_fkey",
                        column: x => x.chapter_id,
                        principalTable: "document_chapters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "document_chunks_document_id_fkey",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "document_types",
                columns: new[] { "id", "created_at", "description", "name" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-111111111111"), new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7372), null, "Giáo trình" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-222222222222"), new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7374), null, "Slide bài giảng" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-333333333333"), new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7375), null, "Đề thi mẫu" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-444444444444"), new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7376), null, "Tài liệu tham khảo" },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-555555555555"), new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7378), null, "Hướng dẫn thực hành" }
                });

            migrationBuilder.InsertData(
                table: "languages",
                columns: new[] { "id", "code", "created_at", "name" },
                values: new object[,]
                {
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-111111111111"), "vi", new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7399), "Tiếng Việt" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-222222222222"), "en", new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7401), "Tiếng Anh" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-333333333333"), "ja", new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(7402), "Tiếng Nhật" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { (short)1, "Admin" },
                    { (short)2, "Lecturer" },
                    { (short)3, "Student" }
                });

            migrationBuilder.InsertData(
                table: "subjects",
                columns: new[] { "id", "academic_term_id", "code", "created_at", "name" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555555"), null, "PRN222", new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(5129), "Phát triển ứng dụng với .NET" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), null, "SWD392", new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(5132), "Kỹ nghệ phần mềm" },
                    { new Guid("77777777-7777-7777-7777-777777777777"), null, "DBI202", new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(5134), "Cơ sở dữ liệu" },
                    { new Guid("88888888-8888-8888-8888-888888888888"), null, "PRN231", new DateTime(2026, 6, 17, 13, 28, 11, 732, DateTimeKind.Utc).AddTicks(5135), "Lập trình ứng dụng phân tán" }
                });

            migrationBuilder.CreateIndex(
                name: "academic_terms_name_key",
                table: "academic_terms",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_chat_messages_created_at",
                table: "chat_messages",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_chat_messages_session_id",
                table: "chat_messages",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_session_documents_document_id",
                table: "chat_session_documents",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "idx_chat_sessions_created_at",
                table: "chat_sessions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_chat_sessions_user_id",
                table: "chat_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_document_chapters_document_id",
                table: "document_chapters",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "idx_document_chapters_order",
                table: "document_chapters",
                columns: new[] { "document_id", "chapter_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_document_chapters_parent_id",
                table: "document_chapters",
                column: "parent_chapter_id");

            migrationBuilder.CreateIndex(
                name: "idx_document_chunks_chapter_id",
                table: "document_chunks",
                column: "chapter_id");

            migrationBuilder.CreateIndex(
                name: "idx_document_chunks_document_id",
                table: "document_chunks",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "idx_document_chunks_metadata_gin",
                table: "document_chunks",
                column: "metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "idx_document_chunks_page_number",
                table: "document_chunks",
                column: "page_number");

            migrationBuilder.CreateIndex(
                name: "idx_document_files_document_id",
                table: "document_files",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_reports_document_id",
                table: "document_reports",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_reports_reporter_user_id",
                table: "document_reports",
                column: "reporter_user_id");

            migrationBuilder.CreateIndex(
                name: "document_sources_name_key",
                table: "document_sources",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_document_tags_tag_id",
                table: "document_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "document_types_name_key",
                table: "document_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_documents_md5_hash",
                table: "documents",
                column: "md5_hash");

            migrationBuilder.CreateIndex(
                name: "idx_documents_owner_user_id",
                table: "documents",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_documents_status",
                table: "documents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_documents_subject_id",
                table: "documents",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "idx_documents_visibility",
                table: "documents",
                column: "visibility");

            migrationBuilder.CreateIndex(
                name: "IX_documents_academic_term_id",
                table: "documents",
                column: "academic_term_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_document_source_id",
                table: "documents",
                column: "document_source_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_document_type_id",
                table: "documents",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_language_id",
                table: "documents",
                column: "language_id");

            migrationBuilder.CreateIndex(
                name: "languages_code_key",
                table: "languages",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "roles_role_name_key",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subjects_academic_term_id",
                table: "subjects",
                column: "academic_term_id");

            migrationBuilder.CreateIndex(
                name: "subjects_code_key",
                table: "subjects",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "tags_name_key",
                table: "tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_upload_jobs_document_id",
                table: "upload_jobs",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "idx_upload_jobs_owner_user_id",
                table: "upload_jobs",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_upload_jobs_status",
                table: "upload_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_bookmarks_document_id",
                table: "user_bookmarks",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_subjects_subject_id",
                table: "user_subjects",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chat_session_documents");

            migrationBuilder.DropTable(
                name: "document_chunks");

            migrationBuilder.DropTable(
                name: "document_files");

            migrationBuilder.DropTable(
                name: "document_reports");

            migrationBuilder.DropTable(
                name: "document_tags");

            migrationBuilder.DropTable(
                name: "upload_jobs");

            migrationBuilder.DropTable(
                name: "user_bookmarks");

            migrationBuilder.DropTable(
                name: "user_subjects");

            migrationBuilder.DropTable(
                name: "chat_sessions");

            migrationBuilder.DropTable(
                name: "document_chapters");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "subjects");

            migrationBuilder.DropTable(
                name: "document_sources");

            migrationBuilder.DropTable(
                name: "document_types");

            migrationBuilder.DropTable(
                name: "languages");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "academic_terms");
        }
    }
}
