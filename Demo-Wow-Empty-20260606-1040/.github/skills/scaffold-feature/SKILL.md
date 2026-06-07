---
name: scaffold-feature
description: |
  Implement TODO items in `TODO.md` one at a time. Use this skill when:
  - TODO.md and DESIGN.md exist
  - At least one TODO is unchecked
  - User says "scaffold", "implement next", "code this", "/scaffold-feature"

  Behavior:
  - Reads first unchecked TODO
  - Implements code following DESIGN.md + copilot-instructions.md
  - Runs build + test for that scope
  - Ticks the checkbox
  - Commits with `[ai] feat: T<N> <desc>`
  - Stops (one TODO per invocation) OR continues based on user choice
---

# scaffold-feature

Implements one or more TODO items, ticking each checkbox after success.

## When to use

- TODO.md has unchecked items
- DESIGN.md (if exists) is approved
- User wants to advance the implementation

## Operating modes

**Single-TODO mode (default):**
- Implement first unchecked TODO
- Tick + commit
- Report status
- Wait for next invocation

**Batch mode (when user says "continue", "do all", "/scaffold-feature all"):**
- Loop through unchecked TODOs in order
- Stop on first failure
- Report batch summary

## Inputs

1. `TODO.md` (must exist with ≥ 1 unchecked task)
2. `DESIGN.md` (if exists, follow it strictly)
3. `copilot-instructions.md` (team convention — must follow)

## Workflow per TODO

```
1. Read TODO.md → identify first unchecked task
2. Read DESIGN.md section relevant to this task
3. Read referenced files via #file:
4. Implement following copilot-instructions.md
5. Run `dotnet build` → fix if fails
6. Run `dotnet test` for related tests (if any exist)
7. Tick the checkbox: [ ] → [x]
8. Commit: `[ai] feat: T<N> <description>`
9. Output completion report
```

## Required output format

```markdown
## ✅ T<N> completed: <one-line>

**Files changed:**
- `path/to/file.cs` (created/modified)

**Build:** ✅ pass
**Tests:** N/N pass
**Commit:** `[ai] feat: T<N> ...`

**Next:** T<N+1> (or "All TODOs done — hand off to @tester")
```

## Failure handling

| Failure | Action |
|---|---|
| Build fails | Try to fix once. If still fails, report error + stop |
| Test fails (existing tests) | Report which test + stop. Don't modify test |
| Design unclear | Don't guess. Output: "T<N> blocked — DESIGN.md ambiguous on X. @architect" |
| Spec ambiguous | Output: "T<N> blocked — spec unclear on X. Request clarification" |

## Convention rules (BLOCKING)

Reading `copilot-instructions.md` is mandatory. Quick reminders:

- Async methods need `Async` suffix + `CancellationToken`
- DTO = `record`, Entity = `class`
- DI: `Scoped` for services, `Singleton` for options
- Forbidden: `DateTime.Now`, `.Result`, `.Wait()`, `MD5`, hardcoded secret
- File-scoped namespace
- `_camelCase` private fields
- **JSON serialization MUST be `JsonSerializerOptions.Web`** (camelCase) so FE camelCase matches BE — never mix conventions. Set in `Program.cs`:
  ```csharp
  builder.Services.ConfigureHttpJsonOptions(o => {
      o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
      o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
  });
  ```

## Final step (BLOCKING) — Generate CONTRACT.md

After ALL [BE] TODOs are ticked and `dotnet build` + `dotnet test` pass, BEFORE handing off to `@frontend-dev`, generate `CONTRACT.md` in workspace root. This is the **single source of truth** for FE/BE wire format and prevents 400 errors caused by shape/validation mismatches.

For every controller endpoint, document:
1. **HTTP method + path** (exactly as routed, e.g. `POST /api/orders`)
2. **Request body example** — REAL JSON with camelCase keys (matching serialization config)
3. **Response body example** — REAL JSON for 200/201
4. **Validation rules** — every FluentValidation rule restated in plain English, exact field names
5. **Error shape** — exact JSON returned on 400 (the ASP.NET ProblemDetails or custom shape)
6. **Required headers** — e.g. `X-Admin: true` for admin endpoints
7. **axios call** — copy-paste-ready TypeScript snippet

Template:

```markdown
# API Contract — generated from BE source

> Source of truth. Frontend `types.ts` and zod schemas MUST mirror this file exactly.
> If you change a DTO or validator in `ShopApi/`, regenerate this file.

## POST /api/orders  (X-Admin not required)

**Request body**
\`\`\`json
{
  "items": [{ "productId": "string-id", "quantity": 1 }],
  "customer": {
    "name": "Nguyen Van A",
    "email": "a@example.com",
    "phone": "0901234567",
    "address": "123 Le Loi, Q1, TP HCM"
  },
  "note": "Optional note"
}
\`\`\`

**Validation rules** (CreateOrderValidator)
- `items` — required, ≥ 1 item
- `items[].productId` — required, non-empty
- `items[].quantity` — required, ≥ 1, ≤ 99
- `customer.name` — required, 2–100 chars
- `customer.email` — required, valid email
- `customer.phone` — required, Vietnamese phone regex `^0\\d{9}$`
- `customer.address` — required, 5–200 chars
- `note` — optional, max 500 chars

**Success 201**
\`\`\`json
{ "orderId": "ord_abc123", "total": 4500000, "createdAt": "2026-05-18T20:00:00Z" }
\`\`\`

**Error 400** (ProblemDetails)
\`\`\`json
{
  "type": "https://...validation",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "customer.email": ["'customer.email' is not a valid email address."]
  }
}
\`\`\`

**axios snippet for FE**
\`\`\`ts
await api.post<{ orderId: string; total: number; createdAt: string }>("/orders", {
  items, customer, note
});
\`\`\`
```

Generate ONE section per endpoint. Use real seed/test data for examples — not placeholder strings. After writing, commit:

```
git add CONTRACT.md && git commit -m "[ai] docs: CONTRACT.md — wire format for FE consumers"
```

## Triggers

- "/scaffold-feature"
- "/scaffold-feature all"
- "implement next TODO"
- "continue implementing"
- `@backend-dev` agent

## Don't

- Don't batch commit "completed T1-T5" — one commit per TODO
- Don't skip the build/test step

## Self-heal (autonomous mode)

After each TODO implement: if `dotnet build` fails → read the CS#### error, patch the offending file, rebuild (max 2 retries per TODO). If build still red → escalate per `deliver-feature/SKILL.md` Autonomous Mode. NEVER skip a TODO without ticking — every TODO either succeeds, retries, or ends up in STUCK report.
- Don't modify TODO.md to add new tasks (raise with @planner)
- Don't change DESIGN.md decisions (raise with @architect)
