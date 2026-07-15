# Software Requirements Specification (SRS)
## Hệ Thống Quản Lý & Tìm Kiếm Tài Liệu Học Thuật Tích Hợp AI RAG

### Introduction
Hệ thống là nền tảng quản lý, tra cứu tài liệu học thuật kết hợp trợ lý ảo thông minh dựa trên công nghệ RAG (Retrieval-Augmented Generation). Hệ thống cho phép người dùng lưu trữ tài liệu, phân tích cấu trúc chương mục, chia cắt văn bản thành các phân đoạn ngữ nghĩa để phục vụ tìm kiếm kết hợp (Hybrid Search) và cung cấp câu trả lời được xác thực từ tài liệu học tập, tránh hiện tượng bịa đặt thông tin của AI.

---

## 1. System Actors

Hệ thống bao gồm các tác nhân chính sau:

*   **Admin:** Có quyền quản trị toàn bộ hệ thống, bao gồm quản lý người dùng, thiết lập siêu dữ liệu danh mục, phân quyền Lecturer phụ trách môn học, theo dõi nhật ký hoạt động (audit log) và duyệt/xử lý báo cáo vi phạm tài liệu.
*   **Lecturer:** Được phân quyền quản lý tài liệu theo môn học cụ thể. Có quyền tải lên, cập nhật, xóa tài liệu thuộc phạm vi phụ trách; tương tác so sánh tài liệu và sử dụng chatbot hỗ trợ học thuật.
*   **Student:** Có quyền tra cứu tài liệu học thuật (công khai hoặc nội bộ trường học), đánh dấu tài liệu yêu thích, gửi báo cáo vi phạm tài liệu, so sánh tài liệu và hội thoại với trợ lý AI để giải đáp kiến thức dựa trên các tài liệu đã nạp.

---

## 2. Business Rules (BR)

Các luật nghiệp vụ bắt buộc phải áp dụng trong logic xử lý của hệ thống:

### BR1: First-Time Login & Password Verification Flow
*   **Tạo tài khoản cục bộ:** Khi Admin tạo tài khoản (đơn lẻ hoặc nhập từ danh sách Excel), hệ thống tự động sinh mật khẩu tạm thời. Trạng thái tài khoản được đánh dấu là chưa xác thực và yêu cầu bắt buộc đổi mật khẩu.
*   **Mã xác thực gửi qua thư điện tử:** Hệ thống gửi liên kết kích hoạt đến email của Lecturer/Student chứa mã xác thực bảo mật có thời hạn sử dụng tối đa là 15 phút.
*   **Bắt buộc đổi mật khẩu:** Lecturer/Student chưa xác thực đăng nhập bằng mật khẩu tạm thời lần đầu tiên bắt buộc phải thay đổi mật khẩu (tối thiểu 6 ký tự) mới được phép truy cập các chức năng khác của hệ thống. Nếu chưa thực hiện đổi mật khẩu thành công, mọi yêu cầu sẽ bị chặn và điều hướng về trang đổi mật khẩu.
*   **Đăng nhập qua bên thứ ba (OAuth Google):** Chỉ chấp nhận các địa chỉ email thuộc tên miền giáo dục hợp lệ của trường (ví dụ: `@fpt.edu.vn` cho Student, `@fe.edu.vn` cho Lecturer). Lecturer/Student đăng nhập bằng tài khoản liên kết sẽ được tự động kích hoạt tài khoản và bỏ qua bước đổi mật khẩu lần đầu, đồng thời không được phép thay đổi mật khẩu cục bộ trên hệ thống.

### BR2: Account Status Constraints
*   **Khóa tài khoản:** Khi Admin thực hiện khóa tài khoản, trạng thái của Lecturer/Student lập tức chuyển sang bị chặn. Hệ thống thực hiện đăng xuất bắt buộc ngay lập tức Lecturer/Student này trên tất cả các phiên làm việc đang kết nối.
*   **Điều kiện phê duyệt/mở khóa:** Admin không được phép kích hoạt hoặc mở khóa tài khoản nếu Lecturer/Student đó chưa hoàn tất quy trình xác thực email và đổi mật khẩu lần đầu.

