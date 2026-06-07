# Demo Prompt 03 — Shortest "Wow"

Phiên bản ngắn nhất có thể — 1 dòng prompt. Dùng khi muốn ấn tượng tối đa.

> **Lưu ý:** 1-dòng prompt KHÔNG enforce autonomous mode rõ ràng. Nếu cần "không dừng kể cả gặp lỗi", dùng `04-autonomous.md` (dài hơn nhưng an toàn cho live demo).

## Setup

Empty workspace với chỉ `.github/` + `feature-specs/*.spec.md` (sinh ra bởi `setup.ps1 -Mode EmptyShell -Fresh`).

## The Prompts (1 dòng mỗi loại)

### Fullstack (~9 phút) — BE + FE end-to-end

```text
/deliver-feature #file:feature-specs/shop-fullstack.spec.md
```

### Backend-only (~5 phút) — chỉ .NET API

```text
/deliver-feature #file:feature-specs/product-search.spec.md
```

Tại sao 1 dòng đủ:
- `/deliver-feature` invoke skill `deliver-feature/SKILL.md` (slash name = skill folder name)
- SKILL.md có đầy đủ instruction để loop qua 6+2 phases
- `#file:` cung cấp input duy nhất cần thiết
- Phase 0b + 3b tự skip nếu spec không có section Frontend

## Alternative — không slash command

Nếu Copilot version chưa support `/skill-name` auto:

```text
@workspace Use the deliver-feature skill to build this: #file:feature-specs/shop-fullstack.spec.md
```

## Alternative — pure natural language

```text
Build everything in #file:feature-specs/shop-fullstack.spec.md following the multi-agent
workflow in .github/. Bootstrap both ShopApi (.NET) and ShopWeb (React+Vite) first,
then act as @planner, @architect, @backend-dev, @frontend-dev, @tester, @reviewer in turn.
```

## Demo time breakdown — Fullstack 1-prompt

```
0:00   Paste prompt + Enter
0:05   AI reads .github/ files
0:30   Phase 0a done — ShopApi bootstrap complete
2:00   Phase 0b done — ShopWeb bootstrap complete (npm install + shadcn)
2:45   Phase 1 done — TODO.md with [BE]/[FE]/[TEST] tags
3:30   Phase 2 done — DESIGN.md (Mermaid: BE layers + FE tree)
5:30   Phase 3a done — 10 [BE] commits, 10 ticks
7:30   Phase 3b done — 6 [FE] commits, 6 ticks
9:00   Phase 4 done — BE xUnit + FE Vitest
9:30   Phase 5 done — REVIEW.md APPROVED
10:00  mở 2 terminal:
         dotnet run --project ShopApi    (http://localhost:5080)
         npm run dev --prefix ShopWeb    (http://localhost:5173)
       → Browser hiện storefront chạy live
```

Total: ~10 phút từ empty folder đến fullstack app chạy được.

## Demo time breakdown — BE-only 1-prompt

```
0:00   Paste prompt + Enter
0:30   Phase 0a — bootstrap
1:00   Phase 1 — TODO.md
1:45   Phase 2 — DESIGN.md
3:15   Phase 3a — 6 commits
4:15   Phase 4 — tests
4:45   Phase 5 — REVIEW APPROVED
5:00   dotnet run → Swagger UI live
```

Total: ~5 phút.

### Fullstack
```
Manual estimate: ~8 hours (4h BE + 4h FE setup + impl)
With multi-agent: ~10 minutes
Saved: 98%+

Code AI generated: ~1,500 lines (BE 700 + FE 800)
Code human wrote: 0 lines
Human time: review + 1 prompt
```

### BE-only
```
Manual: ~3-4 hours
With multi-agent: ~5 minutes
Saved: 95%+

Code AI generated: ~600 lines
Code human wrote: 0 lines
```
