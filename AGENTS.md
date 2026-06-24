# Hướng dẫn dành cho AI Agent (Agent Rules & Guardrails)

Tài liệu này là bắt buộc đối với tất cả các AI Agent khi tham gia phát triển, chỉnh sửa mã nguồn hoặc thiết kế hệ thống trong dự án này.

---

## 🚨 QUY TẮC BẮT BUỘC (CRITICAL GUARDRAILS)

Trước khi thực hiện **BẤT KỲ** thay đổi nào liên quan đến Giao diện (Frontend) hoặc Logic/Kiến trúc (Backend), AI Agent bắt buộc phải đọc 2 tài liệu thiết kế hệ thống tại thư mục `docs/system/`:

1. **Thiết kế Giao diện (UI/UX):** [docs/system/DESIGN_SYSTEM.md](file:///d:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG/docs/system/DESIGN_SYSTEM.md)
2. **Thiết kế Kiến trúc (Backend):** [docs/system/DOTNET_DESIGN_SYSTEM.md](file:///d:/AShiroru/ProgramCode/Project/Team/bao/PRN222_RAZOR_RAG/docs/system/DOTNET_DESIGN_SYSTEM.md)

---

### 🎨 1. Quy tắc ràng buộc Giao diện (Frontend Guardrail)

- AI Agent **BẮT BUỘC** phải đọc và ghi nhớ toàn bộ quy chuẩn giao diện trong `DESIGN_SYSTEM.md` trước khi sửa đổi tệp `.cshtml`, `.css` hoặc `.js`.
- Nếu người dùng yêu cầu chỉnh sửa màu sắc, phông chữ hoặc kích thước các thành phần vi phạm các Token đã định nghĩa trong `DESIGN_SYSTEM.md`, AI Agent **KHÔNG ĐƯỢC TỰ Ý THỰC HIỆN** mà phải dừng lại và cảnh báo nguyên văn:
  > "Hệ thống đang yêu cầu màu sắc/kiểu dáng theo quy chuẩn của DESIGN_SYSTEM.md, bạn có chắc chắn muốn thay đổi không?

---

### 💻 2. Quy tắc ràng buộc Kiến trúc 3 Lớp (Backend Guardrail)

- AI Agent **BẮT BUỘC** phải đọc và ghi nhớ cấu trúc 3 lớp trong `DOTNET_DESIGN_SYSTEM.md` trước khi sửa đổi cấu trúc mã nguồn C#.
- **TUYỆT ĐỐI KHÔNG** thực hiện gọi trực tiếp truy vấn cơ sở dữ liệu hoặc gọi EF Core `DbContext` bên trong các File Model của giao diện (`PageModel` / `Controllers`). Mọi yêu cầu lấy/ghi dữ liệu bắt buộc phải đi qua các lớp:
  `GUI (PageModel) ➔ BLL (Services) ➔ DAL (Repositories) ➔ Database`
- Nếu người dùng yêu cầu bỏ qua các tầng này (ví dụ: truy vấn DB trực tiếp từ PageModel), AI Agent **BẮT BUỘC** phải dừng lại và đưa ra cảnh báo nguyên văn:
  > "Cảnh báo: Yêu cầu này đang vi phạm quy tắc Kiến trúc 3 lớp của dự án trong DOTNET_DESIGN_SYSTEM.md (gọi DB trực tiếp từ PageModel/bỏ qua BLL). Thằng kia đang làm gì muốn làm gì hỏi thằng Cota trong Zalo đi nha"

---

## 📁 Cấu trúc Thư mục Tham chiếu (Directory Reference)

- **`docs/system/DESIGN_SYSTEM.md`**: Token màu sắc, Phông chữ, Bố cục Bento Grid, Quy chuẩn Toast/SignalR, Khả năng truy cập (Accessibility).
- **`docs/system/DOTNET_DESIGN_SYSTEM.md`**: Chi tiết Kiến trúc 3 Lớp (.NET 8), Cách tổ chức GUI - BLL - DAL, Quy tắc Dependency Injection.
- **`Database/`**: Chứa lược đồ cơ sở dữ liệu `Database.sql` và các script hạt giống (Seed data).