### BR3: Subject-Based Document Management Permissions
*   Lecturer chỉ có quyền tải lên và quản trị các tài liệu của môn học mà họ được Admin gán quyền quản lý trực tiếp. Lecturer không có quyền can thiệp vào tài liệu thuộc các môn học khác.

### BR4: Document Duplication Control
*   Hệ thống kiểm tra tính duy nhất của tài liệu bằng cách so sánh mã băm nội dung tệp tin (checksum). Nếu tài liệu tải lên trùng mã băm với tài liệu đã tồn tại trong hệ thống, yêu cầu tải lên sẽ bị từ chối nhằm tránh lãng phí tài nguyên lưu trữ và chỉ mục vector.

### BR5: Document Access Level Constraints
*   **Cấp độ hiển thị tài liệu:** Tài liệu có thể được phân loại hiển thị: *Cá nhân (Private)*, *Nội bộ Trường (School-wide)* hoặc *Công khai (Public)*.
*   **Quy tắc truy cập:**
    *   Tài liệu cá nhân chỉ được xem và tương tác bởi chính người sở hữu tài liệu hoặc Admin.
    *   Lecturer và Student không thể tìm kiếm, đọc hoặc đưa tài liệu cá nhân của người khác vào tiến trình so sánh tài liệu.

### BR6: Document Comparison Limits
*   Tính năng so sánh tài liệu học thuật yêu cầu người dùng lựa chọn tối thiểu 2 tài liệu và tối đa 5 tài liệu đồng thời.

### BR7: AI RAG Principles (Grounded Generation & Citations)
*   **Sinh câu trả lời dựa trên tài liệu (Grounded):** Trợ lý AI chỉ được phép sử dụng thông tin từ các phân đoạn văn bản được truy xuất trực tiếp từ các tài liệu được chỉ định làm ngữ cảnh. Nghiêm cấm AI tự bịa đặt kiến thức hoặc sử dụng tri thức nền ngoài tài liệu để trả lời. Nếu ngữ cảnh không đủ thông tin, AI phải từ chối trả lời một cách lịch sự.
*   **Trích dẫn nguồn bắt buộc:** Mọi phản hồi từ trợ lý AI phải đi kèm danh sách nguồn trích dẫn rõ ràng, hiển thị thông tin tên tài liệu, chương mục và số trang (nếu có) tương ứng với các phân đoạn văn bản đã được dùng để tham chiếu.
*   **Tối ưu hóa câu hỏi tiếng Việt:** Nhằm cải thiện độ chính xác khi tìm kiếm ngữ nghĩa trên các tài liệu học thuật bằng tiếng Anh, các câu hỏi chuyên ngành của Student/Lecturer bằng tiếng Việt sẽ được dịch/mở rộng sang tiếng Anh trước khi thực hiện tìm kiếm vector, sau đó kết hợp cả hai ngôn ngữ để truy xuất thông tin tối ưu.

### BR8: Model Comparison Scope
*   Tính năng so sánh mô hình chỉ hỗ trợ thực hiện đối chiếu giữa các mô hình ngôn ngữ lớn được cấu hình và tích hợp sẵn trong hệ thống (ví dụ: Gemini, ChatGPT/OpenAI GPT-3.5/GPT-4). Người dùng không thể tự thêm các mô hình ngoài danh sách cấu hình.

---

## 3. Technical Constraints (TC)

Các ràng buộc kỹ thuật quy định cấu trúc và giới hạn vận hành của hệ thống:

### TC1: Strict Layered Architecture
*   Hệ thống tuân thủ kiến trúc phân lớp decoupled, tách biệt hoàn toàn giữa giao diện người dùng, logic xử lý nghiệp vụ và tầng truy cập dữ liệu. Giao diện người dùng không được phép truy cập trực tiếp vào cơ sở dữ liệu hoặc thực thi các câu lệnh truy vấn mà phải thông qua các dịch vụ nghiệp vụ trung gian.

