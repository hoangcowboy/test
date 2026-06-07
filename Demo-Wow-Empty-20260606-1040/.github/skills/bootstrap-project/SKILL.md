---
name: bootstrap-project
description: |
  Initialize a .NET 8 solution from scratch with ShopApi + ShopApi.Tests projects,
  install required NuGet packages, and create the minimum baseline (Product entity,
  AppDbContext, basic CRUD service/controller). Use this skill when:
  - The workspace contains `.github/` but no `.csproj` / `.sln` exists
  - User says "bootstrap", "init project", "/bootstrap-project"
  - As the first step of `bootstrap-and-deliver` orchestration

  Skip when an existing `.sln` is found in the workspace root.
---

# bootstrap-project

Initialize the workspace from an empty state to a buildable .NET 8 solution that matches `copilot-instructions.md` conventions.

## Pre-conditions

- Workspace contains `.github/copilot-instructions.md`
- Workspace does NOT contain `.sln` or `.csproj`

If pre-conditions fail (project exists), output: "Skipped: project already initialized" and stop.

## Required terminal commands (run in workspace root)

Run these one by one. After each, verify exit code = 0 before continuing.

```bash
# 1. Solution
dotnet new sln -n ShopApi

# 2. Web API project
dotnet new webapi -n ShopApi -o ShopApi --use-controllers -f net8.0
dotnet sln add ShopApi/ShopApi.csproj

# 3. Test project
dotnet new xunit -n ShopApi.Tests -o ShopApi.Tests -f net8.0
dotnet sln add ShopApi.Tests/ShopApi.Tests.csproj
dotnet add ShopApi.Tests/ShopApi.Tests.csproj reference ShopApi/ShopApi.csproj

# 4. NuGet packages — main project
cd ShopApi
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.8
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.8
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.8
dotnet add package Swashbuckle.AspNetCore --version 6.6.2
dotnet add package FluentValidation.AspNetCore --version 11.3.0
dotnet add package FluentValidation.DependencyInjectionExtensions --version 11.9.2
cd ..

# 5. NuGet packages — test project
cd ShopApi.Tests
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Moq --version 4.20.70
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.8
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.8
dotnet add package coverlet.collector --version 6.0.2
cd ..
```

## Files to create (after terminal commands)

Create these files with content matching `copilot-instructions.md` convention. They are the **minimum baseline** for CRUD on Product — the feature spec will add Search on top.

### `ShopApi/Models/Product.cs`

```csharp
namespace ShopApi.Models;

public class Product
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public required string Url { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class Category
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
```

### `ShopApi/Data/AppDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using ShopApi.Models;

namespace ShopApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Product>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Description).HasMaxLength(2000);
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.HasMany(p => p.Images).WithOne(i => i.Product!).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ProductImage>(e =>
        {
            e.Property(i => i.Url).HasMaxLength(500);
            e.Property(i => i.AltText).HasMaxLength(200);
            e.HasIndex(i => new { i.ProductId, i.DisplayOrder });
        });
        builder.Entity<Category>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100);
            e.Property(c => c.Slug).HasMaxLength(100);
            e.HasIndex(c => c.Slug).IsUnique();
        });
    }
}
```

### `ShopApi/Data/Seed.cs` (IDEMPOTENT, ≥ 100 products with REAL Unsplash images)

```csharp
using Microsoft.EntityFrameworkCore;
using ShopApi.Models;

namespace ShopApi.Data;

public static class Seed
{
    // Each product entry: name + Unsplash query keywords (give relevant real photos)
    // We use Unsplash Source API: https://source.unsplash.com/featured/600x600/?<keywords>
    // Returns a real photo matching keywords. Multiple calls with same query → different photos.
    // Free, no API key. Fallback to placehold.co text if Unsplash down.
    private record ProductTemplate(string Name, string Description, string[] Keywords);

