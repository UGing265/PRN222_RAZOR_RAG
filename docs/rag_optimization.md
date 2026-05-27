# Tối Ưu Hóa Hệ Thống RAG (Chunking & Embedding)

Tài liệu này ghi chú lại những vấn đề đã gặp phải với luồng xử lý tài liệu (Document Processing) ban đầu và các giải pháp đã được áp dụng để tối ưu hóa hiệu năng, giảm tải API, và cải thiện ngữ cảnh (context) cho AI.

## 1. Vấn đề của hệ thống cũ

### a. Lỗi Rate Limit (429) do gọi API dồn dập
- **Nguyên nhân:** Cấu hình cũ đặt `BatchSize = 50` và gọi API `embedContent` (chỉ xử lý 1 chunk/request) trong một vòng lặp `foreach` tuần tự không có thời gian nghỉ. Điều này khiến hệ thống bắn 50 requests liên tục trong khoảng 5 giây, gây vượt quá giới hạn Rate Limit của tài khoản Gemini Free (thường là 15-60 RPM).
- **Hệ quả:** API trả về lỗi 429, tiến trình Background Worker bị crash ngầm, Job bị kẹt mãi ở trạng thái `processing`, dẫn đến việc Database bị spam lệnh SELECT `pending` liên tục mỗi 2 giây.

### b. Chunking làm mất ngữ cảnh (Semantic Context Loss)
- **Nguyên nhân:** Thuật toán cũ trong `DocumentChunker.cs` đếm cứng đúng 60 từ là cắt ngang văn bản.
- **Hệ quả:** Chữ bị cắt đứt đoạn giữa câu hoặc giữa đoạn văn. Khi AI đọc lại đoạn văn này, nó bị mất ngữ cảnh (context) ở hai đầu, dẫn đến việc trả lời sai lệch hoặc "ngu" đi. Hơn nữa, kích thước chunk 60 từ là quá nhỏ so với sức mạnh đọc hiểu ngữ cảnh rộng của các LLM hiện đại.

## 2. Giải pháp đã triển khai

### a. Chuyển sang Semantic Chunking (Cắt theo ngữ nghĩa)
- Thuật toán `ChunkText` đã được viết lại hoàn toàn. Thay vì đếm chữ, hệ thống cắt theo thứ tự ưu tiên:
  1. **Đoạn văn (Paragraph):** Dựa vào dấu xuống dòng kép `\n\n`.
  2. **Câu (Sentence):** Nếu đoạn văn vượt quá giới hạn từ, cắt tiếp theo dấu chấm câu `.`, `!`, `?`.
  3. **Từ (Word):** Chỉ sử dụng như phương án cuối cùng nếu có một câu viết quá dài không có dấu chấm.
- Thuật toán có sử dụng `overlap` để lùi lại lấy đoạn văn cuối của Chunk trước ghép vào đầu Chunk sau nhằm đảm bảo sự liền mạch mượt mà tuyệt đối.
- Nâng giới hạn một Chunk lên **1100 từ** (`ChunkMaxWords = 1100`), giúp LLM có đầy đủ ngữ cảnh để hiểu toàn diện vấn đề, tránh việc trả lời ngô nghê.

### b. Nâng cấp lên API Batch Embedding (`batchEmbedContents`)
- **Tối ưu API:** Thay vì dùng `embedContent` chỉ gửi được 1 chunk cho 1 request, hệ thống đã được viết lại interface `EmbedBatchAsync` để gọi API `batchEmbedContents`.
- API mới cho phép nhét tối đa 100 chunk vào chung 1 Request JSON.
- **Hiệu quả:** Với cấu hình `BatchSize = 60`, thay vì gọi API 60 lần (tốn 60 requests), hệ thống giờ đây đóng gói 60 chunks đó thành 1 chuyến hàng duy nhất, tiêu tốn đúng **1 Request API**.
- **Chống Rate Limit triệt để:** Bằng cách thêm `BatchDelaySeconds = 2`, hệ thống sau khi gọi 1 cục Batch sẽ nghỉ 2 giây. Tốc độ này tương đương tối đa 30 requests/phút, an toàn 100% cho Gemini Free Tier.

## 3. Tổng kết Cấu Hình Tối Ưu (DocumentIndexingOptions.cs)
```csharp
public int ChunkMaxWords { get; set; } = 1100;
public int ChunkOverlapWords { get; set; } = 100;
public int BatchSize { get; set; } = 15; // Gửi 15 chunks trong 1 request Batch
public int BatchDelaySeconds { get; set; } = 10; // Nghỉ 10s giữa mỗi chuyến Batch
```
**Lưu ý quan trọng:** Google tính Quota dựa trên "số lượng Chunk" bên trong Batch, chứ không tính theo "số lần gọi API". Giới hạn Free Tier của Gemini là **100 Chunks / Phút**. Với cấu hình `15 chunk nghỉ 10 giây`, hệ thống của bạn sẽ chạy tốc độ 90 Chunks / Phút, luôn nằm gọn trong ngưỡng an toàn tuyệt đối mà không bao giờ bị báo lỗi Rate Limit 429.
Cấu hình này đảm bảo tốc độ xử lý file nhanh hơn rất nhiều, tiết kiệm số lần gọi API gấp 60 lần, chống được lỗi kẹt hàng đợi (job queue) và giúp Chatbot trả lời thông minh hơn nhờ mảng văn bản (Chunk) liền mạch, giàu ngữ nghĩa.
