-- ==========================================
-- Migration: Add MustChangePassword to users
-- Generated: 2026-06-25
-- Feature: Admin User Verification & Force Change Password
-- ==========================================

BEGIN;

ALTER TABLE public.users
    ADD COLUMN IF NOT EXISTS must_change_password boolean DEFAULT false NOT NULL;

ALTER TABLE public.users
    ADD COLUMN IF NOT EXISTS password_changed_at timestamp with time zone;

COMMIT;

-- Rollback (if needed):
-- ALTER TABLE public.users DROP COLUMN IF EXISTS password_changed_at;
-- ALTER TABLE public.users DROP COLUMN IF EXISTS must_change_password;