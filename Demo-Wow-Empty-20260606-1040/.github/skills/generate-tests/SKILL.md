---
name: generate-tests
description: |
  Generate xUnit (BE) + Vitest (FE hooks/components) + Playwright (FE e2e smoke)
  tests with FULL endpoint + route coverage. Use this skill when:
  - Implementation phase (3a + 3b) is done
  - TODO.md has unchecked [TEST] items
  - User says "test this", "generate tests", "/generate-tests"

  MANDATORY gates:
  - BE: 100% endpoint coverage (every public route has ≥ 1 integration test)
  - BE: ≥ 80% line, ≥ 70% branch
  - FE: 100% route smoke coverage (Playwright @smoke for every <Route> in App.tsx)
  - FE: ≥ 80% hook coverage
  - All tests must PASS (no skipped/expected-failure tests)

  Self-discovers targets via: `git ls-files ShopApi/Controllers/`, parsing App.tsx
  routes, `git ls-files ShopWeb/src/features/*/hooks.ts`. Loops until all gates green.
---

# generate-tests

Generate comprehensive test suite covering EVERY public endpoint + EVERY page route + hooks. 100% coverage gates are non-negotiable in autonomous mode.

## When to use

- After `scaffold-feature` (BE) AND `implement-frontend` (FE, if fullstack) finish
- TODO.md has unchecked [TEST] items
- Smoke gate in Phase 7 reports missing test coverage

## Discovery (do this FIRST)

Before writing tests, enumerate what MUST be tested:

```bash
# BE: every Controller action
grep -rEn "\[Http(Get|Post|Put|Patch|Delete)\]" ShopApi/Controllers/
# Each match = one endpoint → must have ≥ 1 happy + 1 error test

# BE: every public service method
grep -rEn "public.*Async\(" ShopApi/Services/

# FE: every route (look in App.tsx + RootLayout / nested routers)
grep -En "<Route\s+(path|index)" ShopWeb/src/App.tsx ShopWeb/src/**/*.tsx

# FE: every hook
ls ShopWeb/src/features/*/hooks.ts

# Spec: every Feature listed in spec → 1 Playwright integration test
grep -En "^## What|^## Feature|^- \*\*AC-" feature-specs/*.spec.md
```

Output a TEST PLAN before writing:
```
TEST PLAN (gates):
  BE endpoints discovered      : 12 → need 12+ happy + 12+ error tests
  BE services discovered       : 4  → need 4+ unit suites
  FE routes discovered         : 7  → need 7+ Playwright @smoke
  FE hooks discovered          : 4  → need 4+ Vitest hook tests
  Spec features discovered     : 6  → need 6+ Playwright integration tests (1 per feature)
Total target test files        : ~15-20
```

If a target has NO test by phase end → gate fails → ADD missing test, do not proceed.

## Test file layout

```
ShopApi.Tests/
├── ControllerTests/
│   ├── ProductsControllerTests.cs    (1 per controller, integration via WebApplicationFactory)
│   ├── CartControllerTests.cs
│   └── OrdersControllerTests.cs
├── ServiceTests/
│   ├── ProductServiceTests.cs        (unit)
│   ├── CartServiceTests.cs
│   └── OrderServiceTests.cs
└── Helpers/
    └── TestWebApplicationFactory.cs

ShopWeb/
├── src/
│   ├── features/products/__tests__/
│   │   ├── hooks.test.ts             (Vitest unit)
│   │   ├── ProductCard.test.tsx      (Vitest component)
│   │   └── ProductGallery.test.tsx
│   ├── features/cart/__tests__/
│   │   └── hooks.test.ts
│   └── ...
└── tests-e2e/                        (Playwright)
    ├── _setup.ts                     (shared fixtures)
    ├── home.spec.ts                  (@smoke @feature)
    ├── search.spec.ts                (@feature)
    ├── product-detail.spec.ts        (@feature — image gallery interaction)
    ├── cart.spec.ts                  (@feature)
    ├── checkout.spec.ts              (@feature)
    └── admin.spec.ts                 (@feature)
```

