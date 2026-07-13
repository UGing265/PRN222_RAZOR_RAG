using System;

namespace DAL.Entities;

public partial class TokenUsage
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateOnly UsageDate { get; set; }

    public int ChatTokens { get; set; }

    public int DocTokens { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
