# Lab 4.1 — Sample PR với 7 issues

PR mẫu cho lab AI Code Review. Có 5 file thay đổi, chứa **7 vấn đề cố ý** với mức độ khó khác nhau.

> Lưu ý: "review zero-config" trong lab là Built-in Copilot Code Review (nút ở Source Control hoặc chuột phải > Copilot > Review). Copilot KHÔNG có slash command `/review` trong chat.

## Thiết kế: 7 issues

| # | File | Loại | Severity | Built-in review thường bắt được? |
|---|---|---|---|---|
| 1 | `UserService.cs:23` | SQL Injection | Block | Có |
| 2 | `OrderController.cs:45` | N+1 query | Suggest | Đôi khi |
| 3 | `ShippingService.cs:67` | Magic number | Nit | Có |
| 4 | `EmailSender.cs:30` | Missing CancellationToken | Suggest | Đôi khi |
| 5 | `appsettings.json:8` | Hardcoded secret | Block | Có |
| 6 | `CounterService.cs:15` | Race condition | Block | Hiếm |
| 7 | `ProductMapper.cs:18` | Null reference | Suggest | Đôi khi |

**Mục đích của lab:** chứng minh built-in Copilot Code Review bắt ~3-4 issues, custom prompt `review-pr.prompt.md` bắt 6-7.

## Code mẫu cho từng issue

### 1. `Services/UserService.cs` — SQL Injection
```csharp
using Microsoft.EntityFrameworkCore;

public class UserService(AppDbContext db)
{
    public User? GetByEmail(string email)
    {
        // Issue 1: SQL injection - email từ user input, string interpolation vào SQL
        var sql = $"SELECT * FROM Users WHERE Email = '{email}'";
        return db.Users.FromSqlRaw(sql).FirstOrDefault();
    }
}
```

### 2. `Controllers/OrderController.cs` — N+1 query
```csharp
[HttpGet("with-customer")]
public async Task<IActionResult> GetOrdersWithCustomers(CancellationToken ct)
{
    var orders = await _db.Orders.ToListAsync(ct);
    // Issue 2: N+1 - mỗi order 1 query Customer
    foreach (var o in orders)
    {
        o.Customer = await _db.Customers.FindAsync([o.CustomerId], ct);
    }
    return Ok(orders);
}
```

### 3. `Services/ShippingService.cs` — Magic number
```csharp
public decimal CalculateFee(decimal weight)
{
    // Issue 3: Magic number 30 — không có constant/comment
    if (weight > 30) return -1;
    return weight * 10000;
}
```

### 4. `Infrastructure/EmailSender.cs` — Missing CancellationToken
```csharp
public class EmailSender : IEmailSender
{
    // Issue 4: async method nhưng không nhận/truyền CancellationToken
    public async Task SendAsync(string to, string subject, string body)
    {
        using var client = _httpFactory.CreateClient("smtp");
        await client.PostAsJsonAsync("/send", new { to, subject, body });
    }
}
```

### 5. `appsettings.json` — Hardcoded secret
```json
{
  "ConnectionStrings": {
    "Default": "Server=tcp:prod-sql.database.windows.net,1433;Database=ShopDb;User=admin;Password=Pr0d-P@ss-2024!;"
  },
  "Jwt": {
    "SigningKey": "this-is-a-very-long-static-jwt-signing-key-shhhh"
  }
}
```
**Issue 5:** Production credentials commit vào git.

### 6. `Services/CounterService.cs` — Race condition
```csharp
public class CounterService
{
    // Issue 6: not thread-safe; concurrent Increment có thể lose update
    public int Count { get; private set; }

    public void Increment() => Count++;
}
```
Fix: dùng `Interlocked.Increment(ref _count)` với `private int _count`.

### 7. `Mappers/ProductMapper.cs` — Null reference
```csharp
public class ProductMapper
{
    // Issue 7: p.Category có thể null (nếu không Include) → NRE
    public string GetCategoryName(Product p) => p.Category.Name;
}
```

## Setup repo (chạy cục bộ, không cần push)

`sample-repo` đã có sẵn 7 bug. Chỉ cần init git và stage để built-in review có diff:

```powershell
cd Assets\labs\lab-4.1\sample-repo
git init
git add .
# KHÔNG commit, KHÔNG push, KHÔNG cần PR thật — built-in review đọc uncommitted changes cục bộ
```

## Workflow lab

1. `git init` + `git add .` (không commit) trong `sample-repo`
2. Trainer dùng built-in Copilot Code Review (nút Source Control / chuột phải > Copilot > Review) → đếm bao nhiêu issue AI bắt được
3. Học viên tạo `review-pr.prompt.md` với checklist team
4. Invoke: `/review-pr #file:Services/UserService.cs #file:Controllers/OrderController.cs ...`
5. Đếm lại → so sánh
6. Refine prompt nếu miss issue

## Answer key

7 issues + fix:

```csharp
// Issue 1 fix:
public User? GetByEmail(string email)
    => db.Users.FirstOrDefault(u => u.Email == email);

// Issue 2 fix:
var orders = await _db.Orders.Include(o => o.Customer).ToListAsync(ct);

// Issue 3 fix:
private const decimal MaxShipWeightKg = 30m;
if (weight > MaxShipWeightKg) return -1;

// Issue 4 fix:
public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
{
    using var client = _httpFactory.CreateClient("smtp");
    await client.PostAsJsonAsync("/send", new { to, subject, body }, ct);
}

// Issue 5 fix:
// Move secret to Key Vault / User Secrets / env var.
// appsettings.json: chỉ giữ placeholder "ConnectionStrings:Default": ""

// Issue 6 fix:
public class CounterService
{
    private int _count;
    public int Count => _count;
    public void Increment() => Interlocked.Increment(ref _count);
}

// Issue 7 fix:
public string GetCategoryName(Product p) => p.Category?.Name ?? "(unknown)";
```
