---
applyTo: "**/*Tests.cs"
---

# Test convention

These rules apply only to test files (`*Tests.cs`).

## Naming

- Class: `{Sut}Tests` (e.g., `ProductServiceTests`)
- Method: `{Method}_{Scenario}_{Expected}`

## Style

- Use **FluentAssertions** for asserts — never `Assert.True/Equal/NotNull`
- Use `[Fact]` for single test, `[Theory]` + `[InlineData]` for parameterized
- Use `Guid.NewGuid()` for InMemory DB names to isolate tests
- `sut` = system under test, `_xxxMock` for Moq objects

## Forbidden

- ❌ `Thread.Sleep` — use `Task.Delay` or fake `TimeProvider`
- ❌ `DateTime.Now` / `DateTime.UtcNow` — inject `TimeProvider`
- ❌ Mock `DbContext` — use `UseInMemoryDatabase`
- ❌ Tests depending on order of execution

## Coverage per public method

- 1 happy path
- 2 edge cases (null, empty, boundary)
- 1 exception case
- 1 cancellation case (if async)
