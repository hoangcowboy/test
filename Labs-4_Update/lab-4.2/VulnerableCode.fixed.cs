// Lab 4.2 — Answer Key: Fixed version
// Tất cả vulnerability đã được fix theo OWASP best practice.

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ganss.Xss; // HtmlSanitizer package

namespace TrainingLab.Security.Fixed;

[Authorize]  // A01 fix
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly HtmlSanitizer _sanitizer = new();
    private readonly HttpClient _http;

    public AdminController(AppDbContext db, IConfiguration config, HttpClient http)
    {
        _db = db;
        _config = config;
        _http = http;
    }

    // A03 fix: parameterized query via FindAsync (LINQ-translated)
    // A01 fix: check ownership / role
    [HttpGet("/admin/user/{id}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken ct)
    {
        var currentUserId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
        if (id != currentUserId && !User.IsInRole("Admin"))
            return Forbid();

        var user = await _db.Users.FindAsync(new object[] { id }, ct);
        if (user is null) return NotFound();
        return Ok(new { user.Id, user.Email, user.FullName });
    }

    // A03 fix: sanitize HTML before storing
    [HttpPost("/comment")]
    public async Task<IActionResult> PostComment([FromBody] CommentDto dto, CancellationToken ct)
    {
        var sanitized = _sanitizer.Sanitize(dto.Body);
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Html = sanitized,
            CreatedAt = DateTime.UtcNow
        };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    // A03 fix: return as JSON, don't render as HTML
    [HttpGet("/admin/comments")]
    public async Task<IActionResult> GetComments(CancellationToken ct)
    {
        var comments = await _db.Comments
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, c.Html, c.CreatedAt })
            .ToListAsync(ct);
        return Ok(comments);  // JSON, not raw HTML
    }

    // A02 fix: BCrypt với work factor
    public string HashPassword(string pwd)
        => BCrypt.Net.BCrypt.HashPassword(pwd, workFactor: 12);

    public bool VerifyPassword(string pwd, string hash)
        => BCrypt.Net.BCrypt.Verify(pwd, hash);

    // A02 fix: cryptographically secure RNG
    public string GenerateResetToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    // A08 fix: BinaryFormatter REMOVED. Use System.Text.Json or specific deserializer.
    [HttpPost("/import")]
    public async Task<IActionResult> Import([FromBody] ImportDto dto, CancellationToken ct)
    {
        // dto: strongly typed, validated by model binder
        // Process as needed...
        return Ok(new { received = dto.Items.Count });
    }

    // A10 fix: validate URL against allowlist
    private static readonly HashSet<string> AllowedHosts =
        new(StringComparer.OrdinalIgnoreCase) { "api.partner.com", "cdn.partner.com" };

    [HttpGet("/proxy")]
    public async Task<IActionResult> Proxy([FromQuery] string url, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return BadRequest("Invalid URL");
        if (uri.Scheme != "https" || !AllowedHosts.Contains(uri.Host))
            return BadRequest("URL host not allowed");

        var response = await _http.GetStringAsync(uri, ct);
        return Ok(response);  // return as JSON, not raw HTML
    }

    // A09 fix: structured logging without sensitive data
    [HttpPost("/login")]
    public IActionResult Login([FromBody] LoginDto dto, ILogger<AdminController> logger)
    {
        logger.LogInformation("Login attempt for email {Email}", dto.Email);
        // Auth logic — DO NOT log password
        return Ok();
    }
}

public record CommentDto(string Body);
public record LoginDto(string Email, string Password);
public record ImportItemDto(string Name, decimal Value);
public record ImportDto(List<ImportItemDto> Items);

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
}

public class Comment
{
    public Guid Id { get; set; }
    public string Html { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Comment> Comments => Set<Comment>();
}
