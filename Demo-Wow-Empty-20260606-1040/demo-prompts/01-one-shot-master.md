# Demo Prompt 01 — One-shot Master Prompt (Fullstack)

**Use case:** Workspace có CHỈ `.github/` + `feature-specs/`. Không có code. Type 1 prompt → ra app **fullstack** (BE+FE) chạy được trong ~10 phút.

> dùng `04-autonomous.md` thay vì file này — autonomous mode tự fix lỗi, không dừng giữa chừng.

## Yêu cầu trước khi chạy

- ✅ Copilot Chat đang ở **Agent Mode** (icon ⚡ ở góc trên chat)
- ✅ `node --version` ≥ 20 LTS, `npm --version` ≥ 10
- ✅ `dotnet --list-sdks` có .NET 8+
- ✅ Đã enable auto-approve commands (Settings → `chat.tools.terminal.autoApprove`) HOẶC chuẩn bị click "Allow" cho mỗi lệnh

## 🎯 The Master Prompt — FULLSTACK (copy nguyên cụm này)

````text
Read .github/skills/deliver-feature/SKILL.md and execute it end-to-end for this feature:

#file:feature-specs/shop-fullstack.spec.md

Workspace is empty — bootstrap BOTH:
- .NET solution via .github/skills/bootstrap-project/SKILL.md  (Phase 0a)
- React+Vite frontend via .github/skills/bootstrap-frontend/SKILL.md  (Phase 0b)

Act as each sub-agent in sequence:
  @planner → @architect → @backend-dev → @frontend-dev → @tester → @reviewer

Use the corresponding skill from .github/skills/ for each.

Follow .github/copilot-instructions.md + .github/instructions/frontend.instructions.md strictly.

The TODO.md MUST tag every task [BE], [FE], [BE+FE], or [TEST] so each agent picks
up only its work. @backend-dev does [BE], @frontend-dev does [FE], @tester does [TEST].

Commit after each phase with the prefix shown in each skill (FE commits append -FE).
Tick checkboxes in TODO.md as items complete.

Output a one-line progress status after each phase. Stop on any failure.
````

## 🎯 Alternative — BE-only Master Prompt

Nếu chỉ demo backend (5 phút), dùng spec BE-only:

````text
Read .github/skills/deliver-feature/SKILL.md and execute it end-to-end for:

#file:feature-specs/product-search.spec.md

This is a BE-only spec — Phase 0b (bootstrap-frontend) and Phase 3b (implement-frontend)
will be skipped automatically (no Frontend section in spec).

Bootstrap .NET first, then act as @planner → @architect → @backend-dev → @tester → @reviewer.

Follow .github/copilot-instructions.md strictly. Commit + tick checkboxes per phase.
Stop on failure.
````

## Variants

### Variant A — Specify fullstack inline (không reference file)

````text
Read .github/skills/deliver-feature/SKILL.md.

Execute end-to-end delivery for this fullstack feature:

---
Feature: Shop Fullstack — mini e-commerce
Stack: ShopApi (.NET 8) + ShopWeb (React+Vite+TS+Tailwind+shadcn) monorepo

BE: Products CRUD + search (relevance), Cart, Orders (atomic stock decrement)
FE: HomePage, ProductList (search/filter/grid), ProductDetail, Cart Sheet,
    CheckoutPage (react-hook-form + zod), Admin CRUD
NFR: BE coverage ≥80%, FE TypeScript strict, Lighthouse a11y ≥95
---

Bootstrap both ShopApi and ShopWeb (no code exists yet). Use all 6 sub-agents.
Tag every TODO [BE]/[FE]/[BE+FE]/[TEST].
````

### Variant B — Phase-by-phase (control hơn)

Nếu Agent Mode break ngang chừng, fallback sang phase-by-phase. Xem `02-phase-by-phase.md`.

## Expected behavior (fullstack, ~9–10 phút)

Agent Mode sẽ:

1. **Read context** (~15s)
   - `deliver-feature/SKILL.md`, `bootstrap-project/SKILL.md`, `bootstrap-frontend/SKILL.md`
   - `feature-specs/shop-fullstack.spec.md`
   - `copilot-instructions.md` + `instructions/frontend.instructions.md`

2. **Phase 0a — Bootstrap BE** (~30s)
   - `dotnet new sln/webapi/xunit` + NuGet
   - Baseline files
   - Build pass
   - Commit `chore: bootstrap ShopApi baseline`

3. **Phase 0b — Bootstrap FE** (~90s)
   - `npm create vite ShopWeb -- --template react-ts`
   - Install Tailwind v4 + shadcn + TanStack Query + React Router + react-hook-form + zod + axios + lucide + sonner
   - `npx shadcn init` + add 12 primitives
   - Wire App.tsx + RootLayout + axios + queryClient
   - `npm run build` pass
   - Commit `chore: bootstrap ShopWeb baseline`

4. **Phase 1 — Plan** (~45s)
   - `TODO.md` ~18 TODOs tagged `[BE]` (10), `[FE]` (6), `[TEST]` (2)

5. **Phase 2 — Design** (~60s)
   - `DESIGN.md`: Mermaid BE layers + FE component tree + API contract + flow diagram

6. **Phase 3a — Scaffold BE** (~2 min)
   - Loop `[BE]` TODOs: code + build + tick + commit `[ai] feat: T<N> ...`

7. **Phase 3b — Implement FE** (~2 min)
   - Loop `[FE]` TODOs: feature folder + components + router + build + tick + commit `[ai] feat: T<N>-FE ...`

8. **Phase 4 — Tests** (~90s)
   - BE xUnit + FE Vitest, tick + commit

9. **Phase 5 — Review** (~45s)
   - `REVIEW.md` → ✅ APPROVED

10. **Output cuối:**
    ```
    ✅ Phase 0a (bootstrap BE) — 8 .cs files, build OK
    ✅ Phase 0b (bootstrap FE) — ShopWeb + 12 shadcn primitives, npm build OK
    ✅ Phase 1  (plan)         — TODO.md 18 TODOs (10 BE / 6 FE / 2 TEST)
    ✅ Phase 2  (design)       — DESIGN.md ready
    ✅ Phase 3a (scaffold BE)  — 10 [BE] ticked, 10 commits
    ✅ Phase 3b (impl FE)      — 6 [FE] ticked, 6 commits
    ✅ Phase 4  (tests)        — BE 24/24 + FE 12/12 pass
    ✅ Phase 5  (review)       — APPROVED

    🎉 Run BE: dotnet run --project ShopApi    (http://localhost:5080)
    🎉 Run FE: npm run dev --prefix ShopWeb    (http://localhost:5173)
    ```

## Backup nếu Agent Mode dừng giữa chừng

````text
Continue executing .github/skills/deliver-feature/SKILL.md from Phase <N>.

State so far — check:
- ShopApi/ and ShopWeb/ exist?
- TODO.md exists? Which TODOs ticked? Which tags untouched?
- DESIGN.md exists?
- dotnet build status?
- cd ShopWeb && npm run build status?

Resume from the first uncompleted phase. Respect the BE/FE tag ownership.
````