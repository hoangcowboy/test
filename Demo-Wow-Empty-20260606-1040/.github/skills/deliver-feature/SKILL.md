---
name: deliver-feature
description: |
  AUTONOMOUS end-to-end orchestrator. Reads a feature spec and runs all phases
  sequentially: bootstrap (BE + optionally FE) → plan → design → scaffold (BE) →
  implement (FE) → tests → review. Produces a complete, tested, reviewed
  implementation in one invocation. Use this skill when:
  - User has only `.github/` + `feature-specs/` in the workspace (no project yet)
  - User wants the full demo without manually switching agents
  - User says "deliver", "build everything", "/deliver-feature", "run full workflow"

  This skill is the MASTER orchestrator — it calls bootstrap-project,
  bootstrap-frontend, plan-feature, design-feature, scaffold-feature,
  implement-frontend, generate-tests, review-feature in order, acting as each
  agent in turn.

  CRITICAL: Autonomous Mode is ON by default. Self-heal on errors, retry within
  budget, do NOT stop until DONE or STUCK report. See "Autonomous Mode" section.
---

# deliver-feature

Autonomous end-to-end feature delivery via an 8-phase pipeline. **Single invocation produces a complete, tested, reviewed app from spec — self-healing on errors until success or budget exhausted.**

## Pre-conditions

- Workspace contains `.github/agents/`, `.github/skills/`, `.github/copilot-instructions.md`
- Workspace contains at least one `feature-specs/<name>.spec.md`
- Workspace MAY be empty otherwise (will bootstrap if needed)

## Inputs

Required:
- **Feature spec path** — e.g., `feature-specs/product-search.spec.md`

If not provided, ask: "Which feature spec? Available: <list of feature-specs/*.md>". Do not guess.

## Pipeline

You will execute these phases sequentially, acting as the corresponding sub-agent for each phase. Read each sub-agent's `.agent.md` file and each skill's `SKILL.md` for behavior.

```
Phase 0:  spec validation                       → confirm sections, detect type
Phase 0a: bootstrap-project                     (only if no .sln exists)
Phase 0b: bootstrap-frontend                    (only if fullstack spec AND no ShopWeb/package.json)
Phase 1:  plan-feature      (as @planner)       → TODO.md (with [BE]/[FE] tags)
Phase 2:  design-feature    (as @architect)     → DESIGN.md (BE layers + FE component tree)
Phase 3a: scaffold-feature  (as @backend-dev)   → BE code + tick [BE] TODOs + commits + writes CONTRACT.md
Phase 3b: implement-frontend (as @frontend-dev) → reads CONTRACT.md FIRST + FE code + tick [FE] TODOs + commits
Phase 4:  generate-tests    (as @tester)        → BE + FE tests + tick [TEST] TODOs
Phase 5:  review-feature    (as @reviewer)      → REVIEW.md
Phase 6:  launch                                → migrate DB + seed + start BE + start FE (background)
Phase 7:  smoke test                            → verify health + API + UI + CORS + FE↔BE contract round-trip; output verified URLs
```

**Fullstack detection:** Look in the feature spec for a section titled "Frontend", "FE", or "ShopWeb" — its presence triggers Phase 0b + Phase 3b. BE-only specs skip those.

## Phase 0 — Spec validation (always first)

Before any code is touched, validate the feature spec. This catches misformed specs before AI drifts.

1. Read the spec file referenced in user's prompt
2. Verify these required sections exist (case-insensitive header match):
   - **Why** (or "Motivation", "Background")
   - **What** (or "Requirements", "Scope")
   - **Acceptance Criteria** (or "AC", "AC-BE-X" / "AC-FE-X" patterns)
   - **Out of Scope** (or "Non-goals") — explicit boundary
   - **Hand-off** (or "Flow") — agent invocation order
3. Detect spec type:
   - Has section "Frontend" / "FE" / "ShopWeb" → **fullstack** spec → enable Phase 0b + 3b
   - Otherwise → **BE-only** spec → skip Phase 0b + 3b
