using DAL.Entities;
using System.Collections.Generic;

namespace GUI.Models.Documents;

public class AdminMetadataViewModel
{
    public List<Subject> Subjects { get; set; } = new();
    public List<DocumentType> DocumentTypes { get; set; } = new();
    public List<Language> Languages { get; set; } = new();
}
