# Tóm tắt Cải tiến & Tối ưu hóa Xử lý Văn bản (Balanced Chunking & Chapter Segmentation)

Dưới đây là tóm tắt ngắn gọn các cải tiến kỹ thuật đã thực hiện để khắc phục lỗi **"1 Văn bản = 1 Chunk"** và tối ưu hóa hệ thống RAG:

---

### 1. Cấu hình tham số hệ thống (`DocumentIndexingOptions.cs`)
Bổ sung các ngưỡng giới hạn chuẩn vào tệp cấu hình `appsettings.json`: mỗi chunk tối thiểu **50 từ**, tối đa **500 từ**; mỗi chương tối thiểu **2 chunks**, tối đa **8 chunks**. Điều này giúp chuẩn hóa dữ liệu đầu vào cho AI mà không cần viết chết (hardcode) con số trong mã nguồn.

### 2. Khắc phục lỗi "1 Văn bản = 1 Chunk" (`DocumentChunker.cs`)
Cập nhật thuật toán cắt tài liệu để tuân thủ ngưỡng tối thiểu 50 từ. Thêm cơ chế tự động gộp (Merge-on-finish): nếu đoạn văn bản cuối cùng còn sót lại bị quá ngắn (< 50 từ), hệ thống sẽ nối gộp luôn vào chunk liền trước để tránh tạo ra các mảnh vụn rác gây nhiễu RAG.

### 3. Chia chương tự động cân bằng (`GeminiChapterSegmentationService.cs`)
Thay vì gom toàn bộ tài liệu thành 1 chương duy nhất khi AI lỗi hoặc tài liệu không có tiêu đề rõ ràng, hệ thống tự động tính toán chia đều danh sách chunk thành các chương cân đối. Mỗi chương tự động được đặt tên theo thứ tự (*Chương 1, Chương 2...*) đảm bảo mục lục luôn rõ ràng.

### 4. Trang mô phỏng trực quan (`preview_chunking.html`)
Tạo sẵn trang web thử nghiệm độc lập theo chuẩn thiết kế Minimalist Scholarly tại đường link `https://localhost:7065/preview_chunking.html`. Trang này cho phép kéo thanh trượt mô phỏng và so sánh trực tiếp hiệu quả giữa thuật toán cũ (bị lỗi 1 chunk) và thuật toán mới chia cân bằng.
