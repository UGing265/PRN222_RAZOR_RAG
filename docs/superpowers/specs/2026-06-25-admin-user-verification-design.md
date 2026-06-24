# Admin User Verification & Force Change Password — Design Spec

**Date:** 2026-06-25
**Branch:** `feat/send-stmp-email-to-user`
**Status:** Approved

---

## 1. Problem

Currently, when an admin creates a user account on `/admin/users`:

1. Admin must type a password manually.
2. User is created with `IsActive = true` immediately.
3. No email is sent (despite `IEmailService` and the email verification token plumbing already existing).
4. The user has no way to know their account was created or what their password is.
5. Admins have no enforcement mechanism forcing a user to set their own password on first login.

The user-facing requirement: **Admin provides only Full Name + Email + Role. System generates a temporary password, emails it along with a verification link. User clicks link → account is verified → user logs in with temporary password → user must change password before using any other feature.**

---

## 2. Goals

- Admins no longer type passwords; the system generates a cryptographically secure 12-character password.
- Every admin-created account (manual or Excel bulk import) is sent a welcome + verification email.
- Users must verify their email before they can log in.
- Users must change their temporary password before they can access any feature.
- Existing infrastructure (`IEmailService`, `GenerateEmailVerificationToken`, `/Auth/VerifyEmail`) is wired up and reused, not duplicated.
- Gmail SMTP rate limits are respected during bulk imports.

## 3. Non-Goals

- Public self-registration. Users are still created by admin only.
- A new `EmailVerificationToken` database table. We continue to use ASP.NET Core `TimeLimitedDataProtector` (stateless, 15-minute TTL).
- Resend verification email. Admin can re-create the user if needed.
- Login rate-limiting.
- Changing the Excel import template.
- Notification toasts for users after verification.
- Google OAuth user password flows (Google users have `PasswordHash = "EXTERNAL_OAUTH_GOOGLE"` and are out of scope).

---

## 4. Architecture Overview

```
┌────────────────────────────────────────────────────────────────────┐
│                          ADMIN BROWSER                             │
│                                                                    │
│   /Admin/Users  →  Create User (fullName, email, roleId)            │
└────────────────────────────┬───────────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│                       GUI/Pages/Admin/Users.cshtml.cs               │
│   OnPostCreateUserAsync                                            │
│     → IAuthService.RegisterAsync(fullName, email, roleId)          │
└────────────────────────────┬───────────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│                    BLL/Services/Auth/AuthService.cs                 │
│                                                                    │
│   1. PasswordGenerator.Generate(12)  →  "aB3kQ9pLm2xY"              │
│   2. HashPassword("aB3kQ9pLm2xY")     →  "salt.hash"               │
│   3. INSERT users (IsActive=false,                                   │
│                    EmailVerified=false,                             │
│                    MustChangePassword=true,                         │
│                    PasswordHash="salt.hash")                        │
│   4. IEmailQueue.Enqueue(new EmailJob {                            │
│        To = email,                                                 │
│        Subject = "...",                                            │
│        Body = "...",                                               │
│        VerificationUrl = "/Auth/VerifyEmail?token=..."              │
│      })                                                            │
│   5. Audit log "USER_CREATED"                                      │
└────────────────────────────┬───────────────────────────────────────┘
                             │
              ┌──────────────┴──────────────┐
              ▼                             ▼
┌──────────────────────────┐   ┌───────────────────────────────────┐
│   PostgreSQL users       │   │   BLL/Services/Email/EmailQueue    │
│   (committed)            │   │   _channel.Writer.TryWrite(...)   │
└──────────────────────────┘   └──────────────┬────────────────────┘
                                              │
                                              ▼
┌────────────────────────────────────────────────────────────────────┐
│   BLL/Services/Email/EmailQueueHostedService : BackgroundService   │
│                                                                    │
│   await foreach (var job in _channel.Reader.ReadAllAsync(...))     │
│     await _emailService.SendEmailAsync(...)                        │
│     await Task.Delay(200ms)        ← throttle for Gmail limits     │
└────────────────────────────┬───────────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────┐   ┌───────────────────────────────────┐
│   SmtpEmailService        │   │   User mailbox                    │
│   smtp.gmail.com:587     │──▶│   Welcome + verification URL      │
└──────────────────────────┘   └──────────────┬────────────────────┘
                                              │
                                              ▼
┌────────────────────────────────────────────────────────────────────┐
│                       USER BROWSER                                  │
│                                                                    │
│   Click /Auth/VerifyEmail?token=...                                │
└────────────────────────────┬───────────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│                  GUI/Pages/Auth/VerifyEmail.cshtml.cs                │
│   OnGetAsync(token)                                                │
│     → IAuthService.VerifyEmailTokenAsync(token)                    │
│       • set EmailVerified=true                                      │
│       • set IsActive=true                                           │
│     → Redirect /Auth/Login (TempData success)                      │
└────────────────────────────┬───────────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│                       User logs in                                  │
│                                                                    │
│   /Auth/Login  →  email + temp password                            │
│     → ValidateCredentialsAsync                                     │
│     → SignInAsync (cookie 7 days)                                   │
└────────────────────────────┬───────────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│   GUI/Middleware/ForceChangePasswordMiddleware (NEW)                │
│                                                                    │
│   if (User.Identity.IsAuthenticated                                │
│       && MustChangePassword                                         │
│       && !request.Path in AllowList)                               │
│       → Redirect /Auth/ChangePassword                              │
└────────────────────────────┬───────────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────────┐
│                  GUI/Pages/Auth/ChangePassword.cshtml.cs             │
│   OnPostAsync(currentPassword, newPassword, confirmPassword)        │
│     → IAuthService.ChangePasswordAsync(...)                        │
│       • verify currentPassword                                     │
│       • newPassword != currentPassword                             │
│       • newPassword == confirmPassword                             │
│       • hash newPassword                                            │
│       • set MustChangePassword=false                                │
│       • set PasswordChangedAt=Now                                  │
│       • audit log                                                   │
│       • SignalR admin group "ReceiveAuditLogCreated"               │
│     → Redirect /Documents/All (or /Admin/Users if role=Admin)      │
└────────────────────────────────────────────────────────────────────┘
```

