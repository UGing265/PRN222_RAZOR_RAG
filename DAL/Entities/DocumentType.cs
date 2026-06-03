using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class DocumentType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
