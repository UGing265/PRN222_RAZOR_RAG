# Software Requirements Specification (SRS)
**Project:** Hệ Thống Quản Lý & Tìm Kiếm Tài Liệu Học Thuật Tích Hợp AI RAG (PRN222_RAZOR_RAG)

Tài liệu này được trích xuất 100% từ mã nguồn thực tế (từ các tầng `DAL/Entities`, `BLL/Services` và `GUI/Pages`), đảm bảo độ chính xác so với quá trình triển khai hiện tại.

---

## 1. Yêu cầu Nghiệp vụ & Luật Nghiệp vụ (Business Requirements / Rules - BR)

### Ràng buộc về Kiến trúc và Công nghệ
*   **BR1 (Kiến trúc chuẩn):** Hệ thống tuân thủ chặt chẽ kiến trúc 3 lớp (3-Layer Architecture): GUI (Razor Pages) -> BLL (Services) -> DAL (Repositories & Entity Framework Core). Tuyệt đối không gọi trực tiếp Database Context từ giao diện.
*   **BR2 (Luồng xử lý bất đồng bộ - Background Processing):** 
    *   Việc xử lý tài liệu (trích xuất, cắt chunk, sinh vector) **BẮT BUỘC** phải được đưa vào Hàng đợi (UploadJob) và chạy ngầm bằng `UploadJobBackgroundService` để không block request của UI.
    *   Việc gửi email (xác thực, đổi mật khẩu) **BẮT BUỘC** đưa vào hàng đợi `EmailQueue` và gửi ngầm bằng `EmailQueueHostedService`. Lỗi SMTP sẽ không gây rollback giao dịch lưu database.
*   **BR3 (Ràng buộc Dữ liệu Vector):** 
    *   Data type của cột `Embedding` trong database PostgreSQL (sử dụng pgvector) phải là `Vector(3072)`.
    *   Sử dụng model Gemini (ví dụ: `gemini-embedding-2`) và mã nguồn padding/chuẩn hóa kích thước vector về 3072 dimension. Việc nhúng (embedding) được thực hiện qua REST HTTP Client thuần (Không dùng SDK wrapper).

### Ràng buộc về Phân quyền và Logic ứng dụng
*   **BR4 (Sở hữu môn học):** Giảng viên (Lecturer) chỉ có quyền upload và quản lý tài liệu đối với các môn học (`Subject`) mà họ đã được gán quyền trực tiếp trong bảng `UserSubject` bởi Admin.
*   **BR5 (Force Change Password):** Một tài khoản có cờ `MustChangePassword = true` sẽ bị `ForceChangePasswordMiddleware` chặn toàn bộ request và điều hướng về trang đổi mật khẩu trước khi được phép sử dụng hệ thống.
*   **BR6 (Nguyên tắc Chat AI - Grounded Generation):** Prompt RAG được thiết kế để AI **tuyệt đối chỉ dùng thông tin từ tài liệu đã nạp (context chunks)**. Cấm bịa đặt thông tin. Nếu thông tin không có, AI phải từ chối trả lời.
*   **BR7 (Trích dẫn Nguồn - Citations):** Phản hồi của Chatbot bắt buộc phải kèm theo nguồn trích dẫn từ Metadata của tài liệu (`DocumentId`, `PageNumber`, `SectionTitle`).

---

## 2. Yêu cầu Chức năng (Functional Requirements - FR)

### FR1. Module Quản trị Tài khoản & Bảo mật (Auth & User Management)
*   **FR1.1 (Đăng nhập nội bộ):** Chỉ hỗ trợ đăng nhập cục bộ bằng Email/Password thông qua tài khoản đã được cấp. Hệ thống không có chức năng tự đăng ký tài khoản (Public Registration) và không sử dụng đăng nhập qua mạng xã hội (Social Login) trên giao diện.
*   **FR1.2 (Quản lý User - Admin):** Admin xem danh sách User, khóa/mở khóa tài khoản, cấp lại mật khẩu, chỉnh sửa vai trò (Role).
*   **FR1.3 (Import Hàng loạt):** Hỗ trợ Admin tạo User số lượng lớn thông qua việc tải lên tệp Excel (sử dụng `PasswordGenerator` và chuyển email thông báo vào hàng đợi).
*   **FR1.4 (Email Verification):** Khi tạo tài khoản/đăng ký, hệ thống gửi email kèm Token xác thực qua `SmtpEmailService` (chạy qua Background Queue).
*   **FR1.5 (Force Change Password):** Admin có thể ép buộc người dùng đổi mật khẩu. Giao diện thay đổi mật khẩu dành cho người dùng bị ép buộc.

