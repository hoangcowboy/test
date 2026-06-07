# Feature Development Workflow

Visual + textual description of how the **6 agents + 9 skills** collaborate on a fullstack feature.

## Phase diagram

```mermaid
flowchart TD
    Spec[📄 feature-specs/X.spec.md] --> P{@planner}
    P -->|/plan-feature| Todo[📋 TODO.md<br/>tags: BE / FE / TEST]

    Todo --> A{@architect}
    A -->|/design-feature| Design[🏛️ DESIGN.md<br/>BE layers + FE tree]

    Design --> B0a[bootstrap-project]
    B0a --> B0b[bootstrap-frontend]

    B0b --> BD{@backend-dev}
    BD -->|/scaffold-feature BE tasks| BEdone[✅ All BE ticked + commits]
    BD -->|❌ build/test fail| Heal1[🔄 Self-heal: diagnose, patch, retry<br/>budget 2/task, 3/phase]
    Heal1 --> BD

    BEdone --> FD{@frontend-dev}
    FD -->|/implement-frontend FE tasks| FEdone[✅ All FE ticked + commits]
    FD -->|❌ TS/build fail| Heal2[🔄 Self-heal: fix type, rebuild]
    Heal2 --> FD

    FEdone --> T{@tester}
    T -->|/generate-tests BE + FE| Tests[✅ Test TODOs ticked + commits]
    T -->|❌ assert fail| Heal3[🔄 Self-heal: fix correct side]
    Heal3 --> T

    Tests --> R{@reviewer}
    R -->|/review-feature| Decision{Decision}
    Decision -->|✅ Approve| Merge[🚀 Done]
    Decision -->|❌ Changes once| BD
    Decision -->|❌ Stuck after retries| StuckOut[📋 STUCK report<br/>manual recovery]
```

## Autonomous Mode contract

When invoked via `/deliver-feature`, ALL phases run in **autonomous mode**:

| Layer | Budget | After exhaustion |
|---|---|---|
| Per-task retry | 2 attempts | Escalate to phase retry |
| Per-phase retry | 3 attempts | Escalate to STUCK |
| Same-error repeat | 3 consecutive | STUCK immediately (avoid infinite loop) |
| Wall-clock | 90 min total | Finish current task, then STUCK |

**Auto-fix without asking:**
- `dotnet build` / `dotnet test` failures → diagnose + patch + retry
- `npm install` / `npm run build` errors → cache clean / type fix
- Shadcn CLI hang → re-run with `-y` flags
- Port conflict (5080 / 5173) → next free port

**Escalate IMMEDIATELY:**
- Copilot quota / license
- Permission denied / disk full
- Missing tool (dotnet / node / npm)
- User explicitly cancels

See `.github/skills/deliver-feature/SKILL.md` Autonomous Mode section for full details.

## Agent ↔ skill mapping

| Agent | Primary skill | Owns folder | Output |
|---|---|---|---|
| `@planner` | `plan-feature` | — | `TODO.md` (with tags) |
| `@architect` | `design-feature` | — | `DESIGN.md` |
| `@backend-dev` | `scaffold-feature` | `ShopApi/` | BE code + ticked `[BE]` + commits |
| `@frontend-dev` | `implement-frontend` | `ShopWeb/` | FE code + ticked `[FE]` + commits |
| `@tester` | `generate-tests` | both `*.Tests/` + `ShopWeb/**/__tests__/` | Tests + ticked `[TEST]` + commits |
| `@reviewer` | `review-feature` | — | `REVIEW.md` |

Plus 2 bootstrap skills (no dedicated agent — called by `deliver-feature` orchestrator):
- `bootstrap-project` — initial .NET solution
- `bootstrap-frontend` — initial React/Vite app

Plus 1 master orchestrator:
- `deliver-feature` — runs all phases end-to-end (`/deliver-feature` slash command)

## Fullstack demo flow (~9 minutes)

