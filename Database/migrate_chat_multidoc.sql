-- =====================================================
-- MIGRATION: Chat Multi-Document Support
-- Mô tả: Chuyển đổi chat_sessions từ schema 1 document
--        sang nhiều document qua bảng junction mới.
-- Chạy bằng: pgAdmin Query Tool hoặc script runner
-- =====================================================

-- BƯỚC 1: Xóa FK + index cũ của document_id trên chat_sessions
ALTER TABLE public.chat_sessions DROP CONSTRAINT IF EXISTS chat_sessions_document_id_fkey;
DROP INDEX IF EXISTS public.idx_chat_sessions_document_id;

-- BƯỚC 2: Tạo bảng junction chat_session_documents
CREATE TABLE IF NOT EXISTS public.chat_session_documents (
    session_id uuid NOT NULL,
    document_id uuid NOT NULL,
    CONSTRAINT chat_session_documents_pkey PRIMARY KEY (session_id, document_id),
    CONSTRAINT chat_session_documents_session_id_fkey
        FOREIGN KEY (session_id) REFERENCES public.chat_sessions(id) ON DELETE CASCADE,
    CONSTRAINT chat_session_documents_document_id_fkey
        FOREIGN KEY (document_id) REFERENCES public.documents(id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS idx_chat_session_documents_document_id
    ON public.chat_session_documents USING btree (document_id);

-- BƯỚC 3: Di chuyển dữ liệu cũ sang bảng mới (nếu có row cũ với document_id)
INSERT INTO public.chat_session_documents (session_id, document_id)
SELECT id, document_id
FROM public.chat_sessions
WHERE document_id IS NOT NULL
ON CONFLICT DO NOTHING;

-- BƯỚC 4: Xóa cột document_id cũ khỏi chat_sessions
ALTER TABLE public.chat_sessions DROP COLUMN IF EXISTS document_id;

-- BƯỚC 5: Thêm cột retrieved_chunk_ids vào chat_messages nếu chưa có
ALTER TABLE public.chat_messages
    ADD COLUMN IF NOT EXISTS retrieved_chunk_ids jsonb DEFAULT '[]'::jsonb;

-- BƯỚC 6: Ensure title NOT NULL với default value
ALTER TABLE public.chat_sessions ALTER COLUMN title SET DEFAULT 'Chat mới';
UPDATE public.chat_sessions SET title = 'Chat mới' WHERE title IS NULL;
ALTER TABLE public.chat_sessions ALTER COLUMN title SET NOT NULL;

-- DONE
SELECT 'Migration chat multi-document hoàn thành!' AS status;
