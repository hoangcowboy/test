# Demo Prompt 04 — Autonomous Mode (No-Stop)

**Use case:** Live demo trước lớp / khách hàng — AI **không được dừng giữa chừng**, kể cả khi gặp lỗi cũng tự fix, tự chạy đến khi xong.

## Khi nào dùng prompt này

- ✅ Live demo có audience (Day 1 slide 02, Hackathon Day 2)
- ✅ Trainer muốn 1 lần paste rồi đi uống cà phê, quay lại app chạy live
- ✅ Có rủi ro lỗi npm / build / test → cần auto-recover
- ❌ Đang dev thật → dùng `02-phase-by-phase.md` (human verify từng phase)

## Setup

Workspace EmptyShell với `.github/` + `feature-specs/` (sinh từ `setup.ps1 -Mode EmptyShell -Fresh`).

Mở Copilot Chat → bật **Agent Mode** ⚡. Confirm auto-approve commands trong Settings.

## 🎯 The Autonomous Prompt (copy nguyên cụm)

### Fullstack (~10 phút, recovery budget ~30 phút wall-clock)

````text
You are running in AUTONOMOUS MODE. Read .github/skills/deliver-feature/SKILL.md
and execute it end-to-end for #file:feature-specs/shop-fullstack.spec.md.

CRITICAL RULES (override any "stop on error" defaults):

1. DO NOT STOP on errors. Self-heal:
   - Build fail → read error → patch → rebuild (max 2 retries per TODO)
   - Test fail → read assertion → fix correct side (test or source) → re-run (max 2)
   - npm install fail → `npm cache clean --force` → retry; peer-dep → `--legacy-peer-deps`
   - npx shadcn hang → kill, re-run with `-y -d -b slate -s` flags
   - Port 5080/5173 busy → pick next free, update config
   - Network timeout → wait 5s, retry up to 3 times

2. DO NOT ASK for clarification mid-run. Make best-effort decisions,
   document the choice in commit message. Resume next TODO.

3. NEVER fake green:
   - No `[Fact(Skip = "...")]`
   - No `// @ts-ignore` / `// @ts-expect-error`
   - No empty stub returning fake data to pass tests

4. STUCK detection (then and only then output a stuck report):
   - Same error message 3 times in a row OR
   - Per-phase retry budget (3) exhausted OR
   - Wall-clock 90 min reached

5. Bootstrap BOTH ShopApi (.NET 8) and ShopWeb (React+Vite+TS+Tailwind+shadcn)
   from empty. Use 6 sub-agents in turn: @planner → @architect → @backend-dev
   → @frontend-dev → @tester → @reviewer.

6. Tag every TODO [BE]/[FE]/[BE+FE]/[TEST]. Commit after each phase. Tick
   checkboxes as work progresses. Print 1-line status per phase.

7. End condition: all TODOs ticked, `dotnet test` pass, `npm run build` pass,
   REVIEW.md exists with APPROVED decision. Then output:
   "🎉 DELIVERY COMPLETE — BE: dotnet run --project ShopApi (5080), FE: npm run dev --prefix ShopWeb (5173)"

Begin Phase 0a now.
````

### Backend-only (~5 phút)

````text
You are running in AUTONOMOUS MODE. Read .github/skills/deliver-feature/SKILL.md
and execute it end-to-end for #file:feature-specs/product-search.spec.md.

Same rules as fullstack autonomous mode (see deliver-feature/SKILL.md
"Autonomous Mode" section): NEVER stop on errors, self-heal within budget
(per-task 2, per-phase 3, same-error 3 = STUCK, wall-clock 90min).

No Frontend section in this spec → Phase 0b + 3b auto-skip.

End condition: all TODOs ticked, `dotnet test` pass, REVIEW.md APPROVED.
Output: "🎉 DELIVERY COMPLETE — dotnet run --project ShopApi"

Begin Phase 0a now.
````

## Expected behavior khác Prompt 01

| Tình huống | Prompt 01 (default) | Prompt 04 (autonomous) |
|---|---|---|
| `dotnet build` fail trong scaffold T3 | STOP, output error, chờ trainer | Tự đọc CS####, patch code, rebuild (retry tối đa 2) |
| `npm install` lỗi network | STOP | wait 5s, retry 3 lần |
| Test fail trong Phase 4 | STOP | Đọc assertion, sửa đúng side, re-run (retry 2) |
| Mermaid syntax sai trong DESIGN.md | STOP | Re-generate (retry 2) |
| Same error lặp 3 lần | (đã stop từ lần 1) | STUCK report rõ ràng |
| Phase 3a vượt 5 phút (rất hiếm) | (đã stop) | Tiếp tục đến budget 90 min |

## Status hiển thị trong Chat

AI sẽ in mỗi retry:
```
🔄 T5 retry 1/2 — CS0103 'Product' missing namespace → adding using ShopApi.Models;
✅ T5 ticked + commit [ai] feat: T5 product search endpoint
```

Khi STUCK:
```
❌ STUCK at Phase 3a, Task T7
   Repeated error (×3): MSB3027 Could not copy ShopApi.dll
   Tried fixes:
     - Closed file handles via taskkill
     - dotnet clean + restore
     - Restart from fresh build
   Manual recovery suggestion: kill any running dotnet.exe in Task Manager, retry
```

## Failure modes của autonomous mode

| Failure | Likelihood | Mitigation |
|---|---|---|
| Token quota exhausted | Medium (fullstack ~30k tokens) | Có Copilot Enterprise license, không Pro |
| Infinite retry trên cùng lỗi | Low (stuck-detect catch) | Stuck-detect đã built-in |
| AI "tự sửa" theo hướng sai | Medium | DESIGN.md detailed → AI có anchor; STUCK report cuối cùng catch được |
| Network mất giữa chừng | Low | Network retry budget 3 |
| Demo vượt 90 min | Very low | Spec đã constrained scope |

## So sánh 4 prompts

| File | Khi dùng | Behavior on error |
|---|---|---|
| 01-one-shot-master.md | Default demo | Có guideline, có thể stop |
| 02-phase-by-phase.md | Dev/teaching | Trainer kiểm soát từng phase |
| 03-shortest-wow.md | "Wow" 1 dòng | Tối giản, cú pháp gọn |
| **04-autonomous.md** ⚡ | **Live demo no-fail** | **Self-heal 100%, no-stop until done** |
