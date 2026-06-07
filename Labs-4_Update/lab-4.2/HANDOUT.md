# Lab 4.2 — OWASP Top 10 Detection (Hướng dẫn học viên)

## Bạn sẽ làm gì

File `VulnerableCode.cs` (.NET) chứa **11 lỗ hổng** thuộc 7 nhóm OWASP Top 10. Bạn sẽ:
1. Tự viết 1 prompt file security audit theo OWASP.
2. Gõ `/review-security` cho Copilot quét file → đếm bắt được mấy / 11.
3. Fix từng lỗ hổng + viết test chứng minh.
4. Quét lại bản đã fix → mục tiêu 0 lỗi Critical/High.

> Lưu ý: `/review-security` chạy được vì là **prompt file** bạn tự tạo ở Bước 3 (prompt file tự thành slash command, nhận `#file:`). Đây KHÔNG phải lệnh built-in của Copilot.

## 11 lỗ hổng trong `VulnerableCode.cs`

| # | OWASP | Loại | Nằm ở |
|---|---|---|---|
| 1 | A07 | Hardcoded secret | `const string SecretKey` |
| 2 | A03 | SQL Injection | `GetUser` — `FromSqlRaw($"...{id}...")` |
| 3 | A01 | Missing `[Authorize]` | class controller |
| 4 | A01 | IDOR (không check ownership) | `GetUser` |
| 5 | A03 | Stored XSS | `PostComment` — lưu HTML thô |
| 6 | A03 | XSS render | `GetComments` — `Content(..., "text/html")` |
| 7 | A02 | Weak crypto MD5 | `HashPassword` |
| 8 | A02 | RNG đoán được | `GenerateResetToken` — `new Random()` |
| 9 | A08 | Insecure deserialization | `Import` — `BinaryFormatter` |
| 10 | A10 | SSRF | `Proxy` — `HttpClient` tới URL từ user |
| 11 | A09 | Log dữ liệu nhạy cảm | `Login` — log password |

## Bước 0 — Mở lab & build

Mở PowerShell:

```powershell
cd C:\Documents\Training\GithubCopilot\Slides\Assets\labs\lab-4.2
code .
```

Trong VS Code mở Terminal (`` Ctrl+` ``) và build:

```powershell
dotnet restore
dotnet build
```

> Nếu restore gặp lỗi vì NuGet feed nội bộ công ty không truy cập được, repo này đã có `nuget.config` local để dùng public feed `https://api.nuget.org/v3/index.json` mà không cần xóa nguồn của công ty.
>
> Không xóa hay sửa global/company NuGet source; chỉ dùng cấu hình local này cho lab.

Kết quả mong đợi: **Build succeeded**. Có thể có warning `NU1902` (package HtmlSanitizer có CVE — chấp nhận trong lab) và `SYSLIB0011` (đã `#pragma disable`). Đây KHÔNG phải lỗi.

## Bước 1 — Đọc và tự đếm bằng mắt

Mở `VulnerableCode.cs`, đọc một lượt, ghi lại số lỗ hổng bạn tự thấy:

```
Tự thấy bằng mắt: ____ lỗ hổng (thường 4-5)
```

## Bước 2 — Tạo prompt file security audit

Trong Explorer (`Ctrl+Shift+E`), tạo file: chuột phải vào thư mục lab → **New File...** → gõ nguyên đường dẫn:

`.github/prompts/review-security.prompt.md`

Dán nội dung sau rồi **Lưu** (`Ctrl+S`):

```markdown
---
mode: ask
description: Security audit OWASP Top 10 cho code .NET
---

Bạn là application security engineer. Audit file ${file} theo OWASP Top 10 (2021):

## A01 — Broken Access Control
- Missing [Authorize], IDOR, Mass assignment

## A02 — Cryptographic Failures
- MD5/SHA1/DES (weak), ECB mode, fixed IV, plaintext password, RNG đoán được (System.Random)

## A03 — Injection
- SQL: FromSqlRaw/ExecuteSqlRaw không parameterized
- XSS: stored / reflected / render HTML thô

## A07 — Identification & Auth Failures
- Hardcoded secret, weak password policy, missing rate limit

## A08 — Software & Data Integrity Failures
- Insecure deserialization (BinaryFormatter, ...)

## A09 — Security Logging Failures
- Log password/secret/PII

## A10 — SSRF
- HttpClient/request tới URL lấy từ user input

## Output (mỗi finding)
- Severity (Critical/High/Medium/Low)
- File:line
- Description
- Concrete fix (code snippet)
- OWASP reference link
```

## Bước 3 — Chạy audit

1. Mở **Copilot Chat** (`Ctrl+Alt+I`).
2. Gõ `/` → chọn **`/review-security`** trong danh sách.
3. Đính kèm file cần quét, gõ nguyên dòng:

```text
/review-security #file:VulnerableCode.cs
```

4. Nhấn Enter, chờ Copilot liệt kê finding theo severity.

**Ghi lại:**

```
Copilot bắt được: ____ / 11 lỗ hổng
```

Đối chiếu với bảng 11 lỗ hổng ở trên. Lỗ hổng hay bị miss nhất: **log password (#11)** và Copilot hay gộp **2 lỗi XSS (#5 stored + #6 render)** thành một — nhắc rằng đó là 2 finding riêng.

## Bước 4 — Fix + viết test

Với mỗi finding, sửa code (gợi ý fix; đáp án đầy đủ ở `VulnerableCode.fixed.cs`):

```csharp
// SQL Injection -> parameterized / LINQ
var user = await _db.Users.FindAsync(id);

// IDOR + Missing [Authorize]
[Authorize]
public IActionResult GetUser(int id)
{
    var currentUserId = User.GetUserId();
    if (id != currentUserId && !User.IsInRole("Admin")) return Forbid();
    // ...
}

// Stored/render XSS -> sanitize (HtmlSanitizer) hoặc store text + encode khi render
// Hardcoded secret -> User Secrets / Azure Key Vault (IConfiguration)
// Weak crypto -> BCrypt/Argon2 cho password; RandomNumberGenerator cho token
// Deserialization -> bỏ BinaryFormatter, dùng System.Text.Json
// SSRF -> allowlist host, chặn private IP
// Logging -> KHÔNG log password/PII
```

Viết ít nhất 3 regression test chứng minh fix, ví dụ:
- `GetUser` trả 403 khi `id` là của người khác
- `HashPassword` sinh hash khác nhau cho cùng input (BCrypt có salt)
- SSRF: request tới private IP bị chặn

## Bước 5 — Quét lại bản đã fix

```text
/review-security #file:VulnerableCode.fixed.cs
```

Mục tiêu: **0 finding Critical/High** (Medium/Low 0-1, ví dụ note BCrypt workFactor).

## Coi như xong khi

- [ ] Đã tạo `.github/prompts/review-security.prompt.md` và chạy `/review-security`
- [ ] Lập bảng before/after: số finding trước fix vs sau fix
- [ ] Đã fix các lỗ hổng + có test chứng minh
- [ ] Quét lại: 0 Critical/High

> Lab 4.3 sẽ nâng cấp `review-security.prompt.md` này thành skill dùng chung (`.github/skills/review-security/SKILL.md`) áp cho mọi PR đụng auth/SQL/HTML.
