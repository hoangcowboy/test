---
name: review-and-fix
description: |
  Use this skill when the user pastes a PR or branch name and asks
  for an end-to-end review + auto-fix of blocking findings.
  Orchestrates three agents: reviewer, fixer, tester.
---

# Review and Fix — Orchestrator

## Phases
1. Review — persona `senior-reviewer` reads the diff or target file and writes `REVIEW.md`.
2. Fix — persona `fixer` applies minimal safe fixes for findings tagged `[BLOCK]`.
3. Verify — persona `tester` runs `dotnet test` and verifies the fix.

## Retry budget
| Phase | Attempts | Self-heal | Escalation |
|-------|---------:|-----------|------------|
| Review | 1 | refine findings with more context | surface blocked findings in `REVIEW.md` |
| Fix | 3 | try a smaller fix, preserve existing behavior | tag `[DEFER]` in `REVIEW.md` |
| Verify | 3 | rerun tests, inspect failing assertions | stop and report failure |

## Output artefacts
- `REVIEW.md` containing findings and final status
- `REVIEW.md` updated with `[BLOCK]` / `[SUGGEST]` / `[NIT]` marks
- Optional `[ai]` commit messages for applied fixes
- Final pass/fail verification result
