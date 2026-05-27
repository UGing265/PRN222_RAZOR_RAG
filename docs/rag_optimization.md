# Tối Ưu Hóa Hệ Thống RAG (Chunking, Embedding & Segmentation)

Tài liệu này ghi chú lại toàn bộ những vấn đề kỹ thuật đã gặp phải với luồng xử lý tài liệu (Document Processing) từ lúc phân tích file PDF cho đến lúc nhúng Vector, và các giải pháp thực tế đã được áp dụng để hệ thống chạy mượt mà, chính xác, và ổn định nhất.

## Mục lục
- [1. Lỗi Rate Limit (429) do gọi API dồn dập](#1-lỗi-rate-limit-429-do-gọi-api-dồn-dập)
- [2. Chunking làm mất ngữ cảnh (Semantic Context Loss)](#2-chunking-làm-mất-ngữ-cảnh-semantic-context-loss)
- [3. Kích thước Chunk bị lồi lõm do PDF mất Paragraph](#3-kích-thước-chunk-bị-lồi-lõm-do-pdf-mất-paragraph)
- [4. Lỗi Vòng lặp vô tận khi Cắt Chunk (Infinite Loop)](#4-lỗi-vòng-lặp-vô-tận-khi-cắt-chunk-infinite-loop)
- [5. AI Chia chương (Segmentation) bị cắt cụt JSON và bỏ sót chương](#5-ai-chia-chương-segmentation-bị-cắt-cụt-json-và-bỏ-sót-chương)
- [6. Lỗi Database Npgsql 54000 (tsvector quá dài)](#6-lỗi-database-npgsql-54000-tsvector-quá-dài)
- [7. Tổng kết Cấu Hình Tối Ưu (DocumentIndexingOptions.cs)](#7-tổng-kết-cấu-hình-tối-ưu-documentindexingoptionscs)

---

## 1. Lỗi Rate Limit (429) do gọi API dồn dập
### a. Vấn đề của hệ thống cũ
- **Nguyên nhân:** Cấu hình cũ đặt `BatchSize = 50` và gọi API `embedContent` (chỉ xử lý 1 chunk/request) trong một vòng lặp `foreach` tuần tự không có thời gian nghỉ. Điều này khiến hệ thống bắn 50 requests liên tục trong khoảng 5 giây, gây vượt quá giới hạn Rate Limit của tài khoản Gemini Free (thường là 15-60 RPM).
- **Hệ quả:** API trả về lỗi 429, tiến trình Background Worker bị crash ngầm, Job bị kẹt mãi ở trạng thái `processing`, dẫn đến việc Database bị spam lệnh SELECT `pending` liên tục mỗi 2 giây.

### b. Cách giải quyết
- **Nâng cấp lên API Batch Embedding (`batchEmbedContents`):** Thay vì dùng `embedContent` chỉ gửi được 1 chunk cho 1 request, hệ thống đã được viết lại interface `EmbedBatchAsync` để gọi API `batchEmbedContents`. API mới cho phép nhét tối đa 100 chunk vào chung 1 Request JSON.
- **Hiệu quả:** Với cấu hình `BatchSize = 60`, thay vì gọi API 60 lần (tốn 60 requests), hệ thống giờ đây đóng gói 60 chunks đó thành 1 chuyến hàng duy nhất, tiêu tốn đúng **1 Request API**.
- **Chống Rate Limit triệt để:** Bằng cách thêm `BatchDelaySeconds = 2`, hệ thống sau khi gọi 1 cục Batch sẽ nghỉ 2 giây. Tốc độ này tương đương tối đa 30 requests/phút, an toàn 100% cho Gemini Free Tier.

## 2. Chunking làm mất ngữ cảnh (Semantic Context Loss)
### a. Vấn đề của hệ thống cũ
- **Nguyên nhân:** Thuật toán cũ trong `DocumentChunker.cs` đếm cứng đúng 60 từ là cắt ngang văn bản.
- **Hệ quả:** Chữ bị cắt đứt đoạn giữa câu hoặc giữa đoạn văn. Khi AI đọc lại đoạn văn này, nó bị mất ngữ cảnh (context) ở hai đầu, dẫn đến việc trả lời sai lệch hoặc "ngu" đi. Hơn nữa, kích thước chunk 60 từ là quá nhỏ so với sức mạnh đọc hiểu ngữ cảnh rộng của các LLM hiện đại.

### b. Cách giải quyết
- **Chuyển sang Semantic Chunking (Cắt theo ngữ nghĩa):** Thuật toán `ChunkText` đã được viết lại hoàn toàn. Thay vì đếm chữ, hệ thống cắt theo thứ tự ưu tiên:
  1. **Đoạn văn (Paragraph):** Dựa vào dấu xuống dòng kép `\n\n`.
  2. **Câu (Sentence):** Nếu đoạn văn vượt quá giới hạn từ, cắt tiếp theo dấu chấm câu `.`, `!`, `?`.
  3. **Từ (Word):** Chỉ sử dụng như phương án cuối cùng nếu có một câu viết quá dài không có dấu chấm.
- **Cải thiện độ liền mạch:** Thuật toán có sử dụng `overlap = 100` để lùi lại lấy đoạn văn cuối của Chunk trước ghép vào đầu Chunk sau nhằm đảm bảo sự liền mạch mượt mà tuyệt đối.
- **Mở rộng Context:** Nâng giới hạn một Chunk lên **1100 từ** (`ChunkMaxWords = 1100`), giúp LLM có đầy đủ ngữ cảnh để hiểu toàn diện vấn đề, tránh việc trả lời ngô nghê.

## 3. Kích thước Chunk bị lồi lõm do PDF mất Paragraph
### a. Vấn đề
Thư viện bóc tách PDF gom toàn bộ chữ trên trang giấy lại bằng dấu cách (Space), phá hủy hoàn toàn cấu trúc Đoạn văn (`\n\n`). Do không có đoạn văn, `DocumentChunker` phải chuyển sang cắt theo dấu chấm (`.`). Đối với mục lục hoặc trang bìa không hề có dấu chấm, nguyên trang giấy biến thành "một câu khổng lồ". Điều này dẫn tới việc Chunk cắt bị lồi lõm nghiêm trọng (VD: Chunk 0 có 469 từ, Chunk 1 có 217 từ) thay vì xấp xỉ 1100 từ như kỳ vọng.

### b. Cách giải quyết
Viết lại thuật toán `ExtractPdfText` dựa trên **tọa độ Y (BoundingBox.Bottom)** để phục hồi Paragraph:
- Thuật toán so sánh độ chênh lệch chiều cao giữa chữ hiện tại và chữ trước đó.
- Lệch > 2 đơn vị: Hiểu là xuống dòng, chèn `\n`.
- Lệch > 10 đơn vị: Hiểu là sang đoạn văn mới, chèn `\n\n`.
Nhờ vậy, PDF được phục hồi nguyên trạng cấu trúc Paragraph như file gốc, giúp `DocumentChunker` cắt chunk chuẩn xác và đều đặn lấp đầy 1100 từ.

## 4. Lỗi Vòng lặp vô tận khi Cắt Chunk (Infinite Loop)
### a. Vấn đề
Khi hệ thống gặp một câu siêu dài vượt quá MaxWords (VD: 1500 từ viết dính liền không có dấu chấm), thuật toán cũ tính toán số bước nhảy `stride` sai lệch thành số âm, dẫn tới con trỏ cắt chữ lùi lại bằng 0. Điều này khiến hệ thống mắc kẹt trong vòng lặp vô tận `while (true)` làm treo cứng CPU và kẹt Job vĩnh viễn.

### b. Cách giải quyết
Sửa lại công thức fallback cắt theo từ: `var stride = Math.Max(1, maxWords - overlapWords);`. Đảm bảo `stride` luôn lớn hơn 0, con trỏ luôn tiến về phía trước để cắt đứt văn bản dù văn bản gốc có dị biệt đến mức nào.

## 5. AI Chia chương (Segmentation) bị cắt cụt JSON và bỏ sót chương
### a. Vấn đề
Khi đẩy 231 chunks (1 cuốn sách) vào prompt cho Gemini để nhờ nó tự động chia chương:
- Dung lượng Prompt quá lớn (100,000 ký tự) làm Gemini bị "ngợp".
- Sinh JSON được nửa chừng thì đứt gánh (Timeout hoặc MaxTokens), gây ra lỗi vỡ cấu trúc JSON (thiếu dấu đóng ngoặc `]}`).
- Hiện tượng LLM lười biếng (LLM Laziness) xuất hiện: Nó chỉ làm việc tóm tắt 3 chương đầu tiên (từ chunk 0 đến 16) rồi tự kết thúc, bỏ rơi trắng trợn hơn 200 chunks còn lại của cuốn sách.
- Ngoài ra, việc chỉ gửi 300 ký tự đầu tiên của chunk cho AI khiến AI "mù màu" nếu Tiêu đề chương (Header) nằm lọt thỏm ở giữa chunk (VD: ở ký tự thứ 3000), dẫn tới việc AI không nhìn thấy Header và gom sai chương.

### b. Cách giải quyết
Triển khai một kiến trúc "trâu bò" 4 lớp để xử lý triệt để:
1. **Map-Reduce Batching:** Không gửi cả cuốn sách cùng lúc. Chia nhỏ 231 chunks ra gửi theo từng đợt (Batch), mỗi đợt 40 Chunk. Đảm bảo AI nuốt trôi gọn gàng không bao giờ bị lười hay sót chương. Cuối cùng nối kết quả các Batch lại.
2. **Scanner bóc Header:** Dùng thuật toán C# quét sâu vào toàn bộ từng Chunk, móc ra những dòng chữ ngắn (dưới 80 ký tự) và không có dấu chấm/phẩy ở cuối. Đây là đặc điểm của Header. Đưa danh sách "Header tiềm năng" này vào Prompt để AI không bỏ sót bất kỳ Header nào dù nó trốn ở đâu.
3. **Auto-Close JSON Brackets:** Bổ sung thuật toán thông minh trong hàm bóc tách JSON. Nếu chuỗi JSON bị đứt đuôi, hệ thống sẽ tự động đếm số lượng ngoặc đang mở `[{` và bơm đủ ngoặc `]}` tương ứng vào đuôi để cứu sống JSON.
4. **Ép chuẩn Tiếng Việt:** Thêm System Prompt cưỡng chế *"Toàn bộ title và summary BẮT BUỘC PHẢI VIẾT BẰNG TIẾNG VIỆT, cho dù nội dung tài liệu gốc là tiếng Anh"* để đồng bộ UI.

## 6. Lỗi Database Npgsql 54000 (tsvector quá dài)
### a. Vấn đề
PostgreSQL có giới hạn cứng về độ dài của Index Full-Text Search (tsvector). Khi người dùng tải lên tài liệu > 1 triệu ký tự, việc Entity Framework Core tự động gen ra index quá lớn dẫn đến Postgres đá văng lỗi `54000: string is too long for tsvector`.

### b. Cách giải quyết
Trong `DocumentService.cs`, chặn đứng lỗi bằng cách dùng `Substring` chặt độ dài của cột `SearchText` xuống tối đa 50,000 ký tự trước khi lưu vào DB. Phần văn bản gốc vẫn được lưu đủ trong Vector DB để phục vụ RAG, chỉ giảm độ lớn của cột Search để tối ưu bộ nhớ Database mà vẫn giữ được khả năng tìm kiếm cơ bản.

## 7. Tổng kết Cấu Hình Tối Ưu (DocumentIndexingOptions.cs)
```csharp
public int ChunkMaxWords { get; set; } = 1100;
public int ChunkOverlapWords { get; set; } = 100;
public int BatchSize { get; set; } = 10; // Gửi 10 chunks trong 1 request Batch (An toàn)
public int BatchDelaySeconds { get; set; } = 1; // Tạm nghỉ giữa các Batch
```
**Lưu ý quan trọng:** Google tính Quota dựa trên "số lượng Chunk" bên trong Batch, chứ không tính theo "số lần gọi API". Cấu hình này đảm bảo tốc độ xử lý file nhanh hơn rất nhiều, tiết kiệm số lần gọi API, chống được lỗi kẹt hàng đợi (job queue) và giúp Chatbot trả lời thông minh hơn nhờ mảng văn bản (Chunk) liền mạch, giàu ngữ nghĩa.
