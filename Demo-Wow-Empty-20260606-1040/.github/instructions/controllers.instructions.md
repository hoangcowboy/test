---
applyTo: "**/Controllers/**/*.cs"
---

# Controller convention

These rules apply only to files under `Controllers/`.

## Structure

- Inherit `ControllerBase` (not `Controller`)
- Use `[ApiController]` and `[Route("api/[controller]")]`
- Return `ActionResult<T>` (not `IActionResult`) for type-safe responses
- Always async with `CancellationToken` parameter

## HTTP semantics

| Action | Status | Body |
|---|---|---|
| Create success | `201 Created` | with `Location` header |
| Update success | `204 NoContent` | empty |
| Delete success | `204 NoContent` | empty |
| Not found | `404` | `ProblemDetails` |
| Validation fail | `400` | `ValidationProblemDetails` |
| Auth fail | `401` / `403` | empty or `ProblemDetails` |

## Don't

- ❌ DbContext directly in Controller — call a Service
- ❌ Return Entity raw — always map to DTO first
- ❌ Throw exceptions for business errors — return `BadRequest()`
- ❌ Skip pagination on list endpoints
