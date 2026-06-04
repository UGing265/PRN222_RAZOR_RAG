using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Subject
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public Guid? AcademicTermId { get; set; }

    public virtual AcademicTerm? AcademicTerm { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<UserSubject> UserSubjects { get; set; } = new List<UserSubject>();
}

