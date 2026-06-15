# Hệ thống Thiết kế Giao diện (UI Design System) - StudyMate AI

Tài liệu này định nghĩa các quy chuẩn và token thiết kế giao diện (Design Tokens & UI/UX Patterns) được áp dụng thống nhất cho toàn bộ hệ thống **StudyMate AI**. 

Mục tiêu thẩm mỹ của dự án là hướng tới phong cách **"Minimalist Scholarly" (Học thuật tối giản)** kết hợp với hiệu ứng **"Glassmorphism" (Kính mờ)** cao cấp, mang lại cảm giác tri thức, thanh lịch, tinh tế nhưng vô cùng hiện đại.

---

## 1. NGUYÊN TẮC THIẾT KẾ CỐT LÕI (CORE PRINCIPLES)

1. **Tối giản Học thuật (Minimalist Scholarly):**
   * Sử dụng bảng màu đơn sắc (Monochrome) kết hợp với các tông màu nền xương (Bone-white / Alabaster).
   * Phân cấp văn bản chặt chẽ bằng cách kết hợp font chữ Serif cổ điển cho tiêu đề lớn và Sans-serif hiện đại cho nội dung.
   * Loại bỏ các khối đổ bóng dày (Heavy SaaS Shadows), thay vào đó sử dụng viền 1px mảnh sắc nét.
2. **Hiệu ứng Kính (Glassmorphism):**
   * Sử dụng nền bán trong suốt (Semi-transparent backgrounds) với hiệu ứng làm mờ nền phía sau (Backdrop blur) để tạo chiều sâu trực quan.
3. **Phản hồi Tương tác Tinh tế (Tactile Micro-interactions):**
   * Mọi trạng thái Hover/Active phải mượt mà (transitions 150-300ms) và tinh tế, không làm dịch chuyển bố cục (Layout shift).

---

## 2. BẢNG MÀU (COLOR PALETTE TOKENS)

Hệ thống sử dụng hệ màu HSL/Hex đã được tinh chỉnh để tạo ra sự tương phản dễ chịu cho mắt và đáp ứng chuẩn tiếp cận WCAG AA.

| Token | Tên Màu | Giá trị Hex / HSL | Ứng dụng thực tế |
| :--- | :--- | :--- | :--- |
| **`--bg-canvas`** | Bone White | `#FAF9F6` | Nền canvas chính của toàn bộ trang web |
| **`--bg-surface`** | Pure White / Glass | `rgba(255, 255, 255, 0.75)` | Nền thẻ (Cards), Sidebar với Backdrop Blur |
| **`--text-primary`** | Ink Black | `#09090B` (Slate-950) | Tiêu đề chính, văn bản quan trọng |
| **`--text-secondary`**| Charcoal | `#27272A` (Zinc-800) | Nội dung bài viết, nhãn form (Labels) |
| **`--text-muted`** | Muted Slate | `#71717A` (Zinc-500) | Metadata, chú thích, văn bản phụ |
| **`--border-crisp`** | Crisp Zinc | `#E4E4E7` (Zinc-200) | Đường viền ngăn cách 1px siêu mảnh |
| **`--accent-primary`**| Charcoal Ink | `#18181B` (Zinc-900) | Màu nền Button chính, trạng thái active |
| **`--accent-hover`** | Ash Gray | `#F4F4F5` (Zinc-100) | Trạng thái Hover của các mục menu / nút bấm |

### Màu Chỉ Báo Trạng Thái (Subtle Status Colors)
Tuyệt đối không dùng các màu đỏ, xanh lục nguyên bản chói mắt. Hãy dùng các sắc độ pastel dịu nhẹ:
* **Success (Thành công):** Nền `rgba(240, 253, 244, 0.8)` (Emerald-50/80%), Viền `rgba(187, 247, 208, 1)`, Chữ `#166534`.
* **Danger/Alert (Cảnh báo):** Nền `rgba(254, 242, 242, 0.8)` (Red-50/80%), Viền `rgba(254, 226, 226, 1)`, Chữ `#991b1b`.
* **SignalR Real-time Active:** Dấu chấm nhấp nháy xanh lá dịu `#22c55e`.

---

## 3. HỆ THỐNG PHÔNG CHỮ (TYPOGRAPHY)

Hệ thống tích hợp 2 bộ font từ Google Fonts: **Inter** (cho giao diện/hệ thống) và **Source Serif 4** (cho tiêu đề học thuật).

```css
/* Font Khai báo trong CSS */
--font-sans: 'Inter', system-ui, -apple-system, sans-serif;
--font-serif: 'Source Serif 4', Georgia, serif;
```

### Quy tắc phân cấp văn bản (Text Hierarchy)

1. **Academic Headings (Tiêu đề học thuật):**
   * Sử dụng `--font-serif` với độ dày `Medium (500)` hoặc `Semi-bold (600)`.
   * Thường dùng cho tiêu đề bài viết, tên chương tài liệu, tiêu đề lớn trong Dashboard.
   * Tiêu chuẩn: `font-serif tracking-tight text-text-primary`.
2. **Interface Text (Giao diện ứng dụng):**
   * Sử dụng `--font-sans` với độ dày `Normal (400)` hoặc `Medium (500)`.
   * Dùng cho Menu, Form Labels, Bảng dữ liệu, Chat Message.
