---
name: review-feature
description: |
  Senior code review against TODO.md ACs, DESIGN.md decisions, copilot-instructions.md convention, and OWASP/perf checklist. Use this skill when:
  - All implementation + test TODOs are ticked
  - Before merging a PR
  - User says "review", "audit", "/review-feature"

  Outputs grouped feedback (Block/Suggest/Nit) + Approve/Changes-required decision.
---

# review-feature

Final review pass before merge. Outputs structured feedback grouped by severity.

## When to use

- TODO.md shows all implementation + test items ticked
- Before opening a PR or before merge
- After `@backend-dev` and `@tester` are done
- User requests "review", "audit", "/review-feature"

## Inputs

1. `TODO.md` — verify ACs covered
2. `DESIGN.md` — verify implementation matches design
3. `copilot-instructions.md` — verify convention
4. Code changes (`git diff main..HEAD` or selected files)

## Output

Inline review comment OR `REVIEW.md` file, in this format:

```markdown
## 🔍 Review

> Reviewer: @reviewer
> Date: YYYY-MM-DD
> Base: <branch>
> Scope: TODO.md (N/N ticked), DESIGN.md, copilot-instructions.md

### 🔴 Blocking issues

1. **`Services/ProductService.cs:42`** — SQL injection
   - User input → `FromSqlRaw`
   - Fix:
     \`\`\`csharp
     return await _db.Products
         .Where(p => EF.Functions.Like(p.Name, $"%{query}%"))
         .ToListAsync(ct);
     \`\`\`
   - Ref: OWASP A03

### 🟡 Suggestions
2. **`Services/ProductService.cs`** — Add `AsNoTracking()`

### 🟢 Nits
3. **`Models/Product.cs`** — Primary constructor would simplify

### ✅ AC check
- [x] AC1 (T5, T7)
- [x] AC2 (T5)
- [ ] AC3 — **NOT IMPLEMENTED**

### 📊 Metrics
| Metric | Value | Target | ✓ |
|---|---|---|---|
| Build | pass | pass | ✓ |
| Tests | 18/18 | 100% | ✓ |
| Coverage | 87% | ≥ 80% | ✓ |

### 🎯 Decision
**❌ CHANGES REQUIRED**

Priorities:
1. Fix SQL injection
2. Implement AC3
```

## Priority order

1. **Security** 🚨 — block on any Critical/High
2. **Correctness** 🐛 — null refs, race conditions
3. **Performance** ⚡ — N+1, blocking calls
4. **Convention** 📐 — naming, async, DI
5. **Style** 💄 — nice-to-have only

## Checklists

### Security (BLOCKING)
- [ ] SQL injection
- [ ] Hardcoded secret
- [ ] Missing `[Authorize]`
- [ ] XSS
- [ ] Weak crypto
- [ ] Insecure deserialization

### Correctness
- [ ] Null reference
- [ ] Race condition
- [ ] DateTime UTC consistency
- [ ] `CancellationToken` propagation
- [ ] Empty catch swallowing exceptions

### Performance
- [ ] N+1 query
- [ ] Missing `AsNoTracking()`
- [ ] `.Result` / `.Wait()`
- [ ] Pagination cap
- [ ] `IHttpClientFactory`

### Convention
- [ ] `Async` suffix
- [ ] `record` DTO / `class` Entity
- [ ] `_camelCase` private field
- [ ] DI lifecycle correct

## Decision rules

| Condition | Decision |
|---|---|
| 0 blocking + all ACs met | ✅ Approve |
| ≥ 1 blocking OR AC missing | ❌ Changes required |
| Coverage < 80% | ❌ Hand back to @tester |
| Design deviation | ❌ Hand back to @architect |

## Triggers

- "/review-feature"
- "review this PR"
- "audit before merge"
- `@reviewer` agent

## Don't

- Don't write code (suggest fixes only)
- Don't add new requirements
- Don't approve with blocking issues "for later"
- Don't nitpick style if codebase is consistent

## Self-heal (autonomous mode)

If REVIEW.md has Blocking issues → in autonomous mode, hand back to the responsible agent (`@backend-dev` for BE, `@frontend-dev` for FE, `@tester` for tests) WITH the specific fix → wait for re-implementation → re-review. Max 1 review-fix-review cycle per autonomous run; after that, accept with documented "Known issues" section if NOT a security blocker, otherwise output STUCK report.
