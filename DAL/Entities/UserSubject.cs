using System;

namespace DAL.Entities;

public partial class UserSubject
{
    public Guid UserId { get; set; }

    public Guid SubjectId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Subject Subject { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
