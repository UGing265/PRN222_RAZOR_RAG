using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class DocumentSource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
