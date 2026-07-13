# Sơ Đồ Luồng Hệ Thống — FPT RAG
> Mô tả đơn giản, không có tên kỹ thuật.

---

## 1. 🔐 Đăng nhập

### 1A. Đăng nhập bằng Email & Mật khẩu

```mermaid
flowchart TD
    A["👤 Người dùng\nnhập Email + Mật khẩu"] --> B["Hệ thống kiểm tra\nemail và mật khẩu"]
    B --> C{Tài khoản\nbị khoá?}
    C -->|"Có"| D["❌ Từ chối\nHiển thị lỗi"]
    C -->|"Không"| E{Đã xác thực\nemail chưa?}
    E -->|"Chưa"| F["⚠️ Yêu cầu xác thực email\ntrước khi tiếp tục"]
    E -->|"Rồi"| G["✅ Đăng nhập thành công\nLưu phiên làm việc"]
    G --> H{Lần đầu\nđăng nhập?}
    H -->|"Có"| I["🔒 Bắt buộc đổi mật khẩu\ntrước khi vào hệ thống"]
    H -->|"Không"| J["🏠 Vào trang chính"]
```

### 1B. Đăng nhập bằng Google

```mermaid
flowchart TD
    A["👤 Người dùng\nnhấn 'Đăng nhập Google'"] --> B["Chuyển sang\ntrang xác thực Google"]
    B --> C{Email thuộc\ntrường FPT?}
    C -->|"@fpt.edu.vn → Sinh viên\n@fe.edu.vn → Giảng viên"| D["✅ Tự động kích hoạt tài khoản\nKhông cần đổi mật khẩu"]
    C -->|"Email khác"| E["❌ Từ chối\nChỉ nhận email của trường"]
    D --> F["🏠 Vào trang chính"]
```

### 1C. Admin khoá tài khoản → Tự động đăng xuất

```mermaid
flowchart TD
    A["👤 Admin\nnhấn 'Khoá tài khoản'"] --> B["Hệ thống khoá\ntài khoản ngay lập tức"]
    B --> C["Ghi log hành động\ncủa Admin"]
    C --> D["Gửi tín hiệu real-time\nđến thiết bị của người bị khoá"]
    D --> E["Trình duyệt của\nngười bị khoá\nnhận tín hiệu"]
    E --> F["🚪 Tự động đăng xuất\nChuyển về trang đăng nhập"]
    
    G["Mỗi khi người dùng\ngửi yêu cầu đến hệ thống"] --> H{Tài khoản\ncòn hợp lệ?}
    H -->|"Bị khoá / Chưa xác thực"| I["🚪 Đăng xuất bắt buộc\n→ Trang đăng nhập"]
    H -->|"Bình thường"| J["✅ Xử lý tiếp"]
```

---

## 2. 📄 Tải lên & Xử lý tài liệu

### 2A. Tải nhanh từ trang Chat *(có vấn đề — xem ghi chú)*

```mermaid
flowchart TD
    A["👤 Người dùng\ntải file lên từ trang Chat"] --> B["Kiểm tra định dạng file\nchỉ nhận PDF, DOCX, PPTX"]
    B --> C["Kiểm tra file trùng lặp\nbằng mã băm MD5"]
    C --> D{File đã tồn tại?}
    D -->|"Có"| E["❌ Từ chối\n'Tài liệu đã tồn tại'"]
    D -->|"Chưa"| F["Lưu thông tin tài liệu\nvào cơ sở dữ liệu\ntrạng thái: 'đang xử lý'"]
    F --> G["Tải file gốc\nlên bộ nhớ đám mây S3"]
    G --> H["Tạo yêu cầu xử lý\nvào hàng đợi"]
    H --> I["✅ Phản hồi ngay\n'Đang xử lý...'"]
    
    I -.->|"⚠️ Vấn đề"| J["❌ Không có tiến trình nền\nnào nhận và xử lý yêu cầu!\nFile bị kẹt ở đây mãi mãi"]
    
    style J fill:#dc3545,color:#fff
```

