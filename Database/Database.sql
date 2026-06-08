-- ==========================================
-- 1. EXTENSIONS (Các tiện ích mở rộng)
-- ==========================================
CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;
COMMENT ON EXTENSION pg_trgm IS 'text similarity measurement and index searching based on trigrams';

CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;
COMMENT ON EXTENSION "uuid-ossp" IS 'generate universally unique identifiers (UUIDs)';

CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA public;
COMMENT ON EXTENSION vector IS 'vector data type and ivfflat and hnsw access methods';

-- ==========================================
-- 2. SEQUENCES (Tạo chuỗi tự tăng)
-- ==========================================
CREATE SEQUENCE public.roles_id_seq
    AS smallint
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- ==========================================
-- 3. TABLES (Tạo bảng)
-- ==========================================
CREATE TABLE public.academic_terms (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    name character varying(200) NOT NULL,
    term_order integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE TABLE public.audit_logs (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    user_id uuid NOT NULL,
    action character varying(50) NOT NULL,
    target_table character varying(50) NOT NULL,
    target_id uuid NOT NULL,
    ip_address character varying(45),
    description text,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE TABLE public.document_chapters (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    document_id uuid NOT NULL,
    parent_chapter_id uuid,
    title character varying(400) NOT NULL,
    summary text,
    chapter_order integer NOT NULL,
    start_page integer,
    end_page integer,
    start_chunk_index integer,
    end_chunk_index integer,
    is_ai_generated boolean DEFAULT false NOT NULL,
    confidence_score numeric(5,4),
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE TABLE public.document_chunks (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    document_id uuid NOT NULL,
    chapter_id uuid,
    chunk_order integer NOT NULL,
    page_number integer,
    content text NOT NULL,
    content_tokens integer,
    chunk_hash character varying(64),
    metadata jsonb DEFAULT '{}'::jsonb NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    embedding public.vector(3072)
);

CREATE TABLE public.document_files (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    document_id uuid NOT NULL,
    original_filename character varying(255) NOT NULL,
    storage_path text NOT NULL,
    file_url text,
    mime_type character varying(100),
    file_size_bytes bigint NOT NULL,
    checksum_sha256 character varying(64),
    page_count integer,
    extracted_text text,
    extraction_status character varying(30) DEFAULT 'pending'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    s3_bucket character varying(128),
    s3_key character varying(512)
);

CREATE TABLE public.document_reports (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    document_id uuid NOT NULL,
    reporter_user_id uuid NOT NULL,
    reason character varying(255) NOT NULL,
    status character varying(50) DEFAULT 'pending'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE TABLE public.document_sources (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    name character varying(200) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE TABLE public.document_tags (
    document_id uuid NOT NULL,
    tag_id uuid NOT NULL
);

CREATE TABLE public.document_types (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    name character varying(50) NOT NULL,
    description text,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE TABLE public.documents (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    owner_user_id uuid NOT NULL,
    title character varying(300) NOT NULL,
    description text,
    subject_id uuid,
    status character varying(30) DEFAULT 'pending'::character varying NOT NULL,
    visibility character varying(30) DEFAULT 'school_wide'::character varying NOT NULL,
    page_count integer,
    total_chunks integer DEFAULT 0 NOT NULL,
    total_chapters integer DEFAULT 0 NOT NULL,
    view_count integer DEFAULT 0 NOT NULL,
    download_count integer DEFAULT 0 NOT NULL,
    search_text text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    approved_at timestamp with time zone,
    slug character varying(255),
    document_type_id uuid,
    language_id uuid,
    md5_hash character varying(32),
    academic_term_id uuid,
    document_source_id uuid
);

CREATE TABLE public.languages (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    code character varying(10) NOT NULL,
    name character varying(50) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE TABLE public.roles (
    id smallint NOT NULL,
    name character varying(50) NOT NULL
);

ALTER SEQUENCE public.roles_id_seq OWNED BY public.roles.id;

CREATE TABLE public.subjects (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    code character varying(50) NOT NULL,
    name character varying(200) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    academic_term_id uuid
);

CREATE TABLE public.tags (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    name character varying(100) NOT NULL,
    slug character varying(120) NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE TABLE public.upload_jobs (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    owner_user_id uuid NOT NULL,
    document_id uuid,
    file_name character varying(255) NOT NULL,
    file_size_bytes bigint NOT NULL,
    status character varying(50) DEFAULT 'pending'::character varying NOT NULL,
    progress_percent integer DEFAULT 0 NOT NULL,
    message text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    storage_path text,
    is_notified boolean DEFAULT false NOT NULL
);

CREATE TABLE public.user_bookmarks (
    user_id uuid NOT NULL,
    document_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE TABLE public.user_subjects (
    user_id uuid NOT NULL,
    subject_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


CREATE TABLE public.users (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    role_id smallint NOT NULL,
    full_name character varying(200) NOT NULL,
    email character varying(255) NOT NULL,
    email_verified boolean DEFAULT false NOT NULL,
    username character varying(255),
    "displayUsername" character varying(255),
    avatar_url text,
    is_active boolean DEFAULT true NOT NULL,
    is_blocked boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE TABLE public.sessions (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    token text NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    ip_address text,
    user_agent text,
    user_id uuid NOT NULL
);

CREATE TABLE public.accounts (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    account_id text NOT NULL,
    provider_id text NOT NULL,
    user_id uuid NOT NULL,
    access_token text,
    refresh_token text,
    id_token text,
    access_token_expires_at timestamp with time zone,
    refresh_token_expires_at timestamp with time zone,
    scope text,
    password text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone NOT NULL
);

CREATE TABLE public.verifications (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    identifier text NOT NULL,
    value text NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);

-- ==========================================
-- 4. DEFAULTS
-- ==========================================
ALTER TABLE ONLY public.roles ALTER COLUMN id SET DEFAULT nextval('public.roles_id_seq'::regclass);

-- ==========================================
-- 5. PRIMARY KEYS & UNIQUE CONSTRAINTS
-- ==========================================
ALTER TABLE ONLY public.academic_terms ADD CONSTRAINT academic_terms_name_key UNIQUE (name);
ALTER TABLE ONLY public.academic_terms ADD CONSTRAINT academic_terms_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.audit_logs ADD CONSTRAINT audit_logs_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.document_chapters ADD CONSTRAINT document_chapters_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.document_chunks ADD CONSTRAINT document_chunks_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.document_files ADD CONSTRAINT document_files_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.document_reports ADD CONSTRAINT document_reports_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.document_sources ADD CONSTRAINT document_sources_name_key UNIQUE (name);
ALTER TABLE ONLY public.document_sources ADD CONSTRAINT document_sources_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.document_tags ADD CONSTRAINT document_tags_pkey PRIMARY KEY (document_id, tag_id);
ALTER TABLE ONLY public.document_types ADD CONSTRAINT document_types_name_key UNIQUE (name);
ALTER TABLE ONLY public.document_types ADD CONSTRAINT document_types_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.documents ADD CONSTRAINT documents_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.languages ADD CONSTRAINT languages_code_key UNIQUE (code);
ALTER TABLE ONLY public.languages ADD CONSTRAINT languages_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.roles ADD CONSTRAINT roles_name_key UNIQUE (name);
ALTER TABLE ONLY public.roles ADD CONSTRAINT roles_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.subjects ADD CONSTRAINT subjects_code_key UNIQUE (code);
ALTER TABLE ONLY public.subjects ADD CONSTRAINT subjects_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.tags ADD CONSTRAINT tags_name_key UNIQUE (name);
ALTER TABLE ONLY public.tags ADD CONSTRAINT tags_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.tags ADD CONSTRAINT tags_slug_key UNIQUE (slug);
ALTER TABLE ONLY public.upload_jobs ADD CONSTRAINT upload_jobs_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.user_bookmarks ADD CONSTRAINT user_bookmarks_pkey PRIMARY KEY (user_id, document_id);
ALTER TABLE ONLY public.user_subjects ADD CONSTRAINT user_subjects_pkey PRIMARY KEY (user_id, subject_id);
ALTER TABLE ONLY public.users ADD CONSTRAINT users_email_key UNIQUE (email);

ALTER TABLE ONLY public.users ADD CONSTRAINT users_pkey PRIMARY KEY (id);

ALTER TABLE ONLY public.sessions ADD CONSTRAINT sessions_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.sessions ADD CONSTRAINT sessions_token_key UNIQUE (token);
ALTER TABLE ONLY public.accounts ADD CONSTRAINT accounts_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.verifications ADD CONSTRAINT verifications_pkey PRIMARY KEY (id);

-- ==========================================
-- 6. INDEXES
-- ==========================================
CREATE INDEX "IX_documents_document_source_id" ON public.documents USING btree (document_source_id);
CREATE INDEX idx_audit_logs_created_at ON public.audit_logs USING btree (created_at DESC);
CREATE INDEX idx_audit_logs_user_id ON public.audit_logs USING btree (user_id);
CREATE INDEX idx_document_chapters_document_id ON public.document_chapters USING btree (document_id);
CREATE INDEX idx_document_chapters_order ON public.document_chapters USING btree (document_id, chapter_order);
CREATE INDEX idx_document_chunks_chapter_id ON public.document_chunks USING btree (chapter_id);
CREATE INDEX idx_document_chunks_document_id ON public.document_chunks USING btree (document_id);
CREATE INDEX idx_document_chunks_metadata_gin ON public.document_chunks USING gin (metadata);
CREATE INDEX idx_document_files_document_id ON public.document_files USING btree (document_id);
CREATE INDEX idx_documents_md5_hash ON public.documents USING btree (md5_hash);
CREATE INDEX idx_documents_owner_user_id ON public.documents USING btree (owner_user_id);
CREATE INDEX idx_documents_search_fts ON public.documents USING gin (to_tsvector('simple'::regconfig, (((((COALESCE(title, ''::character varying))::text || ' '::text) || COALESCE(description, ''::text)) || ' '::text) || COALESCE(search_text, ''::text))));
CREATE INDEX idx_documents_status ON public.documents USING btree (status);
CREATE INDEX idx_documents_subject_id ON public.documents USING btree (subject_id);
CREATE INDEX idx_users_role_id ON public.users USING btree (role_id);
CREATE INDEX island_docs_visibility ON public.documents USING btree (visibility);
CREATE UNIQUE INDEX ix_documents_slug ON public.documents USING btree (slug);
CREATE INDEX idx_user_subjects_user_id ON public.user_subjects USING btree (user_id);
CREATE INDEX idx_user_subjects_subject_id ON public.user_subjects USING btree (subject_id);

CREATE INDEX sessions_user_id_idx ON public.sessions USING btree (user_id);
CREATE INDEX accounts_user_id_idx ON public.accounts USING btree (user_id);
CREATE INDEX verifications_identifier_idx ON public.verifications USING btree (identifier);


-- ==========================================
-- 7. FOREIGN KEYS
-- ==========================================
ALTER TABLE ONLY public.documents ADD CONSTRAINT "FK_documents_document_sources_document_source_id" FOREIGN KEY (document_source_id) REFERENCES public.document_sources(id) ON DELETE SET NULL;
ALTER TABLE ONLY public.audit_logs ADD CONSTRAINT audit_logs_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.document_chapters ADD CONSTRAINT document_chapters_document_id_fkey FOREIGN KEY (document_id) REFERENCES public.documents(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.document_chapters ADD CONSTRAINT document_chapters_parent_chapter_id_fkey FOREIGN KEY (parent_chapter_id) REFERENCES public.document_chapters(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.document_chunks ADD CONSTRAINT document_chunks_chapter_id_fkey FOREIGN KEY (chapter_id) REFERENCES public.document_chapters(id) ON DELETE SET NULL;
ALTER TABLE ONLY public.document_chunks ADD CONSTRAINT document_chunks_document_id_fkey FOREIGN KEY (document_id) REFERENCES public.documents(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.document_files ADD CONSTRAINT document_files_document_id_fkey FOREIGN KEY (document_id) REFERENCES public.documents(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.document_reports ADD CONSTRAINT document_reports_document_id_fkey FOREIGN KEY (document_id) REFERENCES public.documents(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.document_reports ADD CONSTRAINT document_reports_reporter_user_id_fkey FOREIGN KEY (reporter_user_id) REFERENCES public.users(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.document_tags ADD CONSTRAINT document_tags_document_id_fkey FOREIGN KEY (document_id) REFERENCES public.documents(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.document_tags ADD CONSTRAINT document_tags_tag_id_fkey FOREIGN KEY (tag_id) REFERENCES public.tags(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.documents ADD CONSTRAINT documents_owner_user_id_fkey FOREIGN KEY (owner_user_id) REFERENCES public.users(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.documents ADD CONSTRAINT documents_subject_id_fkey FOREIGN KEY (subject_id) REFERENCES public.subjects(id) ON DELETE SET NULL;
ALTER TABLE ONLY public.documents ADD CONSTRAINT fk_documents_academic_term FOREIGN KEY (academic_term_id) REFERENCES public.academic_terms(id) ON DELETE SET NULL;
ALTER TABLE ONLY public.documents ADD CONSTRAINT fk_documents_document_type FOREIGN KEY (document_type_id) REFERENCES public.document_types(id) ON DELETE SET NULL;
ALTER TABLE ONLY public.documents ADD CONSTRAINT fk_documents_language FOREIGN KEY (language_id) REFERENCES public.languages(id) ON DELETE SET NULL;
ALTER TABLE ONLY public.subjects ADD CONSTRAINT subjects_academic_term_id_fkey FOREIGN KEY (academic_term_id) REFERENCES public.academic_terms(id) ON DELETE SET NULL;
ALTER TABLE ONLY public.upload_jobs ADD CONSTRAINT upload_jobs_document_id_fkey FOREIGN KEY (document_id) REFERENCES public.documents(id) ON DELETE SET NULL;
ALTER TABLE ONLY public.upload_jobs ADD CONSTRAINT upload_jobs_owner_user_id_fkey FOREIGN KEY (owner_user_id) REFERENCES public.users(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.user_bookmarks ADD CONSTRAINT user_bookmarks_document_id_fkey FOREIGN KEY (document_id) REFERENCES public.documents(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.user_bookmarks ADD CONSTRAINT user_bookmarks_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.user_subjects ADD CONSTRAINT user_subjects_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.user_subjects ADD CONSTRAINT user_subjects_subject_id_fkey FOREIGN KEY (subject_id) REFERENCES public.subjects(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.users ADD CONSTRAINT users_role_id_fkey FOREIGN KEY (role_id) REFERENCES public.roles(id);

ALTER TABLE ONLY public.sessions ADD CONSTRAINT sessions_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;
ALTER TABLE ONLY public.accounts ADD CONSTRAINT accounts_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


-- ==========================================
-- 8. INSERT DATA
-- ==========================================
INSERT INTO public.roles (id, name) VALUES 
(1, 'Admin'),
(2, 'Lecturer'),
(3, 'Student')
ON CONFLICT (id) DO NOTHING;

-- Reset lại Sequence cho bảng roles để sau này nếu thêm Role mới sẽ tự tăng từ số 4
SELECT pg_catalog.setval('public.roles_id_seq', 3, true);