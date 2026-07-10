-- =====================================================
-- MIGRATION: Add doc_tokens column to token_usage table
-- Mô tả: Thêm cột doc_tokens để ghi nhận số token sử dụng cho
--        việc xử lý tài liệu (embedding / chunking) theo từng ngày.
-- =====================================================

ALTER TABLE public.token_usage ADD COLUMN IF NOT EXISTS doc_tokens integer DEFAULT 0 NOT NULL;

SELECT 'Migration bổ sung cột doc_tokens vào bảng token_usage hoàn thành!' AS status;