---

## 5. Components

### 5.1 Schema changes

**`DAL/Entities/User.cs`** — add:

```csharp
public bool MustChangePassword { get; set; }
public DateTime? PasswordChangedAt { get; set; }
```

**EF migration** — `AddMustChangePasswordToUser`:
- `must_change_password BOOLEAN NOT NULL DEFAULT FALSE`
- `password_changed_at TIMESTAMP NULL`

### 5.2 New & modified BLL files

| File | Change |
|---|---|
| `BLL/Services/Auth/PasswordGenerator.cs` | NEW. `string Generate(int length)`. 12 chars from `[a-zA-Z0-9]` using `RandomNumberGenerator.GetString`. |
| `BLL/Interfaces/Auth/IEmailQueue.cs` | NEW. `void Enqueue(EmailJob job); int PendingCount { get; }` |
| `BLL/Services/Email/EmailJob.cs` | NEW. POCO with `To`, `Subject`, `Body`, `RetryCount`. |
| `BLL/Services/Email/EmailQueue.cs` | NEW. Wraps `Channel<EmailJob>` (unbounded, single-reader). |
| `BLL/Services/Email/EmailQueueHostedService.cs` | NEW. `BackgroundService` that reads from the channel, calls `IEmailService.SendEmailAsync`, retries up to 3 times on exception (exponential backoff 1s, 2s, 4s), `Task.Delay(200ms)` between sends. |
| `BLL/Interfaces/Auth/IAuthService.cs` | MODIFY. Change `RegisterAsync` signature to remove `password` parameter. Add `ChangePasswordAsync`. |
| `BLL/Services/Auth/AuthService.cs` | MODIFY. `RegisterAsync` generates password, sets flags. `BulkRegisterFromExcelAsync` enqueues instead of inline send. New `ChangePasswordAsync`. `VerifyEmailTokenAsync` also sets `EmailVerified=true`. |
| `BLL/Extensions/ServiceCollectionExtensions.cs` | MODIFY. Register `IEmailQueue` (singleton), `EmailQueueHostedService` (hosted). |

### 5.3 New & modified GUI files

| File | Change |
|---|---|
| `GUI/Program.cs` | MODIFY. `app.UseMiddleware<ForceChangePasswordMiddleware>()` AFTER `UseAuthentication`/`UseAuthorization`. Update `OnValidatePrincipal` to also reject if `!EmailVerified` or `MustChangePassword`. |
| `GUI/Middleware/ForceChangePasswordMiddleware.cs` | NEW. See §6.3. |
| `GUI/Pages/Admin/Users.cshtml` | MODIFY. Remove the `password` input from the Create User modal. Show success message: "Đã tạo user, email đang được gửi". |
| `GUI/Pages/Admin/Users.cshtml.cs` | MODIFY. `OnPostCreateUserAsync` no longer accepts `password`. Calls `_authService.RegisterAsync(fullName, email, roleId)`. |
| `GUI/Pages/Auth/ChangePassword.cshtml` | NEW. Form with current password, new password, confirm new password. |
| `GUI/Pages/Auth/ChangePassword.cshtml.cs` | NEW. `[Authorize]`. `OnPostAsync` calls `ChangePasswordAsync`. |
| `GUI/Pages/Auth/VerifyEmail.cshtml` / `.cshtml.cs` | MODIFY. No structural change, just confirm the message copy. |
| `GUI/appsettings.json` | MODIFY (only if needed). Add `App:BaseUrl` for absolute verification URLs (`https://localhost:7065`). |

