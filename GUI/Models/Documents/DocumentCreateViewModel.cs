using System.ComponentModel.DataAnnotations;

namespace GUI.Models.Documents;

public class DocumentCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề tài liệu.")]
    [StringLength(500, ErrorMessage = "Tiêu đề không được vượt quá 500 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Môn học không được vượt quá 200 ký tự.")]
    public string? Subject { get; set; }

    [StringLength(200, ErrorMessage = "Trường không được vượt quá 200 ký tự.")]
    public string? School { get; set; }

    [StringLength(200, ErrorMessage = "Khoa không được vượt quá 200 ký tự.")]
    public string? Department { get; set; }

    [StringLength(20, ErrorMessage = "Ngôn ngữ không được vượt quá 20 ký tự.")]
    public string? Language { get; set; } = "vi";

    [Required(ErrorMessage = "Vui lòng chọn quyền hiển thị.")]
    [StringLength(30, ErrorMessage = "Quyền hiển thị không được vượt quá 30 ký tự.")]
    public string Visibility { get; set; } = "private";

    [StringLength(30, ErrorMessage = "Source type không được vượt quá 30 ký tự.")]
    public string? SourceType { get; set; } = "upload";

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn file để upload.")]
    public IFormFile? UploadFile { get; set; }
}
