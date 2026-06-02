using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class AcademicTerm
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