## Mandatory Playwright integration suite

Every feature in spec → 1 `.spec.ts` file in `tests-e2e/`. Each file MUST:

1. Tag: `test.describe('@feature <Name>', ...)` so trainer can filter
2. Console error gate: fail on any `pageerror` or `console.error` during the test
3. Image integrity: assert `naturalWidth > 0` for all `<img>` in viewport (catches broken seed URLs)
4. **Network gate: capture all `/api/*` requests, fail on any 4xx OR 5xx that is not part of a test for invalid input** (4xx = contract mismatch, the #1 cause of broken integration)
5. Cleanup: don't leak DB state (e.g. delete created products in `afterEach` for Admin test)

### Console + network error gate (use in every test)

```ts
import { test, expect, type Page } from '@playwright/test';

export function attachErrorGates(page: Page, opts: { allowStatuses?: number[] } = {}) {
  const consoleErrors: string[] = [];
  const networkErrors: { url: string; status: number; body?: string }[] = [];
  const allow = new Set(opts.allowStatuses ?? []);

  page.on('pageerror', e => consoleErrors.push(`pageerror: ${e.message}`));
  page.on('console', m => { if (m.type() === 'error') consoleErrors.push(`console: ${m.text()}`); });
  page.on('response', async r => {
    if (!r.url().includes('/api/')) return;
    if (r.status() < 400) return;
    if (allow.has(r.status())) return;     // expected (e.g. a deliberate validation test)
    let body: string | undefined;
    try { body = (await r.text()).slice(0, 300); } catch { /* ignore */ }
    networkErrors.push({ url: r.url(), status: r.status(), body });
  });

  return { consoleErrors, networkErrors };
}

// In each happy-path test:
test('feature works', async ({ page }) => {
  const gates = attachErrorGates(page);  // any 4xx/5xx fails the test
  // ... test body — happy path only ...
  expect(gates.consoleErrors, 'console errors detected').toEqual([]);
  expect(gates.networkErrors, 'API error responses detected').toEqual([]);
});

// In tests that DELIBERATELY trigger 400 (validation tests):
test('checkout shows field error on invalid email', async ({ page }) => {
  const gates = attachErrorGates(page, { allowStatuses: [400] });
  // ... submit invalid form ...
  // The 400 is expected and allow-listed; everything else still fails the test.
});
```

Put `attachErrorGates` in `tests-e2e/_setup.ts` and import in every spec. **Default to no `allowStatuses`** — only opt in when a test is intentionally exercising validation.

### Contract integration tests (Vitest — REQUIRED per mutation feature)

Every form/mutation feature MUST have a Vitest test that exercises the **real** axios client + a mocked HTTP layer using the EXACT JSON shape from `CONTRACT.md`. This catches divergence between Zod and FluentValidation before it ever reaches Playwright.

```ts
// ShopWeb/src/features/checkout/__tests__/contract.test.ts
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { server } from '@/test/server';  // msw setup
import { http, HttpResponse } from 'msw';
import { createOrder } from '../api';

describe('checkout contract', () => {
  it('sends the exact shape CONTRACT.md documents for POST /api/orders', async () => {
    let receivedBody: any;
    server.use(http.post('*/api/orders', async ({ request }) => {
      receivedBody = await request.json();
      return HttpResponse.json({ orderId: 'ord_x', total: 1, createdAt: '2026-01-01T00:00:00Z' }, { status: 201 });
    }));

    await createOrder({
      items: [{ productId: 'p1', quantity: 1 }],
      customer: { name: 'Nguyen Van A', email: 'a@b.com', phone: '0901234567', address: '1 Le Loi' },
      note: undefined,
    });

    // Field names — must match CONTRACT.md camelCase exactly
    expect(receivedBody).toHaveProperty('items');
    expect(receivedBody).toHaveProperty('customer.email');
    expect(receivedBody).toHaveProperty('customer.phone');
    // Forbidden — no PascalCase, no extra fields
    expect(receivedBody).not.toHaveProperty('Items');
    expect(receivedBody).not.toHaveProperty('customerEmail');
  });

  it('surfaces BE field errors back to the form', async () => {
    server.use(http.post('*/api/orders', () => HttpResponse.json({
      type: 'validation', title: 'Validation', status: 400,
      errors: { 'customer.email': ["'customer.email' is not a valid email address."] }
    }, { status: 400 })));

    await expect(createOrder({ /* ... */ } as any)).rejects.toThrow(/customer\.email/);
  });
});
```

If your project does not have msw set up, install it as a dev dep first: `npm install -D msw@2.4.0`. Add `src/test/server.ts` with `setupServer()` + `beforeAll/afterEach/afterAll` lifecycle in `src/test/setup.ts`.

**Coverage gate (mandatory):** every feature that has a `mutationFn` in `hooks.ts` MUST have at least one matching `contract.test.ts` file.

## Inputs

1. Target file (`#file:Services/ProductService.cs` or similar)
2. `copilot-instructions.md` (for test convention)
3. (Optional) `TODO.md` to identify which test TODO

## Output

Single test file: `tests/<ClassName>Tests.cs`

### File structure

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ShopApi.Tests;

public class ProductServiceTests
{
    private static AppDbContext CreateDb() { ... }
    private static List<Product> SeedProducts() { ... }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsMatchingProducts() { ... }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsValidationError() { ... }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    public async Task SearchAsync_WithInvalidQuery_Throws(string query) { ... }

    [Fact]
    public async Task SearchAsync_WithCancellation_StopsExecution() { ... }
}
```

## Required cases per public method

| # | Type | Example |
|---|---|---|
| 1 | Happy path | Valid input → expected output |
| 2 | Null input | Throw `ArgumentNullException` |
| 3 | Empty input | Empty list / validation error |
| 4 | Boundary low | 0, 1, min |
| 5 | Boundary high | max, max+1 |
| 6 | Special chars (string) | Unicode, SQL-injection-like input |
| 7 | Exception | Dependency throws → propagate |
| 8 | Cancellation | `CancellationToken` triggered → stop |

## Naming convention (MANDATORY)

`{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- ✅ `SearchAsync_WithValidQuery_ReturnsMatchingProducts`
- ✅ `SearchAsync_WhenDbFails_PropagatesException`
- ❌ `TestSearch`, `Test1`, `SearchTest`

## Assertion style (MANDATORY)

Use FluentAssertions:
```csharp
result.Items.Should().HaveCountGreaterThan(0);
result.Items.Should().AllSatisfy(p =>
    p.Name.Should().ContainEquivalentOf("phone"));

await act.Should().ThrowAsync<ArgumentException>()
    .WithMessage("*query*");
```

Don't use:
- `Assert.True(condition)` ❌
- `Assert.Equal(expected, actual)` ❌
- `Assert.NotNull(x)` ❌

## Mock strategy

| Dependency | Approach |
|---|---|
| `DbContext` | `UseInMemoryDatabase(Guid.NewGuid())` — NEVER mock |
| `HttpClient` | Mock `HttpMessageHandler` via Moq |
| Time-dependent | Inject `TimeProvider`, use `FakeTimeProvider` |
| `IConfiguration` | Build test config in-memory |
| `ILogger<T>` | Real `NullLogger<T>` |

## Triggers

- "/generate-tests ClassName"
- "/generate-tests #file:..."
- "generate tests for this"
- "use generate-tests skill"
- `@tester` agent

## Don't

- Don't use `Thread.Sleep` (flaky)
- Don't use `DateTime.Now` in tests (flaky)
- Don't mock `DbContext` (use InMemory)
- Don't skip cancellation test for async methods
- Don't modify production code to make tests pass
- Don't `[Fact(Skip = "...")]` failing tests to fake green

## Self-heal (autonomous mode)

If a test fails: read assertion output → determine if test or source is wrong per DESIGN.md/spec ACs → patch the correct side → re-run (max 2 retries per test class). If still red after retries → escalate per `deliver-feature/SKILL.md` Autonomous Mode. NEVER use `Skip` to bypass.
