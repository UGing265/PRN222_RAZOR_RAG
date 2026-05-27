using System;

namespace DAL.Entities;

public partial class UploadJob
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public Guid? DocumentId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string? StoragePath { get; set; }

    public long FileSizeBytes { get; set; }

    public string Status { get; set; } = string.Empty;

    public int ProgressPercent { get; set; }

    public string? Message { get; set; }

    public bool IsNotified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Document? Document { get; set; }

    public virtual User OwnerUser { get; set; } = null!;
}
