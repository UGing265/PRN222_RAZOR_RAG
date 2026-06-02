using System;

namespace DAL.Entities;

public partial class DocumentReport
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Guid ReporterUserId { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual User ReporterUser { get; set; } = null!;
}