### 5.4 Email template

Inline HTML string built in `AuthService.BuildWelcomeEmailBody(...)`. Subject:

> `[FPT RAG] Bạn đã được cấp quyền truy cập hệ thống`

Body:

```html
<p>Xin chào <strong>{FullName}</strong>,</p>
<p>Bạn vừa được Admin cấp tài khoản truy cập <strong>FPT RAG System</strong> với vai trò <strong>{RoleName}</strong>.</p>
<p><strong>Thông tin đăng nhập tạm:</strong><br>
  Email: <code>{Email}</code><br>
  Mật khẩu tạm: <code>{TempPassword}</code>
</p>
<p><strong>Bước 1:</strong> Click link xác nhận trong vòng 15 phút:<br>
  <a href="{VerificationUrl}">{VerificationUrl}</a>
</p>
<p><strong>Bước 2:</strong> Đăng nhập bằng mật khẩu tạm ở trên.</p>
<p><strong>Bước 3:</strong> Hệ thống sẽ yêu cầu bạn đổi mật khẩu trước khi sử dụng.</p>
```

---

## 6. Detailed behavior

### 6.1 Manual admin user creation

| Step | Behavior |
|---|---|
| 1 | Admin fills form: Full Name, Email, Role (no password field). |
| 2 | Server validates email is unique. |
| 3 | `RegisterAsync` generates 12-char password, hashes with PBKDF2 (existing code). |
| 4 | INSERT user with `IsActive=false, EmailVerified=false, MustChangePassword=true`. |
| 5 | `IEmailQueue.Enqueue(EmailJob)`. Returns immediately. |
| 6 | `HostedService` picks up job, sends email with 200ms delay after. |
| 7 | If SMTP throws, retry 3x (1s/2s/4s backoff). On final failure, log error, drop job. |
| 8 | Admin sees success message: "Đã tạo user {email}, email xác nhận đang được gửi". |
| 9 | Audit log: "USER_CREATED" with admin id and target user id. |
| 10 | SignalR admin group receives `ReceiveAuditLogCreated`. |

**Note:** Per the user's choice, if the **transaction** itself fails (e.g., duplicate email), the user creation rolls back. The email is enqueued **after** commit, so a DB failure never produces a phantom email. The retry path is purely for SMTP transport failures.

### 6.2 Excel bulk import

`BulkRegisterFromExcelAsync` changes:
- Continue to insert users with the new flags (`IsActive=false`, `MustChangePassword=true`).
- Generate temp passwords (existing Vietnamese-name-based logic is replaced by `PasswordGenerator.Generate` for consistency).
- For each row, enqueue an `EmailJob`. The hosted service processes them sequentially with 200ms between sends.
- Return summary: `SuccessCount`, `Errors[]`. Add `PendingEmailCount` field to the response DTO so admin knows how many are still queued.

### 6.3 Force-change-password middleware

```csharp
public class ForceChangePasswordMiddleware
{
    private static readonly HashSet<string> AllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "/Auth/ChangePassword",
        "/Auth/Logout",
        "/Auth/Login",
        "/Auth/VerifyEmail"
    };

    private readonly RequestDelegate _next;
    public ForceChangePasswordMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, IUserRepository repo)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            await _next(ctx);
            return;
        }

        var path = ctx.Request.Path.Value ?? "";
        if (AllowList.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            || path.StartsWith("/css") || path.StartsWith("/js")
            || path.StartsWith("/lib") || path.StartsWith("/images")
            || path.StartsWith("/_framework") || path.StartsWith("/favicon"))
        {
            await _next(ctx);
            return;
        }

        var userIdStr = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            await _next(ctx);
            return;
        }

        // Cache user lookup on the same request
        if (!ctx.Items.TryGetValue("CurrentUser", out var userObj) || userObj is not User user)
        {
            user = await repo.GetByIdAsync(userId);
            ctx.Items["CurrentUser"] = user;
        }

        if (user is not null && user.MustChangePassword)
        {
            ctx.Response.Redirect("/Auth/ChangePassword");
            return;
        }

        await _next(ctx);
    }
}
```