> ⚠️ **Lỗi hiện tại:** File được tải lên S3 thành công nhưng **không bao giờ được tách đoạn và tạo vector embedding** vì thiếu tiến trình xử lý nền.

### 2B. Tải đầy đủ từ trang Giảng viên *(hoạt động đúng)*

```mermaid
flowchart TD
    A["👤 Giảng viên\nđiền thông tin + chọn file\ntại trang Tạo tài liệu"] --> B["Kiểm tra định dạng\nvà file trùng lặp"]
    B --> C["Lưu thông tin tài liệu\nvào cơ sở dữ liệu"]
    C --> D["☁️ Tải file gốc\nlên bộ nhớ đám mây S3"]
    D --> E["Tải file từ S3\nvề máy chủ tạm thời\nđể xử lý"]

    subgraph PIPELINE["⚙️ Quy trình xử lý tài liệu"]
        P1["📖 Trích xuất văn bản\ntừ PDF / DOCX / PPTX"]
        P1 --> P2["✂️ Cắt nhỏ văn bản\nthành từng đoạn\n(chunk)"]
        P2 --> P3["🧮 Tạo vector nhúng\n(embedding)\ncho từng đoạn\nqua Gemini AI"]
        P3 --> P4["💾 Lưu tất cả đoạn\nvào cơ sở dữ liệu\nkèm vector"]
        P4 --> P5["🤖 AI phân tích\ncấu trúc chương mục\nvà tóm tắt nội dung"]
    end

    E --> PIPELINE
    PIPELINE --> F["🗑️ Xoá file tạm\ntrên máy chủ"]
    F --> G["📡 Gửi thông báo tiến độ\ntheo thời gian thực\nđến trình duyệt"]
    G --> H["✅ Tài liệu sẵn sàng\ntrạng thái: 'hoàn tất'"]
    H --> I{Giảng viên\nduyệt tài liệu?}
    I -->|"Duyệt"| J["✅ Trạng thái: 'đã duyệt'\nSẵn sàng cho Chat AI"]
```

---

## 3. 💬 Hỏi đáp với Trợ lý AI (Chat RAG)

### 3A. Luồng chính — Phản hồi trực tiếp (stream)

```mermaid
flowchart TD
    A["👤 Người dùng\ngửi câu hỏi\n(có thể chọn 1 hoặc nhiều tài liệu)"] --> B{Câu hỏi\nbằng tiếng Việt?}
    B -->|"Có"| C["🌐 Dịch câu hỏi sang tiếng Anh\nqua AI\nGiữ cả 2 phiên bản để tìm kiếm tốt hơn"]
    B -->|"Không"| D["Dùng câu hỏi gốc"]
    C --> E["🔢 Chuyển câu hỏi\nthành vector số học\nqua Gemini AI"]
    D --> E
    E --> F["🔍 Tìm kiếm ngữ nghĩa\ntrong cơ sở dữ liệu\nlấy 5 đoạn văn gần nhất"]
    F --> G["📋 Ghép lịch sử\nhội thoại gần nhất\n(10 tin nhắn)"]
    G --> H["🤖 Xây dựng ngữ cảnh\ncho AI:\n• Các đoạn văn liên quan\n• Lịch sử hội thoại\n• Câu hỏi hiện tại"]
    H --> I["✨ Gọi Gemini AI\nsinh câu trả lời\ndựa trên tài liệu"]
    I --> J["📡 Gửi từng phần câu trả lời\nvề trình duyệt ngay lập tức\n(chữ chạy dần)"]
    J --> K{Phản hồi\nxong chưa?}
    K -->|"Chưa"| J
    K -->|"Xong"| L["💾 Lưu toàn bộ hội thoại\nvào cơ sở dữ liệu"]
    L --> M["📚 Hiển thị nguồn trích dẫn\n(tên tài liệu, chương, trang)"]
```

---

## 4. 📊 So sánh tài liệu

### 4A. So sánh file trực tiếp *(cách cũ)*

