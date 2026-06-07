# Copilot Instructions — ShopApi Team

## Stack
- .NET 8, C# 12
- ASP.NET Core minimal API
- EF Core 8 with SQL Server / SQLite
- xUnit + Moq + FluentAssertions

## Code Style
- File-scoped namespaces
- `var` only when the right-hand side makes the type obvious
- Async all the way — public async methods end in `Async`
- `CancellationToken` on every public async method
- Records for immutable DTOs
- Class with primary constructor when there is a dependency

## Naming
- Service: `I{Name}Service` and `{Name}Service`
- Folder: `PascalCase`, plural for `Models/`, `Services/`
- Private field: `_camelCase`
- Constant: `PascalCase`

## Layering
- Controllers stay thin — they orchestrate
- Services hold business logic
- Introduce a repository only when tests need to mock the data layer
- DI lifecycle: `Scoped` for services/repositories, `Singleton` for options

## Error Handling
- Business errors flow as a Result pattern or a custom exception
- Validation errors return ProblemDetails (RFC 7807)
- Log exceptions through `ILogger` with structured logging

## Forbidden
- `BinaryFormatter`
- `JsonConvert.SerializeObject` (use `System.Text.Json`)
- `.Result` or `.Wait()` in async code
- `DateTime.Now` (always `UtcNow`)