    private static readonly Dictionary<string, ProductTemplate[]> Catalog = new()
    {
        ["Electronics"] = new ProductTemplate[] {
            new("iPhone 16 Pro",          "Apple flagship — A18 Pro chip, 48MP camera, titanium frame.",      new[]{"iphone","smartphone","apple"}),
            new("Samsung Galaxy S24",     "Snapdragon 8 Gen 3, AMOLED 120Hz, AI photography.",                new[]{"samsung","galaxy","android-phone"}),
            new("MacBook Air M3",         "13-inch, M3 chip, 18-hour battery, ultra-thin.",                   new[]{"macbook","laptop","apple-laptop"}),
            new("iPad Pro 12.9",          "M2 chip, Liquid Retina XDR, ProMotion 120Hz.",                     new[]{"ipad","tablet","apple-tablet"}),
            new("AirPods Pro 2",          "Active noise cancellation, spatial audio, USB-C case.",            new[]{"airpods","earbuds","wireless-earphones"}),
            new("Sony WH-1000XM5",        "Industry-leading ANC, 30-hour battery, hi-res audio.",             new[]{"sony","headphones","over-ear"}),
            new("Dell XPS 15",            "OLED 4K, RTX 4070, creator workstation.",                          new[]{"dell-laptop","ultrabook","workstation"}),
            new("Logitech MX Master 3S",  "Ergonomic wireless mouse, 8K DPI, quiet click.",                   new[]{"mouse","wireless-mouse","computer-mouse"}),
            new("LG OLED C3 55\"",        "4K OLED, α9 AI Processor Gen6, Dolby Vision IQ.",                  new[]{"oled-tv","television","smart-tv"}),
            new("Bose SoundLink Mini",    "Portable Bluetooth speaker, deep bass, 12-hour battery.",          new[]{"bluetooth-speaker","portable-speaker","bose"}),
        },
        ["Fashion"] = new ProductTemplate[] {
            new("Cotton T-Shirt",         "100% organic cotton, breathable, classic fit.",                    new[]{"t-shirt","cotton-shirt","apparel"}),
            new("Slim-fit Jeans",         "Premium denim, stretch fabric, modern cut.",                       new[]{"jeans","denim","blue-jeans"}),
            new("Leather Jacket",         "Genuine lambskin, biker-style, YKK zippers.",                      new[]{"leather-jacket","biker-jacket","fashion-outerwear"}),
            new("Wool Coat",              "Premium merino wool blend, tailored fit, mid-length.",             new[]{"wool-coat","winter-coat","overcoat"}),
            new("Sneakers Trail",         "Vibram outsole, breathable mesh, all-terrain.",                    new[]{"sneakers","trail-running-shoes","outdoor-shoes"}),
            new("Running Shoes",          "Carbon plate, responsive foam, race-day performance.",             new[]{"running-shoes","athletic-shoes","sport-shoes"}),
            new("Casual Hoodie",          "Fleece-lined, drawstring hood, kangaroo pocket.",                  new[]{"hoodie","pullover","sweatshirt"}),
            new("Denim Shirt",            "Mid-weight denim, button-down, slim fit.",                         new[]{"denim-shirt","button-shirt","mens-shirt"}),
            new("Linen Pants",            "Lightweight linen, relaxed fit, summer essential.",                new[]{"linen-pants","trousers","summer-pants"}),
            new("Cashmere Sweater",       "100% cashmere, crew neck, soft and warm.",                         new[]{"cashmere-sweater","knit-sweater","wool-pullover"}),
        },
        ["Home & Kitchen"] = new ProductTemplate[] {
            new("Pressure Cooker 6L",     "Stainless steel, 10 safety mechanisms, induction-ready.",          new[]{"pressure-cooker","cookware","kitchen-appliance"}),
            new("Espresso Machine",       "15-bar pump, milk frother, café-quality at home.",                 new[]{"espresso-machine","coffee-maker","barista"}),
            new("Air Fryer 5L",           "Rapid air circulation, 8 presets, healthier frying.",              new[]{"air-fryer","kitchen-gadget","fryer"}),
            new("Stand Mixer",            "5-speed, 5L bowl, dough hook + whisk + paddle.",                   new[]{"stand-mixer","mixer","kitchenaid"}),
            new("Blender Pro",            "1500W motor, 6 stainless blades, ice-crushing.",                   new[]{"blender","smoothie-blender","kitchen-blender"}),
            new("Knife Set 8pc",          "German steel, full tang, ergonomic handles, knife block.",         new[]{"knife-set","kitchen-knives","chef-knife"}),
            new("Cast Iron Pan",          "Pre-seasoned, 12-inch, lasts a lifetime.",                         new[]{"cast-iron-pan","skillet","frying-pan"}),
            new("Dinner Set 16pc",        "Porcelain, microwave + dishwasher safe, 4 settings.",              new[]{"dinner-plates","plates","ceramics"}),
            new("Memory Foam Pillow",     "Contour neck support, cooling gel layer, hypoallergenic.",         new[]{"pillow","bedding","bedroom"}),
            new("Bedsheet Cotton 1000TC", "Egyptian cotton, sateen weave, deep pocket.",                      new[]{"bedsheet","linen-sheets","bedding"}),
        },
        ["Books"] = new ProductTemplate[] {
            new("Atomic Habits",          "James Clear — tiny changes, remarkable results.",                  new[]{"book","self-help-book","reading"}),
            new("Clean Code",             "Robert C. Martin — handbook of agile software craftsmanship.",     new[]{"programming-book","tech-book","coding"}),
            new("The Pragmatic Programmer","Hunt & Thomas — from journeyman to master.",                       new[]{"programmer-book","developer","software"}),
            new("Design Patterns",        "GoF — elements of reusable object-oriented software.",             new[]{"design-patterns","textbook","computer-science"}),
            new("Sapiens",                "Yuval Noah Harari — a brief history of humankind.",                new[]{"history-book","sapiens","non-fiction"}),
            new("Educated",               "Tara Westover — a memoir.",                                        new[]{"memoir","biography","book-cover"}),
            new("Project Hail Mary",      "Andy Weir — interstellar science fiction adventure.",              new[]{"science-fiction","novel","space-book"}),
            new("Dune",                   "Frank Herbert — desert planet sci-fi epic.",                       new[]{"dune","sci-fi-book","fantasy"}),
            new("The Hobbit",             "J.R.R. Tolkien — Middle-earth adventure begins.",                  new[]{"fantasy-book","tolkien","adventure-book"}),
            new("Foundation",             "Isaac Asimov — psychohistory and the fall of empire.",             new[]{"asimov","science-fiction","classic-novel"}),
        },
        ["Sports"] = new ProductTemplate[] {
            new("Yoga Mat 6mm",           "Non-slip TPE, eco-friendly, alignment lines.",                     new[]{"yoga-mat","yoga","fitness-mat"}),
            new("Dumbbells 10kg Pair",    "Rubber-coated hex, anti-roll, knurled grip.",                      new[]{"dumbbells","weights","gym-equipment"}),
            new("Resistance Bands Set",   "5 levels, latex, door anchor + handles.",                          new[]{"resistance-bands","workout-bands","fitness"}),
            new("Foam Roller High-Density","18-inch, EVA foam, muscle recovery.",                              new[]{"foam-roller","recovery","massage"}),
            new("Football Pro Match",     "FIFA Quality Pro, hand-stitched, size 5.",                         new[]{"football","soccer-ball","sports-ball"}),
            new("Tennis Racket Pro",      "300g, graphite frame, oversize head.",                             new[]{"tennis-racket","racquet","tennis"}),
            new("Cycling Helmet",         "MIPS protection, 22 vents, dial-fit retention.",                   new[]{"cycling-helmet","bike-helmet","bicycle"}),
            new("Swimming Goggles",       "Anti-fog, UV protection, silicone gaskets.",                       new[]{"swim-goggles","swimming","pool"}),
            new("Hiking Backpack 50L",    "Internal frame, rain cover, ventilated back.",                     new[]{"hiking-backpack","trekking-bag","outdoor-gear"}),
            new("Camping Tent 4-Person",  "Double-wall, 3-season, vestibule + footprint.",                    new[]{"tent","camping-tent","camping"}),
        },
        ["Beauty"] = new ProductTemplate[] {
            new("Hydrating Serum",        "Hyaluronic acid + Vitamin B5, 30ml.",                              new[]{"serum","skincare","cosmetics"}),
            new("SPF 50 Sunscreen",       "Broad-spectrum, water-resistant, no white cast.",                  new[]{"sunscreen","spf","beauty-product"}),
            new("Vitamin C Cream",        "20% Vitamin C + Ferulic acid, brightening, 50ml.",                 new[]{"face-cream","vitamin-c","skincare-bottle"}),
            new("Hair Dryer Ionic",       "2200W, ionic technology, 3 heat + 2 speed.",                       new[]{"hair-dryer","blow-dryer","hair-tool"}),
            new("Electric Toothbrush",    "Sonic 40,000 vibrations/min, 4 modes, 14-day battery.",            new[]{"electric-toothbrush","toothbrush","dental"}),
            new("Razor 5-Blade",          "Precision trimmer, lubricating strip, ergonomic handle.",          new[]{"razor","shaving","grooming"}),
            new("Perfume EDT 100ml",      "Eau de toilette, oriental notes, long-lasting.",                   new[]{"perfume","fragrance","cologne"}),
            new("Lipstick Matte",         "Velvet finish, 12-hour wear, vegan formula.",                      new[]{"lipstick","makeup","cosmetic"}),
            new("Eyeshadow Palette 18",   "18 shades, mix matte + shimmer, highly pigmented.",                new[]{"eyeshadow","palette","makeup-palette"}),
            new("Nail Polish Set 6",      "Quick-dry, chip-resistant, gel-like shine.",                       new[]{"nail-polish","manicure","beauty-set"}),
        },
        ["Toys"] = new ProductTemplate[] {
            new("Lego Classic 500pc",     "Creative building set, ages 4+, ideas booklet.",                   new[]{"lego","building-blocks","toy-bricks"}),
            new("Rubik's Cube 3x3",       "Speed-cubing, smooth rotation, vibrant colors.",                   new[]{"rubiks-cube","puzzle","cube"}),
            new("Board Game Strategy",    "2-5 players, 60-90 min, family fun.",                              new[]{"board-game","tabletop","game"}),
            new("Plush Bear 40cm",        "Ultra-soft, hypoallergenic, machine-washable.",                    new[]{"teddy-bear","plush-toy","stuffed-animal"}),
            new("RC Car 4WD",             "2.4GHz, 30km/h, off-road tires, rechargeable.",                    new[]{"rc-car","remote-control","toy-car"}),
            new("Drone Mini",             "HD camera, gravity sensor, beginner-friendly.",                    new[]{"drone","mini-drone","quadcopter"}),
            new("Puzzle 1000 pieces",     "Vintage world map, premium cardboard.",                            new[]{"jigsaw-puzzle","puzzle","brain-game"}),
            new("Action Figure",          "Articulated, 6-inch scale, collector's edition.",                  new[]{"action-figure","collectible","figurine"}),
            new("Wooden Train Set",       "60-piece, magnetic connectors, eco-friendly wood.",                new[]{"train-set","wooden-toy","kids-train"}),
            new("Building Blocks 200pc",  "STEM educational, ages 3+, mixed shapes.",                         new[]{"blocks","construction-toy","kids-blocks"}),
        },
        ["Grocery"] = new ProductTemplate[] {
            new("Organic Honey 500g",     "Raw, unfiltered, single-origin acacia.",                           new[]{"honey-jar","honey","organic-food"}),
            new("Olive Oil Extra Virgin", "Cold-pressed, first-press, 750ml glass bottle.",                   new[]{"olive-oil","oil-bottle","mediterranean"}),
            new("Dark Chocolate 70%",     "Single-origin, fair-trade, 100g bar.",                             new[]{"dark-chocolate","chocolate-bar","cocoa"}),
            new("Green Tea Premium",      "Loose-leaf, 100g, antioxidant-rich.",                              new[]{"green-tea","tea-leaves","tea"}),
            new("Coffee Beans Arabica",   "Single-origin Ethiopian, medium roast, 500g.",                     new[]{"coffee-beans","coffee","arabica"}),
            new("Almonds Raw 1kg",        "Premium California, no salt, vacuum-sealed.",                      new[]{"almonds","nuts","raw-almonds"}),
            new("Quinoa Organic 500g",    "Tri-color, gluten-free, high-protein.",                            new[]{"quinoa","grain","organic-grain"}),
            new("Pasta Italian 500g",     "Bronze-die, slow-dried, semolina.",                                new[]{"pasta","italian-pasta","spaghetti"}),
            new("Tomato Sauce 400g",      "San Marzano tomatoes, basil, no preservatives.",                   new[]{"tomato-sauce","pasta-sauce","jar"}),
            new("Sea Salt Fine 500g",     "Hand-harvested, mineral-rich, unrefined.",                         new[]{"sea-salt","salt","cooking-salt"}),
        },
        ["Automotive"] = new ProductTemplate[] {
            new("Car Phone Holder",       "Magnetic mount, 360° rotation, vent clip.",                        new[]{"car-phone-holder","car-mount","car-accessory"}),
            new("Dash Cam 1080p",         "G-sensor, loop recording, night vision.",                          new[]{"dash-cam","car-camera","drivecam"}),
            new("Tire Pressure Gauge",    "Digital, 0-150 PSI, LED display.",                                 new[]{"tire-gauge","car-tool","automotive-tool"}),
            new("Jump Starter 12V",       "Portable lithium, 2000A peak, USB-C.",                             new[]{"jump-starter","car-battery","portable-battery"}),
            new("Wax Polish 500ml",       "Carnauba wax, deep gloss, water-beading.",                         new[]{"car-wax","polish","detailing"}),
            new("Microfiber Cloth Pack",  "12-pack, 40x40cm, lint-free.",                                     new[]{"microfiber","cleaning-cloth","car-cleaning"}),
            new("Floor Mats Universal",   "All-weather, anti-slip, 4-piece set.",                             new[]{"floor-mats","car-mats","car-interior"}),
            new("Seat Cover Set",         "Universal fit, breathable mesh, full set.",                        new[]{"seat-cover","car-seat","upholstery"}),
            new("Air Freshener",          "Long-lasting, premium scent, vent clip.",                          new[]{"air-freshener","car-perfume","fragrance"}),
            new("OBD-II Scanner",         "Bluetooth, reads/clears codes, mobile app.",                       new[]{"obd-scanner","car-diagnostic","obd"}),
        },
        ["Garden"] = new ProductTemplate[] {
            new("Pruning Shears",         "Carbon steel blade, ergonomic grip, sap groove.",                  new[]{"pruning-shears","garden-tool","secateurs"}),
            new("Garden Hose 25m",        "Kink-resistant, brass fittings, 5-year warranty.",                 new[]{"garden-hose","watering","gardening"}),
            new("Lawn Mower Electric",    "1600W, 38cm cut, 50L grass collector.",                            new[]{"lawn-mower","mower","yard-tool"}),
            new("Compost Bin 100L",       "Aerated, easy-access door, weather-resistant.",                    new[]{"compost-bin","composter","gardening-bin"}),
            new("Seed Starter Kit",       "72 cells, humidity dome, heat mat.",                               new[]{"seedling","seed-tray","gardening"}),
            new("Watering Can 5L",        "Galvanized steel, rose head, ergonomic handle.",                   new[]{"watering-can","garden-watering","gardening-can"}),
            new("Garden Gloves",          "Leather palm, breathable, thorn-resistant.",                       new[]{"garden-gloves","gardening-gloves","work-gloves"}),
            new("Plant Pots Set 6",       "Terracotta, drainage holes, mixed sizes.",                         new[]{"plant-pots","terracotta","flower-pots"}),
            new("Solar Lights 4-pack",    "Auto on/off, weatherproof, 8-hour runtime.",                       new[]{"solar-lights","garden-lights","outdoor-lighting"}),
            new("Mulch Bag 50L",          "Cedar bark, weed suppression, soil moisture.",                     new[]{"mulch","garden-mulch","bark"}),
        },
    };

