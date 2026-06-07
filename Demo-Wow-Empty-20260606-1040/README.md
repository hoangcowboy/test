# Multi-Agent Demo Wow — Empty Shell (Fullstack)

This workspace contains ONLY:
- `.github/` — 6 agents, 9 skills, instructions (incl. frontend.instructions.md), workflows
- `feature-specs/` — two specs available:
  - `shop-fullstack.spec.md` (BE+FE mini e-commerce, ~10 min demo)
  - `product-search.spec.md` (BE-only, ~5 min demo)
- `demo-prompts/` — copy-paste ready prompts

NO source code exists yet. AI will bootstrap everything.

## Stack AI will scaffold

- **ShopApi/** — .NET 8 + EF Core 8 + SQLite + xUnit
- **ShopWeb/** — React 18 + Vite 5 + TypeScript strict + Tailwind v4 + shadcn/ui
  + TanStack Query + React Router + react-hook-form + zod + axios

## To run the demo

1. Open Copilot Chat in VS Code
2. Switch to **Agent Mode** (lightning icon)
3. Paste ONE of these prompts:

### Fullstack (BE + FE, ~10 min)

```
/deliver-feature #file:feature-specs/shop-fullstack.spec.md
```

### Backend-only (~5 min)

```
/deliver-feature #file:feature-specs/product-search.spec.md
```

### Natural language variant

```
Read .github/skills/deliver-feature/SKILL.md and execute it end-to-end for
#file:feature-specs/shop-fullstack.spec.md. Bootstrap ShopApi (.NET) AND
ShopWeb (React+Vite+TS+Tailwind+shadcn). Use all 6 sub-agents in turn.
Tag every TODO [BE], [FE], [BE+FE], or [TEST].
```

### ⚡ Autonomous Mode (cho live demo no-fail)

Cho demo trước audience, dùng prompt đầy đủ từ `demo-prompts/04-autonomous.md`.
Prompt đó enforce: AI tự fix lỗi build/test/npm, không dừng giữa chừng, budget
bảo vệ (per-task 2 retry, per-phase 3 retry, stuck-detect 3-same-error, 90min cap).

## What AI will do (fullstack flow)

1. **Phase 0a** Bootstrap .NET solution (~30s)
2. **Phase 0b** Bootstrap React+Vite frontend (~90s — npm install + shadcn init)
3. **Phase 1** @planner -> TODO.md (~45s, tags [BE]/[FE]/[TEST])
4. **Phase 2** @architect -> DESIGN.md with Mermaid (~60s)
5. **Phase 3a** @backend-dev -> implement [BE] TODOs (~2 min)
6. **Phase 3b** @frontend-dev -> implement [FE] TODOs (~2 min)
7. **Phase 4** @tester -> BE xUnit + FE Vitest (~90s)
8. **Phase 5** @reviewer -> REVIEW.md APPROVED (~45s)

**Total:** ~10 minutes from empty folder to runnable fullstack app.

## Run after delivery

```powershell
# Terminal 1 — backend
dotnet run --project ShopApi      # http://localhost:5080

# Terminal 2 — frontend
npm run dev --prefix ShopWeb      # http://localhost:5173
```

See `demo-prompts/` for variant prompts (phase-by-phase fallback, shortest wow).
