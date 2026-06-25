# Development Roadmap

Living document tracking project phases, milestones, and progress.

Last updated: **2026-06-25**

---

## Current Phase: Production Hardening

### In Progress
- Admin user verification + force change password (2026-06-25 — **Code complete, smoke test pending**)

### Completed
- ✅ **2026-06-25** — Admin User Verification & Force Change Password
  - Email verification flow with throttled background queue
  - Global middleware enforces password change before feature access
  - SMTP startup warning + config sections
  - DB migration + schema sync
- ✅ **Earlier** — SignalR real-time document CRUD + audit log notifications
- ✅ **Earlier** — Document comparison + PDF export (QuestPDF)
- ✅ **Earlier** — Excel bulk user import (existing flow, not yet refactored to email queue)

### Next Up
- [ ] **Apply DB migration** to production: `dotnet ef database update` or run `migrate_must_change_password.sql`
- [ ] **Manual smoke tests**: happy path, expired token, wrong password, Google user, bulk import (see `docs/superpowers/plans/2026-06-25-admin-user-verification.md` Tasks 25-27)
- [ ] **Unit tests**: write xUnit tests for `PasswordGenerator`, `RegisterAsync`, `ChangePasswordAsync`, `VerifyEmailTokenAsync` (test project `BLL.Tests` already scaffolded)
- [ ] **Wire bulk-import to email queue**: refactor `BulkRegisterFromExcelAsync` to enqueue emails instead of inline send (currently pending — kept old behavior for safety)
- [ ] **Resend verification email** admin action (currently no recovery if SMTP fails)
- [ ] **Persist email queue** to DB (out-of-process queue survives restarts)

---

## Future Phases

### Phase A: Observability
- Structured logging across services
- Email send success/failure metrics
- Audit log dashboard for admins

### Phase B: Auth Hardening
- Rate limiting on login attempts
- 2FA via TOTP (optional per user)
- Session management UI (list/revoke active sessions)

### Phase C: Bulk Import Hardening
- Move `BulkRegisterFromExcelAsync` to use the email queue (Phase 1 leftover)
- Dry-run preview before committing import
- Resend button per pending user
- Per-row error display

### Phase D: Email Templates
- Move inline HTML to `.cshtml` Razor templates (easier to edit)
- Localization (Vietnamese + English)
- Logo + branding header
- Per-template preview in admin UI

---

## Decision Log

### 2026-06-25 — Email failure semantics

**Decision:** Email send failures during background processing do NOT roll back the user creation.

**Why:** The throttle-queue requirement means `RegisterAsync` never observes SMTP errors directly. Auto-rollback on email failure would make SMTP uptime a hard dependency of admin user creation, which contradicts the queue pattern. Failed emails are logged; admin can delete and recreate if SMTP was misconfigured.

### 2026-06-25 — `MustChangePassword` as new boolean (vs. reusing `EmailVerified`)

**Decision:** Add new `MustChangePassword` boolean.

**Why:** Email verification and password change are semantically distinct. A future "password rotation" feature would also need this flag. Coupling them via `!EmailVerified` would force the two states to track each other indefinitely.

### 2026-06-25 — Force-change via middleware (vs. page-level filter)

**Decision:** Global middleware with allowlist.

**Why:** Page-level filter duplicates logic in 30+ places and is error-prone (new pages would forget to add it). Middleware centralizes the rule.

### 2026-06-25 — `IEmailService` Scoped → Singleton

**Decision:** Change `IEmailService` registration lifetime.

**Why:** `EmailQueueHostedService` is a singleton (HostedService pattern) and cannot consume scoped services. `SmtpEmailService` has no per-request state, so singleton is safe.