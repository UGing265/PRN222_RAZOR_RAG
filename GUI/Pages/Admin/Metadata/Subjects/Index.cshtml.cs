using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Pages.Admin.Metadata.Subjects;

[Authorize(Roles = "Admin")]
public class IndexModel : MetadataPageModelBase
{
    public IndexModel(IDocumentService documentService, ILogger<IndexModel> logger)
        : base(documentService, logger) { }

    public List<SubjectDto> Items { get; set; } = new();

    public override async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await base.OnGetAsync(cancellationToken);
        Items = await DocumentService.GetSubjectsAsync(cancellationToken);
        return Page();
    }

    public Task<IActionResult> OnPostCreateAsync(string code, string name, Guid? academicTermId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            SetError("Mã môn học và tên môn học không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/Subjects"));
        }
        if (!academicTermId.HasValue)
        {
            SetError("Vui lòng chọn học kỳ cho môn học.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/Subjects"));
        }
        return ExecuteCreateAsync(
            () => DocumentService.CreateSubjectAsync(code, name, academicTermId, ct),
            $"Đã tạo mới môn học '{code.ToUpper()}' thành công.",
            "/Admin/Metadata/Subjects");
    }

    public Task<IActionResult> OnPostUpdateAsync(Guid id, string code, string name, Guid? academicTermId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            SetError("Mã môn học và tên môn học không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/Subjects"));
        }
        if (!academicTermId.HasValue)
        {
            SetError("Vui lòng chọn học kỳ cho môn học.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/Subjects"));
        }
        return ExecuteUpdateAsync(
            async () => await DocumentService.UpdateSubjectAsync(id, code, name, academicTermId, ct),
            "Không tìm thấy môn học.",
            $"Đã cập nhật môn học '{code.ToUpper()}' thành công.",
            "/Admin/Metadata/Subjects");
    }

    public Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct) =>
        ExecuteDeleteAsync(
            () => DocumentService.DeleteSubjectAsync(id, ct),
            "Không tìm thấy môn học.",
            "Đã xóa môn học thành công.",
            "Có lỗi xảy ra khi xóa môn học. Đảm bảo môn học không bị ràng buộc dữ liệu.",
            "subject",
            id,
            "/Admin/Metadata/Subjects");
}
