using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Pages.Admin.Metadata.AcademicTerms;

[Authorize(Roles = "Admin")]
public class IndexModel : MetadataPageModelBase
{
    public IndexModel(IDocumentService documentService, ILogger<IndexModel> logger)
        : base(documentService, logger) { }

    public List<AcademicTermDto> Items { get; set; } = new();

    public override async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await base.OnGetAsync(cancellationToken);
        Items = AcademicTerms;
        return Page();
    }

    public Task<IActionResult> OnPostCreateAsync(string name, int order, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên học kỳ không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/AcademicTerms/Index"));
        }
        return ExecuteCreateAsync(
            () => DocumentService.CreateAcademicTermAsync(name, order, ct),
            $"Đã tạo mới học kỳ '{name}' thành công.",
            "/Admin/Metadata/AcademicTerms/Index");
    }

    public Task<IActionResult> OnPostUpdateAsync(Guid id, string name, int order, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên học kỳ không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/AcademicTerms/Index"));
        }
        return ExecuteUpdateAsync(
            async () => await DocumentService.UpdateAcademicTermAsync(id, name, order, ct),
            "Không tìm thấy học kỳ này.",
            $"Đã cập nhật học kỳ '{name}' thành công.",
            "/Admin/Metadata/AcademicTerms/Index");
    }

    public Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct) =>
        ExecuteDeleteAsync(
            () => DocumentService.DeleteAcademicTermAsync(id, ct),
            "Không tìm thấy học kỳ này.",
            "Đã xóa học kỳ thành công.",
            "Có lỗi xảy ra khi xóa học kỳ. Đảm bảo dữ liệu không bị ràng buộc.",
            "academic term",
            id,
            "/Admin/Metadata/AcademicTerms/Index");
}
