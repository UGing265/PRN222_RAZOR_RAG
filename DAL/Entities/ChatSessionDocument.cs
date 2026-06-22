using System;

namespace DAL.Entities;

public class ChatSessionDocument
{
    public Guid SessionId { get; set; }
    public Guid DocumentId { get; set; }

    public virtual ChatSession Session { get; set; } = null!;
    public virtual Document Document { get; set; } = null!;
}