    public static async Task RunAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct)) return; // idempotent

        var categories = Catalog.Keys.Select(name => new Category {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLowerInvariant().Replace(" & ", "-").Replace(" ", "-")
        }).ToList();
        db.Categories.AddRange(categories);

        var products = new List<Product>();
        var rng = new Random(42); // deterministic
        foreach (var cat in categories)
        {
            foreach (var tpl in Catalog[cat.Name])
            {
                var slug = $"{cat.Slug}-{tpl.Name.ToLowerInvariant().Replace(" ", "-").Replace("\"", "").Replace("/", "-")}";
                var imgCount = rng.Next(2, 6); // 2..5 images per product
                var kwString = string.Join(",", tpl.Keywords);
                var product = new Product {
                    Id = Guid.NewGuid(),
                    Name = tpl.Name,
                    Description = tpl.Description,
                    Price = Math.Round((decimal)(rng.NextDouble() * 4_900_000 + 100_000), 0),
                    Stock = rng.Next(5, 100),
                    CategoryId = cat.Id,
                    Images = Enumerable.Range(0, imgCount).Select(i => new ProductImage {
                        Id = Guid.NewGuid(),
                        // Unsplash Source — keyword-based REAL photos, no API key needed.
                        // sig=N forces a different photo for the same keyword set.
                        Url = $"https://source.unsplash.com/600x600/?{kwString}&sig={slug}-{i}",
                        AltText = $"{tpl.Name} — view {i + 1}",
                        DisplayOrder = i,
                        IsPrimary = i == 0
                    }).ToList()
                };
                products.Add(product);
            }
        }
        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);
    }
}
```

Total: 10 categories × 10 products = **100 products**, each with 2-5 REAL product images via Unsplash Source API (keyword-matched, no API key).

> **Fallback policy:** Nếu Unsplash chậm/lỗi, FE phải có `onError` handler trên `<img>` → swap sang `https://placehold.co/600x600/eee/666?text={altText}`. Implement trong `<ProductImage>` wrapper component (FE side, xem implement-frontend SKILL).

