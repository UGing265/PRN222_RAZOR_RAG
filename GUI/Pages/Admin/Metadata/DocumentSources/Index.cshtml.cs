using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Pages.Admin.Metadata.DocumentSources;

[Authorize(Roles = "Admin")]
public class IndexModel : MetadataPageModelBase
{
    public IndexModel(IDocumentService documentService, ILogger<IndexModel> logger)
        : base(documentService, logger) { }

    public List<DocumentSourceDto> Items { get; set; } = new();

    public override async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await base.OnGetAsync(cancellationToken);
        Items = await DocumentService.GetDocumentSourcesAsync(cancellationToken);
        return Page();
    }

    public Task<IActionResult> OnPostCreateAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên nguồn tài liệu không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/DocumentSources"));
        }
        return ExecuteCreateAsync(
            () => DocumentService.CreateDocumentSourceAsync(name, ct),
            $"Đã tạo mới nguồn tài liệu '{name}' thành công.",
            "/Admin/Metadata/DocumentSources");
    }

    public Task<IActionResult> OnPostUpdateAsync(Guid id, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên nguồn tài liệu không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/DocumentSources"));
        }
        return ExecuteUpdateAsync(
            async () => await DocumentService.UpdateDocumentSourceAsync(id, name, ct),
            "Không tìm thấy nguồn tài liệu này.",
            $"Đã cập nhật nguồn tài liệu '{name}' thành công.",
            "/Admin/Metadata/DocumentSources");
    }

    public Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct) =>
        ExecuteDeleteAsync(
            () => DocumentService.DeleteDocumentSourceAsync(id, ct),
            "Không tìm thấy nguồn tài liệu này.",
            "Đã xóa nguồn tài liệu thành công.",
            "Có lỗi xảy ra khi xóa nguồn tài liệu. Đảm bảo dữ liệu không bị ràng buộc.",
            "document source",
            id,
            "/Admin/Metadata/DocumentSources");
}
