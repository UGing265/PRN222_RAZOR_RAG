using System;
using System.Collections.Generic;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace DAL.Data;

public partial class DBContext : DbContext
{
    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentChapter> DocumentChapters { get; set; }

    public virtual DbSet<DocumentChunk> DocumentChunks { get; set; }

    public virtual DbSet<DocumentFile> DocumentFiles { get; set; }

    public virtual DbSet<UploadJob> UploadJobs { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }


    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<UserBookmark> UserBookmarks { get; set; }

    public virtual DbSet<DocumentReport> DocumentReports { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<DocumentType> DocumentTypes { get; set; }

    public virtual DbSet<Language> Languages { get; set; }

    public virtual DbSet<DocumentSource> DocumentSources { get; set; }

    public virtual DbSet<AcademicTerm> AcademicTerms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("pg_trgm")
            .HasPostgresExtension("uuid-ossp")
            .HasPostgresExtension("vector");

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subjects_pkey");
            entity.ToTable("subjects");
            entity.HasIndex(e => e.Code, "subjects_code_key").IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.Code).HasMaxLength(50).HasColumnName("code");
            entity.Property(e => e.Name).HasMaxLength(200).HasColumnName("name");
            entity.Property(e => e.AcademicTermId).HasColumnName("academic_term_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(d => d.AcademicTerm).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.AcademicTermId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_subjects_academic_term");
        });

        modelBuilder.Entity<UserBookmark>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.DocumentId }).HasName("user_bookmarks_pkey");
            entity.ToTable("user_bookmarks");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(d => d.User).WithMany(p => p.UserBookmarks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_bookmarks_user_id_fkey");

            entity.HasOne(d => d.Document).WithMany(p => p.UserBookmarks)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("user_bookmarks_document_id_fkey");
        });

        modelBuilder.Entity<DocumentReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("document_reports_pkey");
            entity.ToTable("document_reports");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.ReporterUserId).HasColumnName("reporter_user_id");
            entity.Property(e => e.Reason).HasMaxLength(255).HasColumnName("reason");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValueSql("'pending'::character varying").HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentReports)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("document_reports_document_id_fkey");

            entity.HasOne(d => d.ReporterUser).WithMany(p => p.DocumentReports)
                .HasForeignKey(d => d.ReporterUserId)
                .HasConstraintName("document_reports_reporter_user_id_fkey");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_logs_pkey");
            entity.ToTable("audit_logs");
            entity.HasIndex(e => e.CreatedAt, "idx_audit_logs_created_at");
            entity.HasIndex(e => e.UserId, "idx_audit_logs_user_id");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Action).HasMaxLength(50).HasColumnName("action");
            entity.Property(e => e.TargetTable).HasMaxLength(50).HasColumnName("target_table");
            entity.Property(e => e.TargetId).HasColumnName("target_id");
            entity.Property(e => e.IpAddress).HasMaxLength(45).HasColumnName("ip_address");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("audit_logs_user_id_fkey");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("documents_pkey");

            entity.ToTable("documents");

            entity.HasIndex(e => e.OwnerUserId, "idx_documents_owner_user_id");

        
            entity.HasIndex(e => e.Status, "idx_documents_status");

            entity.HasIndex(e => e.SubjectId, "idx_documents_subject_id");

            entity.HasIndex(e => e.Visibility, "idx_documents_visibility");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.Md5Hash)
                .HasMaxLength(32)
                .HasColumnName("md5_hash");
            entity.HasIndex(e => e.Md5Hash, "idx_documents_md5_hash");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.LanguageId).HasColumnName("language_id");
            entity.Property(e => e.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(e => e.PageCount).HasColumnName("page_count");
                    entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.DocumentTypeId).HasColumnName("document_type_id");
            entity.Property(e => e.AcademicTermId).HasColumnName("academic_term_id");
            entity.Property(e => e.ViewCount).HasDefaultValue(0).HasColumnName("view_count");
            entity.Property(e => e.DownloadCount).HasDefaultValue(0).HasColumnName("download_count");
            entity.Property(e => e.SearchText).HasColumnName("search_text");
            entity.Property(e => e.Slug)
                .HasMaxLength(255)
                .HasColumnName("slug");
            entity.Property(e => e.SourceType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'upload'::character varying")
                .HasColumnName("source_type");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TotalChunks)
                .HasDefaultValue(0)
                .HasColumnName("total_chunks");
            entity.Property(e => e.TotalChapters)
                .HasDefaultValue(0)
                .HasColumnName("total_chapters");
            entity.Property(e => e.Title)
                .HasMaxLength(500)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Visibility)
                .HasMaxLength(50)
                .HasDefaultValueSql("'school_wide'::character varying")
                .HasColumnName("visibility");

            entity.HasOne(d => d.OwnerUser).WithMany(p => p.Documents)
                .HasForeignKey(d => d.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("documents_owner_user_id_fkey");

            entity.HasOne(d => d.Subject).WithMany(p => p.Documents)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("documents_subject_id_fkey");

            entity.HasOne(d => d.DocumentType).WithMany(p => p.Documents)
                .HasForeignKey(d => d.DocumentTypeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_documents_document_type");

            entity.HasOne(d => d.Language).WithMany(p => p.Documents)
                .HasForeignKey(d => d.LanguageId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_documents_language");

            entity.HasOne(d => d.AcademicTerm).WithMany(p => p.Documents)
                .HasForeignKey(d => d.AcademicTermId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_documents_academic_term");

            entity.HasMany(d => d.Tags).WithMany(p => p.Documents)
                .UsingEntity<Dictionary<string, object>>(
                    "DocumentTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .HasConstraintName("document_tags_tag_id_fkey"),
                    l => l.HasOne<Document>().WithMany()
                        .HasForeignKey("DocumentId")
                        .HasConstraintName("document_tags_document_id_fkey"),
                    j =>
                    {
                        j.HasKey("DocumentId", "TagId").HasName("document_tags_pkey");
                        j.ToTable("document_tags");
                        j.HasIndex(new[] { "TagId" }, "idx_document_tags_tag_id");
                        j.IndexerProperty<Guid>("DocumentId").HasColumnName("document_id");
                        j.IndexerProperty<Guid>("TagId").HasColumnName("tag_id");
                    });
        });

        modelBuilder.Entity<DocumentChapter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("document_chapters_pkey");

            entity.ToTable("document_chapters");

            entity.HasIndex(e => e.DocumentId, "idx_document_chapters_document_id");

            entity.HasIndex(e => e.ParentChapterId, "idx_document_chapters_parent_id");

            entity.HasIndex(e => new { e.DocumentId, e.ChapterOrder }, "idx_document_chapters_order").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
            entity.Property(e => e.EndPage).HasColumnName("end_page");
            entity.Property(e => e.StartPage).HasColumnName("start_page");
            entity.Property(e => e.IsAiGenerated).HasColumnName("is_ai_generated");
            entity.Property(e => e.ChapterOrder).HasColumnName("chapter_order");
            entity.Property(e => e.StartChunkIndex).HasColumnName("start_chunk_index");
            entity.Property(e => e.EndChunkIndex).HasColumnName("end_chunk_index");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.ParentChapterId).HasColumnName("parent_chapter_id");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.Title)
                .HasMaxLength(400)
                .HasColumnName("title");

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentChapters)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("document_chapters_document_id_fkey");

            entity.HasOne(d => d.ParentChapter).WithMany(p => p.InverseParentChapter)
                .HasForeignKey(d => d.ParentChapterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("document_chapters_parent_chapter_id_fkey");
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("document_chunks_pkey");

            entity.ToTable("document_chunks");

            entity.HasIndex(e => e.ChapterId, "idx_document_chunks_chapter_id");

            entity.HasIndex(e => e.DocumentId, "idx_document_chunks_document_id");

            entity.HasIndex(e => e.Metadata, "idx_document_chunks_metadata_gin").HasMethod("gin");

            entity.HasIndex(e => e.PageNumber, "idx_document_chunks_page_number");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ChapterId).HasColumnName("chapter_id");
            entity.Property(e => e.ChunkHash)
                .HasMaxLength(64)
                .HasColumnName("chunk_hash");
            entity.Property(e => e.ChunkOrder).HasColumnName("chunk_order");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.ContentTokens).HasColumnName("content_tokens");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.Embedding)
                .HasColumnType("vector(3072)")
                .HasColumnName("embedding");
            entity.Property(e => e.Metadata)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.PageNumber).HasColumnName("page_number");

            entity.HasOne(d => d.Chapter).WithMany(p => p.DocumentChunks)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("document_chunks_chapter_id_fkey");

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentChunks)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("document_chunks_document_id_fkey");
        });

        modelBuilder.Entity<DocumentFile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("document_files_pkey");

            entity.ToTable("document_files");

            entity.HasIndex(e => e.DocumentId, "idx_document_files_document_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ChecksumSha256)
                .HasMaxLength(64)
                .HasColumnName("checksum_sha256");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.ExtractedText).HasColumnName("extracted_text");
            entity.Property(e => e.FileUrl).HasColumnName("file_url");
            entity.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(e => e.MimeType)
                .HasMaxLength(100)
                .HasColumnName("mime_type");
            entity.Property(e => e.OriginalFilename)
                .HasMaxLength(255)
                .HasColumnName("original_filename");
            entity.Property(e => e.PageCount).HasColumnName("page_count");
            entity.Property(e => e.ExtractionStatus)
                .HasMaxLength(50)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("extraction_status");
            entity.Property(e => e.StoragePath).HasColumnName("storage_path");
            entity.Property(e => e.S3Bucket)
                .HasMaxLength(128)
                .HasColumnName("s3_bucket");
            entity.Property(e => e.S3Key)
                .HasMaxLength(512)
                .HasColumnName("s3_key");

            entity.HasOne(d => d.Document).WithMany(p => p.DocumentFiles)
                .HasForeignKey(d => d.DocumentId)
                .HasConstraintName("document_files_document_id_fkey");
        });

        modelBuilder.Entity<UploadJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("upload_jobs_pkey");
            entity.ToTable("upload_jobs");
            entity.HasIndex(e => e.OwnerUserId, "idx_upload_jobs_owner_user_id");
            entity.HasIndex(e => e.DocumentId, "idx_upload_jobs_document_id");
            entity.HasIndex(e => e.Status, "idx_upload_jobs_status");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.FileName).HasMaxLength(255).HasColumnName("file_name");
            entity.Property(e => e.StoragePath).HasColumnName("storage_path");
            entity.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValueSql("'pending'::character varying").HasColumnName("status");
            entity.Property(e => e.ProgressPercent).HasDefaultValue(0).HasColumnName("progress_percent");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.IsNotified).HasDefaultValue(false).HasColumnName("is_notified");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()").HasColumnName("updated_at");
            entity.HasOne(d => d.OwnerUser).WithMany(p => p.UploadJobs).HasForeignKey(d => d.OwnerUserId).HasConstraintName("upload_jobs_owner_user_id_fkey");
            entity.HasOne(d => d.Document).WithMany(p => p.UploadJobs).HasForeignKey(d => d.DocumentId).HasConstraintName("upload_jobs_document_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "roles_role_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tags_pkey");

            entity.ToTable("tags");

            entity.HasIndex(e => e.Name, "tags_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Slug)
                .HasMaxLength(120)
                .HasColumnName("slug");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.RoleId, "idx_users_role_id");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("users_role_id_fkey");
        });

        // Seed Roles
        var roleAdmin = new Role { Id = 1, Name = "Admin" };
        var roleLecturer = new Role { Id = 2, Name = "Lecturer" };
        var roleStudent = new Role { Id = 3, Name = "Student" };
        modelBuilder.Entity<Role>().HasData(roleAdmin, roleLecturer, roleStudent);

        // Seed Subjects
        var subjectPRN222 = new Subject { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Code = "PRN222", Name = "Phát triển ứng dụng với .NET", CreatedAt = DateTime.UtcNow };
        var subjectSWD392 = new Subject { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Code = "SWD392", Name = "Kỹ nghệ phần mềm", CreatedAt = DateTime.UtcNow };
        var subjectDBI202 = new Subject { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Code = "DBI202", Name = "Cơ sở dữ liệu", CreatedAt = DateTime.UtcNow };
        var subjectPRN231 = new Subject { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Code = "PRN231", Name = "Lập trình ứng dụng phân tán", CreatedAt = DateTime.UtcNow };

        modelBuilder.Entity<Subject>().HasData(subjectPRN222, subjectSWD392, subjectDBI202, subjectPRN231);

        // Map DocumentType table
        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("document_types_pkey");
            entity.ToTable("document_types");
            entity.HasIndex(e => e.Name, "document_types_name_key").IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(50).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        });

        // Map Language table
        modelBuilder.Entity<Language>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("languages_pkey");
            entity.ToTable("languages");
            entity.HasIndex(e => e.Code, "languages_code_key").IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.Code).HasMaxLength(10).HasColumnName("code");
            entity.Property(e => e.Name).HasMaxLength(50).HasColumnName("name");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        });

        // Map DocumentSource table
        modelBuilder.Entity<DocumentSource>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("document_sources_pkey");
            entity.ToTable("document_sources");
            entity.HasIndex(e => e.Name, "document_sources_name_key").IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(200).HasColumnName("name");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        });

        // Map AcademicTerm table
        modelBuilder.Entity<AcademicTerm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("academic_terms_pkey");
            entity.ToTable("academic_terms");
            entity.HasIndex(e => e.Name, "academic_terms_name_key").IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(200).HasColumnName("name");
            entity.Property(e => e.Order).HasColumnName("term_order");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()").HasColumnName("created_at");
        });

        // Seed DocumentTypes
        var docTypeSyllabus = new DocumentType { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-111111111111"), Name = "Giáo trình", CreatedAt = DateTime.UtcNow };
        var docTypeSlides = new DocumentType { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-222222222222"), Name = "Slide bài giảng", CreatedAt = DateTime.UtcNow };
        var docTypeExams = new DocumentType { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-333333333333"), Name = "Đề thi mẫu", CreatedAt = DateTime.UtcNow };
        var docTypeReferences = new DocumentType { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-444444444444"), Name = "Tài liệu tham khảo", CreatedAt = DateTime.UtcNow };
        var docTypeLabs = new DocumentType { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-555555555555"), Name = "Hướng dẫn thực hành", CreatedAt = DateTime.UtcNow };

        modelBuilder.Entity<DocumentType>().HasData(docTypeSyllabus, docTypeSlides, docTypeExams, docTypeReferences, docTypeLabs);

        // Seed Languages
        var langVi = new Language { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-111111111111"), Code = "vi", Name = "Tiếng Việt", CreatedAt = DateTime.UtcNow };
        var langEn = new Language { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-222222222222"), Code = "en", Name = "Tiếng Anh", CreatedAt = DateTime.UtcNow };
        var langJa = new Language { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-333333333333"), Code = "ja", Name = "Tiếng Nhật", CreatedAt = DateTime.UtcNow };

        modelBuilder.Entity<Language>().HasData(langVi, langEn, langJa);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
