# Project Changelog

All notable changes to this project will be documented here.

Format: [Semantic Versioning](https://semver.org/) + dated entries.

---

## [Unreleased]

### Added

#### Admin User Verification & Force Change Password (2026-06-25)

Wired up the existing-but-unused email and verification token infrastructure to implement a secure admin-driven user onboarding flow.

- **Auto-generated temporary passwords**: `BLL/Services/Auth/PasswordGenerator.cs` generates cryptographically secure 12-character alphanumeric passwords via `RandomNumberGenerator.GetString`.
- **Welcome + verification email**: `AuthService.RegisterAsync` now enqueues a welcome email containing the temp password and a 15-minute verification link.
- **Throttled email queue**: New `BLL/Services/Email/EmailQueue` (Channel-based) + `EmailQueueHostedService` (BackgroundService) throttle sends at 200ms intervals with 3-retry exponential backoff (1s/2s/4s) to respect Gmail rate limits.
- **Force change password**: New `MustChangePassword` boolean on `User` entity + `PasswordChangedAt` timestamp. Enforced by a global `ForceChangePasswordMiddleware` (with allowlist for `/Auth/ChangePassword`, `/Auth/Logout`, `/Auth/Login`, `/Auth/VerifyEmail`, static files).
- **`/Auth/ChangePassword` page**: New Razor page where users must set a new password before using any feature. Role-based redirect after success (Admin → `/Admin/Users`, others → `/Documents/All`).
- **Cookie hardening**: `OnValidatePrincipal` now rejects cookies if `!EmailVerified`, bouncing users to `/Auth/Login` with a meaningful error message.
- **SMTP startup check**: Application warns at startup if SMTP is not configured (helps catch prod misconfiguration).
- **New config sections**: `App:BaseUrl` (for absolute verification URLs) + `EmailQueue:ThrottleDelayMs`/`MaxRetries`.
- **Migration**: `DAL/Migrations/20260624231828_AddMustChangePasswordToUser.cs` adds `must_change_password` and `password_changed_at` columns.
- **Schema sync**: `database/Database.sql` + `Database/Database.sql` updated to include the new columns. Migration script `migrate_must_change_password.sql` provided for existing databases.
- **Test project**: `BLL.Tests` xUnit project scaffolded for future test coverage.

### Changed

- `AuthService.RegisterAsync` signature changed — `password` parameter removed; system generates it.
- `AuthService.VerifyEmailTokenAsync` now sets `EmailVerified=true` in addition to `IsActive=true`.
- Admin "Create User" modal in `GUI/Pages/Admin/Users.cshtml` no longer accepts a password input.
- `IEmailService` DI lifetime changed from Scoped → Singleton (so it can be consumed by the singleton `EmailQueueHostedService`).
- `OnValidatePrincipal` cookie validation tightened to also reject `!EmailVerified`.

### Security

- New admin-created users cannot log in until they verify their email (cookie rejected).
- Verified users cannot use any feature until they change their temporary password (middleware redirect).
- Google OAuth users (`PasswordHash == "EXTERNAL_OAUTH_GOOGLE"`) cannot change password — explicit guard throws `InvalidOperationException`.
- Verification tokens remain short-lived (15 minutes) via existing `ITimeLimitedDataProtector`.

### Known Limitations

- Email queue is in-process — restart during a large bulk import may drop queued emails.
- No "resend verification email" UI — recovery is admin-deletes-and-recreates.
- `BulkRegisterFromExcelAsync` does not yet generate emails via the queue (existing inline send path retained for backward compatibility).

---

## Earlier versions

See git history for pre-Unreleased changes.