| Time | Phase | Action |
|---|---|---|
| 0:00 | open spec | Trainer opens `feature-specs/shop-fullstack.spec.md` + paste `/deliver-feature` |
| 0:30 | Phase 0a | `bootstrap-project` runs — ShopApi baseline + restore + build |
| 2:00 | Phase 0b | `bootstrap-frontend` runs — npm create vite, install deps, shadcn init |
| 2:45 | Phase 1 | `@planner` → `TODO.md` (18 TODOs tagged [BE]/[FE]/[TEST]) |
| 3:30 | Phase 2 | `@architect` → `DESIGN.md` with Mermaid: BE layers + FE component tree |
| 5:30 | Phase 3a | `@backend-dev` loops `[BE]` TODOs, ticks + commits |
| 7:30 | Phase 3b | `@frontend-dev` loops `[FE]` TODOs, ticks + commits |
| 9:00 | Phase 4 | `@tester` writes BE xUnit + FE Vitest, ticks |
| 9:30 | Phase 5 | `@reviewer` → `REVIEW.md` Approve |
| 10:00 | done | `dotnet run` + `npm run dev` → open browser, demo flow |

## BE-only demo flow (~5 minutes)

When the spec has no Frontend section (e.g. `product-search.spec.md`), Phase 0b + 3b are skipped:

| Time | Phase | Action |
|---|---|---|
| 0:00 | open spec | `feature-specs/product-search.spec.md` |
| 0:30 | Phase 0a | `bootstrap-project` |
| 1:00 | Phase 1 | `@planner` → TODO.md |
| 1:30 | Phase 2 | `@architect` → DESIGN.md |
| 3:00 | Phase 3a | `@backend-dev` scaffolds |
| 4:00 | Phase 4 | `@tester` |
| 4:30 | Phase 5 | `@reviewer` Approve |

## Invocation reference

In Copilot Chat (Agent Mode):

```text
# One-shot delivery — recommended for demo
/deliver-feature #file:feature-specs/shop-fullstack.spec.md

# Or invoke phase-by-phase
@planner /plan-feature #file:feature-specs/shop-fullstack.spec.md
@architect /design-feature
@backend-dev /scaffold-feature
@frontend-dev /implement-frontend
@tester /generate-tests
@reviewer /review-feature

# Bootstraps individually
/bootstrap-project   → ShopApi baseline
/bootstrap-frontend  → ShopWeb baseline

# Slash command = skill folder name (under .github/skills/<name>/)
```

## TODO.md is the source of truth

All agents check `TODO.md` for:
- Current progress (which checkboxes are ticked)
- Next task (first unchecked **matching their tag**)
- Acceptance Criteria coverage

### Tag convention

Each TODO line MUST be tagged:

```markdown
- [ ] **T1** `[BE]` Add `SearchAsync` to `IProductService` — covers AC-BE-1, AC-BE-2
- [ ] **T9** `[FE]` Build `ProductListPage` with search + filter — covers AC-FE-2, AC-FE-3
- [ ] **T15** `[BE+FE]` Wire stock-check on add-to-cart — covers AC-BE-7, AC-FE-9
- [ ] **T17** `[TEST]` xUnit + Vitest for order flow — covers AC-BE-7, AC-FE-8
```

When `@frontend-dev` completes T9:
```diff
- [ ] **T9** `[FE]` Build `ProductListPage` with search + filter — ...
+ [x] **T9** `[FE]` Build `ProductListPage` with search + filter — ...
```
Plus commit: `[ai] feat: T9-FE product list page with search + filter`

## Why this design

| Property | How |
|---|---|
| **Transparent** | Every agent's output is a file (TODO/DESIGN/REVIEW) committed to git |
| **Trackable** | Commits tagged `[ai]` show what AI did; checkboxes show progress; tags route work |
| **Composable** | Each agent has 1 job, owns 1 folder. Users can run them in any order |
| **Recoverable** | Hand-off lines explicit; if @backend-dev fails, reviewer hands back exactly |
| **Convention-aware** | All agents read `copilot-instructions.md` + path-scoped `*.instructions.md` (controllers, tests, frontend) |
| **Fullstack-ready** | Tag-based work routing means BE and FE can run in series safely; same model could parallelize across two chats |
