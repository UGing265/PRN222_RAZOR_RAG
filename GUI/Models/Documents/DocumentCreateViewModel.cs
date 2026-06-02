using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace GUI.Models.Documents;

public class DocumentCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề tài liệu.")]
    [StringLength(500, ErrorMessage = "Tiêu đề không được vượt quá 500 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn môn học.")]
    public Guid? SubjectId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại học liệu.")]
    public Guid? DocumentTypeId { get; set; }

    public Guid? AcademicTermId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngôn ngữ.")]
    public Guid? LanguageId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn quyền hiển thị.")]
    [StringLength(30, ErrorMessage = "Quyền hiển thị không được vượt quá 30 ký tự.")]
    public string Visibility { get; set; } = "school_wide";

    [StringLength(30, ErrorMessage = "Source type không được vượt quá 30 ký tự.")]
    public string? SourceType { get; set; } = "upload";

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn file để upload.")]
    public IFormFile? UploadFile { get; set; }
}
