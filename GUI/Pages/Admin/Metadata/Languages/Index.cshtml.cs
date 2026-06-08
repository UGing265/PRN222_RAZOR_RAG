using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Pages.Admin.Metadata.Languages;

[Authorize(Roles = "Admin")]
public class IndexModel : MetadataPageModelBase
{
    public IndexModel(IDocumentService documentService, ILogger<IndexModel> logger)
        : base(documentService, logger) { }

    public List<LanguageDto> Items { get; set; } = new();

    public override async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await base.OnGetAsync(cancellationToken);
        Items = await DocumentService.GetLanguagesAsync(cancellationToken);
        return Page();
    }

    public Task<IActionResult> OnPostCreateAsync(string code, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            SetError("Mã ngôn ngữ và tên ngôn ngữ không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/Languages/Index"));
        }
        return ExecuteCreateAsync(
            () => DocumentService.CreateLanguageAsync(code, name, ct),
            $"Đã tạo mới ngôn ngữ '{name}' thành công.",
            "/Admin/Metadata/Languages/Index");
    }

    public Task<IActionResult> OnPostUpdateAsync(Guid id, string code, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            SetError("Mã ngôn ngữ và tên ngôn ngữ không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/Languages/Index"));
        }
        return ExecuteUpdateAsync(
            async () => await DocumentService.UpdateLanguageAsync(id, code, name, ct),
            "Không tìm thấy ngôn ngữ này.",
            $"Đã cập nhật ngôn ngữ '{name}' thành công.",
            "/Admin/Metadata/Languages/Index");
    }

    public Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct) =>
        ExecuteDeleteAsync(
            () => DocumentService.DeleteLanguageAsync(id, ct),
            "Không tìm thấy ngôn ngữ này.",
            "Đã xóa ngôn ngữ thành công.",
            "Có lỗi xảy ra khi xóa ngôn ngữ. Đảm bảo dữ liệu không bị ràng buộc.",
            "language",
            id,
            "/Admin/Metadata/Languages/Index");
}