3. **Thông số chi tiết:**
   * **H1 (Hero):** 2.25rem (36px), Line-height: 1.2
   * **H2 (Section):** 1.5rem (24px), Line-height: 1.3
   * **H3 (Subsection):** 1.25rem (20px), Line-height: 1.4
   * **Body text:** 1rem (16px), Line-height: 1.6 (Không dùng cỡ chữ dưới 14px cho phần nội dung đọc dài).

---

## 4. BỐ CỤC & HỘP DỮ LIỆU (BENTO GRID & CARDS)

Hệ thống khuyến khích sử dụng phong cách **Bento Grid** để nhóm thông tin một cách ngăn nắp và trực quan.

### Thiết kế Thẻ (Card Design Specifications)
* **Khung viền:** `border: 1px solid var(--border-crisp)`
* **Độ bo góc:** Bo nhẹ góc học thuật `border-radius: 8px` (Tuyệt đối không dùng `rounded-2xl` hoặc `rounded-3xl` quá tròn).
* **Độ bóng (Shadow):** Không dùng bóng hoặc chỉ dùng bóng siêu nhẹ:
  `box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.02)`
* **Hiệu ứng Glassmorphism:**
  ```css
  background: rgba(255, 255, 255, 0.75);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  ```

---

## 5. CÁC THÀNH PHẦN GIAO DIỆN CHUẨN (UI COMPONENTS)

### 5.1. Nút Bấm (Buttons)
* **Primary Button:**
  * Nền: `var(--accent-primary)` (Đen/Zinc-900).
  * Chữ: `#FFFFFF` (Sans-serif, Semi-bold).
  * Hover: Nền chuyển dịch nhẹ sang đen hoàn toàn, giảm opacity hoặc thay đổi sang màu xám đậm.
* **Secondary / Glass Button:**
  * Nền: `rgba(255, 255, 255, 0.5)` hoặc trong suốt.
  * Viền: `1px solid var(--border-crisp)`.
  * Chữ: `var(--text-primary)`.
  * Hover: `background-color: var(--accent-hover)`.

### 5.2. Nhập Liệu (Input Fields)
* **Trạng thái thường:** Viền `1px solid var(--border-crisp)`, nền màu xương hoặc trắng mờ.
* **Trạng thái Focus:** Viền chuyển sang đen `var(--text-primary)`, thêm vòng focus mảnh `outline: 2px solid rgba(9, 9, 11, 0.05)`.
* **Trạng thái Lỗi (Error):** Viền chuyển sang đỏ nhạt `#fca5a5`, thông điệp báo lỗi xuất hiện ngay dưới input với cỡ chữ `13px` màu đỏ tối `#991b1b`.

### 5.3. Bảng Dữ Liệu (Data Tables)
* **Header:** Nền xám xương siêu nhạt `var(--accent-hover)`, chữ in hoa nhẹ, cỡ chữ `13px`, độ dày `Semi-bold (600)`.
* **Hàng (Rows):** Ngăn cách bằng đường kẻ dưới 1px mảnh. Hover hàng đổi sang màu xám xương nhạt để tăng trải nghiệm tra cứu dữ liệu.

---

## 6. QUY TẮC HIỆN THỊ TOAST VÀ THÔNG BÁO THỜI GIAN THỰC

### 6.1. Hộp thoại Toast Cao Cấp (Premium Toast Notification)
* Phải được cố định ở góc trên bên phải trang web (`top-4 right-4`).
* Thiết kế dạng Glassmorphism, bo góc `8px`, viền 1px tinh tế.
* Có thanh trượt thời gian chạy ngầm phía dưới báo hiệu thời điểm biến mất.
* Lớp hiệu ứng CSS chuyển động nhẹ nhàng khi trượt từ phải qua trái.

### 6.2. SignalR Status Dot
* Tại các giao diện CRUD Real-time hoặc trạng thái Chat, bổ sung một biểu tượng nhỏ biểu thị trạng thái kết nối thời gian thực:
  * **Đang kết nối:** Dấu chấm nhấp nháy xanh lá dịu kèm chú thích nhỏ "Trực tuyến".
  * **Mất kết nối:** Dấu chấm màu đỏ kèm chữ "Ngoại tuyến (đang thử kết nối lại...)".

---

## 7. TIÊU CHUẨN TRUY CẬP (ACCESSIBILITY & LIGHT/DARK CONTRACT)

1. **Chuẩn Contrast (Tương phản):**
   * Tất cả văn bản hiển thị phải có độ tương phản tối thiểu là **4.5:1** so với màu nền.
2. **Khả năng điều hướng bằng bàn phím (Keyboard Navigation):**
   * Mọi phần tử click được (Buttons, Links, Inputs) bắt buộc phải có thuộc tính `:focus` rõ ràng để người dùng dùng phím `Tab` nhận biết được vị trí con trỏ.
3. **Cursor Pointer:**
   * Mọi thành phần có thể tương tác (bao gồm cả các Card tài liệu có thể click) bắt buộc phải có thuộc tính `cursor: pointer`.
4. **Không lạm dụng biểu tượng cảm xúc (No Emojis as Icons):**
   * Sử dụng biểu tượng SVG đồng nhất (khuyến nghị Lucide Icons hoặc Heroicons) thay cho biểu tượng Emoji để giữ nguyên dáng vẻ học thuật nghiêm túc.