### FR2. Module Quản trị Dữ Liệu Nền (Metadata & Master Data)
*   **FR2.1 (Quản lý Siêu Dữ Liệu):** Admin có đầy đủ tính năng CRUD (Thêm, Sửa, Xóa) cho các bảng danh mục (Metadata): `AcademicTerm` (Học kỳ), `DocumentSource` (Nguồn tài liệu), `DocumentType` (Loại tài liệu), `Language` (Ngôn ngữ), `Subject` (Môn học), và `Tag` (Thẻ).
*   **FR2.2 (Gán Môn Học):** Admin thực hiện chức năng gán Giảng viên phụ trách quản lý từng Môn học (`UserSubject`).

### FR3. Module Khởi tạo & Tiền Xử lý Tài Liệu (Document Pipeline)
*   **FR3.1 (Upload & S3 Storage):** Giảng viên tải tài liệu lên (hỗ trợ .PDF, .DOCX, .PPTX). File vật lý được đẩy lên lưu trữ đám mây qua `S3StorageService`.
*   **FR3.2 (Tiến trình xử lý ngầm - UploadJobs):** Hệ thống tạo `UploadJob` để theo dõi. Background worker sẽ:
    *   Trích xuất văn bản (`SimpleFileParserService`).
    *   Phân tích cấu trúc, chia chương (`GeminiChapterSegmentationService`).
    *   Cắt văn bản thành các chunks nhỏ, giữ liền mạch câu/đoạn (`DocumentChunker`).
    *   Sinh Vector hàng loạt qua `GeminiEmbeddingService` và lưu (`Bulk Insert`) xuống DB.
*   **FR3.3 (Cập nhật thời gian thực - Realtime):** Hệ thống thông báo tiến độ xử lý Upload Job và các thay đổi tài liệu về UI cho Admin/Giảng viên thông qua `SignalRNotificationService`.

### FR4. Module Tương tác & Tra cứu Tài liệu (Document Interaction)
*   **FR4.1 (Tìm kiếm - Hybrid Search):** Hỗ trợ tra cứu tài liệu học thuật theo từ khóa (Full-Text Search kết hợp Semantic Search bằng Vector).
*   **FR4.2 (Hiển thị & Duyệt):** Hiển thị danh sách tất cả tài liệu. Cho phép xem chi tiết, tải xuống tài liệu.
*   **FR4.3 (So sánh Tài liệu - Document Comparison):** Cung cấp giao diện PageModel để chọn nhiều tài liệu, gửi câu hỏi so sánh (`DocumentComparisonService`).
*   **FR4.4 (Xuất PDF):** Cung cấp tính năng xuất kết quả so sánh ra định dạng PDF sử dụng thư viện QuestPDF (`QuestPdfComparisonExporter`).
*   **FR4.5 (Đánh dấu & Báo cáo):** Sinh viên có thể lưu/đánh dấu tài liệu yêu thích (`UserBookmark`) và gửi báo cáo lỗi/nội dung xấu (`DocumentReport`) cho Admin xử lý.
*   **FR4.6 (Audit Log):** Hệ thống ghi nhận mọi hành động (Tạo, Sửa, Xóa, Phân quyền) vào bảng `AuditLog` để Admin tra cứu.

### FR5. Module Trợ lý AI (RAG Chatbot)
*   **FR5.1 (Single/Multi-Document RAG):** Cung cấp giao diện Chat trực tiếp với 1 tài liệu (Single) hoặc với tất cả tài liệu của 1 môn học (Multi).
*   **FR5.2 (Truy xuất Ngữ nghĩa - Retrieval):** Nhúng câu hỏi của người dùng và thực hiện tìm kiếm Cosine Distance để lọc top K chunks tài liệu phù hợp nhất.
*   **FR5.3 (Streaming LLM):** Lắp ghép Prompt và gửi sang `GeminiChatService`. Streaming kết quả trả về màn hình dưới dạng Server-Sent Events (SSE).
*   **FR5.4 (Hiển thị Nguồn - Citations):** Sau khi Stream hoàn tất, Server gửi kèm danh sách Nguồn đã trích xuất (`Document`, `Chapter`, `Page`) để UI vẽ box tham chiếu.
*   **FR5.5 (Quản lý Phiên Chat):** Lưu lịch sử các đoạn hội thoại (`ChatSession`, `ChatMessage` và `ChatSessionDocument`). Hỗ trợ chọn lại phiên chat cũ để tiếp tục trò chuyện.