4. If ANY required section is missing → output:
   ```
   ❌ SPEC INVALID at <path>:
      Missing required section(s): <list>
      Cannot proceed. Add the section(s) and re-invoke /deliver-feature.
   ```
   STOP (this is one of the few legit STOPs — caller's spec is broken).

5. If all valid → output:
   ```
   ✅ Spec validated: <name> (type: BE-only | fullstack)
      Sections present: Why, What, AC, Out-of-Scope, Hand-off
      Phases enabled: 0a, 1, 2, 3a, 4, 5 (+ 0b, 3b for fullstack)
   ```
   Continue to Phase 0a.

## Phase 0a — Bootstrap backend (conditional)

If `.sln` does not exist in workspace root:
1. Read `.github/skills/bootstrap-project/SKILL.md`
2. Execute it (terminal commands + baseline files)
3. Commit: `chore: bootstrap ShopApi baseline`

Skip if `.sln` already exists.

## Phase 0b — Bootstrap frontend (conditional)

Run only if:
- The spec contains a section titled "Frontend" / "FE" / "ShopWeb", AND
- `ShopWeb/package.json` does not exist

Steps:
1. Read `.github/skills/bootstrap-frontend/SKILL.md`
2. Execute it (npm create vite + Tailwind + shadcn + lib install + baseline files)
3. Commit: `chore: bootstrap ShopWeb baseline (React+Vite+TS+Tailwind+shadcn)`

Skip silently otherwise.

## Phase 1 — Plan

1. Read `.github/agents/planner.agent.md` (your role)
2. Read `.github/skills/plan-feature/SKILL.md` (the skill)
3. Read the feature spec
4. Read codebase via `@workspace` (to understand existing patterns)
5. Output `TODO.md` per the skill's required format
6. Commit: `docs: add TODO.md for <feature>`

## Phase 2 — Design

1. Read `.github/agents/architect.agent.md`
2. Read `.github/skills/design-feature/SKILL.md`
3. Read `TODO.md` + spec
4. Output `DESIGN.md` (with Mermaid diagram)
5. Commit: `docs: add DESIGN.md for <feature>`

## Phase 3a — Scaffold backend

1. Read `.github/agents/backend-dev.agent.md`
2. Read `.github/skills/scaffold-feature/SKILL.md`
3. Read `TODO.md` + `DESIGN.md` + `.github/copilot-instructions.md` + `.github/instructions/controllers.instructions.md`
4. For each unchecked **`[BE]`** implementation TODO (skip `[FE]` and `[TEST]` in this phase):
   - Implement code in `ShopApi/`
   - `dotnet build` → fix if fails
   - Tick checkbox in TODO.md
   - Commit: `[ai] feat: T<N> <description>`
5. Stop after all `[BE]` implementation TODOs done

## Phase 3b — Implement frontend (conditional)

Run only if Phase 0b ran (spec has Frontend section).

1. Read `.github/agents/frontend-dev.agent.md`
2. Read `.github/skills/implement-frontend/SKILL.md`
3. Read `TODO.md` + `DESIGN.md` + `.github/instructions/frontend.instructions.md`
4. For each unchecked **`[FE]`** TODO (skip `[BE]` and `[TEST]`):
   - Implement code in `ShopWeb/` (feature folder pattern)
   - `npm run build` → fix TS errors
   - Tick checkbox in TODO.md
   - Commit: `[ai] feat: T<N>-FE <description>`
5. Stop after all `[FE]` TODOs done

## Phase 4 — Tests (BE xUnit + FE Vitest + Playwright integration)

1. Read `.github/agents/tester.agent.md` + `.github/skills/generate-tests/SKILL.md` + `.github/instructions/tests.instructions.md`
2. Run DISCOVERY (see generate-tests SKILL) to enumerate test targets
3. Output TEST PLAN with gate counts
4. For each `[TEST]` TODO + every discovered target without a test:

   **4a. BE tests** (`ShopApi.Tests/`)
   - xUnit class per controller (integration via `WebApplicationFactory<Program>`)
   - xUnit class per service (unit, InMemory DB)
   - `dotnet test` → ALL pass, coverage ≥ 80%
   - Tick `[TEST]` TODO + commit `[ai] test: T<N> <description>`

   **4b. FE Vitest tests** (`ShopWeb/src/features/*/__tests__/`)
   - 1 file per hook (mock axios, test query/mutation behavior)
   - 1 file per non-trivial component
   - `npm run test` → ALL pass, hook coverage ≥ 80%
   - Tick + commit `[ai] test: T<N>-FE <description>`

   **4c. Playwright integration** (`ShopWeb/tests-e2e/`) — MANDATORY, not optional
   - 1 `.spec.ts` file per Feature in spec (Search, Detail, Cart, Checkout, Admin, Home)
   - Use real BE + real FE (Playwright `webServer` auto-starts)
   - Each test: attach error gates, assert visible UI, verify no 5xx, verify images load
   - `npx playwright test` → ALL pass, 0 skipped
   - Tick + commit `[ai] test: T<N>-E2E <feature>`

5. **Gate check** — before finishing Phase 4:
   - `dotnet test` exit 0 ✓
   - BE coverage report ≥ 80% line, ≥ 70% branch ✓
   - 100% endpoints have ≥ 1 test ✓
   - `npm run test` exit 0 ✓
   - FE hook coverage ≥ 80% ✓
   - `npx playwright test` exit 0, 0 skipped ✓
   - 1 Playwright test per feature in spec ✓
   - All console errors caught by gates ✓

   If ANY gate fails → loop back to add missing tests. Do NOT proceed to Phase 5.

## Phase 5 — Review

1. Read `.github/agents/reviewer.agent.md`
2. Read `.github/skills/review-feature/SKILL.md`
3. Compute `git diff` since baseline
4. Output `REVIEW.md` with structured findings
5. Final decision: ✅ Approve / ❌ Changes required
6. Commit: `docs: add REVIEW.md`

## Phase 6 — Launch (auto-boot everything)

After REVIEW.md APPROVED. Boot the entire stack so user just clicks a URL.

### 6.1 — Backend: migration + database + start

```bash
# Apply EF migrations (idempotent — safe to re-run)
cd ShopApi
dotnet ef migrations add InitialCreate --no-build 2>/dev/null || true   # only if not exists
dotnet ef database update --no-build

# Seed database with realistic demo data (idempotent — check before insert)
# Seed by calling a one-shot endpoint OR running `dotnet run --no-build -- --seed`
# If seed endpoint doesn't exist, backend-dev should have created `Data/Seed.cs` invoked on startup if DB empty

# Start backend in BACKGROUND
# Windows (PowerShell):
Start-Process -FilePath dotnet -ArgumentList "run","--project","ShopApi","--no-build","--urls","http://localhost:5080" -NoNewWindow -RedirectStandardOutput "shopapi.log" -RedirectStandardError "shopapi.err"

# macOS/Linux:
# (cd ShopApi && nohup dotnet run --no-build --urls http://localhost:5080 > ../shopapi.log 2>&1 &)
cd ..
```

Wait up to 30s for `http://localhost:5080/health` to return 200. If timeout → check `shopapi.log` for error → diagnose + patch + retry (per Autonomous Mode budget).

### 6.2 — Frontend: start dev server

```bash
# Start FE in BACKGROUND
# Windows:
Start-Process -FilePath npm -ArgumentList "run","dev","--prefix","ShopWeb","--","--port","5173" -NoNewWindow -RedirectStandardOutput "shopweb.log" -RedirectStandardError "shopweb.err"

# macOS/Linux:
# nohup npm run dev --prefix ShopWeb -- --port 5173 > shopweb.log 2>&1 &
```

Wait up to 30s for `http://localhost:5173/` to return HTML. If timeout → check `shopweb.log` → diagnose + patch.

### 6.3 — Verify config is consistent

- `ShopApi/Program.cs` has CORS allowing `http://localhost:5173` (origin of FE dev server)
- `ShopWeb/vite.config.ts` proxy `/api` → `http://localhost:5080`
- `ShopWeb/.env.development` has `VITE_API_BASE=http://localhost:5080/api`
- If any inconsistent → patch + restart that service

### 6.4 — Output URLs (clickable)

```
🚀 LAUNCH COMPLETE
   BE Swagger:    http://localhost:5080/         (clickable in VS Code terminal)
   BE Health:     http://localhost:5080/health
   FE Home:       http://localhost:5173/
   FE Products:   http://localhost:5173/products
   FE Admin:      http://localhost:5173/admin/products  (if admin in scope)
```

## Phase 7 — Smoke Test (verify it actually works)

NEVER claim "DELIVERY COMPLETE" until smoke tests pass. UI must render, API must respond, no console errors.

### 7.1 — BE smoke (HTTP)

```bash
# Health
curl -fsS http://localhost:5080/health
# Expect: 200 + {"status":"healthy"} OR similar

# List endpoint (should return ≥ 100 seed products)
curl -fsS "http://localhost:5080/api/products?page=1&pageSize=5"
# Expect: 200 + JSON with items[] and meta. meta.total MUST be ≥ 100.

# Verify each product has images[] populated
curl -fsS "http://localhost:5080/api/products?page=1&pageSize=1" | python -c "import sys,json; d=json.load(sys.stdin); assert d['items'][0]['images'] and len(d['items'][0]['images'])>=1 and d['items'][0]['primaryImageUrl'], 'images/primary missing'"
# Or with PowerShell:
# $r = curl -fsS "http://localhost:5080/api/products?page=1&pageSize=1" | ConvertFrom-Json
# if (-not $r.items[0].images -or -not $r.items[0].primaryImageUrl) { throw "FAIL: images missing" }

# Search (if feature spec includes search)
curl -fsS "http://localhost:5080/api/products/search?q=iphone&page=1&pageSize=5"
# Expect: 200 + matching items

# Invalid input (validation)
curl -fsS -o /dev/null -w "%{http_code}" "http://localhost:5080/api/products/search?q=a"
# Expect: 400 (query < 2 chars rejected)
```

If any returns non-2xx (except the deliberate 400 above) → patch BE → restart service → re-run smoke.

### 7.2 — FE smoke (HTTP + headless render check)

```bash
# Root page returns HTML
curl -fsS http://localhost:5173/ | grep -qi '<div id="root"' || echo "FAIL: root div missing"
# Expect: success (no FAIL line)

# Bundle loads (Vite HMR script)
curl -fsS http://localhost:5173/@vite/client | head -c 100 | grep -qi 'createHotContext' && echo "Vite OK"

# Product list page returns HTML (SPA — same index.html, route handled client-side)
curl -fsS http://localhost:5173/products | grep -qi '<div id="root"' && echo "Products route OK"
```

### 7.3 — Cross-cutting (FE ↔ BE integration) — CONTRACT ROUND-TRIP

This is the section that catches 400 errors. Do NOT skip.

```bash
# 1. Proxy GET — basic forward through Vite
curl -fsS "http://localhost:5173/api/products?page=1&pageSize=3"
# Expect: 200 + same JSON shape as direct BE call

# 2. POST/PUT round-trip using the EXACT payload shape from CONTRACT.md
# For every mutation endpoint documented in CONTRACT.md, send the documented
# example body verbatim and assert 200/201. If BE returns 400 here, FE and BE
# have drifted — either CONTRACT.md is stale (re-run scaffold-feature to
# regenerate) or BE serialization differs from the documented camelCase.

# Example — order creation (uses the JSON from CONTRACT.md verbatim):
curl -fsS -X POST "http://localhost:5173/api/orders" \
  -H "Content-Type: application/json" \
  -d '{
    "items":[{"productId":"<real-id-from-seed>","quantity":1}],
    "customer":{"name":"Smoke Tester","email":"smoke@test.local","phone":"0901234567","address":"1 Smoke St, Test City"},
    "note":null
  }'
# Expect: 201 + { orderId, total, createdAt }. If 400 → fail loudly with the body.

# Example — admin product create (uses X-Admin header from CONTRACT.md):
curl -fsS -X POST "http://localhost:5173/api/admin/products" \
  -H "Content-Type: application/json" -H "X-Admin: true" \
  -d '{"name":"Smoke Product","categoryId":"<real-cat-id>","price":100000,"stock":1,"images":[{"url":"https://placehold.co/600","altText":"smoke","isPrimary":true}]}'
# Expect: 201

# 3. Deliberate 400 — ensure validation surfaces field errors in ProblemDetails shape
curl -fsS -o /tmp/badpost.json -w "%{http_code}" -X POST "http://localhost:5173/api/orders" \
  -H "Content-Type: application/json" -d '{"items":[],"customer":{"email":"not-an-email"}}'
# Expect: 400. Body must have "errors" object keyed by field path (so FE can surface per-field).
python -c "import json; d=json.load(open('/tmp/badpost.json')); assert 'errors' in d, 'BE 400 missing errors object'"
```

**If any happy-path mutation returns 400:**
1. Print the request body sent AND the response body.
2. Diff field names against `CONTRACT.md` — usually one of: PascalCase leaked through, a required field missing, a Zod rule looser than FluentValidation so FE accepted bad data, or a nested object got flattened.
3. Fix on the FE side (Zod/`api.ts`) — CONTRACT.md is authoritative because BE validators are authoritative. If BE truly is wrong, also fix BE and regenerate CONTRACT.md.
4. Re-run smoke from step 7.3.1.

If proxy itself fails → fix `vite.config.ts` proxy block → restart FE → re-run.

### 7.4 — UI render smoke (if Playwright available, optional)

```bash
# OPTIONAL — only if Playwright is installed (it's not by default)
# Skip silently if not available.
which playwright >/dev/null 2>&1 && npx playwright test --grep="@smoke" --reporter=line || echo "Playwright skipped (not installed)"
```

If Playwright IS available, generate a minimal `tests-e2e/smoke.spec.ts`:
```ts
import { test, expect } from '@playwright/test';
test('@smoke home page renders without console errors', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', e => errors.push(e.message));
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  await page.goto('http://localhost:5173/');
  await expect(page.locator('h1')).toBeVisible();
  expect(errors).toEqual([]);
});
```

### 7.5 — Final report

```
✅ SMOKE TEST PASSED
   BE  /health         → 200
   BE  GET /products   → 200, 8 items returned
   BE  GET /search     → 200, relevant results
   BE  validation 400  → 400 (correct)
   FE  GET /           → 200, root div present
   FE  Vite HMR        → OK
   FE  /products SPA   → 200
   FE→BE proxy         → 200 (CORS not required, Vite proxy)
   Playwright @smoke   → SKIPPED (not installed) | PASSED

🎉 DELIVERY VERIFIED — ready to demo:
   👉 http://localhost:5173/         (open this in browser)
   👉 http://localhost:5080/         (Swagger UI for BE)
```

### 7.6 — Failure recovery

If any smoke step fails:
1. Read service log (`shopapi.log` or `shopweb.log`)
2. Identify root cause (e.g. CORS missing, DB seed empty, route typo, prop mismatch)
3. Patch the offending file(s)
4. Restart only the affected service (don't tear down all)
5. Re-run smoke from step 1
6. Budget: per-smoke retry 2, total smoke phase 3. After exhaustion → STUCK with which check failed + the log tail.

NEVER claim success until smoke passes. A working demo means: user clicks URL → sees app → can search → can add to cart. NOT "build green + tests pass".

## Progress reporting

After each phase, output a one-line status to the user:

```
✅ Phase 0  (spec validate) — fullstack spec, 5 required sections present
✅ Phase 0a (bootstrap BE)  — 8 .cs files created, dotnet build OK
✅ Phase 0b (bootstrap FE)  — ShopWeb scaffolded, 12 shadcn primitives, npm build OK
✅ Phase 1  (plan)          — TODO.md with 18 TODOs (10 [BE], 6 [FE], 2 [TEST])
✅ Phase 2  (design)        — DESIGN.md with Mermaid + API contract + component tree
✅ Phase 3a (scaffold BE)   — 10 [BE] TODOs ticked, 10 commits
✅ Phase 3b (impl FE)       — 6 [FE] TODOs ticked, 6 commits
✅ Phase 4  (tests)         — 2 [TEST] TODOs ticked, BE 24 cases / FE 12 cases pass
✅ Phase 5  (review)        — APPROVED, 0 blocking, 3 suggestions
✅ Phase 6  (launch)        — Migration applied, seed OK, BE↑ on 5080, FE↑ on 5173
✅ Phase 7  (smoke)         — 8/8 checks PASS

🎉 DELIVERY VERIFIED — open these:
   👉 http://localhost:5173/                 (FE storefront — main demo URL)
   👉 http://localhost:5173/products         (product search)
   👉 http://localhost:5080/                 (Swagger BE)
   👉 http://localhost:5080/health           (BE health endpoint)
```

## Autonomous Mode (default ON)

**You MUST NOT stop on errors. Self-heal and retry until done.** This is a live demo — human intervention is the failure mode.

### Per-task retry budget: 2
- A TODO fails (build/test/TS error) → diagnose error message → fix the offending file → retry → max **2** times.
- After 2 failed retries → escalate to phase-level retry.

### Per-phase retry budget: 3
- A whole phase fails to complete (≥ 1 TODO still unticked AND budget at task-level exhausted) → reset phase, re-read inputs, retry phase from scratch → max **3** times.
- After 3 failed phase retries → escalate to STUCK.

### Stuck detection
- If the **same error message** occurs **3 times in a row** across retries → STOP immediately. Do not loop forever on the same diagnosis.

### Total wall-clock soft cap: 90 min
- Track time from first prompt. At 90 min mark, finish current task then STOP with state report.

### Auto-fix these errors (don't ask, just fix)
| Symptom | Auto-fix action |
|---|---|
| `dotnet build` non-zero exit | Read CS#### error → patch file → rebuild |
| `dotnet test` failure | Read failing test name → read assertion → patch source or test (whichever is wrong per DESIGN.md) → re-run |
| `npm install` fail | `npm cache clean --force` → retry; if peer-dep conflict → `--legacy-peer-deps` |
| `npm run build` TS error | Read TS#### → add/fix type → rebuild |
| `npx shadcn add` prompt hanging | Re-run with `-y -d -b slate` flags |
| Network timeout | wait 5s → retry (max 3 times per cmd) |
| Port 5080 / 5173 already in use | Pick next free port, update `appsettings.json` / `vite.config.ts` to match |
| File-locked by editor | wait 2s → retry |

### Escalate IMMEDIATELY (don't retry)
- Copilot quota / rate-limit / license error
- `permission denied` on workspace folder
- Disk full / out of memory
- `dotnet` / `node` / `npm` not found (env missing)
- Explicit user cancel

### Status reporting during retries
Every retry, print a 1-line status:
```
🔄 T5 retry 1/2 — was: CS0103 'Product' missing namespace → adding using ShopApi.Models;
```

### Stuck output format
```
❌ STUCK at Phase <N>, Task <T>
   Repeated error (×3): <error message>
   Tried fixes: <list>
   Manual recovery: <suggestion for trainer>
```

### Don't (in autonomous mode)
- Don't ask for clarification mid-run — make best-effort decision and document in commit message
- Don't "skip" a TODO without fixing — every TODO must end ticked or be in STUCK report
- Don't suppress errors to fake green — if test fails, fix it, don't `[Fact(Skip=...)]`

## Triggers

- "/deliver-feature" or "/deliver-feature #file:feature-specs/X.spec.md"
- "build everything from this spec"
- "run the full workflow"
- "deliver this feature end-to-end"

## Don't

- Don't ask for clarification mid-phase — gather everything upfront, make best-effort decisions, document in commits
- Don't merge phases (keep one-job-per-phase clarity)
- Don't skip the commit step (transparency matters)
- Don't run tests during phase 3 (that's phase 4's job)
- Don't STOP on first error — see "Autonomous Mode" section for retry budgets
- Don't escalate errors that fall under "Auto-fix these errors" list — fix them silently within budget
- **Don't commit generated artifacts.** Before every `git add`, ensure `.gitignore` excludes: `node_modules/`, `dist/`, `dist-ssr/`, `.vite/`, `playwright-report/`, `test-results/`, `bin/`, `obj/`, `*.log`, `.env*`. If `git status` shows these as untracked, the gitignore is wrong — fix it before committing. The bootstrap skills create the right `.gitignore`; if it has been overwritten or truncated, restore it.

## Time expectation

### BE-only spec (~10 TODOs)
- Phase 0a: ~30s
- Phase 1:  ~30s
- Phase 2:  ~45s
- Phase 3a: ~90s
- Phase 4:  ~60s
- Phase 5:  ~30s
- **Total: ~5 minutes**

### Fullstack spec (~18 TODOs, BE + FE)
- Phase 0a: ~30s (BE bootstrap)
- Phase 0b: ~90s (FE bootstrap — npm install is the slow bit)
- Phase 1:  ~45s
- Phase 2:  ~60s
- Phase 3a: ~2 min
- Phase 3b: ~2 min
- Phase 4:  ~90s
- Phase 5:  ~45s
- **Total: ~9–10 minutes**
