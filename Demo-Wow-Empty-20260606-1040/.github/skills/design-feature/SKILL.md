---
name: design-feature
description: |
  Generate a DESIGN.md from a feature spec + TODO.md. Use this skill when:
  - TODO.md exists but DESIGN.md does not
  - Feature touches data model, public API, or multiple services
  - Implementation has non-trivial decisions (algorithm, indexing, contract)
  - User says "design this", "what's the architecture", "/design-feature"

  Skip when:
  - Feature is pure refactor (no contract change)
  - Feature is trivial CRUD on existing pattern
  - DESIGN.md already exists for this feature
---

# design-feature

Produces a `DESIGN.md` documenting technical approach, data model changes, API contract, sequence diagram, and risk.

## When to use

- TODO.md exists
- Feature changes data model OR public API OR has algorithm decisions
- Spec contains words like "search", "calculate", "migrate", "integrate"

## Inputs required

1. `TODO.md` (output of `plan-feature`)
2. Feature spec (`#file:feature-specs/<name>.spec.md`)
3. Codebase context (`@workspace` for existing patterns)

## Output

Single file: `DESIGN.md` in repo root.

### Required sections

1. **Approach** — 2-3 sentence summary + why this approach
2. **Data Model** — entity changes (C# snippet) + migration outline
3. **API Contract** — verb, path, params table, response example, error codes
4. **Sequence Diagram** — Mermaid for non-CRUD flow
5. **Implementation notes** — algorithm, indexing, phasing
6. **Edge Cases** — table of case + handling
7. **Risk** — table of risk + mitigation
8. **Hand-off** — pointer to next agent

### Mermaid example

\`\`\`mermaid
sequenceDiagram
    participant C as Client
    participant API as Controller
    participant S as Service
    participant DB as Database

    C->>API: GET /products/search?q=phone
    API->>S: SearchAsync(query, filters)
    S->>DB: SQL with LIKE
    DB-->>S: Products
    S-->>API: Response
    API-->>C: 200 JSON
\`\`\`

## Rules

- Every section in the output template must be filled
- API contract: always state error codes (400, 404, 429, 500)
- At least 1 Mermaid diagram if flow > 3 steps
- Phasing: if scope is large, split into Phase 1 (this PR) / Phase 2 / Phase 3

## Triggers

- "/design-feature"
- "what's the design"
- "use design-feature skill"
- `@architect` agent in multi-agent flow

## Don't

- Don't write production code (only contract snippets)
- Don't add or change TODOs
- Don't over-engineer — KISS, phase if needed

## Self-heal (autonomous mode)

If Mermaid fails to render (syntax error), if DESIGN.md missing required sections, or if API contract conflicts with TODO.md → re-read inputs → regenerate (max 2 retries). Do NOT stop and ask. See `deliver-feature/SKILL.md` Autonomous Mode.