### `ShopApi/DTOs/ProductDtos.cs`

```csharp
namespace ShopApi.DTOs;

public record ProductImageDto(Guid Id, string Url, string? AltText, int DisplayOrder, bool IsPrimary);

public record CreateProductDto(string Name, string? Description, decimal Price, int Stock, Guid CategoryId, IReadOnlyList<ProductImageDto>? Images);
public record UpdateProductDto(Guid Id, string Name, string? Description, decimal Price, int Stock, Guid CategoryId, IReadOnlyList<ProductImageDto>? Images);

public record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string CategoryName,
    IReadOnlyList<ProductImageDto> Images,
    string? PrimaryImageUrl,         // convenience for list cards (= first IsPrimary or first by order)
    DateTime CreatedAt);

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}
```

### `ShopApi/Services/IProductService.cs` + `ProductService.cs`

(Implement basic CRUD: List, GetById, Create, Update, Delete with `AsNoTracking`, `CancellationToken`, pagination cap.)

### `ShopApi/Controllers/ProductsController.cs`

(Thin controller calling `IProductService`, return `ActionResult<T>`, 201/204/404 status codes.)

### `ShopApi/Program.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using ShopApi.Data;
using ShopApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=shop.db"));
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for FE dev server
const string CorsPolicy = "AllowFE";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

