# Tối Ưu Hóa Hệ Thống RAG (Chunking, Embedding & Segmentation)

Tài liệu này ghi chú lại những vấn đề đã gặp phải với luồng xử lý tài liệu (Document Processing) từ lúc phân tích file PDF cho đến lúc nhúng Vector, và các giải pháp thực tế đã được áp dụng để hệ thống chạy mượt mà, chính xác, và "trâu bò" nhất.

## Mục lục (Table of Contents)
- [1. Lỗi Rate Limit (429) do gọi API dồn dập](#1-lỗi-rate-limit-429-do-gọi-api-dồn-dập)
- [2. Chunking làm mất ngữ cảnh (Semantic Context Loss)](#2-chunking-làm-mất-ngữ-cảnh-semantic-context-loss)
- [3. Kích thước Chunk bị lồi lõm do PDF mất Paragraph](#3-kích-thước-chunk-bị-lồi-lõm-do-pdf-mất-paragraph)
- [4. Lỗi Vòng lặp vô tận khi Cắt Chunk (Infinite Loop)](#4-lỗi-vòng-lặp-vô-tận-khi-cắt-chunk-infinite-loop)
- [5. AI Chia chương (Segmentation) bị cắt cụt JSON và bỏ sót chương](#5-ai-chia-chương-segmentation-bị-cắt-cụt-json-và-bỏ-sót-chương)
- [6. Lỗi Database Npgsql 54000 (tsvector quá dài)](#6-lỗi-database-npgsql-54000-tsvector-quá-dài)

---

## 1. Lỗi Rate Limit (429) do gọi API dồn dập
### a. Vấn đề
Cấu hình cũ đặt `BatchSize = 50` và gọi API `embedContent` (chỉ xử lý 1 chunk/request) trong một vòng lặp `foreach` tuần tự không có thời gian nghỉ. Điều này khiến hệ thống bắn 50 requests liên tục trong 5 giây, gây vượt quá giới hạn Rate Limit của tài khoản Gemini Free. API trả về lỗi 429, tiến trình Background Worker bị crash ngầm, Job kẹt `processing`.

### b. Cách giải quyết
Thay vì dùng `embedContent`, hệ thống đã nâng cấp lên API `batchEmbedContents` để nhét 60 chunk vào chung 1 Request duy nhất. Kết hợp cấu hình `BatchDelaySeconds = 2`, tốc độ xử lý đảm bảo tối đa 30 requests/phút. Google tính Quota dựa trên "số lượng Chunk", cấu hình này ép hệ thống chạy mức 90 Chunks/phút, luôn an toàn dưới giới hạn 100 Chunks/phút của Gemini Free Tier.

## 2. Chunking làm mất ngữ cảnh (Semantic Context Loss)
### a. Vấn đề
Thuật toán cũ đếm cứng 60 từ là cắt ngang văn bản. Chữ bị đứt đoạn giữa câu, dẫn đến AI mất ngữ cảnh (context) ở hai đầu, sinh ra ảo giác (hallucination) hoặc trả lời ngô nghê.

### b. Cách giải quyết
Chuyển sang **Semantic Chunking**. Thuật toán cắt theo mức ưu tiên: Đoạn văn (`\n\n`) > Câu (`.!?`) > Từ (Words). Nâng giới hạn kích thước chunk lên **1100 từ** để bao quát bức tranh lớn. Thêm `overlap` 100 từ lùi lại lấy đoạn cuối của Chunk trước ghép vào đầu Chunk sau để đảm bảo mạch văn trơn tru.

## 3. Kích thước Chunk bị lồi lõm do PDF mất Paragraph
### a. Vấn đề
Thư viện bóc tách PDF gom toàn bộ chữ trên trang giấy lại bằng dấu cách (Space), phá hủy hoàn toàn cấu trúc Đoạn văn (`\n\n`). Do không có đoạn văn, DocumentChunker phải cắt theo dấu chấm (`.`). Đối với mục lục hoặc trang bìa không có dấu chấm, nguyên trang giấy biến thành "một câu khổng lồ". Điều này dẫn tới việc Chunk 0 có 469 từ, Chunk 1 có 217 từ, rất lồi lõm thay vì xấp xỉ 1100 từ như kỳ vọng.

### b. Cách giải quyết
Viết lại thuật toán `ExtractPdfText` dựa trên **tọa độ Y (BoundingBox.Bottom)**. Thuật toán so sánh độ chênh lệch chiều cao giữa các chữ:
- Lệch > 2 đơn vị: chèn `\n` (xuống dòng).
- Lệch > 10 đơn vị: chèn `\n\n` (đoạn văn mới).
Nhờ vậy, PDF được phục hồi nguyên trạng cấu trúc Paragraph, giúp DocumentChunker cắt chunk chuẩn xác và đều đặn tuyệt đối.

## 4. Lỗi Vòng lặp vô tận khi Cắt Chunk (Infinite Loop)
### a. Vấn đề
Khi gặp một câu siêu dài vượt quá MaxWords (VD: 1500 từ không có dấu chấm), thuật toán cũ tính toán `stride` sai lệch, dẫn tới con trỏ cắt chữ lùi lại bằng 0, khiến hệ thống mắc kẹt trong vòng lặp vô tận `while (true)` làm treo cứng CPU.

### b. Cách giải quyết
Sửa lại công thức fallback cắt theo từ: `var stride = Math.Max(1, maxWords - overlapWords);`. Đảm bảo `stride` luôn > 0, con trỏ luôn tiến về phía trước dù văn bản gốc có dị biệt đến mức nào.

## 5. AI Chia chương (Segmentation) bị cắt cụt JSON và bỏ sót chương
### a. Vấn đề
Khi đẩy 231 chunks (1 cuốn sách) vào prompt cho Gemini để nhờ chia chương:
- Dung lượng Prompt quá lớn (100,000 ký tự) làm Gemini "ngợp".
- Nó sinh JSON được nửa chừng thì bị đứt gánh (Timeout hoặc MaxTokens), gây ra lỗi vỡ JSON (thiếu dấu đóng ngoặc `]}`).
- Hiện tượng LLM lười biếng (LLM Laziness) xuất hiện: Nó chỉ tóm tắt 3 chương đầu tiên (từ chunk 0 đến 16) rồi tự kết thúc, bỏ rơi hơn 200 chunks còn lại của sách.
- Hơn nữa, vì chỉ gửi 300 ký tự đầu tiên của chunk cho AI, nếu Tiêu đề chương (Header) nằm ở giữa chunk (VD: từ thứ 500) thì AI sẽ mù màu không thấy được, dẫn tới gom sai chương.

### b. Cách giải quyết
Triển khai một kiến trúc "trâu bò" 4 lớp:
1. **Map-Reduce Batching:** Không gửi cả 231 chunk cùng lúc. Chia nhỏ ra gửi theo đợt (Batch), mỗi đợt 40 Chunk. Đảm bảo AI nuốt trôi gọn gàng không bao giờ bị lười hay sót chương.
2. **Scanner bóc Header:** Dùng thuật toán C# quét sâu vào từng Chunk, móc ra những dòng chữ ngắn dưới 80 ký tự (có tiềm năng là Header) để đẩy vào Prompt, giúp AI không bỏ sót bất kỳ Header nào dù nó nằm ở đâu.
3. **Auto-Close JSON Brackets:** Bổ sung thuật toán thông minh trong hàm bóc JSON. Nếu chuỗi JSON từ AI trả về bị cắt cụt đuôi, C# sẽ tự động đếm ngoặc đang mở và bơm đủ ngoặc `]}` vào đuôi để cứu sống JSON.
4. **Ép chuẩn Tiếng Việt:** Thêm System Prompt cưỡng chế toàn bộ Output (Tên chương, Tóm tắt) phải xuất ra bằng Tiếng Việt bất chấp file gốc.

## 6. Lỗi Database Npgsql 54000 (tsvector quá dài)
### a. Vấn đề
PostgreSQL có giới hạn độ dài của Index Full-Text Search (tsvector). Khi tải lên tài liệu > 1 triệu ký tự, việc EF Core tự động gen ra index quá lớn dẫn đến Postgres đá văng lỗi `54000: string is too long for tsvector`.

### b. Cách giải quyết
Trong `DocumentService.cs`, chặn đứng lỗi bằng cách dùng `Substring` chặt độ dài của cột `SearchText` xuống tối đa 50,000 ký tự trước khi lưu vào DB. Phần văn bản gốc vẫn được lưu đủ trong Vector DB, chỉ giảm độ lớn của cột Search để tối ưu bộ nhớ Database.
