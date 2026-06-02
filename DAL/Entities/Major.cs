using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Major
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
