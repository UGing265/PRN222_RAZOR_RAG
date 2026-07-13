-- =====================================================
-- MIGRATION: Create token_usage table
-- Mô tả: Tạo bảng token_usage để lưu trữ lượng token
--        sử dụng (hỏi đáp & embedding) của từng user theo từng ngày.
-- =====================================================

CREATE TABLE IF NOT EXISTS public.token_usage (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    user_id uuid NOT NULL,
    usage_date date DEFAULT CURRENT_DATE NOT NULL,
    chat_tokens integer DEFAULT 0 NOT NULL,
    doc_tokens integer DEFAULT 0 NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT token_usage_pkey PRIMARY KEY (id),
    CONSTRAINT token_usage_user_date_key UNIQUE (user_id, usage_date),
    CONSTRAINT token_usage_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE
);

ALTER TABLE public.token_usage ADD COLUMN IF NOT EXISTS doc_tokens integer DEFAULT 0 NOT NULL;

CREATE INDEX IF NOT EXISTS idx_token_usage_user_id ON public.token_usage USING btree (user_id);
CREATE INDEX IF NOT EXISTS idx_token_usage_usage_date ON public.token_usage USING btree (usage_date);

SELECT 'Migration tạo bảng token_usage hoàn thành!' AS status;
