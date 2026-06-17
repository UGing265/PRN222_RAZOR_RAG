# Implementation Plan — RAG Chatbot Đa Tài Liệu (PRN222_RAZOR_RAG)

> Phiên bản đã đối chiếu và đồng bộ với kiến trúc thực tế của dự án (xem mục "Đối chiếu kiến trúc" ở cuối file). Đưa file này cho Antigravity để thực thi theo đúng thứ tự Task.

## Role

Act as a Senior .NET Backend Developer and AI/RAG Architect, có kinh nghiệm triển khai production các hệ thống document-AI dùng N-Tier Architecture.

## Tech Stack (Bắt buộc tuân thủ)

- Backend: ASP.NET Core 8, Entity Framework Core 8
- GUI: Razor Pages (giao diện) + Minimal APIs (route AJAX/SSE) — KHÔNG dùng MVC Controllers
- Database & Vector Store: PostgreSQL với pgvector extension (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- LLM: Google Gemini 1.5 Flash (gọi qua REST HTTP Client thuần, KHÔNG dùng SDK wrapper)
- Embedding Model: Google `text-embedding-004` (output: 768 dims)
- Auth: ASP.NET Core Identity, lấy `userId` từ JWT claim `"sub"`
- Kiến trúc: N-Tier (BLL / DAL / GUI)
- KHÔNG dùng LangChain.NET, Semantic Kernel — toàn bộ logic RAG viết thủ công.

## Mục tiêu

Viết code backend hoàn chỉnh, production-ready cho hệ thống RAG Chatbot đa tài liệu, gồm **cả 2 giai đoạn**: Indexing (upload → chunk → embed → lưu) và Chat & Retrieval (embed câu hỏi → semantic search → build prompt → stream → lưu lịch sử + nguồn trích dẫn). Code phải chia đúng theo cấu trúc thư mục dưới đây và biên dịch được ngay.

## Cấu trúc thư mục bắt buộc

```text
PRN222_RAZOR_RAG/
├── BLL/
│   ├── DTOs/
│   │   ├── Chat/ChatDtos.cs
│   │   └── Documents/DocumentDtos.cs
│   ├── Interfaces/
│   │   ├── IChatService.cs
│   │   ├── ICompareService.cs
│   │   ├── IGeminiChatService.cs
│   │   ├── IDocumentService.cs
│   │   └── IGeminiEmbeddingService.cs
│   ├── Constants/
│   │   └── PromptTemplates.cs
│   ├── Exceptions/
│   │   └── RagExceptions.cs
│   └── Services/
│       ├── Chat/
│       │   ├── ChatService.cs
│       │   └── GeminiChatService.cs
│       └── Documents/
│           ├── DocumentService.cs
│           ├── DocumentChunker.cs
│           ├── GeminiEmbeddingService.cs
│           └── CompareService.cs          # ← Tách riêng theo SRP (không gộp vào ChatService)
├── DAL/
│   ├── Entities/
│   │   ├── Document.cs
│   │   ├── DocumentChunk.cs
│   │   ├── ChatSession.cs
│   │   ├── ChatSessionDocument.cs         # ← Bảng trung gian N-N (thay thế JSON column)
│   │   └── ChatMessage.cs
│   ├── Interfaces/
│   │   ├── IChatRepository.cs
│   │   └── IDocumentRepository.cs
│   └── Data/                              # ← Thư mục chứa DbContext (không phải Repositories/)
│       └── DBContext.cs                   # ← Tên thực tế của DbContext
│   └── Repositories/
│       ├── ChatRepository.cs
│       └── DocumentRepository.cs
└── GUI/
    ├── Endpoints/
    │   ├── ChatEndpoints.cs
    │   └── DocumentEndpoints.cs
    ├── Pages/
    │   ├── Chat/Index.cshtml(.cs)
    │   └── Documents/
    │       └── Compare.cshtml(.cs)        # ← Xử lý Compare trực tiếp tại PageModel (POST)
    └── Program.cs
```

> Ghi chú: Lần triển khai này chỉ tập trung vào BLL + DAL + GUI/Endpoints (API layer). Razor Pages (.cshtml) KHÔNG nằm trong scope của lần code này — chỉ cần đảm bảo các endpoint trả đúng dữ liệu để Razor Pages/JS phía trên gọi vào được. **Ngoại lệ:** `Compare.cshtml.cs` xử lý POST trực tiếp tại PageModel, không đi qua Minimal API.

---

## Task 1 — DAL (Data Access Layer)

### 1a. `DAL/Entities/` — Entity Models

Viết entity class với Data Annotations + Fluent API config trong `DBContext`:

- **`Document`**: `Id (Guid)`, `UserId (string)`, `FileName`, `FilePath`, `UploadedAt (DateTime)`, `IsDeleted (bool)`, `Status (enum DocumentStatus)`.
  - `enum DocumentStatus { Pending, Processing, Completed, Approved, Failed }`
  - **Quy ước workflow status (ghi rõ comment trong code để tránh AI tự đoán sai):**
    1. `Pending` — vừa upload, chưa xử lý.
    2. `Processing` — Background Service đang chunking + embedding.
    3. `Completed` — xử lý kỹ thuật xong (đã có chunks + vectors trong DB), nhưng **chưa chắc đã được người có quyền (Lecturer) duyệt**.
    4. `Approved` — đã được duyệt thủ công, ưu tiên hiển thị cho Student.
    5. `Failed` — lỗi trong quá trình extract/chunk/embed.
  - **Điều kiện được phép Chat (cả 2 đều hợp lệ):** `Status == Completed || Status == Approved`.

- **`DocumentChunk`**: `Id (Guid)`, `DocumentId (Guid, FK)`, `Content (string)`, `ChunkIndex (int)`, `Embedding (Vector(768))` dùng kiểu `Pgvector.Vector` của Npgsql.
  - Thêm `PageNumber (int?)` và `SectionTitle (string?)` nếu có thể trích xuất được — để phục vụ tính năng "Nguồn trích dẫn" (trang/chương) hiển thị cho UI.

- **`ChatSession`**: `Id (Guid)`, `UserId (string)`, `Title (string)`, `CreatedAt`, `UpdatedAt`.
  - **KHÔNG có cột `ActiveDocumentIds` (JSON).** Quan hệ với Document được quản lý qua bảng trung gian `ChatSessionDocument`.

- **`ChatSessionDocument`** *(bảng trung gian N-N)*:
  - `ChatSessionId (Guid, FK → ChatSession.Id)`
  - `DocumentId (Guid, FK → Document.Id)`
  - Khóa chính composite: `(ChatSessionId, DocumentId)`.
  - Navigation properties: `ChatSession`, `Document`.
  - **Lý do dùng bảng trung gian thay vì JSON column:** Đảm bảo tính toàn vẹn tham chiếu (Foreign Key constraint), dễ query/filter hơn, chuẩn nguyên tắc CSDL quan hệ.

- **`ChatMessage`**: `Id (Guid)`, `SessionId (Guid, FK)`, `Role (enum: User/Assistant)`, `Content (string)`, `CreatedAt`, `RetrievedChunkIds (List<Guid>, JSON)` — dùng để dựng lại "Nguồn trích dẫn" khi load lại lịch sử chat.
  - `RetrievedChunkIds` vẫn dùng JSON ValueConverter vì đây là dữ liệu phụ trợ, không cần query/filter trực tiếp theo từng ChunkId.

### 1b. `DAL/Data/DBContext.cs`

> **Lưu ý vị trí:** DbContext nằm tại `DAL/Data/DBContext.cs`, KHÔNG phải `DAL/Repositories/ApplicationDbContext.cs`.

- **`DBContext`** (kế thừa `IdentityDbContext` hoặc `DbContext` tùy project):
  - Đăng ký đầy đủ `DbSet<Document>`, `DbSet<DocumentChunk>`, `DbSet<ChatSession>`, `DbSet<ChatSessionDocument>`, `DbSet<ChatMessage>`.
  - `modelBuilder.HasPostgresExtension("vector")`.
  - Cấu hình `Embedding` là `vector(768)` qua Fluent API (`HasColumnType("vector(768)")`).
  - Cấu hình quan hệ N-N `ChatSession ↔ Document` qua `ChatSessionDocument` bằng Fluent API:
    ```csharp
    modelBuilder.Entity<ChatSessionDocument>()
        .HasKey(csd => new { csd.ChatSessionId, csd.DocumentId });

    modelBuilder.Entity<ChatSessionDocument>()
        .HasOne(csd => csd.ChatSession)
        .WithMany(cs => cs.ChatSessionDocuments)
        .HasForeignKey(csd => csd.ChatSessionId);

    modelBuilder.Entity<ChatSessionDocument>()
        .HasOne(csd => csd.Document)
        .WithMany()
        .HasForeignKey(csd => csd.DocumentId);
    ```
  - Cấu hình JSON column cho `RetrievedChunkIds` trong `ChatMessage` dùng `ValueConverter` + `ValueComparer` (EF Core 8 chuẩn, tránh warning "no value comparer configured").

### 1c. `DAL/Repositories/`

- **`IDocumentRepository` / `DocumentRepository`**:
  - CRUD cho `Document`.
  - `AddChunksAsync(List<DocumentChunk> chunks, CancellationToken ct)` — bulk insert.
  - `UpdateStatusAsync(Guid documentId, DocumentStatus status, CancellationToken ct)`.
  - `GetDocumentTextAsync(List<Guid> documentIds, CancellationToken ct)` — ghép toàn bộ `Content` của các chunk theo `DocumentId`, dùng cho `CompareService`.

- **`IChatRepository` / `ChatRepository`**:
  - `SearchSimilarChunksAsync(Vector questionEmbedding, List<Guid> activeDocumentIds, int topK, CancellationToken ct)`:
    - **Tên hàm chính xác: `SearchSimilarChunksAsync`** (không phải `SearchTopKChunksAsync`).
    - Dùng `EF.Functions.CosineDistance` (Npgsql.pgvector) để sắp xếp `DocumentChunk` theo độ gần với vector câu hỏi.
    - Filter `DocumentChunk.DocumentId IN activeDocumentIds` VÀ join với `Document` để chỉ lấy chunk thuộc Document có `Status == Completed || Status == Approved`.
    - Tham số `activeDocumentIds` kiểu `List<Guid>` — **hỗ trợ lọc theo nhiều Document cùng lúc**.
    - Trả về top K kèm `DocumentId`, `PageNumber`/`SectionTitle` (phục vụ nguồn trích dẫn).
  - `GetChatHistoryAsync(Guid sessionId, int lastN, CancellationToken ct)`.
  - `SaveMessageAsync(ChatMessage message, CancellationToken ct)`.
  - `AddDocumentToSessionAsync(Guid sessionId, Guid documentId, CancellationToken ct)` — thêm record vào `ChatSessionDocument`.
  - `GetActiveDocumentIdsAsync(Guid sessionId, CancellationToken ct)` — lấy danh sách DocumentId từ `ChatSessionDocument` theo SessionId.

---

## Task 2 — BLL (Business Logic Layer)

### 2a. `BLL/DTOs/`

- **`Chat/ChatDtos.cs`**:
  - `ChatRequestDto { Guid SessionId; string Question; List<Guid> DocumentIds; }`
  - `ChatSourceDto { Guid DocumentId; string FileName; int? PageNumber; string? SectionTitle; string ContentSnippet; }` — dùng để trả "Nguồn trích dẫn" về UI sau khi stream xong.
  - `ComparisonResultDto { decimal SimilarityPercentage; string SimilarityExplanation; List<string> SimilarPoints; Dictionary<string, List<string>> DifferentPoints; }`

- **`Documents/DocumentDtos.cs`**:
  - `DocumentUploadResultDto { Guid DocumentId; string FileName; DocumentStatus Status; }`
  - `DocumentChunkPreviewDto { int ChunkIndex; string ContentPreview; }`

### 2b. `BLL/Services/Documents/` — Indexing Pipeline

- **`DocumentChunker`** (logic thuần, không cần interface):
  - `List<string> ChunkText(string fullText, int chunkSizeWords = 400, int overlapWords = 50)`.
  - Cắt theo số từ, có overlap; cố gắng cắt tại ranh giới câu/đoạn (tránh cắt giữa câu) bằng cách ưu tiên dấu `.`, `\n` gần điểm cắt.

- **`IGeminiEmbeddingService` / `GeminiEmbeddingService`**:
  - `Task<float[]> EmbedTextAsync(string text, CancellationToken ct)` và `Task<List<float[]>> EmbedBatchAsync(List<string> texts, CancellationToken ct)`.
  - Gọi REST API Google `text-embedding-004` bằng `HttpClient` thuần.
  - Cache bằng `IMemoryCache`, key = `SHA256(text)`, TTL hợp lý (ví dụ 24h) để tránh embed lại text giống nhau.
  - Retry/backoff khi gặp rate limit (429) dùng Polly (`AddRetryPolicy` với exponential backoff).
  - Throw `EmbeddingFailedException` (custom) khi API lỗi sau khi retry hết.

- **`IDocumentService` / `DocumentService`**:
  - `Task<DocumentUploadResultDto> UploadAndProcessAsync(IFormFile file, string userId, CancellationToken ct)`:
    1. Lưu file vào storage (local path hoặc cloud — abstraction qua interface riêng nếu cần), tạo record `Document` với `Status = Pending`.
    2. Trigger xử lý nền (có thể inline async hoặc enqueue background job — ghi rõ TODO nếu dùng `IHostedService`/Hangfire sau): extract text (PDF/Word) → `DocumentChunker.ChunkText` → `GeminiEmbeddingService.EmbedBatchAsync` → lưu `DocumentChunk` qua `IDocumentRepository.AddChunksAsync`.
    3. Cập nhật `Status = Completed` nếu thành công, `Status = Failed` nếu lỗi (catch và log rõ nguyên nhân).
  - Text extraction: dùng thư viện phù hợp cho PDF (ví dụ `PdfPig` hoặc `iText7`) và Word (`DocumentFormat.OpenXml`) — KHÔNG cần cài đặt chi tiết trong plan này, chỉ cần interface `ITextExtractor.ExtractTextAsync(string filePath, CancellationToken ct)` để dễ thay thế.

- **`ICompareService` / `CompareService`** *(tách riêng, KHÔNG nằm trong ChatService)*:
  - **Vị trí file: `BLL/Services/Documents/CompareService.cs`**
  - **Lý do tách riêng:** Đảm bảo nguyên tắc Single Responsibility (SRP) — So sánh tài liệu là nghiệp vụ độc lập với Chat.
  - `Task<ComparisonResultDto> CompareDocumentsAsync(List<Guid> documentIds, string question, string userId, CancellationToken ct)`:
    - Lấy toàn bộ text các Document (`IDocumentRepository.GetDocumentTextAsync`).
    - Build `COMPARISON_PROMPT` với `{doc_contexts}`.
    - Gọi `GeminiChatService.GenerateAsync` (non-stream), parse JSON trả về thành `ComparisonResultDto`.
    - Throw `LlmResponseParsingException` (custom) nếu parse JSON thất bại, kèm log raw response để debug.

### 2c. `BLL/Services/Chat/` — Core RAG Logic

- **`IGeminiChatService` / `GeminiChatService`**: gọi Gemini 1.5 Flash REST API, hỗ trợ cả streaming (SSE từ Google) và non-streaming.
  - `IAsyncEnumerable<string> StreamGenerateAsync(string systemPrompt, List<(string role, string content)> history, string userMessage, CancellationToken ct)`.
  - `Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct)` — dùng cho `CompareService` (non-stream).

- **`IChatService` / `ChatService`** — điều phối luồng Chat RAG:
  - **Lưu ý:** `ChatService` KHÔNG chứa logic Compare. Nhiệm vụ duy nhất là xử lý luồng Chat.

  1. `IAsyncEnumerable<string> StreamMessageAsync(ChatRequestDto request, string userId, CancellationToken ct)`
     - Lấy lịch sử chat gần nhất (`IChatRepository.GetChatHistoryAsync`).
     - Embed câu hỏi (`IGeminiEmbeddingService.EmbedTextAsync`).
     - Semantic Search — gọi **`IChatRepository.SearchSimilarChunksAsync`** (tên hàm thực tế), truyền `request.DocumentIds` là `List<Guid>` — chỉ trong các document có `Status` hợp lệ (`Completed`/`Approved`).
     - Nếu không tìm được chunk nào liên quan → vẫn build prompt với context rỗng, để `RAG_SYSTEM_PROMPT` tự xử lý việc từ chối trả lời.
     - Build `RAG_SYSTEM_PROMPT` với `{context_chunks}` đã ghép.
     - Gọi `GeminiChatService.StreamGenerateAsync`, yield từng token ra ngoài.
     - Sau khi stream xong: lưu `ChatMessage` (role User + role Assistant) bất đồng bộ, kèm `RetrievedChunkIds` để phục vụ hiển thị "Nguồn trích dẫn" khi load lại lịch sử.
     - Throw `DocumentNotReadyException` nếu tất cả documentIds được chọn đều không ở trạng thái hợp lệ.

### 2d. `BLL/Constants/PromptTemplates.cs`

- `RAG_SYSTEM_PROMPT`: ràng buộc AI CHỈ trả lời dựa trên `{context_chunks}`; bắt buộc từ chối lịch sự nếu context không chứa thông tin liên quan (chống hallucination); yêu cầu AI trích dẫn rõ chunk/nguồn nào đã dùng nếu có thể (hỗ trợ tính năng nguồn trích dẫn ở UI).
- `COMPARISON_PROMPT`: ép AI trả về đúng cấu trúc JSON khớp 100% với `ComparisonResultDto` (field names trùng khớp để parse trực tiếp), liệt kê rõ schema mẫu trong prompt.

### 2e. `BLL/Exceptions/RagExceptions.cs`

Định nghĩa custom exception, không throw raw `Exception`:
- `EmbeddingFailedException`
- `DocumentNotReadyException`
- `LlmResponseParsingException`
- `SemanticSearchException`

---

## Task 3 — GUI (Presentation Layer)

### `GUI/Endpoints/ChatEndpoints.cs`

1. `MapPost("/api/chat/messages/stream")`:
   - Nhận `ChatRequestDto`, lấy `userId` từ JWT claim `sub`.
   - Set response `Content-Type: text/event-stream`.
   - Gọi `IChatService.StreamMessageAsync`, ghi từng token ra response stream theo format SSE (`data: {token}\n\n`), flush sau mỗi chunk.
   - Bắt exception custom (`DocumentNotReadyException`, v.v.) và trả SSE event lỗi rõ ràng (`event: error\ndata: {message}\n\n`) trước khi đóng stream.

> **Lưu ý:** Endpoint `/api/chat/compare` **KHÔNG tồn tại** trong hệ thống thực tế. Tính năng Compare được xử lý hoàn toàn phía Razor Page (xem mục dưới).

### `GUI/Pages/Documents/Compare.cshtml.cs` — Xử lý Compare tại PageModel

- Tính năng So sánh Tài liệu (Compare) được thực hiện trực tiếp tại Razor Page, KHÔNG đi qua Minimal API endpoint.
- `OnPostAsync()` trong `Compare.cshtml.cs`:
  - Nhận danh sách `DocumentIds` và `Question` từ form POST.
  - Inject và gọi `ICompareService.CompareDocumentsAsync(...)`.
  - Bind kết quả `ComparisonResultDto` vào Model để Razor Page render bảng so sánh.

### `GUI/Endpoints/DocumentEndpoints.cs`

1. `MapPost("/api/documents/upload")`: nhận `IFormFile`, gọi `IDocumentService.UploadAndProcessAsync`, trả `DocumentUploadResultDto`.
2. `MapGet("/api/documents")`: liệt kê document của user hiện tại (kèm `Status` để UI hiển thị dropdown chọn tài liệu và disable các document chưa `Completed`/`Approved`).
3. `MapPut("/api/documents/{id}/approve")`: đổi `Status` sang `Approved` (giới hạn role `Lecturer` qua `[Authorize(Roles = "Lecturer")]` hoặc check thủ công trong handler).

---

## Constraint chung (áp dụng toàn bộ Task)

- Mọi async method phải nhận `CancellationToken` và truyền xuyên suốt.
- KHÔNG throw raw `Exception` — luôn dùng custom exception định nghĩa ở `BLL/Exceptions/`.
- Log đầy đủ (dùng `ILogger<T>`) tại các điểm quan trọng: bắt đầu/kết thúc embedding, kết quả semantic search (số chunk tìm được + thời gian), lỗi gọi Gemini API, lỗi parse JSON.
- Toàn bộ gọi Gemini API dùng `HttpClient` thuần qua `IHttpClientFactory`, KHÔNG dùng SDK wrapper của Google.

---

## Đối chiếu kiến trúc — 4 điểm thay đổi so với Plan gốc

| # | Điểm xung đột | Plan gốc | Thực tế hệ thống (file này) |
|---|---|---|---|
| 1 | **Quan hệ ChatSession ↔ Document** | Cột `ActiveDocumentIds` (JSON) trong `ChatSession` | Bảng trung gian `ChatSessionDocument` (N-N chuẩn, có Foreign Key) |
| 2 | **Tên & vị trí DbContext** | `DAL/Repositories/ApplicationDbContext.cs` | `DAL/Data/DBContext.cs` |
| 3 | **Vị trí logic Compare** | `CompareDocumentsAsync` trong `ChatService` + endpoint `/api/chat/compare` | `BLL/Services/Documents/CompareService.cs` + POST handler tại `Compare.cshtml.cs` |
| 4 | **Tên hàm Semantic Search** | `SearchTopKChunksAsync` | `SearchSimilarChunksAsync` (có hỗ trợ `List<Guid> documentIds`) |

---

## Output Format yêu cầu

Trả lời theo đúng thứ tự này, mỗi phần trong code block riêng với đường dẫn file rõ trên đầu (ví dụ: `// BLL/Services/Chat/ChatService.cs`):

1. **DAL Entities & DBContext** (Task 1a, 1b)
2. **DAL Interfaces & Repositories** (Task 1c)
3. **BLL Exceptions & Constants (Prompts)** (Task 2d, 2e)
4. **BLL DTOs** (Task 2a)
5. **BLL Services — Documents (Chunker, Embedding, DocumentService, CompareService)** (Task 2b)
6. **BLL Services — Chat (GeminiChatService, ChatService)** (Task 2c)
7. **GUI Endpoints + Compare PageModel** (Task 3)