---
name: generate-tests
description: |
  Use this skill when the user asks to generate or update unit tests
  for a service, validator, or controller.
  - generates xUnit + Moq + FluentAssertions
  - follows the team naming and AAA pattern
  - covers happy path, edge cases, exceptions
---

# Generate Tests Skill

## When to use
- The user names a file or service and asks for tests
- The user asks to raise coverage above a threshold

## Workflow
1. Read the target file and its public methods
2. Read `tests.instructions.md` for naming and structure rules
3. Generate AAA tests: 1 happy + 2 edge + 1 exception per public method
4. Place the test in `Tests/<Name>Tests.cs`
5. Run `dotnet test --no-build`

## Output
A single test file. Constructor sets up the SUT and mocks once.
Every async test passes a `CancellationToken` and asserts that it propagates to the mocked dependency.
