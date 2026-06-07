---
name: plan-feature
description: |
  Generate a TODO.md from a feature spec. Use this skill when:
  - User pastes a feature spec / user story / PRD
  - Repo has no TODO.md yet for the feature
  - You need to break work down before writing any code
  - User says "plan this feature", "break this down", "/plan-feature"

  Skip this skill when TODO.md already exists for the feature.
---

# plan-feature

Generates a TODO.md by analyzing a feature spec + the existing codebase.

## When to use

- Starting a non-trivial feature (touches > 1 file, > 1 layer)
- User pastes spec/PRD and asks for "next steps"
- A `feature-specs/<name>.spec.md` file is open or referenced

## Inputs required

1. **Spec** — either a markdown file (`#file:feature-specs/xxx.spec.md`) or pasted text
2. **Codebase context** — `@workspace` to understand existing patterns

If spec is missing, **ask for it before producing TODO.md**. Do not guess.

## Output

Single file: `TODO.md` in repo root.

### Structure

```markdown
# Feature: <Name>

> Spec: feature-specs/<name>.spec.md
> Created: YYYY-MM-DD
> Estimated total: X hours

## Acceptance Criteria
- [ ] AC1: <copied from spec>
- [ ] AC2: ...

## TODO List

### 🗄️ Data
- [ ] **T1** 🟢 <atomic task> — `path/to/file.cs`

### ⚙️ Service
- [ ] **T2** 🟡 ... — `path/to/file.cs`

### 🌐 API
- [ ] **T3** ... — `path/to/file.cs`

### 🧪 Test
- [ ] **T4** ...

### 📚 Docs
- [ ] **T5** ...

## Dependencies
- T2 needs T1
- ...

## Hand-off
- @architect for DESIGN.md (if complex)
- @backend-dev for T1
```

## Rules

- Each TODO touches **1-2 files max** and is **< 30 min** of work
- Order by dependency — never reference a task that depends on an unmarked one
- Effort estimate: 🟢 < 30 min / 🟡 30-90 min / 🔴 > 90 min
- Reference exact file paths (not "the service")
- Every AC must map to ≥ 1 TODO

## Triggers (invocation)

User says any of:
- "plan this feature"
- "break this down"
- "what's the TODO list"
- "/plan-feature"
- "use plan-feature skill"

Or in a multi-agent flow: `@planner` activates this skill implicitly.

## Don't

- Don't write code
- Don't make architectural decisions (Result vs Exception, etc.)
- Don't estimate beyond 🟢🟡🔴 buckets
- Don't skip the AC section

## Self-heal (autonomous mode)

If output `TODO.md` is malformed (missing tags, missing AC mapping, etc.) → re-read this SKILL.md + spec → regenerate (max 2 retries). Do NOT stop and ask user. See `deliver-feature/SKILL.md` "Autonomous Mode" for budget rules.