### TC2: Asynchronous Background Processing
*   **Tiến trình xử lý tài liệu:** Các tác vụ nặng như trích xuất văn bản thô, phân tích chương mục bằng AI, cắt nhỏ văn bản và tạo chỉ mục vector phải được đưa vào hàng đợi xử lý ngầm nhằm tránh nghẽn luồng xử lý chính của người dùng.
*   **Tiến trình gửi email:** Tác vụ gửi email kích hoạt, thông báo mật khẩu phải được quản lý qua hàng đợi riêng biệt, đảm bảo các lỗi kết nối máy chủ thư tín không làm ảnh hướng đến các giao dịch lưu trữ thông tin tài khoản.

### TC3: Vector Database & Embedding Model
*   **Kích thước Vector:** Cơ sở dữ liệu hỗ trợ lưu trữ và truy vấn vector với kích thước cố định là 3072 chiều để tương thích với mô hình nhúng văn bản thế hệ mới của nhà cung cấp dịch vụ AI.
*   **Truy cập API:** Các yêu cầu sinh vector nhúng và hội thoại với mô hình ngôn ngữ lớn phải được thực hiện thông qua kết nối API trực tiếp, sử dụng cơ chế xoay vòng khóa truy cập (API Keys) để tối ưu hạn mức sử dụng (rate limit).

### TC4: Document Object Storage
*   Tất cả các tệp gốc tài liệu khi tải lên được lưu trữ an toàn tại hệ thống lưu trữ đối tượng trên đám mây (cloud object storage) thông qua các khóa định danh duy nhất, đảm bảo tính bảo mật và khả năng mở rộng.

### TC5: Real-Time Progress Notification
*   Hệ thống duy trì cơ chế đẩy thông báo trực tiếp từ máy chủ xuống giao diện trình duyệt để cập nhật tiến độ xử lý tài liệu (trích xuất, cắt chunk, tạo vector) theo thời gian thực mà không cần người dùng tải lại trang.

### TC6: Dynamic Offline Chunking Configuration
*   Các thông số phân mảnh văn bản (ChunkMinWords, ChunkMaxWords, ChunkOverlapWords) phải được lưu trữ trong một tệp cấu hình riêng biệt ngoài mã nguồn, cho phép đọc và ghi động từ giao diện của Admin.

### TC7: LLM API Integration for Comparison
*   Việc kết nối và gọi phản hồi từ các mô hình so sánh (như OpenAI GPT) phải được thực hiện qua các kênh kết nối API độc lập, thiết lập thời gian chờ (timeout) tối đa là 30 giây để tránh nghẽn luồng xử lý và không làm ảnh hưởng đến dịch vụ RAG chính sử dụng Gemini.

---

## 4. Functional Requirements (FR)

Các yêu cầu chức năng dưới đây mô tả ngắn gọn vai trò tác nhân, tên chức năng chính và nội dung nghiệp vụ:

### FR1: Authentication & Security Module
*   **FR1.1 (Login):** `Admin/Lecturer/Student` đăng nhập hệ thống bằng Email/Mật khẩu cục bộ hoặc Google OAuth.
*   **FR1.2 (Change Password):** `Admin/Lecturer/Student` thay đổi mật khẩu bằng cách cung cấp mật khẩu hiện tại và mật khẩu mới.
*   **FR1.3 (Force Change Password on First Login):** `Lecturer/Student chưa xác thực` bắt buộc phải đổi mật khẩu tạm thời khi đăng nhập lần đầu.
*   **FR1.4 (Single Account Creation):** `Admin` tạo tài khoản Lecturer hoặc Student mới.
*   **FR1.5 (Bulk Account Import):** `Admin` import danh sách tài khoản từ file Excel; hệ thống tự động tạo tài khoản và gửi email kích hoạt.
*   **FR1.6 (Account List & Filtering):** `Admin` xem, tìm kiếm và lọc danh sách tài khoản kèm trạng thái.
*   **FR1.7 (Lock/Unlock Account & Force Logout):** `Admin` khóa hoặc mở khóa tài khoản; tự động đăng xuất tài khoản bị khóa khỏi mọi thiết bị đang hoạt động.
*   **FR1.8 (Reissue Temporary Password):** `Admin` gửi lại mật khẩu tạm thời cho Lecturer/Student chưa kích hoạt tài khoản.

