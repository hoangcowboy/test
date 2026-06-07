# Lab 4.3 — Build Skill Pack & Orchestrator của team (Hướng dẫn học viên)

## Bạn sẽ làm gì

Lắp ráp 4 thành phần cấu hình Copilot dùng chung cho cả team, đặt trong `.github/`:

1. `copilot-instructions.md` — luật chung, Copilot tự load.
2. `instructions/*.instructions.md` — luật theo path (`applyTo`).
3. `agents/*.agent.md` — persona (vd senior-reviewer).
4. `skills/<name>/SKILL.md` — skill folder, trong đó có 1 ORCHESTRATOR `/review-and-fix` bạn tự thiết kế.

Trong handout này mọi file đã có sẵn nội dung **để copy-paste**. Bài tự thiết kế sẽ làm ở phần bài tập cuối khóa.

## Phân biệt 3 thứ hay nhầm (đọc kỹ trước khi làm)

| Loại | File | Cách dùng |
|---|---|---|
| Prompt file | `.github/prompts/<n>.prompt.md` | Gõ `/<n>` trong chat (slash command), nhận `#file:` |
| Skill | `.github/skills/<n>/SKILL.md` | Copilot tự load khi câu hỏi khớp `description`; HOẶC gõ `/<n>` kèm tham số (vd `/review-and-fix branch=feat-x`). Quản lý bằng `/skills` |
| Agent (persona) | `.github/agents/<n>.agent.md` | Chọn từ **dropdown agent** trong khung Chat (hoặc `/agents`). KHÔNG gõ `@<n>` — `@` chỉ cho agent built-in `@workspace`/`@terminal`/`@vscode` |

## Bước 0 — Mở lab

```powershell
cd C:\Documents\Training\GithubCopilot\Slides\Assets\labs\lab-4.3
code .
```

## Bước 1 — Tạo cấu trúc `.github/`

