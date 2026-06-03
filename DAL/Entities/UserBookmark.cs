using System;

namespace DAL.Entities;

public partial class UserBookmark
{
    public Guid UserId { get; set; }

    public Guid DocumentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
