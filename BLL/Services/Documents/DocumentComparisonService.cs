using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs.Chat;
using BLL.Interfaces.Chat;
using BLL.Interfaces.Documents;
using DAL.Interfaces.Documents;

namespace BLL.Services.Documents;

public class DocumentComparisonService : IDocumentComparisonService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IGeminiChatService _geminiChatService;

    public DocumentComparisonService(IDocumentRepository documentRepository, IGeminiChatService geminiChatService)
    {
        _documentRepository = documentRepository;
        _geminiChatService = geminiChatService;
    }

    public async Task<string> CompareDocumentsAsync(List<Guid> documentIds, Guid? requesterUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (documentIds == null || documentIds.Count < 2 || documentIds.Count > 5)
        {
            throw new ArgumentException("Số lượng tài liệu để so sánh phải từ 2 đến 5 tài liệu.");
        }

        var documentIdsDistinct = documentIds.Distinct().ToList();
        var documentContents = new List<string>();

        foreach (var id in documentIdsDistinct)
        {
            var document = await _documentRepository.GetDocumentWithFilesAsync(id, cancellationToken);
            if (document == null)
            {
                throw new InvalidOperationException($"Không tìm thấy tài liệu với ID: {id}");
            }

            // Check permissions
            if (!isAdmin)
            {
                if (document.Visibility == "private" && document.OwnerUserId != requesterUserId)
                {
                    throw new UnauthorizedAccessException($"Bạn không có quyền truy cập tài liệu: {document.Title}");
                }
            }

            var textContent = new StringBuilder();
            if (document.DocumentFiles != null && document.DocumentFiles.Any())
            {
                foreach (var file in document.DocumentFiles)
                {
                    if (!string.IsNullOrWhiteSpace(file.ExtractedText))
                    {
                        textContent.AppendLine(file.ExtractedText);
                    }
                }
            }

            var finalContent = textContent.ToString().Trim();
            if (string.IsNullOrWhiteSpace(finalContent))
            {
                // Fallback to chunks if no ExtractedText is available
                if (document.DocumentChunks != null && document.DocumentChunks.Any())
                {
                    foreach (var chunk in document.DocumentChunks.OrderBy(c => c.ChunkOrder))
                    {
                        textContent.AppendLine(chunk.Content);
                    }
                    finalContent = textContent.ToString().Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(finalContent))
            {
                throw new InvalidOperationException($"Tài liệu '{document.Title}' không có nội dung văn bản để so sánh.");
            }

            documentContents.Add($"--- Tài liệu: {document.Title} ---\n{finalContent}\n");
        }

        var systemPrompt = "Bạn là một chuyên gia phân tích và đánh giá tài liệu xuất sắc. Bạn được cung cấp nội dung của một số tài liệu. Nhiệm vụ của bạn là so sánh chúng, tìm ra điểm giống và khác biệt, và xuất kết quả dưới dạng một bảng Markdown chi tiết. BẮT BUỘC bảng phải đúng chuẩn Markdown. Cột đầu tiên là 'Tiêu chí so sánh', các cột tiếp theo tương ứng với TỪNG TÀI LIỆU được cung cấp (ví dụ nếu có 3 tài liệu thì bảng phải có 4 cột). Đảm bảo dòng phân cách Markdown khớp với số lượng cột. Nếu các tài liệu hoàn toàn khác biệt, hãy BẮT BUỘC ghi rõ 'Không có điểm chung'. Nếu nội dung có vẻ giống nhau, hãy chỉ ra điểm khác biệt chi tiết. YÊU CẦU QUAN TRỌNG: CHỈ XUẤT RA DUY NHẤT BẢNG MARKDOWN, KHÔNG CÓ BẤT KỲ CÂU CHÀO HỎI HAY VĂN BẢN GIẢI THÍCH NÀO TRƯỚC VÀ SAU BẢNG. Vui lòng sử dụng tiếng Việt.";
        var userPrompt = "Hãy so sánh các tài liệu sau đây:\n\n" + string.Join("\n\n", documentContents);

        var history = new List<GeminiChatMessage>
        {
            new GeminiChatMessage { Role = "user", Content = userPrompt }
        };

        var result = await _geminiChatService.GenerateAsync(systemPrompt, history, cancellationToken);
        return result;
    }
}