// Apply migrations + seed (idempotent)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await Seed.RunAsync(db);
}

app.UseCors(CorsPolicy);
app.UseSwagger();
app.UseSwaggerUI(c => c.RoutePrefix = string.Empty);
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.Run();

public partial class Program { } // for WebApplicationFactory<Program> in tests
```

### `ShopApi.Tests/ProductServiceTests.cs`

Minimum 2-3 baseline tests (List + GetById) using `UseInMemoryDatabase`.

## Verification

After all files created, run:

```bash
dotnet restore
dotnet build
dotnet test
```

All three must succeed. If any fails:
1. Read error
2. Fix the offending file
3. Re-run

## Output to user

```markdown
## ✅ Project bootstrapped

**Created:**
- `ShopApi.sln`
- `ShopApi/` (7 .cs files, 6 packages)
- `ShopApi.Tests/` (1 .cs file, 5 packages)

**Status:**
- ✅ Build OK
- ✅ Baseline tests pass (N/N)

**Ready for:** `@planner` to read feature spec and produce `TODO.md`.

**Commit:** `chore: bootstrap ShopApi baseline`
```

## Triggers

- "/bootstrap-project"
- "bootstrap project"
- "init dotnet solution"
- As Phase 0 of `bootstrap-and-deliver`

## Don't

- Don't run `dotnet new` if `.sln` already exists
- Don't add packages not in this skill's list (let @planner/architect propose new ones)
- Don't implement the feature itself (that's @backend-dev's job)

## Self-heal (autonomous mode)

If `dotnet restore` / `dotnet build` fails: read the CS#### error, patch the offending baseline file, retry (max 2 retries). If a NuGet package fails to download → wait 5s, retry; if persistent → use closest pre-installed version. Network timeout → retry up to 3 times. See `deliver-feature/SKILL.md` Autonomous Mode.
