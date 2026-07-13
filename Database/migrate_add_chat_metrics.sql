-- =====================================================
-- MIGRATION: Add Chat Metrics (TokenCount & LatencyMs)
-- Mô tả: Thêm 2 trường token_count và latency_ms vào bảng chat_messages
--        để phục vụ Dashboard TokenUsage và Benchmark RAG
-- =====================================================

ALTER TABLE public.chat_messages
    ADD COLUMN IF NOT EXISTS token_count integer DEFAULT 0,
    ADD COLUMN IF NOT EXISTS latency_ms integer DEFAULT 0;

SELECT 'Migration bổ sung token_count & latency_ms hoàn thành!' AS status;
