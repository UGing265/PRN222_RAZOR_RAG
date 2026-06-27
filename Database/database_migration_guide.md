# Hướng Dẫn Toàn Tập: Loại Bỏ "Học Kỳ" (Academic Term) Khỏi Hệ Thống

Tài liệu này ghi chú lại toàn bộ các bước từ lúc bắt đầu lập kế hoạch loại bỏ logic "Học kỳ" khỏi ứng dụng, cho đến khi thực thi và xử lý triệt để các lỗi bất đồng bộ của Database PostgreSQL với Entity Framework (EF) Core.

---

## Phần 1: Giai Đoạn Dọn Dẹp Mã Nguồn (Code Refactoring)

Trước khi can thiệp vào Database, toàn bộ mã nguồn của hệ thống đã được dọn dẹp sạch sẽ để không còn bất kỳ dòng code nào phụ thuộc vào "Học kỳ":

1. **Lớp Data Access (DAL):** 
   - Xóa thực thể `AcademicTerm.cs`.
   - Gỡ bỏ thuộc tính `AcademicTermId` và liên kết (Navigation Property) khỏi các lớp `Subject.cs` và `Document.cs`.
   - Xóa khai báo bảng và các ràng buộc (Fluent API) trong `ApplicationDbContext.cs`.
2. **Lớp Business Logic (BLL):** 
   - Xóa `AcademicTermDto.cs`.
   - Loại bỏ các logic truy vấn/lọc theo Học kỳ ở trong `DocumentService.cs` và các DTO liên quan.
3. **Lớp Giao Diện Người Dùng (GUI):**
   - Loại bỏ các tab, bảng, menu thả xuống (dropdown) và các logic Javascript lọc theo Học kỳ ở tất cả các Razor Pages (`Categories`, `All`, `Mine`, `Chat`, `Create`, `Edit`, v.v.).
4. **Kiểm duyệt Code:** 
   - Lệnh `dotnet build GUI` chạy thành công (0 Lỗi), đảm bảo mã nguồn hoàn toàn ổn định.

---

## Phần 2: Khởi Tạo EF Core Migration

Khi hệ thống code C# đã thay đổi, ta cần sử dụng công cụ của EF Core để sinh ra đoạn mã tự động cập nhật cấu trúc bảng cho Database.

**Lệnh đã chạy trong Terminal:**
```bash
dotnet ef migrations add RemoveAcademicTerms -p DAL -s GUI
```
Lệnh này phân tích phiên bản code hiện tại so với lịch sử, và sinh ra một file C# (trong thư mục `DAL/Migrations/`) chứa các chỉ thị tự động như:
- `DropTable(name: "academic_terms")`
- `DropForeignKey(name: "fk_subjects_academic_term")`
- `DropColumn(name: "academic_term_id")`

---

## Phần 3: Lỗi Phát Sinh Khi Cập Nhật Database

Quá trình chuẩn bị hoàn hảo, nhưng khi chạy lệnh cập nhật database (`dotnet ef database update`), chúng ta đã gặp lỗi thực tế do sự lệch pha (out of sync) giữa Database trên máy và lịch sử Migration.

### 1. Lỗi Cũ Cố Tình Chạy Lại 
> **Lỗi:** `42P07: relation "academic_terms" already exists`
>
> **Lý do:** Postgres báo lỗi bảng đã tồn tại. Nguyên nhân là do bảng lịch sử của EF Core (`__EFMigrationsHistory`) bị mất/trống dữ liệu lịch sử của các lần chạy trước (có thể do bạn đã khôi phục CSDL bằng script SQL `Database.sql` thủ công thay vì dùng EF Core). Do đó, EF Core lầm tưởng đây là CSDL mới tinh nên nó cố gắng chạy lại các Migration cũ (chứa lệnh tạo bảng), gây ra xung đột.

### 2. Lỗi Không Tìm Thấy Khóa Để Xóa
> **Lỗi:** `42704: constraint "fk_subjects_academic_term" of relation "subjects" does not exist`
>
> **Lý do:** Ở Migration mới nhất (`RemoveAcademicTerms`), EF Core được chỉ thị xóa khóa ngoại của "Học kỳ" đi. Nhưng do trước đó chúng ta có thể đã dùng thao tác SQL gỡ bỏ thủ công trong Database, nên EF Core tìm không thấy đối tượng để xóa và sinh ra lỗi.

---

## Phần 4: Kịch Bản (Script) SQL Giải Quyết Triệt Để

Để xử lý dứt điểm tình trạng bất đồng bộ này mà không làm hỏng cấu trúc các file Migration của dự án (giữ nguyên để sau này deploy lên server), giải pháp chuẩn nhất là **chạy Script SQL thủ công để chèn lịch sử và xóa tàn dư**.

Bạn chỉ cần mở công cụ quản lý Database (như pgAdmin/DataGrip/DBeaver) và chạy đoạn SQL an toàn sau đây:

```sql
-- =====================================================================
-- BƯỚC 1: ĐÁNH DẤU CÁC MIGRATION CŨ LÀ "ĐÃ CHẠY" ĐỂ TRÁNH LỖI 1
-- =====================================================================
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260617132815_MultiDocumentChat', '8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260625002826_AddMustChangePasswordToUser', '8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;

-- =====================================================================
-- BƯỚC 2: DỌN DẸP THỦ CÔNG CÁC CỘT/BẢNG DƯ THỪA ĐỂ TRÁNH LỖI 2
-- =====================================================================
-- (Sử dụng lệnh IF EXISTS / CASCADE để không bị lỗi nếu object đã mất)

-- Xóa bảng academic_terms và mọi khóa ngoại liên kết tới nó
DROP TABLE IF EXISTS academic_terms CASCADE;

-- Xóa cột academic_term_id trong bảng subjects và documents
ALTER TABLE subjects DROP COLUMN IF EXISTS academic_term_id;
ALTER TABLE documents DROP COLUMN IF EXISTS academic_term_id;

-- =====================================================================
-- BƯỚC 3: ĐÁNH DẤU MIGRATION MỚI NHẤT LÀ "ĐÃ CHẠY" (Tùy chọn)
-- =====================================================================
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260627045433_RemoveAcademicTerms', '8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;
```

> Mẹo:
> Việc sử dụng `ON CONFLICT DO NOTHING` và `IF EXISTS` đảm bảo bạn có thể chạy kịch bản này nhiều lần thoải mái mà không bao giờ gặp lỗi CSDL.

## Phần 5: Kết Luận & Xác Nhận

Sau khi chạy xong kịch bản SQL trên, nếu bạn gõ lại lệnh:
```bash
dotnet ef database update -p DAL -s GUI
```
Hệ thống sẽ nhận diện chính xác và trả về thông báo:
**`No migrations were applied. The database is already up to date.`**

Quá trình gỡ bỏ "Học kỳ" và đồng bộ hóa Cơ sở dữ liệu kết thúc thành công 100%!
