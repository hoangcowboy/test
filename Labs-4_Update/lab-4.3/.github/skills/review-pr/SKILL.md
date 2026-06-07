---
name: review-pr
description: |
  Use this skill when the user asks for a checklist-driven review of a diff or pull request.
  Targets the team's blocking categories: security, performance, correctness, style.
---

# Review PR Skill

## When to use
- The user pastes a diff, branch name, or PR description and asks for review
- The user wants a review based on team conventions and blockers

## Workflow
1. Read the diff (`git diff origin/main..HEAD` or `${selection}`)
2. Read `copilot-instructions.md` for team conventions
3. Walk the four-category checklist below
4. Group findings by severity and write the summary to `REVIEW.md`

## Checklist
### Security
- SQL injection / parameterized query
- Hardcoded secret / connection string
- Missing `[Authorize]`, input validation, output encoding

### Performance
- N+1 query, missing `AsNoTracking`, hot-path allocations
- Missing pagination on list endpoints

### Correctness
- Null checks, race conditions (singleton + mutable state)
- `DateTime` UTC, `CancellationToken` propagation
- Sync-over-async (`.Result`, `.Wait()`)

### Style
- Magic number without constant
- Naming convention (`PascalCase` service, `_camelCase` field)

## Output format
```
## Review by review-pr skill
### Blocking issues (Block)
…
### Improvements (Suggest)
…
### Nits (Nit)
…
### Summary
Top 3 priorities.
```