The middleware is registered in `Program.cs` immediately after `app.UseAuthorization()`.

`Program.cs` `OnValidatePrincipal` is also updated to:
- Reject the cookie if `!EmailVerified` → bounce to `/Auth/Login` with "Email chưa xác nhận".
- Reject the cookie if `MustChangePassword` → bounce to `/Auth/Login` with "Cần đổi mật khẩu" (the middleware handles the redirect on next request).

### 6.4 Change password

`ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string confirmPassword)`:

| Check | Behavior on failure |
|---|---|
| `newPassword == confirmPassword` | Return false, "Mật khẩu xác nhận không khớp" |
| `ValidatePassword(userId, currentPassword)` is true | Return false, "Mật khẩu hiện tại không đúng" |
| `newPassword == currentPassword` | Return false, "Mật khẩu mới phải khác mật khẩu cũ" |
| `newPassword.Length >= 6` | Return false, "Mật khẩu phải có ít nhất 6 ký tự" |
| User `PasswordHash == "EXTERNAL_OAUTH_GOOGLE"` | Throw `InvalidOperationException` "Tài khoản Google không thể đổi mật khẩu" |

On success:
1. Hash new password with `Rfc2898DeriveBytes` (existing helper).
2. UPDATE `users SET password_hash=..., must_change_password=false, password_changed_at=now() WHERE id=...`.
3. Audit log `PASSWORD_CHANGED` with `TargetUserId=userId, PerformedBy=userId`.
4. SignalR `ReceiveAuditLogCreated` to admin group.

### 6.5 Verification flow

`VerifyEmailTokenAsync` modification:
- Unprotect token → get email.
- Look up user by email.
- If `IsBlocked` → throw (existing behavior).
- Set `EmailVerified=true`, `IsActive=true` (preserved behavior, was already setting `IsActive=true`).
- Save and return.

`/Auth/VerifyEmail?token=...`:
- `OnGetAsync(token)` calls `VerifyEmailTokenAsync`.
- Success → redirect `/Auth/Login` with TempData `"Email đã được xác nhận, vui lòng đăng nhập"`.
- Failure (expired / invalid) → redirect `/Auth/Login` with TempData `"Link xác nhận không hợp lệ hoặc đã hết hạn"`.

### 6.6 Login flow (no change to validation, only to flags)

`ValidateCredentialsAsync` checks:
- User exists.
- `!IsBlocked` (otherwise "Tài khoản đã bị khóa").
- `EmailVerified` (otherwise "Email chưa được xác nhận, kiểm tra hộp thư").
- `IsActive` (otherwise "Tài khoản chưa được kích hoạt").
- Password matches PBKDF2 hash.

After successful sign-in, the cookie auth + middleware take over and redirect to `/Auth/ChangePassword` if `MustChangePassword=true`.

---

## 7. Configuration

`appsettings.json`:

```json
{
  "SmtpSettings": {
    "Server": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "FPT RAG System",
    "SenderEmail": "baontgse183271@fpt.edu.vn",
    "Password": "<app-password>"
  },
  "App": {
    "BaseUrl": "https://localhost:7065"
  },
  "EmailQueue": {
    "ThrottleDelayMs": 200,
    "MaxRetries": 3
  }
}
```

The `App:BaseUrl` is used to construct the absolute verification URL embedded in the email.

---

## 8. Error handling summary

| Scenario | Behavior |
|---|---|
| Email invalid format on create | Razor validation, "Email không hợp lệ" |
| Duplicate email on create | 400 "Email đã tồn tại" |
| DB insert fails | Rollback, 500 "Tạo user thất bại" (no email sent) |
| SMTP fails (manual) | Email stays in queue, retries; admin still sees success. User won't verify until email arrives. |
| SMTP fails (bulk) | Retry per-job up to 3 times; after that, log warning and drop. Admin sees `PendingEmailCount` shrinking in real-time via SignalR or refresh. |
| Token expired (>15 min) | VerifyEmail page → "Link đã hết hạn, liên hệ admin" |
| Token malformed / wrong email | Same as above |
| User changes password same as old | "Mật khẩu mới phải khác mật khẩu cũ" |
| Google user tries to change password | "Tài khoản Google không thể đổi mật khẩu" |
| User accesses forbidden page before change | Middleware redirects to /Auth/ChangePassword |
| Admin tries to delete a pending user | Existing `IsActive`/`IsBlocked` logic still applies; pending users are `IsActive=false` so they appear in the "Pending" tab |

