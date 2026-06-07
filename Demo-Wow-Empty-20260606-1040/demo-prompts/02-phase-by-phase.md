# Demo Prompt 02 — Phase-by-Phase (Control mode)

Dùng khi muốn show CHẬM từng phase hoặc backup khi one-shot fail.

## Setup

Workspace state: `.github/` + `feature-specs/` only (no code).

Mở Copilot Chat → bật **Agent Mode** ⚡.

---

## Phase 0 — Bootstrap

```text
@workspace Read .github/skills/bootstrap-project/SKILL.md and execute it.

Use Agent Mode. Run all terminal commands.
Create all baseline files matching .github/copilot-instructions.md convention.
Verify: dotnet build + dotnet test must pass.
Commit: "chore: bootstrap ShopApi baseline"
```

**Watch for:** terminal commands chạy → 8 file `.cs` được tạo → `dotnet build` pass → 1 commit.

**Verify:**
```powershell
Test-Path ShopApi.sln              # True
Test-Path ShopApi/Program.cs       # True
dotnet build                       # OK
```

---

## Phase 1 — Plan

```text
@planner

Use the plan-feature skill from .github/skills/plan-feature/SKILL.md.

Input: #file:feature-specs/product-search.spec.md
Output: TODO.md in workspace root.

Commit when done: "docs: add TODO.md for product search feature"
```

**Watch for:** `TODO.md` xuất hiện với 10 unchecked TODOs.

**Verify:**
```powershell
cat TODO.md   # 10 checkboxes [ ]
```

---

## Phase 2 — Design

```text
@architect

Use design-feature skill.

Inputs:
- #file:TODO.md
- #file:feature-specs/product-search.spec.md

Output: DESIGN.md with Approach, Data Model, API Contract, Mermaid sequence diagram,
Edge Cases, Risk Assessment.

Commit: "docs: add DESIGN.md for product search"
```

**Watch for:** `DESIGN.md` xuất hiện. Mở Mermaid preview để show diagram render.

---

## Phase 3 — Scaffold (Implementation)

```text
@backend-dev

Use scaffold-feature skill in BATCH mode.

Read:
- #file:TODO.md
- #file:DESIGN.md
- #file:.github/copilot-instructions.md

For each unchecked IMPLEMENTATION TODO (T1..T6, NOT test TODOs T7/T8):
1. Implement following DESIGN.md
2. Run dotnet build (fix if fails)
3. Tick the checkbox in TODO.md ([ ] → [x])
4. Commit: "[ai] feat: T<N> <description>"

Stop after T6. Report status.
```

**Watch for:**
- TODO.md tick real-time T1, T2, T3, T4, T5, T6
- 6 commits với `[ai] feat:` prefix
- `git log` show progress

**Verify:**
```powershell
cat TODO.md | Select-String "\[x\]"   # 6 ticked
git log --oneline | Select -First 7
dotnet build   # OK
```

---

## Phase 4 — Tests

```text
@tester

Use generate-tests skill.

Read:
- #file:TODO.md (test TODOs T7, T8)
- #file:ShopApi/Services/ProductService.cs (the SearchAsync method)
- #file:.github/instructions/tests.instructions.md

For each test TODO:
1. Write test class with happy + edge + boundary + unicode + SQL-injection + cancellation cases
2. Run dotnet test (all pass)
3. Tick checkbox
4. Commit: "[ai] test: T<N> <description>"

Target ≥ 80% line coverage.
```

**Watch for:**
- T7, T8 tick
- ~12 test cases created
- All tests pass
- 2 commits với `[ai] test:`

**Verify:**
```powershell
dotnet test --collect:"XPlat Code Coverage"   # X% coverage
```

---

## Phase 5 — Review

```text
@reviewer

Use review-feature skill.

Reviewer inputs:
- #file:TODO.md (verify all ACs covered)
- #file:DESIGN.md (verify design adherence)
- #file:.github/copilot-instructions.md (verify convention)
- git diff since first commit (review the actual code changes)

Output REVIEW.md with:
- Blocking issues 🔴
- Suggestions 🟡
- Nits 🟢
- AC checklist
- Quality metrics table
- Final decision (APPROVE or CHANGES REQUIRED)
```

**Watch for:**
- `REVIEW.md` tạo với structured findings
- Decision: APPROVED (ideally)
- Coverage + metrics table

---

## Closing

Chạy commands cuối để show kết quả:

```powershell
# 1. Show all commits
git log --oneline
# Expect ~10 commits:
#   docs: add REVIEW.md
#   [ai] test: T8 ...
#   [ai] test: T7 ...
#   [ai] feat: T6 ...
#   ...
#   chore: bootstrap ShopApi baseline

# 2. Show TODO progress
cat TODO.md | Select-String "\[x\]"
# Expect 10 ticked

# 3. Run the app live
dotnet run --project ShopApi

# 4. Open Swagger UI
start http://localhost:5180/swagger
# Show new /api/products/search endpoint live
```

## Failure recovery

Nếu phase nào fail:

```text
Phase <N> failed. Show me:
1. What was the last successful commit?
2. What error occurred?
3. Proposed fix?

After I confirm the fix, re-run phase <N>.
```
