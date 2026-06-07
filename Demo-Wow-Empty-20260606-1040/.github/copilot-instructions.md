# Copilot Instructions — Shop Fullstack (Demo Wow)

Monorepo: **`ShopApi/`** (.NET backend) + **`ShopWeb/`** (React frontend).
Path-scoped instructions live in `.github/instructions/` — they layer on top of this file when relevant files are touched.

## Stack — Backend (`ShopApi/`)
- .NET 8 / C# 12
- ASP.NET Core Controllers + minimal API
- EF Core 8 + SQLite (dev) / SQL Server (prod)
- xUnit + Moq + FluentAssertions
- FluentValidation

## Stack — Frontend (`ShopWeb/`)
- React 18 + Vite 5 + TypeScript strict
- Tailwind CSS v4 + shadcn/ui (vendored primitives)
- TanStack Query v5 (server state)
- React Router v6 (routing)
- react-hook-form + zod (forms + validation)
- axios (HTTP client, baseURL from `VITE_API_BASE`)
- lucide-react (icons), sonner (toasts)
- Vitest + Testing Library (unit), optional Playwright (e2e smoke)

## Multi-agent workflow

This repo uses `.github/agents/` (6 sub-agents) + `.github/skills/` (9 skills).

**Standard fullstack feature flow:**

```
spec.md → @planner (/plan-feature)        → TODO.md (tags: [BE] [FE] [BE+FE] [TEST])
       → @architect (/design-feature)     → DESIGN.md (Mermaid: BE layers + FE component tree + flow)
       → /bootstrap-project               → ShopApi baseline (skip if .sln exists)
       → /bootstrap-frontend              → ShopWeb baseline (skip if ShopWeb/package.json exists)
       → @backend-dev (/scaffold-feature) → tick [BE] TODOs + commits
       → @frontend-dev (/implement-frontend) → tick [FE] TODOs + commits
       → @tester (/generate-tests)        → BE + FE test TODOs
       → @reviewer (/review-feature)      → APPROVE or CHANGES REQUIRED

Or one-shot: /deliver-feature #file:feature-specs/<name>.spec.md
```

**Always tick TODO.md checkboxes** as work progresses — PM tracks via the file.

**TODO tagging is mandatory** so each agent only picks up its tasks.

## Autonomous Mode (ON by default for `/deliver-feature`)

When the orchestrator skill `/deliver-feature` is running, every sub-agent operates in **autonomous self-healing** mode:

- **NEVER stop on errors** — diagnose, fix, retry within budget
- **Per-task retry budget:** 2 attempts; **per-phase budget:** 3; **stuck-detect:** same error ×3 → STOP
- **Wall-clock soft cap:** 90 minutes total
- **Auto-fix:** build/test errors, npm errors, TS errors, port conflicts, network timeouts
- **Escalate immediately:** Copilot quota, permission denied, missing tools, user cancel
- **Never** `[Fact(Skip=...)]` / `// @ts-ignore` to fake green — fix the actual issue

If a sub-agent is invoked directly (not via `/deliver-feature`), it operates in **interactive mode**: it MAY ask the user on the first hard error. Use autonomous mode only when running the orchestrator end-to-end.

Full details: `.github/skills/deliver-feature/SKILL.md` → "Autonomous Mode" section.

## Code style

- File-scoped namespace: `namespace ShopApi.Services;`
- `var` when RHS type is obvious
- `async` all the way — public method has `Async` suffix
- `CancellationToken` on every public async method
- `record` for DTO, `class` for Entity
- Primary constructor (C# 12) for DI

## Naming

- Service interface: `I{Name}Service` / `{Name}Service`
- Folder: PascalCase, plural for `Models/`, `Services/`
- Private field: `_camelCase`
- Constant: `PascalCase`
- Async method: suffix `Async`
- Test: `{Method}_{Scenario}_{Expected}`

## Layering

- Controller = thin orchestration
- Service = business logic
- Repository only when needed for testability (don't add by default)
- DI lifecycle: `Scoped` for service, `Singleton` for options, `Transient` for stateless helpers

## Error handling

- Business error → `Result<T>` pattern OR custom exception
- Validation → `ProblemDetails` (RFC 7807)
- Log all exceptions through `ILogger` with structured fields

## Testing

- AAA (Arrange / Act / Assert)
- 1 happy + 2 edge + 1 exception + 1 cancellation per public method
- Coverage target: 80% line, 70% branch
- `UseInMemoryDatabase(Guid.NewGuid())` — never mock `DbContext`
- FluentAssertions — never `Assert.True/Equal`

## Security

- Never hardcode: secret, connection string, API key
- Never use: MD5, SHA1, DES, `System.Random` for tokens
- SQL: parameterized query — never string concat into `FromSqlRaw`
- Validate input at edge (Controller or Validator)
- BCrypt or Argon2 for password (work factor ≥ 12)

## Performance

- `AsNoTracking()` on read-only queries
- Pagination cap: `pageSize = Math.Min(pageSize, 100)`
- `IAsyncEnumerable<T>` for streaming large data
- `IHttpClientFactory` — never `new HttpClient()`

## Forbidden APIs

- ❌ `BinaryFormatter`
- ❌ `JsonConvert` (use `System.Text.Json`)
- ❌ `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` in async
- ❌ `DateTime.Now` (use `DateTime.UtcNow`)
- ❌ `MD5`, `SHA1`, `DES`, `RC4`
- ❌ `Random` for security tokens (use `RandomNumberGenerator`)
- ❌ `Console.WriteLine` in production code (use `ILogger`)

## Git

- Conventional Commits: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `perf`, `ci`
- AI-generated commits prefix with `[ai]`: `[ai] feat: T5 add search endpoint`
- FE commits append `-FE` suffix to TODO number: `[ai] feat: T5-FE product list page`
- 1 TODO = 1 commit (never batch)

---

# Frontend conventions (high-level)

Detailed rules in `.github/instructions/frontend.instructions.md` (applies to `ShopWeb/**`). Highlights:

## Layout

```
ShopWeb/src/
├─ components/ui/        # shadcn primitives — VENDORED, don't edit
├─ components/           # shared composed components
├─ features/<name>/      # api.ts | hooks.ts | types.ts | schemas.ts | <Page>.tsx
├─ pages/                # route-level composition
├─ lib/                  # axios, queryClient, utils
├─ App.tsx | main.tsx
```

## State

- **Server state** → TanStack Query — never `useState` for API data, never `useEffect+fetch`
- **Form state** → `useForm` + `zodResolver`
- **UI state** → `useState`, lift up; context only if 3+ levels deep

## Styling

- Tailwind utilities only — no CSS files (except `index.css`), no inline `style={}` (unless dynamic)
- Mobile-first: `sm: md: lg:`
- Use design tokens (`bg-background`, `text-muted-foreground`) — not raw hex / gray-XXX

## Forbidden (FE)

- ❌ `dangerouslySetInnerHTML`
- ❌ `any` type
- ❌ `console.log` in committed code
- ❌ Edit `src/components/ui/*` (re-add via `npx shadcn add` if buggy)
- ❌ `useEffect` for data fetching
- ❌ New global state lib without architect sign-off