### FR2: Subject & Master Metadata Module
*   **FR2.1 (Metadata CRUD):** `Admin` thực hiện CRUD các danh mục phụ trợ bao gồm: Nguồn tài liệu, Loại tài liệu, Ngôn ngữ và Môn học.
*   **FR2.2 (Assign Subject Management):** `Admin` gán hoặc hủy quyền quản lý môn học cho Lecturer.
*   **FR2.3 (Manage Chunking Configuration) (NEW):** `Admin` điều chỉnh các thông số ChunkMinWords, ChunkMaxWords, ChunkOverlapWords lưu trực tiếp vào tệp cấu hình riêng biệt trong settings hệ thống.

### FR3: Document Pipeline Module
*   **FR3.1 (Document Upload):** `Lecturer` tải lên file tài liệu học thuật (PDF, DOCX, PPTX); hệ thống kiểm tra định dạng và tệp trùng lặp.
*   **FR3.2 (AI Structural Segmentation):** `Hệ thống` tự động nhận diện tiêu đề chương và tóm tắt nội dung bằng tiếng Việt qua AI.
*   **FR3.3 (Chunking & Embedding):** `Hệ thống` tự động cắt nhỏ văn bản và sinh vector nhúng ngữ nghĩa (embeddings).
*   **FR3.4 (Real-Time Progress Synchronization):** `Hệ thống` hiển thị phần trăm tiến độ xử lý tài liệu cho Lecturer/Admin theo thời gian thực.

### FR4: Document Search & Interaction Module
*   **FR4.1 (Hybrid Search):** `Student/Lecturer` tìm kiếm tài liệu bằng Hybrid Search (kết hợp từ khóa truyền thống và ngữ nghĩa vector).
*   **FR4.2 (View Details & Download):** `Student/Lecturer` xem thông tin chi tiết hoặc tải xuống tệp gốc tài liệu (nếu được phân quyền).
*   **FR4.3 (Bookmark Document):** `Student/Lecturer` lưu hoặc xóa tài liệu khỏi danh mục yêu thích cá nhân.
*   **FR4.4 (Report Document):** `Student/Lecturer` gửi báo cáo lỗi/nội dung xấu của tài liệu kèm mô tả chi tiết cho Admin.
*   **FR4.5 (Resolve Document Reports):** `Admin` xem danh sách và xử lý các báo cáo vi phạm tài liệu.
*   **FR4.6 (AI Document Comparison):** `Student/Lecturer` so sánh từ 2 đến 5 tài liệu; AI tự động sinh bảng phân tích tương đồng và khác biệt.
*   **FR4.7 (Export Comparison to PDF):** `Student/Lecturer` xuất bảng phân tích so sánh tài liệu sang file PDF.

### FR5: Academic Assistant Module (AI Chatbot)
*   **FR5.1 (Initialize Chat Session):** `Student/Lecturer` mở phiên hội thoại mới gắn với một tài liệu cụ thể hoặc toàn bộ môn học.
*   **FR5.2 (Contextual Q&A):** `Student/Lecturer` gửi câu hỏi; AI tự động truy xuất ngữ cảnh phù hợp từ tài liệu để trả lời (grounded).
*   **FR5.3 (Real-Time Streaming Output):** `Hệ thống` stream câu trả lời của AI theo thời gian thực dưới dạng chữ chạy.
*   **FR5.4 (Source Citation Linkage):** `Hệ thống` hiển thị link nguồn trích dẫn (tên tài liệu, chương mục, số trang) bên dưới câu trả lời của AI để nhấp vào xem văn bản gốc.
*   **FR5.5 (Chat History View):** `Student/Lecturer` xem danh sách và tiếp tục các phiên hội thoại cũ.
*   **FR5.6 (Delete Chat Session):** `Student/Lecturer` xóa một hoặc nhiều phiên hội thoại khỏi danh sách cá nhân.