```mermaid
flowchart TD
    A["👤 Người dùng\ntải lên 2 file để so sánh"] --> B["Trích xuất văn bản\ntừ cả 2 file"]
    B --> C["Chuyển văn bản\nthành vector số học"]
    C --> D["Tính điểm tương đồng\nbằng Cosine Similarity"]
    D --> E["Gọi AI phân tích\nsự giống và khác nhau"]
    E --> F["✅ Hiển thị kết quả\n• % tương đồng\n• Phân tích của AI"]
```

### 4B. So sánh tài liệu đã lưu trong hệ thống *(cách hiện tại)*

```mermaid
flowchart TD
    A["👤 Người dùng chọn 2 đến 5 tài liệu trong hệ thống (có thể kèm câu hỏi bổ sung)"] --> B{Đủ tối thiểu 2 tài liệu?}
    B -->|"Không"| C["❌ Yêu cầu chọn thêm tài liệu"]
    B -->|"Đủ"| D["Lấy nội dung văn bản từng tài liệu đã lưu trong cơ sở dữ liệu"]
    D --> E{"Tài liệu đã được xử lý?"}
    E -->|"Chưa"| F["❌ Báo lỗi 'Tài liệu chưa sẵn sàng'"]
    E -->|"Rồi"| G["🤖 Gọi Gemini AI phân tích và so sánh tất cả tài liệu"]
    G --> H["AI trả về kết quả dạng JSON có cấu trúc"]
    H --> I{Kết quả hợp lệ?}
    I -->|"Lỗi định dạng"| J["❌ Thông báo lỗi 'AI phản hồi không đúng định dạng'"]
    I -->|"OK"| K["✅ Hiển thị bảng so sánh • % tương đồng • Điểm giống nhau • Điểm khác nhau • Kết luận tổng thể • Nguồn trích dẫn"]
```

---

## 5. 📝 Nhật ký hoạt động (Audit Log)

```mermaid
flowchart TD
    A["🔄 Bất kỳ hành động quan trọng nào\n(đăng nhập, khoá tài khoản, tải tài liệu,\nphân quyền, duyệt tài liệu...)"] --> B["Ghi lại:\n• Ai làm\n• Làm gì\n• Lúc mấy giờ\n• Địa chỉ IP"]
    B --> C["Gửi thông báo real-time\nđến Admin đang online"]
    C --> D["Bảng nhật ký Admin\ntự cập nhật không cần\ntải lại trang"]

    E["👤 Admin\nvào trang Nhật ký"] --> F["Xem, lọc, tìm kiếm\ntoàn bộ lịch sử hoạt động\ncủa hệ thống"]
```

---

## 6. ⚠️ Các vấn đề cần xử lý

| Mức độ | Vấn đề | Ảnh hưởng |
|--------|---------|-----------|
| 🔴 **Nghiêm trọng** | Tải file từ trang Chat xong nhưng **không bao giờ được xử lý** (tách đoạn, tạo vector) | Chat AI không dùng được file này |
| 🟡 **Trung bình** | Admin **không có giao diện** để chỉnh độ dài đoạn văn (chunk) — đang cố định trong code | Vi phạm yêu cầu FR2.3 + TC6 |
| 🟡 **Trung bình** | Hệ thống **không đếm số token** sử dụng thực tế khi tạo vector | Không có dữ liệu cho dashboard thống kê FR7.2 |
| 🟠 **Cao** | Chưa có trang **Thống kê hệ thống** cho Admin (FR7) | Thiếu chức năng mới |
| 🟠 **Cao** | Chưa có tính năng **So sánh mô hình AI** (FR8) | Thiếu chức năng mới |
| 🟠 **Cao** | Chưa có nút **Gửi lại mật khẩu tạm** cho Admin (FR1.8) | Thiếu chức năng |
| 🟡 **Trung bình** | Chưa có nút **Xuất kết quả so sánh ra PDF** (FR4.7) | Chức năng còn thiếu ở UI |