Mở Terminal (`` Ctrl+` ``):

```powershell
New-Item -ItemType Directory -Force .github\instructions, .github\agents, .github\skills\generate-tests, .github\skills\review-pr, .github\skills\review-security, .github\skills\review-and-fix | Out-Null
```

Các bước dưới: tạo từng file bằng **chuột phải thư mục tương ứng → New File...**, dán nội dung, **Lưu** (`Ctrl+S`).

## Bước 2 — `.github/copilot-instructions.md` (copy-paste)

```markdown
# Copilot Instructions — ShopApi Team

## Stack
- .NET 8, C# 12
- ASP.NET Core minimal API
- EF Core 8 with SQL Server / SQLite
- xUnit + Moq + FluentAssertions

## Code Style
- File-scoped namespaces
- `var` only when the right-hand side makes the type obvious
- Async all the way — public async methods end in `Async`
- `CancellationToken` on every public async method
- Records for immutable DTOs
- Class with primary constructor (C# 12) when there is a dependency

## Naming
- Service: `I{Name}Service` and `{Name}Service`
- Folder: `PascalCase`, plural for `Models/`, `Services/`
- Private field: `_camelCase`
- Constant: `PascalCase`

## Layering
- Controllers stay thin — they orchestrate
- Services hold business logic
- Introduce a Repository only when tests need to mock the data layer
- DI lifecycle: `Scoped` for services/repositories, `Singleton` for options

## Error Handling
- Business errors flow as a Result pattern or a custom exception
- Validation errors return ProblemDetails (RFC 7807)
- Log exceptions through `ILogger` with structured logging

## Forbidden
- `BinaryFormatter`
- `JsonConvert.SerializeObject` (use `System.Text.Json`)
- `.Result` or `.Wait()` in async code
- `DateTime.Now` (always `UtcNow`)
```

## Bước 3 — `.github/instructions/controllers.instructions.md` (copy-paste)

```markdown
---
applyTo: "**/Controllers/**/*.cs"
description: Conventions cho ASP.NET Core controllers
---

# Controller conventions

- Controller mỏng: chỉ orchestrate, KHÔNG chứa business logic.
- KHÔNG gọi thẳng DbContext trong controller — đi qua service.
- Action async phải nhận và truyền `CancellationToken`.
- Return type: `IActionResult` hoặc `Results<...>` (typed results), không trả entity thô.
- HTTP semantics đúng: 200 OK, 201 Created (kèm Location), 204 NoContent, 400/404/409 hợp lý.
- Validate input ở edge (model binding + ProblemDetails cho lỗi).
- KHÔNG bắt `Exception` chung rồi nuốt — để middleware xử lý.
```

## Bước 4 — `.github/agents/senior-reviewer.agent.md` (copy-paste)

````markdown
---
name: senior-reviewer
description: Senior .NET reviewer — chọn agent này khi muốn review chặn (blocking) một diff/file
---

# Senior .NET Reviewer

You are a senior backend engineer with ten years of .NET experience,
specialising in high-performance APIs, distributed systems and security hardening.

## Review style
- Direct, no padding
- Concrete suggestions with code snippets
- Cite references (docs.microsoft.com, OWASP, RFC)
- Group feedback by severity: Block / Suggest / Nit
- End every review with a top-three priorities list

## Bias
- Prefer simple over clever
- Prefer explicit over implicit
- Prefer composition over inheritance
- Prefer testable over tightly coupled

## Output format
```
## Review by Senior Reviewer

### Blocking issues (Block)
1. [File:Line] Problem. Fix: <csharp snippet>

### Improvements (Suggest)
…

### Nits (Nit)
…

### Summary
Top 3 priorities to fix before merge.
```
````

**Cách dùng:** mở Copilot Chat → **dropdown chọn agent** (mặc định "Agent") → chọn **senior-reviewer**. KHÔNG gõ `@senior-reviewer`. Gõ `/agents` để quản lý nếu không thấy.

## Bước 5 — Promote 3 prompt thành 3 skill (copy-paste từng file)

### 5a. `.github/skills/review-pr/SKILL.md`

```markdown
---
name: review-pr
description: |
  Use this skill when the user asks for a checklist-driven review of a diff
  or a pull request. Targets the team's blocking categories: security,
  performance, correctness, style.
---

# Review PR Skill

## When to use
- The user pastes a diff, a branch name, or a PR URL and asks for review
- The CI workflow invokes this skill on every PR

## Workflow
1. Read the diff (`git diff origin/main..HEAD` or `${selection}`)
2. Read `copilot-instructions.md` for team conventions
3. Walk the four-category checklist below
4. Group findings by severity, write the summary to PR comment / `REVIEW.md`

## Checklist
### Security
- SQL injection / parameterised query
- Hardcoded secret / connection string
- Missing `[Authorize]`, input validation, output encoding
### Performance
- N+1 query, missing AsNoTracking, hot-path allocations
- Pagination on list endpoints
### Correctness
- Null checks, race conditions (Singleton + mutable state)
- DateTime UTC, CancellationToken propagation
- Sync-over-async (`.Result`, `.Wait()`)
### Style
- Magic number with no const
- Naming convention (PascalCase service, _camelCase field)

## Output format
Group theo severity (Block / Suggest / Nit). Mỗi issue: File:line, vấn đề, fix snippet. Kết bằng top-3 priorities.
```

### 5b. `.github/skills/review-security/SKILL.md`

```markdown
---
name: review-security
description: |
  Use this skill when the user asks for an OWASP Top 10 security audit
  of a file, a diff, or a folder, especially auth / SQL / HTML rendering changes.
---

# Review Security Skill

## When to use
- User names a file (`#file:UserService.cs`) and asks for a security audit
- Before merging a PR touching credential-handling, SQL, or HTML rendering

## Workflow
1. Read the target file (or `${selection}`)
2. Walk every OWASP category below
3. For each finding: severity, file:line, description, fix snippet, OWASP link

## OWASP categories
- A01 Broken Access Control: missing [Authorize], IDOR, mass assignment
- A02 Cryptographic Failures: MD5/SHA1/DES, plaintext password, weak random
- A03 Injection: FromSqlRaw/ExecuteSqlRaw không parameterized, stored/reflected XSS
- A07 Auth Failures: hardcoded secret, weak password policy, predictable token
- A08 Data Integrity: insecure deserialization (BinaryFormatter)
- A09 Logging Failures: log password/PII
- A10 SSRF: HttpClient tới URL từ user input

## Output format
Sort theo severity Critical / High / Medium / Low. Mỗi finding kèm fix snippet + OWASP link.
Verify: chạy lại trên file đã fix, target 0 Critical / 0 High.
```

### 5c. `.github/skills/generate-tests/SKILL.md`

```markdown
---
name: generate-tests
description: |
  Use this skill when the user asks to generate or update unit tests
  for a service, validator, or controller.
---

# Generate Tests Skill

## When to use
- The user names a file/service and asks for tests
- The user asks to raise coverage above 80%

## Workflow
1. Read the target file and its public methods
2. Read team naming/structure rules in copilot-instructions.md
3. Generate AAA tests: 1 happy + 2 edge + 1 exception per method
4. Use xUnit + Moq + FluentAssertions

## Convention
- Test name: MethodName_Scenario_ExpectedResult
- Arrange / Act / Assert rõ ràng
- Mock dependency qua Moq; assert bằng FluentAssertions
```

**Verify:** trong Copilot Chat gõ `/skills` → phải thấy review-pr, review-security, generate-tests.

## Bước 6 — ORCHESTRATOR `.github/skills/review-and-fix/SKILL.md` (copy-paste)

```markdown
---
name: review-and-fix
description: |
  Use this skill when the user pastes a PR or branch name and asks for an
  end-to-end review + auto-fix of blocking findings.
argument-hint: branch=<branch-name>
---

# Review and Fix

## Phases
1. Review  -> persona senior-reviewer -> REVIEW.md, tag [BLOCK]/[SUGGEST]/[NIT]
2. Fix     -> persona fixer  -> loop từng [BLOCK], commit "fix: ... [ai]"
3. Verify  -> persona tester -> dotnet test; test hỏng thì regenerate qua generate-tests

## Retry budget
| Phase  | Attempts | Self-heal | Escalation |
|--------|---------:|-----------|------------|
| Review | 1 | none | surface to user |
| Fix    | 3 per [BLOCK] | đọc lại build error, đơn giản hoá fix | mark [DEFER], đi tiếp |
| Verify | 2 | siết assertion, regenerate test | test còn đỏ -> escalate |

## Stop conditions
- Mọi [BLOCK] đã fix hoặc [DEFER], tests xanh
- Hoặc chạm trần 90 phút wall-clock -> dừng, surface state
```

## Bước 7 — Test orchestrator

Trong Copilot Chat, gõ (gõ `/` rồi chọn `review-and-fix`, thêm tham số):

```text
/review-and-fix branch=<nhánh-của-bạn>
```

Xác nhận chạy đủ 3 phase và đẻ ra đúng artefact (REVIEW.md, commit fix, test run).

## Coi như xong khi

- [ ] `.github/` có đủ: copilot-instructions.md + instructions/ + agents/ + skills/ (4 skill)
- [ ] `/skills` liệt kê đủ skill; senior-reviewer xuất hiện trong dropdown agent
- [ ] `/review-and-fix branch=...` chạy đủ 3 phase
- [ ] (Tự chọn) push `.github/` lên repo team

> Lab 4.4 sẽ cho CI tự chạy review trên mọi PR, dùng chính prompt/skill này.