---

## 9. Testing strategy

### 9.1 Unit tests (BLL)

| Test | File |
|---|---|
| `PasswordGenerator.Generate(12)` returns 12 chars, mixed case + digits | `PasswordGeneratorTests` |
| `RegisterAsync` generates password, sets flags, enqueues email | `AuthServiceRegisterTests` |
| `BulkRegisterFromExcelAsync` enqueues all emails | `AuthServiceBulkTests` |
| `ChangePasswordAsync` rejects same password | `AuthServiceChangePasswordTests` |
| `ChangePasswordAsync` rejects Google user | `AuthServiceChangePasswordTests` |
| `VerifyEmailTokenAsync` sets `EmailVerified=true` and `IsActive=true` | `AuthServiceVerifyTests` |

### 9.2 Integration tests (manual via curl)

| Flow | Steps |
|---|---|
| Happy path | Create user → check email → click link → login → change password → use system |
| Expired token | Create user → wait 16 min → click link → expect error |
| Wrong password on change | Login with temp → change page → submit wrong current → expect error |
| Google user change | Login via Google → visit /Auth/ChangePassword → submit → expect "Tài khoản Google" error |
| Bulk import 50 users | Import Excel → check 50 emails queue → wait → all 50 received within ~10s |

### 9.3 UI smoke tests

- Modal "Create User" no longer shows password input.
- Toast on success.
- VerifyEmail page works with both valid and invalid tokens.
- ChangePassword page renders, validation messages show, redirect on success.
- Force-change middleware blocks `/Documents/All` but allows `/Auth/Logout`.

---

## 10. Risks & mitigations

| Risk | Mitigation |
|---|---|
| Gmail rate limit (500/day) on bulk import | Throttle 200ms; document manual rate-limit warning in admin UI; if needed, allow swapping SMTP provider |
| Email link stolen in transit | HTTPS, short TTL (15 min), token is bound to email (changing email invalidates token) |
| User shares temp password with attacker | `MustChangePassword` is enforced before any feature access; user must know email account |
| Token leaks via referrer | Verification URL has no other sensitive params; document this in the URL contract |
| Middleware DB hit per request | Cache in `HttpContext.Items` per request; PK lookup is O(1) |
| Cookie middleware rejects user mid-session after `MustChangePassword=true` is set elsewhere | Acceptable: user logs back in, is redirected to change page. Edge case unlikely since only admin can flip this flag. |

---

## 11. Files affected (final list)

### New
- `BLL/Services/Auth/PasswordGenerator.cs`
- `BLL/Interfaces/Auth/IEmailQueue.cs`
- `BLL/Services/Email/EmailJob.cs`
- `BLL/Services/Email/EmailQueue.cs`
- `BLL/Services/Email/EmailQueueHostedService.cs`
- `GUI/Middleware/ForceChangePasswordMiddleware.cs`
- `GUI/Pages/Auth/ChangePassword.cshtml`
- `GUI/Pages/Auth/ChangePassword.cshtml.cs`
- `DAL/Migrations/2026XXXXXX_AddMustChangePasswordToUser.cs` (auto-generated)

### Modified
- `DAL/Entities/User.cs`
- `BLL/Interfaces/Auth/IAuthService.cs`
- `BLL/Services/Auth/AuthService.cs`
- `BLL/Extensions/ServiceCollectionExtensions.cs`
- `GUI/Program.cs`
- `GUI/Pages/Admin/Users.cshtml`
- `GUI/Pages/Admin/Users.cshtml.cs`
- `GUI/Pages/Auth/VerifyEmail.cshtml`
- `GUI/Pages/Auth/VerifyEmail.cshtml.cs`
- `GUI/appsettings.json`
- `GUI/appsettings.Development.json`

### Unchanged but referenced
- `BLL/Services/Auth/SmtpEmailService.cs` (already complete)
- `BLL/Interfaces/Auth/IEmailService.cs` (already complete)
- `GUI/Components/Layout/NavMenu` (no change)

---

## 12. Rollout

1. Create branch `feat/admin-user-verification-and-force-change-password` from `feat/send-stmp-email-to-user`.
2. Apply DB migration.
3. Implement BLL changes (PasswordGenerator, EmailQueue, AuthService).
4. Implement GUI changes (middleware, page, modal).
5. Run unit tests.
6. Manual smoke test of all 5 error scenarios + happy path.
7. Update `docs/project-changelog.md` and `docs/development-roadmap.md`.
8. PR review, merge into `feat/send-stmp-email-to-user`.