### FR6: Audit Log Module
*   **FR6.1 (Activity Logging):** `Hệ thống` tự động ghi log mọi hành động nhạy cảm (đăng nhập lỗi, phê duyệt/khóa tài khoản, thay đổi tài liệu, phân quyền môn học) kèm IP, thời gian và định danh tác nhân.
*   **FR6.2 (Audit Log Viewer):** `Admin` tra cứu, lọc và xem toàn bộ lịch sử hoạt động của hệ thống.

### FR7: System Statistics & Reporting Module (Admin Dashboard) (NEW)
*   **FR7.1 (System Usage Overview):** `Admin` xem tổng số lượng tài khoản (Student/Lecturer), tổng số môn học và tổng số tài liệu học thuật đã được lưu trữ trong hệ thống.
*   **FR7.2 (AI Token Consumption Monitoring):** `Admin` xem báo cáo thống kê số lượng Token AI (Prompt Tokens, Completion Tokens, Total Tokens) đã tiêu thụ của mô hình Gemini qua các phiên hội thoại và phân tích tài liệu theo thời gian (ngày/tuần/tháng) dưới dạng biểu đồ trực quan.

### FR8: LLM Benchmarking & Comparison Module (NEW)
*   **FR8.1 (Model Comparison Interface):** `Admin/Student/Lecturer` gửi cùng một câu hỏi và so sánh trực quan câu trả lời trả về song song từ các mô hình (ví dụ: Gemini vs ChatGPT/OpenAI GPT-4) trên giao diện chia đôi màn hình (Side-by-Side).
*   **FR8.2 (Model Metrics Comparison):** `Admin/Lecturer/Student` xem biểu đồ hoặc bảng so sánh các chỉ số hiệu năng cơ bản giữa các mô hình (như tốc độ phản hồi/latency, số lượng token tiêu thụ, đánh giá độ chính xác mẫu).
*   **FR8.3 (Benchmarking Settings):** `Admin` cấu hình bật/tắt kích hoạt các mô hình tham chiếu được phép dùng để so sánh (ví dụ: tắt/bật ChatGPT/OpenAI API) và thiết lập hạn mức (rate limit) tối đa cho mỗi lượt thử nghiệm của người dùng.

---

## 5. Requirements Traceability Matrix (Ma Trận Truy Vết Yêu Cầu)

Bảng ma trận ánh xạ mối quan hệ giữa các Yêu cầu chức năng (FR), Quy tắc nghiệp vụ (BR) và Ràng buộc kỹ thuật (TC) tương ứng trong hệ thống:

| Module Chức năng (FR) | Tên Chức năng (Functional Requirements) | Quy tắc nghiệp vụ liên quan (Business Rules) | Ràng buộc kỹ thuật liên quan (Technical Constraints) |
| :--- | :--- | :--- | :--- |
| **FR1** | Authentication & Security | BR1, BR2 | TC1, TC2 |
| **FR2** | Subject & Master Metadata | BR3 | TC1, TC6 |
| **FR3** | Document Pipeline | BR3, BR4, BR5 | TC1, TC2, TC3, TC4, TC5, TC6 |
| **FR4** | Document Search & Interaction | BR5, BR6 | TC1, TC3 |
| **FR5** | Academic Assistant (AI Chatbot) | BR7 | TC1, TC3 |
| **FR6** | Audit Log | *Không có* | TC1 |
| **FR7** | System Statistics & Reporting | *Không có* | TC1 |
| **FR8** | LLM Benchmarking & Comparison | BR8 | TC1, TC7 |

