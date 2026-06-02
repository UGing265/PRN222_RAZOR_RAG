using BLL.DTOs.Documents;
using System.Collections.Generic;

namespace GUI.Models.Documents;

public class AdminMetadataViewModel
{
    public List<SubjectDto> Subjects { get; set; } = new();
    public List<DocumentTypeDto> DocumentTypes { get; set; } = new();
    public List<LanguageDto> Languages { get; set; } = new();
    public List<DocumentSourceDto> DocumentSources { get; set; } = new();
    public List<AcademicTermDto> AcademicTerms { get; set; } = new();
}
