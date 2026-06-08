using BLL.DTOs.Documents;
using BLL.Interfaces.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GUI.Pages.Admin.Metadata.DocumentTypes;

[Authorize(Roles = "Admin")]
public class IndexModel : MetadataPageModelBase
{
    public IndexModel(IDocumentService documentService, ILogger<IndexModel> logger)
        : base(documentService, logger) { }

    public List<DocumentTypeDto> Items { get; set; } = new();

    public override async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await base.OnGetAsync(cancellationToken);
        Items = await DocumentService.GetDocumentTypesAsync(cancellationToken);
        return Page();
    }

    public Task<IActionResult> OnPostCreateAsync(string name, string? description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên loại học liệu không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/DocumentTypes"));
        }
        return ExecuteCreateAsync(
            () => DocumentService.CreateDocumentTypeAsync(name, description, ct),
            $"Đã tạo mới loại học liệu '{name}' thành công.",
            "/Admin/Metadata/DocumentTypes");
    }

    public Task<IActionResult> OnPostUpdateAsync(Guid id, string name, string? description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetError("Tên loại học liệu không được để trống.");
            return Task.FromResult<IActionResult>(RedirectToPage("/Admin/Metadata/DocumentTypes"));
        }
        return ExecuteUpdateAsync(
            async () => await DocumentService.UpdateDocumentTypeAsync(id, name, description, ct),
            "Không tìm thấy loại học liệu này.",
            $"Đã cập nhật loại học liệu '{name}' thành công.",
            "/Admin/Metadata/DocumentTypes");
    }

    public Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken ct) =>
        ExecuteDeleteAsync(
            () => DocumentService.DeleteDocumentTypeAsync(id, ct),
            "Không tìm thấy loại học liệu này.",
            "Đã xóa loại học liệu thành công.",
            "Có lỗi xảy ra khi xóa loại học liệu. Đảm bảo dữ liệu không bị ràng buộc.",
            "document type",
            id,
            "/Admin/Metadata/DocumentTypes");
}
