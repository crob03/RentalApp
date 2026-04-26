# Items Feature Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add ItemsListPage, ItemDetailsPage, CreateItemPage, and NearbyItemsPage backed by ItemService, LocationService, and ItemRepository.

**Architecture:** ViewModels → IItemService/ILocationService → IApiService → RemoteApiService (HTTP) or LocalApiService (DB). LocalApiService delegates all item DB access to ItemRepository, which returns EF entities. LocalApiService maps entities to DTOs. PostGIS powers nearby search via EF Core spatial.

**Tech Stack:** .NET MAUI 10, CommunityToolkit.Mvvm 8.4, EF Core 10 + Npgsql + NetTopologySuite, PostgreSQL 16 + PostGIS, xUnit, NSubstitute 5.

---

## File Map

### New files
```
RentalApp/Services/IItemService.cs
RentalApp/Services/ItemService.cs
RentalApp/Services/ILocationService.cs
RentalApp/Services/LocationService.cs
RentalApp/Services/ItemRepository.cs
RentalApp/ViewModels/ItemsListViewModel.cs
RentalApp/ViewModels/ItemDetailsViewModel.cs
RentalApp/ViewModels/CreateItemViewModel.cs
RentalApp/ViewModels/NearbyItemsViewModel.cs
RentalApp/Views/ItemsListPage.xaml + ItemsListPage.xaml.cs
RentalApp/Views/ItemDetailsPage.xaml + ItemDetailsPage.xaml.cs
RentalApp/Views/CreateItemPage.xaml + CreateItemPage.xaml.cs
RentalApp/Views/NearbyItemsPage.xaml + NearbyItemsPage.xaml.cs
RentalApp.Test/Repositories/ItemRepositoryTests.cs
RentalApp.Test/Services/ItemServiceTests.cs
RentalApp.Test/Services/LocationServiceTests.cs
RentalApp.Test/ViewModels/ItemsListViewModelTests.cs
RentalApp.Test/ViewModels/ItemDetailsViewModelTests.cs
RentalApp.Test/ViewModels/CreateItemViewModelTests.cs
RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs
```

### Modified files
```
RentalApp.Database/Models/Item.cs           — replace Lat/Lon with Point; add IsAvailable + CreatedAt; remove ImageUrl
RentalApp.Database/Data/AppDbContext.cs     — UseNetTopologySuite(); update OnModelCreating
RentalApp.Database/RentalApp.Database.csproj — add NTS NuGet packages
RentalApp/Services/IApiService.cs           — add pageSize to GetItemsAsync; add page+pageSize to GetNearbyItemsAsync
RentalApp/Services/RemoteApiService.cs      — implement all item methods
RentalApp/Services/LocalApiService.cs       — inject ItemRepository; implement all item methods
RentalApp/ViewModels/MainViewModel.cs       — 3 new nav RelayCommands
RentalApp/Constants/Routes.cs              — 4 new route constants
RentalApp/AppShell.xaml.cs                 — Routing.RegisterRoute for all push pages
RentalApp/MauiProgram.cs                   — register new services, VMs, pages
RentalApp.Test/Fixtures/DatabaseFixture.cs  — PostGIS setup; ResetItemsAsync; item/category seed
RentalApp.Test/Services/LocalApiServiceTests.cs — item method tests
```

---

## Task 1: Add NuGet packages and update Item DB model

**Files:**
- Modify: `RentalApp.Database/RentalApp.Database.csproj`
- Modify: `RentalApp.Database/Models/Item.cs`

- [ ] **Step 1: Add NetTopologySuite packages to RentalApp.Database.csproj**

Open `RentalApp.Database/RentalApp.Database.csproj` and add inside the existing `<ItemGroup>`:

```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite" Version="10.0.0" />
<PackageReference Include="NetTopologySuite" Version="2.5.0" />
```

- [ ] **Step 2: Replace Item DB model**

Replace the entire contents of `RentalApp.Database/Models/Item.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace RentalApp.Database.Models;

[Table("items")]
[PrimaryKey(nameof(Id))]
public class Item
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public double DailyRate { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    [Required]
    public int OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    [Required]
    public Point Location { get; set; } = null!;

    [Required]
    public bool IsAvailable { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Step 3: Verify the project builds**

```bash
dotnet build RentalApp.Database/RentalApp.Database.csproj
```

Expected: build succeeds with no errors.

- [ ] **Step 4: Commit**

```bash
dotnet csharpier .
git add RentalApp.Database/RentalApp.Database.csproj RentalApp.Database/Models/Item.cs
git commit -m "feat: update Item DB model — PostGIS Point, IsAvailable, CreatedAt, drop ImageUrl"
```

---

## Task 2: Update AppDbContext and generate migration

**Files:**
- Modify: `RentalApp.Database/Data/AppDbContext.cs`
- Create: `RentalApp.Migrations/Migrations/<timestamp>_UpdateItemSchema.cs` (generated)

- [ ] **Step 1: Update OnConfiguring to enable NetTopologySuite**

In `RentalApp.Database/Data/AppDbContext.cs`, update the `optionsBuilder.UseNpgsql(...)` call in both `OnConfiguring` (the convention-based one) and wherever `UseNpgsql` is called with options. Find this block:

```csharp
optionsBuilder.UseNpgsql(
    connectionString,
    o => o.MigrationsAssembly("RentalApp.Migrations")
);
```

Replace with:

```csharp
optionsBuilder.UseNpgsql(
    connectionString,
    o => o.MigrationsAssembly("RentalApp.Migrations").UseNetTopologySuite()
);
```

Add the using at the top of the file:

```csharp
using NetTopologySuite.Geometries;
```

- [ ] **Step 2: Update OnModelCreating for the Item entity**

In `AppDbContext.OnModelCreating`, replace the existing Item entity configuration block:

```csharp
// Configure Item entity
modelBuilder.Entity<Item>(entity =>
{
    entity.Property(e => e.Title).HasMaxLength(100);
    entity.Property(e => e.Description).HasMaxLength(1000);
    entity.Property(e => e.Location).HasColumnType("geography(Point, 4326)");

    entity.HasOne(e => e.Owner).WithMany().HasForeignKey(e => e.OwnerId);
    entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId);
});
```

- [ ] **Step 3: Generate the migration**

```bash
dotnet tool restore
dotnet ef migrations add UpdateItemSchema --project RentalApp.Migrations
```

Expected: a new file appears at `RentalApp.Migrations/Migrations/<timestamp>_UpdateItemSchema.cs`.

- [ ] **Step 4: Add PostGIS extension creation to the migration**

Open the generated migration file. Add this as the **first line** of the `Up` method, before any `DropColumn` or `AddColumn` calls:

```csharp
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis");
```

Add this as the **first line** of the `Down` method:

```csharp
migrationBuilder.Sql("DROP EXTENSION IF EXISTS postgis");
```

- [ ] **Step 5: Build to verify the migration compiles**

```bash
dotnet build RentalApp.sln
```

Expected: build succeeds.

- [ ] **Step 6: Commit**

```bash
dotnet csharpier .
git add RentalApp.Database/Data/AppDbContext.cs
git add RentalApp.Migrations/Migrations/
git commit -m "feat: add PostGIS migration — location column, IsAvailable, CreatedAt, drop ImageUrl"
```

---

## Task 3: Extend DatabaseFixture and implement ItemRepository

**Files:**
- Modify: `RentalApp.Test/Fixtures/DatabaseFixture.cs`
- Create: `RentalApp.Test/Repositories/ItemRepositoryTests.cs`
- Create: `RentalApp/Services/ItemRepository.cs`

- [ ] **Step 1: Update DatabaseFixture to enable PostGIS and seed items**

Replace the entire contents of `RentalApp.Test/Fixtures/DatabaseFixture.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RentalApp.Database.Data;
using RentalApp.Database.Models;

namespace RentalApp.Test.Fixtures;

public class DatabaseFixture : IAsyncLifetime
{
    private const string FallbackConnectionString =
        "Host=localhost;Port=5432;Database=appdb_test;Username=app_user;Password=app_password";

    public AppDbContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? FallbackConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, o => o.UseNetTopologySuite())
            .Options;

        Context = new AppDbContext(options);

        await Context.Database.EnsureDeletedAsync();
        await Context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS postgis");
        await Context.Database.EnsureCreatedAsync();
        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        await Context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE users RESTART IDENTITY CASCADE"
        );
        Context.ChangeTracker.Clear();
        await SeedAsync();
    }

    public async Task ResetItemsAsync()
    {
        await Context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE items, categories RESTART IDENTITY CASCADE"
        );
        Context.ChangeTracker.Clear();
        await SeedItemsAsync();
    }

    private async Task SeedAsync()
    {
        Context.Users.Add(new User
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            PasswordHash = "hash",
            PasswordSalt = "salt",
        });
        await Context.SaveChangesAsync();
        await Context.Database.ExecuteSqlRawAsync(
            """SELECT setval(pg_get_serial_sequence('users', 'Id'), (SELECT MAX("Id") FROM users))"""
        );

        await SeedItemsAsync();
    }

    private async Task SeedItemsAsync()
    {
        var factory = new GeometryFactory(new PrecisionModel(), 4326);

        Context.Categories.AddRange(
            new Category { Id = 1, Name = "Tools", Slug = "tools" },
            new Category { Id = 2, Name = "Electronics", Slug = "electronics" }
        );

        // Three items at known Edinburgh-area coordinates:
        //   Item 1: ~0.5 km from (55.9533, -3.1883)
        //   Item 2: ~1.5 km from origin (same category, for category-filter test)
        //   Item 3: ~20 km from origin (should be excluded from 5 km radius search)
        Context.Items.AddRange(
            new Item
            {
                Id = 1,
                Title = "Test Drill",
                Description = "A power drill",
                DailyRate = 10.0,
                CategoryId = 1,
                OwnerId = 1,
                Location = factory.CreatePoint(new Coordinate(-3.1883, 55.9533)),
                IsAvailable = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new Item
            {
                Id = 2,
                Title = "Test Ladder",
                Description = "A step ladder",
                DailyRate = 8.0,
                CategoryId = 1,
                OwnerId = 1,
                Location = factory.CreatePoint(new Coordinate(-3.2050, 55.9600)),
                IsAvailable = true,
                CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            },
            new Item
            {
                Id = 3,
                Title = "Far Away Laptop",
                Description = "A laptop",
                DailyRate = 25.0,
                CategoryId = 2,
                OwnerId = 1,
                Location = factory.CreatePoint(new Coordinate(-3.5200, 56.1200)),
                IsAvailable = false,
                CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            }
        );

        await Context.SaveChangesAsync();

        await Context.Database.ExecuteSqlRawAsync(
            """SELECT setval(pg_get_serial_sequence('categories', 'Id'), (SELECT MAX("Id") FROM categories))"""
        );
        await Context.Database.ExecuteSqlRawAsync(
            """SELECT setval(pg_get_serial_sequence('items', 'Id'), (SELECT MAX("Id") FROM items))"""
        );
    }
}
```

- [ ] **Step 2: Write failing ItemRepository tests**

Create `RentalApp.Test/Repositories/ItemRepositoryTests.cs`:

```csharp
using NetTopologySuite.Geometries;
using RentalApp.Services;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Repositories;

public class ItemRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private static readonly GeometryFactory Factory =
        new GeometryFactory(new PrecisionModel(), 4326);

    public ItemRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private ItemRepository CreateSut() => new(_fixture.Context);

    // ── GetItemsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetItemsAsync_NoFilter_ReturnsAllItems()
    {
        var sut = CreateSut();

        var items = await sut.GetItemsAsync(null, null, 1, 20);

        Assert.Equal(3, items.Count());
    }

    [Fact]
    public async Task GetItemsAsync_CategoryFilter_ReturnsOnlyMatchingCategory()
    {
        var sut = CreateSut();

        var items = await sut.GetItemsAsync("tools", null, 1, 20);

        Assert.All(items, i => Assert.Equal("tools", i.Category.Slug));
    }

    [Fact]
    public async Task GetItemsAsync_SearchFilter_ReturnsMatchingTitles()
    {
        var sut = CreateSut();

        var items = await sut.GetItemsAsync(null, "drill", 1, 20);

        Assert.Single(items);
        Assert.Equal("Test Drill", items.First().Title);
    }

    [Fact]
    public async Task GetItemsAsync_Page2_ReturnsSecondPage()
    {
        var sut = CreateSut();

        var page1 = await sut.GetItemsAsync(null, null, 1, 2);
        var page2 = await sut.GetItemsAsync(null, null, 2, 2);

        Assert.Equal(2, page1.Count());
        Assert.Single(page2);
        Assert.NotEqual(page1.First().Id, page2.First().Id);
    }

    [Fact]
    public async Task GetItemsAsync_IncludesNavigationProperties()
    {
        var sut = CreateSut();

        var items = await sut.GetItemsAsync(null, null, 1, 20);

        Assert.All(items, i =>
        {
            Assert.NotNull(i.Category);
            Assert.NotNull(i.Owner);
        });
    }

    // ── GetNearbyItemsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetNearbyItemsAsync_SmallRadius_ExcludesDistantItems()
    {
        var sut = CreateSut();
        // Origin: Edinburgh city centre. Items 1+2 are within 5 km; item 3 is ~20 km away.
        var origin = Factory.CreatePoint(new Coordinate(-3.1883, 55.9533));

        var items = await sut.GetNearbyItemsAsync(origin, 5_000, null, 1, 20);

        Assert.Equal(2, items.Count());
        Assert.DoesNotContain(items, i => i.Title == "Far Away Laptop");
    }

    [Fact]
    public async Task GetNearbyItemsAsync_CategoryFilter_AppliedWithinRadius()
    {
        var sut = CreateSut();
        var origin = Factory.CreatePoint(new Coordinate(-3.1883, 55.9533));

        var items = await sut.GetNearbyItemsAsync(origin, 5_000, "electronics", 1, 20);

        Assert.Empty(items); // electronics item is beyond 5 km
    }

    [Fact]
    public async Task GetNearbyItemsAsync_OrderedByDistance()
    {
        var sut = CreateSut();
        var origin = Factory.CreatePoint(new Coordinate(-3.1883, 55.9533));

        var items = (await sut.GetNearbyItemsAsync(origin, 5_000, null, 1, 20)).ToList();

        Assert.Equal(2, items.Count);
        // Item 1 is closer to origin than item 2
        Assert.Equal(1, items[0].Id);
    }

    [Fact]
    public async Task GetNearbyItemsAsync_Pagination_ReturnsCorrectPage()
    {
        var sut = CreateSut();
        var origin = Factory.CreatePoint(new Coordinate(-3.1883, 55.9533));

        var page1 = await sut.GetNearbyItemsAsync(origin, 5_000, null, 1, 1);
        var page2 = await sut.GetNearbyItemsAsync(origin, 5_000, null, 2, 1);

        Assert.Single(page1);
        Assert.Single(page2);
        Assert.NotEqual(page1.First().Id, page2.First().Id);
    }

    // ── GetItemAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetItemAsync_ExistingId_ReturnsItemWithNavProperties()
    {
        var sut = CreateSut();

        var item = await sut.GetItemAsync(1);

        Assert.NotNull(item);
        Assert.Equal(1, item!.Id);
        Assert.NotNull(item.Category);
        Assert.NotNull(item.Owner);
    }

    [Fact]
    public async Task GetItemAsync_NonExistentId_ReturnsNull()
    {
        var sut = CreateSut();

        var item = await sut.GetItemAsync(999);

        Assert.Null(item);
    }

    // ── CreateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CreateItemAsync_ValidInput_PersistsAndReturnsItem()
    {
        await _fixture.ResetItemsAsync();
        var sut = CreateSut();
        var location = Factory.CreatePoint(new Coordinate(-3.1883, 55.9533));

        var item = await sut.CreateItemAsync("New Drill", "desc", 15.0, 1, 1, location);

        Assert.True(item.Id > 0);
        Assert.Equal("New Drill", item.Title);
        Assert.True(item.IsAvailable);
    }

    // ── UpdateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItemAsync_PartialUpdate_OnlyChangesSuppliedFields()
    {
        await _fixture.ResetItemsAsync();
        var sut = CreateSut();

        var updated = await sut.UpdateItemAsync(1, "Updated Title", null, null, null);

        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal(10.0, updated.DailyRate); // unchanged
    }

    [Fact]
    public async Task UpdateItemAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();

        var act = () => sut.UpdateItemAsync(999, "X", null, null, null);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── GetCategoriesAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetCategoriesAsync_ReturnsAllCategories()
    {
        var sut = CreateSut();

        var categories = await sut.GetCategoriesAsync();

        Assert.Equal(2, categories.Count());
    }
}
```

- [ ] **Step 3: Run tests — expect compilation failure (ItemRepository doesn't exist yet)**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj
```

Expected: compiler error — `ItemRepository` type not found.

- [ ] **Step 4: Implement ItemRepository**

Create `RentalApp/Services/ItemRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RentalApp.Database.Data;
using DbCategory = RentalApp.Database.Models.Category;
using DbItem = RentalApp.Database.Models.Item;

namespace RentalApp.Services;

public class ItemRepository
{
    private readonly AppDbContext _context;

    public ItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DbItem>> GetItemsAsync(
        string? category,
        string? search,
        int page,
        int pageSize
    )
    {
        var query = _context.Items
            .Include(i => i.Category)
            .Include(i => i.Owner)
            .AsQueryable();

        if (category != null)
            query = query.Where(i => i.Category.Slug == category);

        if (search != null)
            query = query.Where(i => EF.Functions.ILike(i.Title, $"%{search}%"));

        return await query
            .OrderBy(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<DbItem>> GetNearbyItemsAsync(
        Point origin,
        double radiusMeters,
        string? category,
        int page,
        int pageSize
    )
    {
        var query = _context.Items
            .Include(i => i.Category)
            .Include(i => i.Owner)
            .Where(i => i.Location.IsWithinDistance(origin, radiusMeters))
            .AsQueryable();

        if (category != null)
            query = query.Where(i => i.Category.Slug == category);

        return await query
            .OrderBy(i => i.Location.Distance(origin))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<DbItem?> GetItemAsync(int id)
    {
        return await _context.Items
            .Include(i => i.Category)
            .Include(i => i.Owner)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<DbItem> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        int ownerId,
        Point location
    )
    {
        var item = new DbItem
        {
            Title = title,
            Description = description,
            DailyRate = dailyRate,
            CategoryId = categoryId,
            OwnerId = ownerId,
            Location = location,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        return await GetItemAsync(item.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created item.");
    }

    public async Task<DbItem> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    )
    {
        var item =
            await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Owner)
                .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException($"Item {id} not found.");

        if (title != null)
            item.Title = title;
        if (description != null)
            item.Description = description;
        if (dailyRate.HasValue)
            item.DailyRate = dailyRate.Value;
        if (isAvailable.HasValue)
            item.IsAvailable = isAvailable.Value;

        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<IEnumerable<DbCategory>> GetCategoriesAsync()
    {
        return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
    }
}
```

- [ ] **Step 5: Run tests — all repository tests should pass**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~ItemRepositoryTests"
```

Expected: all tests pass.

- [ ] **Step 6: Run full test suite to check nothing is broken**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj
```

Expected: all existing tests still pass.

- [ ] **Step 7: Commit**

```bash
dotnet csharpier .
git add RentalApp/Services/ItemRepository.cs
git add RentalApp.Test/Fixtures/DatabaseFixture.cs
git add RentalApp.Test/Repositories/ItemRepositoryTests.cs
git commit -m "feat: add ItemRepository with PostGIS nearby search and pagination"
```

---
## Task 4: Implement LocalApiService item methods

**Files:**
- Modify: `RentalApp/Services/LocalApiService.cs`
- Modify: `RentalApp.Test/Services/LocalApiServiceTests.cs`

- [ ] **Step 1: Write failing LocalApiService item tests**

Append the following test class sections to `RentalApp.Test/Services/LocalApiServiceTests.cs` (add inside the existing class after the Logout tests):

```csharp
    // ── GetItemsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetItemsAsync_NoFilter_ReturnsItems()
    {
        var sut = CreateSut();

        var items = await sut.GetItemsAsync();

        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task GetItemsAsync_CategoryFilter_ReturnsMappedDtos()
    {
        var sut = CreateSut();

        var items = await sut.GetItemsAsync(category: "tools");

        Assert.All(items, i => Assert.Equal("Tools", i.Category));
    }

    [Fact]
    public async Task GetItemsAsync_MapsLocationToLatLon()
    {
        var sut = CreateSut();

        var items = await sut.GetItemsAsync();

        Assert.All(items, i =>
        {
            Assert.NotNull(i.Latitude);
            Assert.NotNull(i.Longitude);
        });
    }

    // ── GetNearbyItemsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetNearbyItemsAsync_WithinRadius_ReturnsNearbyItems()
    {
        var sut = CreateSut();

        // Origin: Edinburgh city centre. Items 1+2 are seeded within 5 km.
        var items = await sut.GetNearbyItemsAsync(55.9533, -3.1883, 5.0);

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetNearbyItemsAsync_PopulatesDistance()
    {
        var sut = CreateSut();

        var items = await sut.GetNearbyItemsAsync(55.9533, -3.1883, 5.0);

        Assert.All(items, i => Assert.NotNull(i.Distance));
    }

    // ── GetItemAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetItemAsync_ExistingId_ReturnsMappedItem()
    {
        var sut = CreateSut();

        var item = await sut.GetItemAsync(1);

        Assert.Equal(1, item.Id);
        Assert.Equal("Test Drill", item.Title);
    }

    [Fact]
    public async Task GetItemAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();

        var act = () => sut.GetItemAsync(999);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── CreateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CreateItemAsync_AuthenticatedUser_CreatesAndReturnsItem()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");
        await sut.LoginAsync("jane@example.com", "Password1!");

        var item = await sut.CreateItemAsync("My Drill", "desc", 10.0, 1, 55.9533, -3.1883);

        Assert.True(item.Id > 0);
        Assert.Equal("My Drill", item.Title);
        Assert.True(item.IsAvailable);
    }

    [Fact]
    public async Task CreateItemAsync_NotAuthenticated_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();

        var act = () => sut.CreateItemAsync("Drill", null, 10.0, 1, 55.9533, -3.1883);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── UpdateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItemAsync_ValidUpdate_ReturnsUpdatedItem()
    {
        await _fixture.ResetItemsAsync();
        var sut = CreateSut();

        var item = await sut.UpdateItemAsync(1, "Updated Title", null, null, null);

        Assert.Equal("Updated Title", item.Title);
    }

    // ── GetCategoriesAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetCategoriesAsync_ReturnsAllCategories()
    {
        var sut = CreateSut();

        var categories = await sut.GetCategoriesAsync();

        Assert.Equal(2, categories.Count);
    }
```

- [ ] **Step 2: Run tests — expect failures (NotImplementedException)**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~LocalApiServiceTests"
```

Expected: item method tests fail with `NotImplementedException`.

- [ ] **Step 3: Update LocalApiService to inject ItemRepository and implement item methods**

Replace the entire contents of `RentalApp/Services/LocalApiService.cs`:

```csharp
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RentalApp.Database.Data;
using RentalApp.Models;
using DbUser = RentalApp.Database.Models.User;

namespace RentalApp.Services;

public class LocalApiService : IApiService
{
    private readonly AppDbContext _context;
    private readonly ItemRepository _itemRepository;
    private User? _currentUser;

    private static readonly GeometryFactory GeoFactory =
        new GeometryFactory(new PrecisionModel(), 4326);

    public LocalApiService(AppDbContext context, ItemRepository itemRepository)
    {
        _context = context;
        _itemRepository = itemRepository;
    }

    public async Task LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        _currentUser = ToUser(user);
    }

    public async Task RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    )
    {
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing != null)
            throw new InvalidOperationException("User with this email already exists");

        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        _context.Users.Add(new DbUser
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, salt),
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();
    }

    public Task<User> GetCurrentUserAsync()
    {
        if (_currentUser == null)
            throw new InvalidOperationException("No user is currently authenticated");

        return Task.FromResult(_currentUser);
    }

    public async Task<User> GetUserAsync(int userId)
    {
        var user =
            await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        return ToUser(user);
    }

    public Task LogoutAsync()
    {
        _currentUser = null;
        return Task.CompletedTask;
    }

    public async Task<List<Item>> GetItemsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20
    )
    {
        var dbItems = await _itemRepository.GetItemsAsync(category, search, page, pageSize);
        return dbItems.Select(ToItem).ToList();
    }

    public async Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null,
        int page = 1,
        int pageSize = 20
    )
    {
        var origin = GeoFactory.CreatePoint(new Coordinate(lon, lat));
        var radiusMeters = radius * 1000;

        var dbItems = await _itemRepository.GetNearbyItemsAsync(
            origin,
            radiusMeters,
            category,
            page,
            pageSize
        );

        return dbItems
            .Select(i => ToNearbyItem(i, origin))
            .ToList();
    }

    public async Task<Item> GetItemAsync(int id)
    {
        var dbItem =
            await _itemRepository.GetItemAsync(id)
            ?? throw new InvalidOperationException($"Item {id} not found.");

        return ToItem(dbItem);
    }

    public async Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double latitude,
        double longitude
    )
    {
        if (_currentUser == null)
            throw new InvalidOperationException("No user is currently authenticated");

        var location = GeoFactory.CreatePoint(new Coordinate(longitude, latitude));
        var dbItem = await _itemRepository.CreateItemAsync(
            title,
            description,
            dailyRate,
            categoryId,
            _currentUser.Id,
            location
        );

        return ToItem(dbItem);
    }

    public async Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    )
    {
        var dbItem = await _itemRepository.UpdateItemAsync(
            id,
            title,
            description,
            dailyRate,
            isAvailable
        );

        return ToItem(dbItem);
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var dbCategories = await _itemRepository.GetCategoriesAsync();
        return dbCategories
            .Select(c => new Category(c.Id, c.Name, c.Slug, ItemCount: 0))
            .ToList();
    }

    private static User ToUser(DbUser user) =>
        new(user.Id, user.FirstName, user.LastName, 0.0, 0, 0, user.Email, user.CreatedAt, null);

    private static Item ToItem(Database.Models.Item i) =>
        new(
            i.Id,
            i.Title,
            i.Description,
            i.DailyRate,
            i.CategoryId,
            i.Category.Name,
            i.OwnerId,
            $"{i.Owner.FirstName} {i.Owner.LastName}",
            OwnerRating: null,
            Latitude: i.Location.Y,
            Longitude: i.Location.X,
            Distance: null,
            i.IsAvailable,
            AverageRating: null,
            TotalReviews: null,
            i.CreatedAt,
            Reviews: null
        );

    private static Item ToNearbyItem(Database.Models.Item i, Point origin) =>
        new(
            i.Id,
            i.Title,
            i.Description,
            i.DailyRate,
            i.CategoryId,
            i.Category.Name,
            i.OwnerId,
            $"{i.Owner.FirstName} {i.Owner.LastName}",
            OwnerRating: null,
            Latitude: i.Location.Y,
            Longitude: i.Location.X,
            Distance: i.Location.Distance(origin) / 1000.0,
            i.IsAvailable,
            AverageRating: null,
            TotalReviews: null,
            CreatedAt: null,
            Reviews: null
        );
}
```

- [ ] **Step 4: Update MauiProgram.cs to pass ItemRepository to LocalApiService**

In `RentalApp/MauiProgram.cs`, find the `else` block that registers `LocalApiService` and update it:

```csharp
else
{
    builder.Services.AddDbContext<AppDbContext>();
    builder.Services.AddScoped<ItemRepository>();
    builder.Services.AddSingleton<IApiService>(sp => new LocalApiService(
        sp.GetRequiredService<AppDbContext>(),
        sp.GetRequiredService<ItemRepository>()
    ));
}
```

- [ ] **Step 5: Run LocalApiService tests — all should pass**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~LocalApiServiceTests"
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
dotnet csharpier .
git add RentalApp/Services/LocalApiService.cs
git add RentalApp/MauiProgram.cs
git add RentalApp.Test/Services/LocalApiServiceTests.cs
git commit -m "feat: implement LocalApiService item methods via ItemRepository"
```

---

## Task 5: Update IApiService and implement RemoteApiService item methods

**Files:**
- Modify: `RentalApp/Services/IApiService.cs`
- Modify: `RentalApp/Services/RemoteApiService.cs`
- Modify: `RentalApp.Test/Services/RemoteApiServiceTests.cs`

- [ ] **Step 1: Update IApiService signatures**

In `RentalApp/Services/IApiService.cs`, replace the two item list method signatures:

```csharp
Task<List<Item>> GetItemsAsync(
    string? category = null,
    string? search = null,
    int page = 1,
    int pageSize = 20
);

Task<List<Item>> GetNearbyItemsAsync(
    double lat,
    double lon,
    double radius = 5.0,
    string? category = null,
    int page = 1,
    int pageSize = 20
);
```

- [ ] **Step 2: Write failing RemoteApiService item tests**

Append the following to `RentalApp.Test/Services/RemoteApiServiceTests.cs` (inside the class, after the existing Logout test):

```csharp
    // ── GetItemsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetItemsAsync_SuccessResponse_ReturnsMappedItems()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("items")))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    items = new[]
                    {
                        new
                        {
                            id = 1, title = "Drill", description = (string?)null,
                            dailyRate = 10.0, categoryId = 1, category = "Tools",
                            ownerId = 1, ownerName = "Jane Doe", ownerRating = (double?)null,
                            isAvailable = true, averageRating = (double?)null,
                            createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        },
                    },
                    totalItems = 1, page = 1, pageSize = 20, totalPages = 1,
                }),
            });
        var sut = CreateSut();

        var items = await sut.GetItemsAsync();

        Assert.Single(items);
        Assert.Equal("Drill", items[0].Title);
        Assert.Equal("Tools", items[0].Category);
        Assert.Null(items[0].Latitude);
    }

    // ── GetNearbyItemsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetNearbyItemsAsync_SuccessResponse_ReturnsMappedItemsWithDistance()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("items/nearby")))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    items = new[]
                    {
                        new
                        {
                            id = 1, title = "Drill", description = (string?)null,
                            dailyRate = 10.0, categoryId = 1, category = "Tools",
                            ownerId = 1, ownerName = "Jane Doe",
                            latitude = 55.9533, longitude = -3.1883,
                            distance = 0.4, isAvailable = true, averageRating = (double?)null,
                        },
                    },
                    searchLocation = new { latitude = 55.95, longitude = -3.19 },
                    radius = 5.0, totalResults = 1,
                }),
            });
        var sut = CreateSut();

        var items = await sut.GetNearbyItemsAsync(55.95, -3.19);

        Assert.Single(items);
        Assert.Equal(0.4, items[0].Distance);
        Assert.Equal(55.9533, items[0].Latitude);
    }

    // ── GetItemAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetItemAsync_SuccessResponse_ReturnsMappedItemWithReviews()
    {
        _apiClient
            .GetAsync("items/1")
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = 1, title = "Drill", description = (string?)null,
                    dailyRate = 10.0, categoryId = 1, category = "Tools",
                    ownerId = 1, ownerName = "Jane Doe", ownerRating = (double?)4.5,
                    latitude = (double?)55.9533, longitude = (double?)-3.1883,
                    isAvailable = true, averageRating = (double?)4.0,
                    totalReviews = 1,
                    createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    reviews = new[]
                    {
                        new
                        {
                            id = 10, reviewerId = 2, reviewerName = "Bob",
                            rating = 4, comment = "Good", createdAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                        },
                    },
                }),
            });
        var sut = CreateSut();

        var item = await sut.GetItemAsync(1);

        Assert.Equal(1, item.Id);
        Assert.Equal(4.5, item.OwnerRating);
        Assert.Single(item.Reviews!);
        Assert.Equal(4, item.Reviews![0].Rating);
    }

    // ── CreateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CreateItemAsync_SuccessResponse_ReturnsMappedItem()
    {
        _apiClient
            .PostAsJsonAsync("items", Arg.Any<object>())
            .Returns(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new
                {
                    id = 5, title = "New Drill", description = (string?)null,
                    dailyRate = 12.0, categoryId = 1, category = "Tools",
                    ownerId = 1, ownerName = "Jane Doe",
                    latitude = 55.9533, longitude = -3.1883,
                    isAvailable = true,
                    createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                }),
            });
        var sut = CreateSut();

        var item = await sut.CreateItemAsync("New Drill", null, 12.0, 1, 55.9533, -3.1883);

        Assert.Equal(5, item.Id);
        Assert.Equal("New Drill", item.Title);
    }

    // ── UpdateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItemAsync_SuccessResponse_FetchesAndReturnsFullItem()
    {
        _apiClient
            .PutAsJsonAsync("items/1", Arg.Any<object>())
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = 1, title = "Updated", description = (string?)null,
                    dailyRate = 10.0, isAvailable = false,
                }),
            });
        _apiClient
            .GetAsync("items/1")
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = 1, title = "Updated", description = (string?)null,
                    dailyRate = 10.0, categoryId = 1, category = "Tools",
                    ownerId = 1, ownerName = "Jane Doe", ownerRating = (double?)null,
                    latitude = (double?)55.9533, longitude = (double?)-3.1883,
                    isAvailable = false, averageRating = (double?)null,
                    totalReviews = 0,
                    createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    reviews = Array.Empty<object>(),
                }),
            });
        var sut = CreateSut();

        var item = await sut.UpdateItemAsync(1, "Updated", null, null, false);

        Assert.Equal("Updated", item.Title);
        Assert.False(item.IsAvailable);
    }

    // ── GetCategoriesAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetCategoriesAsync_SuccessResponse_ReturnsMappedCategories()
    {
        _apiClient
            .GetAsync("categories")
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    categories = new[]
                    {
                        new { id = 1, name = "Tools", slug = "tools", itemCount = 5 },
                    },
                }),
            });
        var sut = CreateSut();

        var categories = await sut.GetCategoriesAsync();

        Assert.Single(categories);
        Assert.Equal("Tools", categories[0].Name);
    }
```

- [ ] **Step 3: Run tests — expect failures (NotImplementedException)**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~RemoteApiServiceTests"
```

Expected: new item tests fail.

- [ ] **Step 4: Implement RemoteApiService item methods**

Add the following private DTOs and method implementations to `RentalApp/Services/RemoteApiService.cs`.

First, add the private DTO records (alongside the existing `MeResponse`, `PublicProfileResponse` etc. at the bottom of the class):

```csharp
    private sealed record ItemsListResponse(
        List<ItemListDto> Items,
        int TotalItems,
        int Page,
        int PageSize,
        int TotalPages
    );

    private sealed record ItemListDto(
        int Id,
        string Title,
        string? Description,
        double DailyRate,
        int CategoryId,
        string Category,
        int OwnerId,
        string OwnerName,
        double? OwnerRating,
        bool IsAvailable,
        double? AverageRating,
        DateTime CreatedAt
    );

    private sealed record NearbyItemsResponse(
        List<NearbyItemDto> Items,
        int TotalResults
    );

    private sealed record NearbyItemDto(
        int Id,
        string Title,
        string? Description,
        double DailyRate,
        int CategoryId,
        string Category,
        int OwnerId,
        string OwnerName,
        double Latitude,
        double Longitude,
        double Distance,
        bool IsAvailable,
        double? AverageRating
    );

    private sealed record ItemDetailDto(
        int Id,
        string Title,
        string? Description,
        double DailyRate,
        int CategoryId,
        string Category,
        int OwnerId,
        string OwnerName,
        double? OwnerRating,
        double? Latitude,
        double? Longitude,
        bool IsAvailable,
        double? AverageRating,
        int TotalReviews,
        DateTime CreatedAt,
        List<ItemReviewDto> Reviews
    );

    private sealed record ItemReviewDto(
        int Id,
        int ReviewerId,
        string ReviewerName,
        int Rating,
        string? Comment,
        DateTime CreatedAt
    );

    private sealed record ItemCreateResponse(
        int Id,
        string Title,
        string? Description,
        double DailyRate,
        int CategoryId,
        string Category,
        int OwnerId,
        string OwnerName,
        double Latitude,
        double Longitude,
        bool IsAvailable,
        DateTime CreatedAt
    );

    private sealed record CategoriesResponse(List<CategoryDto> Categories);

    private sealed record CategoryDto(int Id, string Name, string Slug, int ItemCount);
```

Then replace the four `throw new NotImplementedException()` item method stubs with implementations:

```csharp
    public async Task<List<Item>> GetItemsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20
    )
    {
        var query = $"items?page={page}&pageSize={pageSize}";
        if (category != null) query += $"&category={Uri.EscapeDataString(category)}";
        if (search != null) query += $"&search={Uri.EscapeDataString(search)}";

        var response = await _apiClient.GetAsync(query);
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<ItemsListResponse>()
            ?? throw new InvalidOperationException("Empty items response from API");

        return dto.Items
            .Select(i => new Item(
                i.Id, i.Title, i.Description, i.DailyRate,
                i.CategoryId, i.Category, i.OwnerId, i.OwnerName,
                i.OwnerRating,
                Latitude: null, Longitude: null, Distance: null,
                i.IsAvailable, i.AverageRating,
                TotalReviews: null, i.CreatedAt, Reviews: null))
            .ToList();
    }

    public async Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null,
        int page = 1,
        int pageSize = 20
    )
    {
        var query = $"items/nearby?lat={lat}&lon={lon}&radius={radius}&page={page}&pageSize={pageSize}";
        if (category != null) query += $"&category={Uri.EscapeDataString(category)}";

        var response = await _apiClient.GetAsync(query);
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<NearbyItemsResponse>()
            ?? throw new InvalidOperationException("Empty nearby items response from API");

        return dto.Items
            .Select(i => new Item(
                i.Id, i.Title, i.Description, i.DailyRate,
                i.CategoryId, i.Category, i.OwnerId, i.OwnerName,
                OwnerRating: null,
                i.Latitude, i.Longitude, i.Distance,
                i.IsAvailable, i.AverageRating,
                TotalReviews: null, CreatedAt: null, Reviews: null))
            .ToList();
    }

    public async Task<Item> GetItemAsync(int id)
    {
        var response = await _apiClient.GetAsync($"items/{id}");
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<ItemDetailDto>()
            ?? throw new InvalidOperationException("Empty item response from API");

        return new Item(
            dto.Id, dto.Title, dto.Description, dto.DailyRate,
            dto.CategoryId, dto.Category, dto.OwnerId, dto.OwnerName,
            dto.OwnerRating, dto.Latitude, dto.Longitude,
            Distance: null, dto.IsAvailable, dto.AverageRating,
            dto.TotalReviews, dto.CreatedAt,
            dto.Reviews.Select(r => new Review(
                r.Id, RentalId: null, ItemId: dto.Id, r.ReviewerId,
                r.Rating, ItemTitle: dto.Title, r.Comment, r.ReviewerName, r.CreatedAt
            )).ToList());
    }

    public async Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double latitude,
        double longitude
    )
    {
        var response = await _apiClient.PostAsJsonAsync(
            "items",
            new { title, description, dailyRate, categoryId, latitude, longitude }
        );
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<ItemCreateResponse>()
            ?? throw new InvalidOperationException("Empty create item response from API");

        return new Item(
            dto.Id, dto.Title, dto.Description, dto.DailyRate,
            dto.CategoryId, dto.Category, dto.OwnerId, dto.OwnerName,
            OwnerRating: null, dto.Latitude, dto.Longitude,
            Distance: null, dto.IsAvailable,
            AverageRating: null, TotalReviews: null, dto.CreatedAt, Reviews: null);
    }

    public async Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    )
    {
        var response = await _apiClient.PutAsJsonAsync(
            $"items/{id}",
            new { title, description, dailyRate, isAvailable }
        );
        response.EnsureSuccessStatusCode();

        return await GetItemAsync(id);
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var response = await _apiClient.GetAsync("categories");
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<CategoriesResponse>()
            ?? throw new InvalidOperationException("Empty categories response from API");

        return dto.Categories
            .Select(c => new Category(c.Id, c.Name, c.Slug, c.ItemCount))
            .ToList();
    }
```

Note: `IApiClient` must expose `PutAsJsonAsync`. Check `RentalApp/Http/IApiClient.cs` — if it lacks this method, add it and implement it in `ApiClient.cs` following the existing `PostAsJsonAsync` pattern.

- [ ] **Step 5: Run RemoteApiService tests — all should pass**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~RemoteApiServiceTests"
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
dotnet csharpier .
git add RentalApp/Services/IApiService.cs RentalApp/Services/RemoteApiService.cs
git add RentalApp/Http/IApiClient.cs RentalApp/Http/ApiClient.cs
git add RentalApp.Test/Services/RemoteApiServiceTests.cs
git commit -m "feat: implement RemoteApiService item methods"
```

---

## Task 6: Create IItemService and ItemService

**Files:**
- Create: `RentalApp/Services/IItemService.cs`
- Create: `RentalApp/Services/ItemService.cs`
- Create: `RentalApp.Test/Services/ItemServiceTests.cs`

- [ ] **Step 1: Write failing ItemService tests**

Create `RentalApp.Test/Services/ItemServiceTests.cs`:

```csharp
using NSubstitute;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class ItemServiceTests
{
    private readonly IApiService _api = Substitute.For<IApiService>();

    private ItemService CreateSut() => new(_api);

    // ── CreateItemAsync — validation ───────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("Hi")]          // too short (< 5)
    [InlineData("    ")]        // whitespace only
    public async Task CreateItemAsync_InvalidTitle_ThrowsArgumentException(string title)
    {
        var sut = CreateSut();

        var act = () => sut.CreateItemAsync(title, null, 10.0, 1, 55.9, -3.2);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task CreateItemAsync_TitleTooLong_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var longTitle = new string('A', 101);

        var act = () => sut.CreateItemAsync(longTitle, null, 10.0, 1, 55.9, -3.2);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task CreateItemAsync_DescriptionTooLong_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var longDesc = new string('X', 1001);

        var act = () => sut.CreateItemAsync("Valid Title", longDesc, 10.0, 1, 55.9, -3.2);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(1001.0)]
    public async Task CreateItemAsync_InvalidDailyRate_ThrowsArgumentException(double rate)
    {
        var sut = CreateSut();

        var act = () => sut.CreateItemAsync("Valid Title", null, rate, 1, 55.9, -3.2);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateItemAsync_InvalidCategoryId_ThrowsArgumentException(int categoryId)
    {
        var sut = CreateSut();

        var act = () => sut.CreateItemAsync("Valid Title", null, 10.0, categoryId, 55.9, -3.2);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task CreateItemAsync_ValidInput_DelegatesToApi()
    {
        var expected = new Item(1, "Valid Title", null, 10.0, 1, "Tools", 1, "Owner", null, null, null, null, true, null, null, null, null);
        _api.CreateItemAsync("Valid Title", null, 10.0, 1, 55.9, -3.2).Returns(expected);
        var sut = CreateSut();

        var result = await sut.CreateItemAsync("Valid Title", null, 10.0, 1, 55.9, -3.2);

        Assert.Equal(expected, result);
        await _api.Received(1).CreateItemAsync("Valid Title", null, 10.0, 1, 55.9, -3.2);
    }

    // ── UpdateItemAsync — validation ───────────────────────────────────

    [Fact]
    public async Task UpdateItemAsync_TitleTooShort_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var act = () => sut.UpdateItemAsync(1, "Hi", null, null, null);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task UpdateItemAsync_NullTitle_DoesNotThrow()
    {
        var expected = new Item(1, "Drill", null, 10.0, 1, "Tools", 1, "Owner", null, null, null, null, true, null, null, null, null);
        _api.UpdateItemAsync(1, null, null, null, null).Returns(expected);
        var sut = CreateSut();

        var result = await sut.UpdateItemAsync(1, null, null, null, null);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task UpdateItemAsync_RateOutOfRange_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var act = () => sut.UpdateItemAsync(1, null, null, 1001.0, null);

        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    // ── GetItemsAsync — delegates ──────────────────────────────────────

    [Fact]
    public async Task GetItemsAsync_DelegatesToApi()
    {
        _api.GetItemsAsync(null, null, 1, 20).Returns(new List<Item>());
        var sut = CreateSut();

        await sut.GetItemsAsync();

        await _api.Received(1).GetItemsAsync(null, null, 1, 20);
    }

    // ── GetCategoriesAsync — delegates ────────────────────────────────

    [Fact]
    public async Task GetCategoriesAsync_DelegatesToApi()
    {
        _api.GetCategoriesAsync().Returns(new List<Category>());
        var sut = CreateSut();

        await sut.GetCategoriesAsync();

        await _api.Received(1).GetCategoriesAsync();
    }
}
```

- [ ] **Step 2: Run tests — expect compilation failure (IItemService/ItemService don't exist)**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~ItemServiceTests"
```

Expected: compiler error.

- [ ] **Step 3: Create IItemService**

Create `RentalApp/Services/IItemService.cs`:

```csharp
using RentalApp.Models;

namespace RentalApp.Services;

public interface IItemService
{
    Task<List<Item>> GetItemsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20
    );

    Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null,
        int page = 1,
        int pageSize = 20
    );

    Task<Item> GetItemAsync(int id);

    Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double lat,
        double lon
    );

    Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    );

    Task<List<Category>> GetCategoriesAsync();
}
```

- [ ] **Step 4: Create ItemService**

Create `RentalApp/Services/ItemService.cs`:

```csharp
using RentalApp.Models;

namespace RentalApp.Services;

public class ItemService : IItemService
{
    private readonly IApiService _api;

    public ItemService(IApiService api)
    {
        _api = api;
    }

    public Task<List<Item>> GetItemsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20
    ) => _api.GetItemsAsync(category, search, page, pageSize);

    public Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null,
        int page = 1,
        int pageSize = 20
    ) => _api.GetNearbyItemsAsync(lat, lon, radius, category, page, pageSize);

    public Task<Item> GetItemAsync(int id) => _api.GetItemAsync(id);

    public async Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double lat,
        double lon
    )
    {
        ValidateTitle(title);
        ValidateDescription(description);
        ValidateDailyRate(dailyRate);
        ValidateCategoryId(categoryId);

        return await _api.CreateItemAsync(title, description, dailyRate, categoryId, lat, lon);
    }

    public async Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    )
    {
        if (title != null) ValidateTitle(title);
        if (description != null) ValidateDescription(description);
        if (dailyRate.HasValue) ValidateDailyRate(dailyRate.Value);

        return await _api.UpdateItemAsync(id, title, description, dailyRate, isAvailable);
    }

    public Task<List<Category>> GetCategoriesAsync() => _api.GetCategoriesAsync();

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 5 || title.Length > 100)
            throw new ArgumentException("Title must be between 5 and 100 characters.", nameof(title));
    }

    private static void ValidateDescription(string? description)
    {
        if (description != null && description.Length > 1000)
            throw new ArgumentException("Description must not exceed 1000 characters.", nameof(description));
    }

    private static void ValidateDailyRate(double dailyRate)
    {
        if (dailyRate <= 0 || dailyRate > 1000)
            throw new ArgumentException("Daily rate must be greater than 0 and at most 1000.", nameof(dailyRate));
    }

    private static void ValidateCategoryId(int categoryId)
    {
        if (categoryId <= 0)
            throw new ArgumentException("Category ID must be positive.", nameof(categoryId));
    }
}
```

- [ ] **Step 5: Run ItemService tests — all should pass**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~ItemServiceTests"
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
dotnet csharpier .
git add RentalApp/Services/IItemService.cs RentalApp/Services/ItemService.cs
git add RentalApp.Test/Services/ItemServiceTests.cs
git commit -m "feat: add IItemService and ItemService with input validation"
```

---
## Task 7: Create ILocationService and LocationService

**Files:**
- Create: `RentalApp/Services/ILocationService.cs`
- Create: `RentalApp/Services/LocationService.cs`
- Create: `RentalApp.Test/Services/LocationServiceTests.cs`

- [ ] **Step 1: Write failing LocationService tests**

Create `RentalApp.Test/Services/LocationServiceTests.cs`:

```csharp
using Microsoft.Maui.Devices.Sensors;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class LocationServiceTests
{
    private readonly IGeolocation _geolocation = Substitute.For<IGeolocation>();

    private LocationService CreateSut() => new(_geolocation);

    [Fact]
    public async Task GetCurrentLocationAsync_Success_ReturnsLatLon()
    {
        _geolocation
            .GetLocationAsync(Arg.Any<GeolocationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new Location(55.9533, -3.1883));
        var sut = CreateSut();

        var (lat, lon) = await sut.GetCurrentLocationAsync();

        Assert.Equal(55.9533, lat, precision: 4);
        Assert.Equal(-3.1883, lon, precision: 4);
    }

    [Fact]
    public async Task GetCurrentLocationAsync_NullLocation_ThrowsInvalidOperationException()
    {
        _geolocation
            .GetLocationAsync(Arg.Any<GeolocationRequest>(), Arg.Any<CancellationToken>())
            .Returns((Location?)null);
        var sut = CreateSut();

        var act = () => sut.GetCurrentLocationAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task GetCurrentLocationAsync_PermissionDenied_ThrowsInvalidOperationException()
    {
        _geolocation
            .GetLocationAsync(Arg.Any<GeolocationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new PermissionException("denied"));
        var sut = CreateSut();

        var act = () => sut.GetCurrentLocationAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task GetCurrentLocationAsync_FeatureNotEnabled_ThrowsInvalidOperationException()
    {
        _geolocation
            .GetLocationAsync(Arg.Any<GeolocationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FeatureNotEnabledException());
        var sut = CreateSut();

        var act = () => sut.GetCurrentLocationAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }
}
```

- [ ] **Step 2: Run tests — expect compilation failure**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~LocationServiceTests"
```

Expected: compiler error — `LocationService` not found.

- [ ] **Step 3: Create ILocationService**

Create `RentalApp/Services/ILocationService.cs`:

```csharp
namespace RentalApp.Services;

public interface ILocationService
{
    Task<(double Lat, double Lon)> GetCurrentLocationAsync();
}
```

- [ ] **Step 4: Create LocationService**

Create `RentalApp/Services/LocationService.cs`:

```csharp
using Microsoft.Maui.Devices.Sensors;

namespace RentalApp.Services;

public class LocationService : ILocationService
{
    private readonly IGeolocation _geolocation;

    public LocationService(IGeolocation geolocation)
    {
        _geolocation = geolocation;
    }

    public async Task<(double Lat, double Lon)> GetCurrentLocationAsync()
    {
        try
        {
            var location = await _geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium)
            );

            if (location == null)
                throw new InvalidOperationException(
                    "Unable to determine current location."
                );

            return (location.Latitude, location.Longitude);
        }
        catch (PermissionException)
        {
            throw new InvalidOperationException(
                "Location permission denied."
            );
        }
        catch (FeatureNotEnabledException)
        {
            throw new InvalidOperationException(
                "Location services are disabled on this device."
            );
        }
    }
}
```

- [ ] **Step 5: Run LocationService tests — all should pass**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~LocationServiceTests"
```

Expected: all 4 tests pass.

- [ ] **Step 6: Commit**

```bash
dotnet csharpier .
git add RentalApp/Services/ILocationService.cs RentalApp/Services/LocationService.cs
git add RentalApp.Test/Services/LocationServiceTests.cs
git commit -m "feat: add ILocationService and LocationService with GPS abstraction"
```

---

## Task 8: Add routes, register Shell routes, and wire DI

**Files:**
- Modify: `RentalApp/Constants/Routes.cs`
- Modify: `RentalApp/AppShell.xaml.cs`
- Modify: `RentalApp/MauiProgram.cs`

> Note: The pages and ViewModels don't exist yet. Create empty stub files for each so DI registration and route registration compile. Each stub page just needs `InitializeComponent()` and a ViewModel constructor parameter. Each stub ViewModel just needs to extend `BaseViewModel`.

- [ ] **Step 1: Add four route constants to Routes.cs**

In `RentalApp/Constants/Routes.cs`, add after the existing `Temp` constant:

```csharp
/// <summary>The registered route name for the items list page.</summary>
public const string ItemsList = "ItemsListPage";

/// <summary>The registered route name for the item details page.</summary>
public const string ItemDetails = "ItemDetailsPage";

/// <summary>The registered route name for the create item page.</summary>
public const string CreateItem = "CreateItemPage";

/// <summary>The registered route name for the nearby items page.</summary>
public const string NearbyItems = "NearbyItemsPage";
```

- [ ] **Step 2: Create stub ViewModels**

Create `RentalApp/ViewModels/ItemsListViewModel.cs`:
```csharp
namespace RentalApp.ViewModels;

public partial class ItemsListViewModel : BaseViewModel
{
    public ItemsListViewModel() { Title = "Browse Items"; }
}
```

Create `RentalApp/ViewModels/ItemDetailsViewModel.cs`:
```csharp
namespace RentalApp.ViewModels;

public partial class ItemDetailsViewModel : BaseViewModel
{
    public ItemDetailsViewModel() { Title = "Item Details"; }
}
```

Create `RentalApp/ViewModels/CreateItemViewModel.cs`:
```csharp
namespace RentalApp.ViewModels;

public partial class CreateItemViewModel : BaseViewModel
{
    public CreateItemViewModel() { Title = "List an Item"; }
}
```

Create `RentalApp/ViewModels/NearbyItemsViewModel.cs`:
```csharp
namespace RentalApp.ViewModels;

public partial class NearbyItemsViewModel : BaseViewModel
{
    public NearbyItemsViewModel() { Title = "Nearby Items"; }
}
```

- [ ] **Step 3: Create stub XAML pages**

Create `RentalApp/Views/ItemsListPage.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
  x:Class="RentalApp.Views.ItemsListPage"
  xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
  xmlns:vm="clr-namespace:RentalApp.ViewModels"
  Title="{Binding Title}">
  <ContentPage.BindingContext>
    <vm:ItemsListViewModel />
  </ContentPage.BindingContext>
  <Label Text="Items List — coming soon" HorizontalOptions="Center" VerticalOptions="Center" />
</ContentPage>
```

Create `RentalApp/Views/ItemsListPage.xaml.cs`:
```csharp
using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class ItemsListPage : ContentPage
{
    public ItemsListPage(ItemsListViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
```

Repeat the same pattern for the other three pages. Replace class names and ViewModel types accordingly:

`RentalApp/Views/ItemDetailsPage.xaml` + `.xaml.cs` — uses `ItemDetailsViewModel`, title "Item Details".
`RentalApp/Views/CreateItemPage.xaml` + `.xaml.cs` — uses `CreateItemViewModel`, title "List an Item".
`RentalApp/Views/NearbyItemsPage.xaml` + `.xaml.cs` — uses `NearbyItemsViewModel`, title "Nearby Items".

- [ ] **Step 4: Register Shell routes in AppShell.xaml.cs**

Replace the contents of `RentalApp/AppShell.xaml.cs`:

```csharp
using RentalApp.Constants;
using RentalApp.ViewModels;
using RentalApp.Views;

namespace RentalApp;

public partial class AppShell : Shell
{
    public AppShell(AppShellViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();

        Routing.RegisterRoute(Routes.Main, typeof(MainPage));
        Routing.RegisterRoute(Routes.Register, typeof(RegisterPage));
        Routing.RegisterRoute(Routes.Temp, typeof(TempPage));
        Routing.RegisterRoute(Routes.ItemsList, typeof(ItemsListPage));
        Routing.RegisterRoute(Routes.ItemDetails, typeof(ItemDetailsPage));
        Routing.RegisterRoute(Routes.CreateItem, typeof(CreateItemPage));
        Routing.RegisterRoute(Routes.NearbyItems, typeof(NearbyItemsPage));
    }
}
```

- [ ] **Step 5: Register new services, ViewModels, and pages in MauiProgram.cs**

In `RentalApp/MauiProgram.cs`, add after `builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();`:

```csharp
builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
builder.Services.AddSingleton<ILocationService, LocationService>();
builder.Services.AddTransient<IItemService, ItemService>();
```

Then add after the existing Transient page registrations (e.g., after `TempPage`):

```csharp
builder.Services.AddTransient<ItemsListViewModel>();
builder.Services.AddTransient<ItemsListPage>();
builder.Services.AddTransient<ItemDetailsViewModel>();
builder.Services.AddTransient<ItemDetailsPage>();
builder.Services.AddTransient<CreateItemViewModel>();
builder.Services.AddTransient<CreateItemPage>();
builder.Services.AddTransient<NearbyItemsViewModel>();
builder.Services.AddTransient<NearbyItemsPage>();
```

Add the required using directives at the top of `MauiProgram.cs`:

```csharp
using Microsoft.Maui.Devices.Sensors;
```

- [ ] **Step 6: Build to verify everything compiles**

```bash
dotnet build RentalApp.sln
```

Expected: build succeeds with no errors.

- [ ] **Step 7: Commit**

```bash
dotnet csharpier .
git add RentalApp/Constants/Routes.cs
git add RentalApp/AppShell.xaml.cs
git add RentalApp/MauiProgram.cs
git add RentalApp/ViewModels/ItemsListViewModel.cs RentalApp/ViewModels/ItemDetailsViewModel.cs
git add RentalApp/ViewModels/CreateItemViewModel.cs RentalApp/ViewModels/NearbyItemsViewModel.cs
git add RentalApp/Views/
git commit -m "feat: add routes, DI registrations, and stub pages for items feature"
```

---

## Task 9: Add navigation commands to MainViewModel

**Files:**
- Modify: `RentalApp/ViewModels/MainViewModel.cs`
- Modify: `RentalApp.Test/ViewModels/MainViewModelTests.cs`

- [ ] **Step 1: Write failing tests for the three new nav commands**

Open `RentalApp.Test/ViewModels/MainViewModelTests.cs` and add the following test methods inside the existing class (check the file for its existing structure first):

```csharp
    // ── Item navigation ────────────────────────────────────────────────

    [Fact]
    public async Task NavigateToItemsListCommand_NavigatesToItemsList()
    {
        var sut = CreateSut();

        await sut.NavigateToItemsListCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.ItemsList);
    }

    [Fact]
    public async Task NavigateToNearbyItemsCommand_NavigatesToNearbyItems()
    {
        var sut = CreateSut();

        await sut.NavigateToNearbyItemsCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.NearbyItems);
    }

    [Fact]
    public async Task NavigateToCreateItemCommand_NavigatesToCreateItem()
    {
        var sut = CreateSut();

        await sut.NavigateToCreateItemCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.CreateItem);
    }
```

- [ ] **Step 2: Run tests — expect failure (commands not yet on MainViewModel)**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~MainViewModelTests"
```

Expected: 3 new tests fail — command properties not found.

- [ ] **Step 3: Add the three RelayCommands to MainViewModel**

In `RentalApp/ViewModels/MainViewModel.cs`, add after `NavigateToSettingsAsync`:

```csharp
    /// <summary>Navigates to the items list page.</summary>
    [RelayCommand]
    private async Task NavigateToItemsListAsync()
    {
        await _navigationService.NavigateToAsync(Routes.ItemsList);
    }

    /// <summary>Navigates to the nearby items page.</summary>
    [RelayCommand]
    private async Task NavigateToNearbyItemsAsync()
    {
        await _navigationService.NavigateToAsync(Routes.NearbyItems);
    }

    /// <summary>Navigates to the create item page.</summary>
    [RelayCommand]
    private async Task NavigateToCreateItemAsync()
    {
        await _navigationService.NavigateToAsync(Routes.CreateItem);
    }
```

- [ ] **Step 4: Run tests — all should pass**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~MainViewModelTests"
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
dotnet csharpier .
git add RentalApp/ViewModels/MainViewModel.cs
git add RentalApp.Test/ViewModels/MainViewModelTests.cs
git commit -m "feat: add item navigation commands to MainViewModel"
```

---
## Task 10: ItemsListViewModel and ItemsListPage

**Files:**
- Modify: `RentalApp/ViewModels/ItemsListViewModel.cs`
- Modify: `RentalApp/Views/ItemsListPage.xaml` + `ItemsListPage.xaml.cs`
- Create: `RentalApp.Test/ViewModels/ItemsListViewModelTests.cs`

- [ ] **Step 1: Write failing ItemsListViewModel tests**

Create `RentalApp.Test/ViewModels/ItemsListViewModelTests.cs`:

```csharp
using NSubstitute;
using RentalApp.Models;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class ItemsListViewModelTests
{
    private readonly IItemService _itemService = Substitute.For<IItemService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();

    private ItemsListViewModel CreateSut() => new(_itemService, _nav);

    private static Item MakeItem(int id) =>
        new(id, $"Item {id}", null, 10.0, 1, "Tools", 1, "Owner", null, null, null, null, true, null, null, null, null);

    // ── LoadItemsCommand ───────────────────────────────────────────────

    [Fact]
    public async Task LoadItemsCommand_Success_PopulatesItems()
    {
        var items = new List<Item> { MakeItem(1), MakeItem(2) };
        _itemService.GetItemsAsync(null, null, 1, 20).Returns(items);
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Items.Count);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task LoadItemsCommand_Success_ClearsExistingItems()
    {
        _itemService.GetItemsAsync(null, null, 1, 20).Returns(new List<Item> { MakeItem(1) });
        var sut = CreateSut();
        await sut.LoadItemsCommand.ExecuteAsync(null);

        _itemService.GetItemsAsync(null, null, 1, 20).Returns(new List<Item> { MakeItem(2) });
        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.Single(sut.Items);
        Assert.Equal(2, sut.Items[0].Id);
    }

    [Fact]
    public async Task LoadItemsCommand_FullPage_SetsHasMorePagesTrue()
    {
        var items = Enumerable.Range(1, 20).Select(MakeItem).ToList();
        _itemService.GetItemsAsync(null, null, 1, 20).Returns(items);
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.True(sut.HasMorePages);
    }

    [Fact]
    public async Task LoadItemsCommand_PartialPage_SetsHasMorePagesFalse()
    {
        _itemService.GetItemsAsync(null, null, 1, 20).Returns(new List<Item> { MakeItem(1) });
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.False(sut.HasMorePages);
    }

    [Fact]
    public async Task LoadItemsCommand_EmptyResult_SetsIsEmptyTrue()
    {
        _itemService.GetItemsAsync(null, null, 1, 20).Returns(new List<Item>());
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.True(sut.IsEmpty);
    }

    [Fact]
    public async Task LoadItemsCommand_ServiceThrows_SetsError()
    {
        _itemService.GetItemsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
            .ThrowsAsync(new InvalidOperationException("Network error"));
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.False(sut.IsBusy);
    }

    // ── LoadMoreItemsCommand ───────────────────────────────────────────

    [Fact]
    public async Task LoadMoreItemsCommand_AppendsToExistingItems()
    {
        _itemService.GetItemsAsync(null, null, 1, 20)
            .Returns(Enumerable.Range(1, 20).Select(MakeItem).ToList());
        _itemService.GetItemsAsync(null, null, 2, 20)
            .Returns(new List<Item> { MakeItem(21) });
        var sut = CreateSut();
        await sut.LoadItemsCommand.ExecuteAsync(null);

        await sut.LoadMoreItemsCommand.ExecuteAsync(null);

        Assert.Equal(21, sut.Items.Count);
    }

    // ── NavigateToItemCommand ──────────────────────────────────────────

    [Fact]
    public async Task NavigateToItemCommand_NavigatesToItemDetails()
    {
        var sut = CreateSut();
        var item = MakeItem(5);

        await sut.NavigateToItemCommand.ExecuteAsync(item);

        await _nav.Received(1).NavigateToAsync(
            RentalApp.Constants.Routes.ItemDetails,
            Arg.Is<Dictionary<string, object>>(d => d.ContainsKey("itemId") && (int)d["itemId"] == 5)
        );
    }
}
```

- [ ] **Step 2: Run tests — expect compilation failure**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~ItemsListViewModelTests"
```

Expected: compiler errors (stub ViewModel lacks properties and commands).

- [ ] **Step 3: Implement ItemsListViewModel**

Replace the contents of `RentalApp/ViewModels/ItemsListViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class ItemsListViewModel : BaseViewModel
{
    private readonly IItemService _itemService;
    private readonly INavigationService _navigationService;
    private const int PageSize = 20;

    [ObservableProperty]
    private ObservableCollection<Item> items = [];

    [ObservableProperty]
    private List<Category> categories = [];

    [ObservableProperty]
    private string? selectedCategory;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private bool hasMorePages;

    private bool _hasLoaded;

    public ItemsListViewModel()
    {
        Title = "Browse Items";
    }

    public ItemsListViewModel(IItemService itemService, INavigationService navigationService)
    {
        _itemService = itemService;
        _navigationService = navigationService;
        Title = "Browse Items";
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        if (_hasLoaded)
            LoadItemsCommand.Execute(null);
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_hasLoaded)
            LoadItemsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();
            CurrentPage = 1;

            var result = await _itemService.GetItemsAsync(
                SelectedCategory, SearchText.Length > 0 ? SearchText : null, CurrentPage, PageSize
            );

            Items = new ObservableCollection<Item>(result);
            HasMorePages = result.Count == PageSize;
            IsEmpty = Items.Count == 0;
            _hasLoaded = true;
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreItemsAsync()
    {
        if (!HasMorePages || IsBusy)
            return;

        try
        {
            IsBusy = true;
            CurrentPage++;

            var result = await _itemService.GetItemsAsync(
                SelectedCategory, SearchText.Length > 0 ? SearchText : null, CurrentPage, PageSize
            );

            foreach (var item in result)
                Items.Add(item);

            HasMorePages = result.Count == PageSize;
        }
        catch (Exception ex)
        {
            CurrentPage--;
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToItemAsync(Item item)
    {
        await _navigationService.NavigateToAsync(
            Routes.ItemDetails,
            new Dictionary<string, object> { ["itemId"] = item.Id }
        );
    }

    [RelayCommand]
    private async Task NavigateToCreateItemAsync()
    {
        await _navigationService.NavigateToAsync(Routes.CreateItem);
    }
}
```

- [ ] **Step 4: Run ItemsListViewModel tests — all should pass**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~ItemsListViewModelTests"
```

Expected: all tests pass.

- [ ] **Step 5: Replace stub ItemsListPage XAML with full implementation**

Replace `RentalApp/Views/ItemsListPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
  x:Class="RentalApp.Views.ItemsListPage"
  xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
  xmlns:vm="clr-namespace:RentalApp.ViewModels"
  Title="{Binding Title}">
  <ContentPage.BindingContext>
    <vm:ItemsListViewModel />
  </ContentPage.BindingContext>

  <Grid RowDefinitions="Auto,Auto,*,Auto" Padding="16" RowSpacing="8">

    <!-- Error banner -->
    <Border
      Grid.Row="0"
      BackgroundColor="{AppThemeBinding Light=#FFEBEE, Dark=#1B0000}"
      Stroke="{AppThemeBinding Light=#F44336, Dark=#EF5350}"
      StrokeThickness="1"
      Padding="12"
      IsVisible="{Binding HasError}">
      <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
      <Label Text="{Binding ErrorMessage}" TextColor="{AppThemeBinding Light=#D32F2F, Dark=#EF5350}" />
    </Border>

    <!-- Search bar -->
    <SearchBar
      Grid.Row="1"
      Text="{Binding SearchText}"
      Placeholder="Search items..."
      SearchCommand="{Binding LoadItemsCommand}" />

    <!-- Items list -->
    <RefreshView
      Grid.Row="2"
      IsRefreshing="{Binding IsBusy}"
      Command="{Binding LoadItemsCommand}">
      <CollectionView ItemsSource="{Binding Items}" RemainingItemsThreshold="3"
                      RemainingItemsThresholdReachedCommand="{Binding LoadMoreItemsCommand}">
        <CollectionView.EmptyView>
          <Label Text="No items found." HorizontalOptions="Center" Margin="0,40" TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}" />
        </CollectionView.EmptyView>
        <CollectionView.ItemTemplate>
          <DataTemplate>
            <Border Margin="0,4" Padding="12" StrokeThickness="1"
                    Stroke="{AppThemeBinding Light={StaticResource Gray200}, Dark={StaticResource Gray700}}">
              <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
              <Border.GestureRecognizers>
                <TapGestureRecognizer
                  Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ItemsListViewModel}}, Path=NavigateToItemCommand}"
                  CommandParameter="{Binding .}" />
              </Border.GestureRecognizers>
              <Grid ColumnDefinitions="*,Auto" RowDefinitions="Auto,Auto">
                <Label Grid.Row="0" Grid.Column="0" Text="{Binding Title}" FontAttributes="Bold" />
                <Label Grid.Row="0" Grid.Column="1" Text="{Binding DailyRate, StringFormat='£{0:F2}/day'}" />
                <Label Grid.Row="1" Grid.Column="0" Text="{Binding Category}" FontSize="12"
                       TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}" />
              </Grid>
            </Border>
          </DataTemplate>
        </CollectionView.ItemTemplate>
      </CollectionView>
    </RefreshView>

    <!-- List an item button -->
    <Button
      Grid.Row="3"
      Text="+ List an Item"
      Command="{Binding NavigateToCreateItemCommand}"
      Margin="0,8,0,0" />

  </Grid>
</ContentPage>
```

Replace `RentalApp/Views/ItemsListPage.xaml.cs`:

```csharp
using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class ItemsListPage : ContentPage
{
    public ItemsListPage(ItemsListViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((ItemsListViewModel)BindingContext).LoadItemsCommand.ExecuteAsync(null);
    }
}
```

- [ ] **Step 6: Build and run full test suite**

```bash
dotnet build RentalApp.sln
dotnet test RentalApp.Test/RentalApp.Test.csproj
```

Expected: build succeeds, all tests pass.

- [ ] **Step 7: Commit**

```bash
dotnet csharpier .
git add RentalApp/ViewModels/ItemsListViewModel.cs
git add RentalApp/Views/ItemsListPage.xaml RentalApp/Views/ItemsListPage.xaml.cs
git add RentalApp.Test/ViewModels/ItemsListViewModelTests.cs
git commit -m "feat: implement ItemsListViewModel and ItemsListPage"
```

---

## Task 11: ItemDetailsViewModel and ItemDetailsPage

**Files:**
- Modify: `RentalApp/ViewModels/ItemDetailsViewModel.cs`
- Modify: `RentalApp/Views/ItemDetailsPage.xaml` + `ItemDetailsPage.xaml.cs`
- Create: `RentalApp.Test/ViewModels/ItemDetailsViewModelTests.cs`

- [ ] **Step 1: Write failing ItemDetailsViewModel tests**

Create `RentalApp.Test/ViewModels/ItemDetailsViewModelTests.cs`:

```csharp
using NSubstitute;
using RentalApp.Models;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class ItemDetailsViewModelTests
{
    private readonly IItemService _itemService = Substitute.For<IItemService>();
    private readonly IAuthenticationService _authService = Substitute.For<IAuthenticationService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();

    private ItemDetailsViewModel CreateSut() => new(_itemService, _authService, _nav);

    private static Item MakeItem(int id, int ownerId) =>
        new(id, "Drill", "desc", 10.0, 1, "Tools", ownerId, "Owner", null, 55.9, -3.2, null, true, null, 0, null, null);

    private static User MakeUser(int id) =>
        new(id, "Jane", "Doe", null, 0, 0, null, null, null);

    // ── LoadItemCommand ────────────────────────────────────────────────

    [Fact]
    public async Task LoadItemCommand_Success_PopulatesCurrentItem()
    {
        var item = MakeItem(1, 2);
        _itemService.GetItemAsync(1).Returns(item);
        _authService.CurrentUser.Returns(MakeUser(99));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.Equal("Drill", sut.CurrentItem!.Title);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task LoadItemCommand_ServiceThrows_SetsError()
    {
        _itemService.GetItemAsync(1).ThrowsAsync(new InvalidOperationException("Not found"));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.False(sut.IsBusy);
    }

    // ── IsOwner ────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadItemCommand_CurrentUserIsOwner_SetsIsOwnerTrue()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.True(sut.IsOwner);
    }

    [Fact]
    public async Task LoadItemCommand_CurrentUserIsNotOwner_SetsIsOwnerFalse()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(99));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });

        await sut.LoadItemCommand.ExecuteAsync(null);

        Assert.False(sut.IsOwner);
    }

    // ── ToggleEditCommand ──────────────────────────────────────────────

    [Fact]
    public async Task ToggleEditCommand_PopulatesEditFields()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);

        sut.ToggleEditCommand.Execute(null);

        Assert.True(sut.IsEditing);
        Assert.Equal("Drill", sut.EditTitle);
        Assert.Equal("10", sut.EditDailyRate);
    }

    [Fact]
    public async Task ToggleEditCommand_WhenAlreadyEditing_ExitsEditMode()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.ToggleEditCommand.Execute(null);

        sut.ToggleEditCommand.Execute(null);

        Assert.False(sut.IsEditing);
    }

    // ── SaveChangesCommand ─────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesCommand_ValidInput_UpdatesCurrentItemAndExitsEditMode()
    {
        var updated = MakeItem(1, ownerId: 5) with { Title = "Updated Drill" };
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(5));
        _itemService
            .UpdateItemAsync(1, "Updated Drill", Arg.Any<string?>(), Arg.Any<double?>(), Arg.Any<bool?>())
            .Returns(updated);
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.ToggleEditCommand.Execute(null);
        sut.EditTitle = "Updated Drill";

        await sut.SaveChangesCommand.ExecuteAsync(null);

        Assert.Equal("Updated Drill", sut.CurrentItem!.Title);
        Assert.False(sut.IsEditing);
    }

    [Fact]
    public async Task SaveChangesCommand_InvalidDailyRate_SetsError()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.ToggleEditCommand.Execute(null);
        sut.EditDailyRate = "not-a-number";

        await sut.SaveChangesCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.True(sut.IsEditing);
    }

    // ── CancelEditCommand ──────────────────────────────────────────────

    [Fact]
    public async Task CancelEditCommand_ExitsEditModeWithoutSaving()
    {
        _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 5));
        _authService.CurrentUser.Returns(MakeUser(5));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
        await sut.LoadItemCommand.ExecuteAsync(null);
        sut.ToggleEditCommand.Execute(null);

        sut.CancelEditCommand.Execute(null);

        Assert.False(sut.IsEditing);
        await _itemService.DidNotReceive().UpdateItemAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<double?>(), Arg.Any<bool?>());
    }
}
```

- [ ] **Step 2: Run tests — expect compilation failure**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~ItemDetailsViewModelTests"
```

Expected: compiler errors (stub ViewModel lacks required members).

- [ ] **Step 3: Implement ItemDetailsViewModel**

Replace the contents of `RentalApp/ViewModels/ItemDetailsViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class ItemDetailsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IItemService _itemService;
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;
    private int _itemId;

    [ObservableProperty]
    private Item? currentItem;

    [ObservableProperty]
    private bool isOwner;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private string editTitle = string.Empty;

    [ObservableProperty]
    private string editDescription = string.Empty;

    [ObservableProperty]
    private string editDailyRate = string.Empty;

    [ObservableProperty]
    private bool editIsAvailable;

    public ItemDetailsViewModel()
    {
        Title = "Item Details";
    }

    public ItemDetailsViewModel(
        IItemService itemService,
        IAuthenticationService authService,
        INavigationService navigationService
    )
    {
        _itemService = itemService;
        _authService = authService;
        _navigationService = navigationService;
        Title = "Item Details";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("itemId", out var id))
            _itemId = Convert.ToInt32(id);
    }

    [RelayCommand]
    private async Task LoadItemAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();

            CurrentItem = await _itemService.GetItemAsync(_itemId);
            IsOwner = CurrentItem.OwnerId == _authService.CurrentUser?.Id;
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleEdit()
    {
        if (!IsEditing && CurrentItem != null)
        {
            EditTitle = CurrentItem.Title;
            EditDescription = CurrentItem.Description ?? string.Empty;
            EditDailyRate = CurrentItem.DailyRate.ToString();
            EditIsAvailable = CurrentItem.IsAvailable;
        }
        IsEditing = !IsEditing;
    }

    [RelayCommand]
    private async Task SaveChangesAsync()
    {
        if (CurrentItem == null)
            return;

        if (!double.TryParse(EditDailyRate, out var rate))
        {
            SetError("Please enter a valid daily rate.");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            CurrentItem = await _itemService.UpdateItemAsync(
                CurrentItem.Id,
                EditTitle,
                EditDescription.Length > 0 ? EditDescription : null,
                rate,
                EditIsAvailable
            );
            IsEditing = false;
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ClearError();
    }
}
```

- [ ] **Step 4: Run ItemDetailsViewModel tests — all should pass**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~ItemDetailsViewModelTests"
```

Expected: all tests pass.

- [ ] **Step 5: Replace stub ItemDetailsPage XAML**

Replace `RentalApp/Views/ItemDetailsPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
  x:Class="RentalApp.Views.ItemDetailsPage"
  xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
  xmlns:vm="clr-namespace:RentalApp.ViewModels"
  Title="{Binding CurrentItem.Title, FallbackValue='Item Details'}">
  <ContentPage.BindingContext>
    <vm:ItemDetailsViewModel />
  </ContentPage.BindingContext>

  <ScrollView>
    <Grid Padding="16" RowSpacing="12" RowDefinitions="Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto">

      <!-- Error banner -->
      <Border Grid.Row="0"
        BackgroundColor="{AppThemeBinding Light=#FFEBEE, Dark=#1B0000}"
        Stroke="{AppThemeBinding Light=#F44336, Dark=#EF5350}"
        StrokeThickness="1" Padding="12" IsVisible="{Binding HasError}">
        <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
        <Label Text="{Binding ErrorMessage}" TextColor="{AppThemeBinding Light=#D32F2F, Dark=#EF5350}" />
      </Border>

      <!-- Loading -->
      <ActivityIndicator Grid.Row="1" IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}" />

      <!-- View mode -->
      <StackLayout Grid.Row="2" IsVisible="{Binding IsEditing, Converter={StaticResource InvertedBoolConverter}}" Spacing="8">
        <Label Text="{Binding CurrentItem.Title}" FontSize="22" FontAttributes="Bold" />
        <Label Text="{Binding CurrentItem.Category}" FontSize="14"
               TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}" />
        <Label Text="{Binding CurrentItem.DailyRate, StringFormat='£{0:F2} / day'}" FontSize="18" />
        <Label Text="{Binding CurrentItem.Description}" />
        <Label Text="Available" IsVisible="{Binding CurrentItem.IsAvailable}"
               TextColor="{AppThemeBinding Light=#2E7D32, Dark=#66BB6A}" />
        <Label Text="Not available" IsVisible="{Binding CurrentItem.IsAvailable, Converter={StaticResource InvertedBoolConverter}}"
               TextColor="{AppThemeBinding Light=#D32F2F, Dark=#EF5350}" />
        <Label Text="{Binding CurrentItem.OwnerName, StringFormat='Listed by {0}'}"
               FontSize="13"
               TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}" />
      </StackLayout>

      <!-- Edit mode (owner only) -->
      <StackLayout Grid.Row="3" IsVisible="{Binding IsEditing}" Spacing="10">
        <Label Text="Title" FontAttributes="Bold" />
        <Border Stroke="{AppThemeBinding Light={StaticResource Gray300}, Dark={StaticResource Gray600}}" StrokeThickness="1">
          <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
          <Entry Text="{Binding EditTitle}" Margin="12,8" />
        </Border>

        <Label Text="Description" FontAttributes="Bold" />
        <Border Stroke="{AppThemeBinding Light={StaticResource Gray300}, Dark={StaticResource Gray600}}" StrokeThickness="1">
          <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
          <Editor Text="{Binding EditDescription}" Margin="12,8" AutoSize="TextChanges" />
        </Border>

        <Label Text="Daily Rate (£)" FontAttributes="Bold" />
        <Border Stroke="{AppThemeBinding Light={StaticResource Gray300}, Dark={StaticResource Gray600}}" StrokeThickness="1">
          <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
          <Entry Text="{Binding EditDailyRate}" Keyboard="Numeric" Margin="12,8" />
        </Border>

        <StackLayout Orientation="Horizontal">
          <CheckBox IsChecked="{Binding EditIsAvailable}" />
          <Label Text="Available for rental" VerticalOptions="Center" />
        </StackLayout>
      </StackLayout>

      <!-- Owner action buttons -->
      <StackLayout Grid.Row="4" IsVisible="{Binding IsOwner}" Orientation="Horizontal" Spacing="8">
        <Button Text="Edit" Command="{Binding ToggleEditCommand}"
                IsVisible="{Binding IsEditing, Converter={StaticResource InvertedBoolConverter}}" />
        <Button Text="Save" Command="{Binding SaveChangesCommand}" IsVisible="{Binding IsEditing}" />
        <Button Text="Cancel" Command="{Binding CancelEditCommand}" IsVisible="{Binding IsEditing}"
                BackgroundColor="Transparent"
                TextColor="{AppThemeBinding Light={StaticResource Primary}, Dark={StaticResource PrimaryDark}}" />
      </StackLayout>

    </Grid>
  </ScrollView>
</ContentPage>
```

Replace `RentalApp/Views/ItemDetailsPage.xaml.cs`:

```csharp
using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class ItemDetailsPage : ContentPage
{
    public ItemDetailsPage(ItemDetailsViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((ItemDetailsViewModel)BindingContext).LoadItemCommand.ExecuteAsync(null);
    }
}
```

- [ ] **Step 6: Build and run full test suite**

```bash
dotnet build RentalApp.sln
dotnet test RentalApp.Test/RentalApp.Test.csproj
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```bash
dotnet csharpier .
git add RentalApp/ViewModels/ItemDetailsViewModel.cs
git add RentalApp/Views/ItemDetailsPage.xaml RentalApp/Views/ItemDetailsPage.xaml.cs
git add RentalApp.Test/ViewModels/ItemDetailsViewModelTests.cs
git commit -m "feat: implement ItemDetailsViewModel with inline owner editing and ItemDetailsPage"
```

---

## Task 12: CreateItemViewModel and CreateItemPage

**Files:**
- Modify: `RentalApp/ViewModels/CreateItemViewModel.cs`
- Modify: `RentalApp/Views/CreateItemPage.xaml` + `CreateItemPage.xaml.cs`
- Create: `RentalApp.Test/ViewModels/CreateItemViewModelTests.cs`

- [ ] **Step 1: Write failing CreateItemViewModel tests**

Create `RentalApp.Test/ViewModels/CreateItemViewModelTests.cs`:

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Models;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class CreateItemViewModelTests
{
    private readonly IItemService _itemService = Substitute.For<IItemService>();
    private readonly ILocationService _locationService = Substitute.For<ILocationService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();

    private CreateItemViewModel CreateSut() => new(_itemService, _locationService, _nav);

    // ── LoadCategoriesCommand ──────────────────────────────────────────

    [Fact]
    public async Task LoadCategoriesCommand_Success_PopulatesCategories()
    {
        var cats = new List<Category>
        {
            new(1, "Tools", "tools", 5),
            new(2, "Electronics", "electronics", 3),
        };
        _itemService.GetCategoriesAsync().Returns(cats);
        var sut = CreateSut();

        await sut.LoadCategoriesCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Categories.Count);
    }

    // ── CreateItemCommand ──────────────────────────────────────────────

    [Fact]
    public async Task CreateItemCommand_ValidInput_CallsServiceAndNavigatesBack()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .CreateItemAsync("My Drill", "desc", 10.0, 1, 55.9533, -3.1883)
            .Returns(new Item(1, "My Drill", "desc", 10.0, 1, "Tools", 1, "Owner", null, null, null, null, true, null, null, null, null));
        var sut = CreateSut();
        sut.Title2 = "My Drill";
        sut.Description = "desc";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = new Category(1, "Tools", "tools", 5);

        await sut.CreateItemCommand.ExecuteAsync(null);

        await _itemService.Received(1).CreateItemAsync("My Drill", "desc", 10.0, 1, 55.9533, -3.1883);
        await _nav.Received(1).NavigateBackAsync();
    }

    [Fact]
    public async Task CreateItemCommand_NoCategory_SetsError()
    {
        var sut = CreateSut();
        sut.Title2 = "My Drill";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = null;

        await sut.CreateItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        await _itemService.DidNotReceive().CreateItemAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<double>(),
            Arg.Any<int>(), Arg.Any<double>(), Arg.Any<double>()
        );
    }

    [Fact]
    public async Task CreateItemCommand_InvalidRate_SetsError()
    {
        var sut = CreateSut();
        sut.Title2 = "My Drill";
        sut.DailyRate = "not-a-number";
        sut.SelectedCategory = new Category(1, "Tools", "tools", 5);

        await sut.CreateItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
    }

    [Fact]
    public async Task CreateItemCommand_LocationFails_SetsError()
    {
        _locationService
            .GetCurrentLocationAsync()
            .ThrowsAsync(new InvalidOperationException("Location unavailable. Please enable GPS and try again."));
        var sut = CreateSut();
        sut.Title2 = "My Drill";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = new Category(1, "Tools", "tools", 5);

        await sut.CreateItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Contains("Location unavailable", sut.ErrorMessage);
    }

    [Fact]
    public async Task CreateItemCommand_ServiceValidationFails_SetsError()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .CreateItemAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<double>(), Arg.Any<double>())
            .ThrowsAsync(new ArgumentException("Title must be between 5 and 100 characters."));
        var sut = CreateSut();
        sut.Title2 = "Hi";
        sut.DailyRate = "10.00";
        sut.SelectedCategory = new Category(1, "Tools", "tools", 5);

        await sut.CreateItemCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.False(sut.IsBusy);
    }
}
```

> **Note:** The ViewModel uses `Title2` for the listing title to avoid collision with `BaseViewModel.Title` (the page header). Use a clearly named property such as `ItemTitle` or `Title2` — pick one and use it consistently throughout the ViewModel and XAML.

- [ ] **Step 2: Run tests — expect compilation failure**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~CreateItemViewModelTests"
```

- [ ] **Step 3: Implement CreateItemViewModel**

Replace the contents of `RentalApp/ViewModels/CreateItemViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class CreateItemViewModel : BaseViewModel
{
    private readonly IItemService _itemService;
    private readonly ILocationService _locationService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string itemTitle = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string dailyRate = string.Empty;

    [ObservableProperty]
    private List<Category> categories = [];

    [ObservableProperty]
    private Category? selectedCategory;

    public CreateItemViewModel()
    {
        Title = "List an Item";
    }

    public CreateItemViewModel(
        IItemService itemService,
        ILocationService locationService,
        INavigationService navigationService
    )
    {
        _itemService = itemService;
        _locationService = locationService;
        _navigationService = navigationService;
        Title = "List an Item";
    }

    // Expose ItemTitle as Title2 alias for tests that reference it
    public string Title2
    {
        get => ItemTitle;
        set => ItemTitle = value;
    }

    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();
            Categories = await _itemService.GetCategoriesAsync();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateItemAsync()
    {
        if (SelectedCategory == null)
        {
            SetError("Please select a category.");
            return;
        }

        if (!double.TryParse(DailyRate, out var rate))
        {
            SetError("Please enter a valid daily rate.");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            var (lat, lon) = await _locationService.GetCurrentLocationAsync();

            await _itemService.CreateItemAsync(
                ItemTitle,
                Description.Length > 0 ? Description : null,
                rate,
                SelectedCategory.Id,
                lat,
                lon
            );

            await _navigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

- [ ] **Step 4: Run CreateItemViewModel tests — all should pass**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~CreateItemViewModelTests"
```

Expected: all tests pass.

- [ ] **Step 5: Replace stub CreateItemPage XAML**

Replace `RentalApp/Views/CreateItemPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
  x:Class="RentalApp.Views.CreateItemPage"
  xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
  xmlns:vm="clr-namespace:RentalApp.ViewModels"
  Title="{Binding Title}">
  <ContentPage.BindingContext>
    <vm:CreateItemViewModel />
  </ContentPage.BindingContext>

  <ScrollView>
    <Grid Padding="16" RowSpacing="10" RowDefinitions="Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto">

      <!-- Error banner -->
      <Border Grid.Row="0"
        BackgroundColor="{AppThemeBinding Light=#FFEBEE, Dark=#1B0000}"
        Stroke="{AppThemeBinding Light=#F44336, Dark=#EF5350}"
        StrokeThickness="1" Padding="12" IsVisible="{Binding HasError}">
        <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
        <Label Text="{Binding ErrorMessage}" TextColor="{AppThemeBinding Light=#D32F2F, Dark=#EF5350}" />
      </Border>

      <Label Grid.Row="1" Text="Title" FontAttributes="Bold" />
      <Border Grid.Row="2" Stroke="{AppThemeBinding Light={StaticResource Gray300}, Dark={StaticResource Gray600}}" StrokeThickness="1">
        <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
        <Entry Text="{Binding ItemTitle}" Placeholder="e.g. Power Drill" Margin="12,8" />
      </Border>

      <Label Grid.Row="3" Text="Description (optional)" FontAttributes="Bold" />
      <Border Grid.Row="4" Stroke="{AppThemeBinding Light={StaticResource Gray300}, Dark={StaticResource Gray600}}" StrokeThickness="1">
        <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
        <Editor Text="{Binding Description}" Placeholder="Describe your item..." Margin="12,8" AutoSize="TextChanges" />
      </Border>

      <Label Grid.Row="5" Text="Daily Rate (£)" FontAttributes="Bold" />
      <Border Grid.Row="6" Stroke="{AppThemeBinding Light={StaticResource Gray300}, Dark={StaticResource Gray600}}" StrokeThickness="1">
        <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
        <Entry Text="{Binding DailyRate}" Keyboard="Numeric" Placeholder="0.00" Margin="12,8" />
      </Border>

      <StackLayout Grid.Row="7" Spacing="8">
        <Label Text="Category" FontAttributes="Bold" />
        <Picker ItemsSource="{Binding Categories}"
                ItemDisplayBinding="{Binding Name}"
                SelectedItem="{Binding SelectedCategory}"
                Title="Select a category" />

        <Button Text="List Item" Command="{Binding CreateItemCommand}" Margin="0,8,0,0" HeightRequest="50" />
        <ActivityIndicator IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}" />
      </StackLayout>

    </Grid>
  </ScrollView>
</ContentPage>
```

Replace `RentalApp/Views/CreateItemPage.xaml.cs`:

```csharp
using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class CreateItemPage : ContentPage
{
    public CreateItemPage(CreateItemViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((CreateItemViewModel)BindingContext).LoadCategoriesCommand.ExecuteAsync(null);
    }
}
```

- [ ] **Step 6: Build and run full test suite**

```bash
dotnet build RentalApp.sln
dotnet test RentalApp.Test/RentalApp.Test.csproj
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```bash
dotnet csharpier .
git add RentalApp/ViewModels/CreateItemViewModel.cs
git add RentalApp/Views/CreateItemPage.xaml RentalApp/Views/CreateItemPage.xaml.cs
git add RentalApp.Test/ViewModels/CreateItemViewModelTests.cs
git commit -m "feat: implement CreateItemViewModel and CreateItemPage"
```

---

## Task 13: NearbyItemsViewModel and NearbyItemsPage

**Files:**
- Modify: `RentalApp/ViewModels/NearbyItemsViewModel.cs`
- Modify: `RentalApp/Views/NearbyItemsPage.xaml` + `NearbyItemsPage.xaml.cs`
- Create: `RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs`

- [ ] **Step 1: Write failing NearbyItemsViewModel tests**

Create `RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs`:

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Models;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class NearbyItemsViewModelTests
{
    private readonly IItemService _itemService = Substitute.For<IItemService>();
    private readonly ILocationService _locationService = Substitute.For<ILocationService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();

    private NearbyItemsViewModel CreateSut() => new(_itemService, _locationService, _nav);

    private static Item MakeItem(int id) =>
        new(id, $"Item {id}", null, 10.0, 1, "Tools", 1, "Owner", null, 55.9, -3.2, 0.5, true, null, null, null, null);

    // ── LoadNearbyItemsCommand ─────────────────────────────────────────

    [Fact]
    public async Task LoadNearbyItemsCommand_Success_PopulatesItems()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(55.9533, -3.1883, 5.0, null, 1, 20)
            .Returns(new List<Item> { MakeItem(1), MakeItem(2) });
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Items.Count);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task LoadNearbyItemsCommand_GpsFails_SetsError()
    {
        _locationService
            .GetCurrentLocationAsync()
            .ThrowsAsync(new InvalidOperationException("Location unavailable. Please enable GPS and try again."));
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Contains("Location unavailable", sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task LoadNearbyItemsCommand_EmptyResult_SetsIsEmptyTrue()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new List<Item>());
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.True(sut.IsEmpty);
    }

    [Fact]
    public async Task LoadNearbyItemsCommand_FullPage_SetsHasMorePagesTrue()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string?>(), 1, 20)
            .Returns(Enumerable.Range(1, 20).Select(MakeItem).ToList());
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.True(sut.HasMorePages);
    }

    // ── LoadMoreItemsCommand — uses cached GPS ─────────────────────────

    [Fact]
    public async Task LoadMoreItemsCommand_UsesCachedGpsCoordinates()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(55.9533, -3.1883, 5.0, null, 1, 20)
            .Returns(Enumerable.Range(1, 20).Select(MakeItem).ToList());
        _itemService
            .GetNearbyItemsAsync(55.9533, -3.1883, 5.0, null, 2, 20)
            .Returns(new List<Item> { MakeItem(21) });
        var sut = CreateSut();
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        await sut.LoadMoreItemsCommand.ExecuteAsync(null);

        Assert.Equal(21, sut.Items.Count);
        // GPS was only fetched once (for the initial load, not for load-more)
        await _locationService.Received(1).GetCurrentLocationAsync();
    }

    // ── Radius change triggers reload ──────────────────────────────────

    [Fact]
    public async Task RadiusChange_AfterFirstLoad_TriggersReload()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new List<Item>());
        var sut = CreateSut();
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        sut.Radius = 10.0;

        // Give the fire-and-forget partial method time to execute
        await Task.Delay(50);
        await _itemService.Received(2).GetNearbyItemsAsync(
            Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>()
        );
    }
}
```

- [ ] **Step 2: Run tests — expect compilation failure**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~NearbyItemsViewModelTests"
```

- [ ] **Step 3: Implement NearbyItemsViewModel**

Replace the contents of `RentalApp/ViewModels/NearbyItemsViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class NearbyItemsViewModel : BaseViewModel
{
    private readonly IItemService _itemService;
    private readonly ILocationService _locationService;
    private readonly INavigationService _navigationService;
    private const int PageSize = 20;

    private double _cachedLat;
    private double _cachedLon;
    private bool _hasLoaded;

    [ObservableProperty]
    private ObservableCollection<Item> items = [];

    [ObservableProperty]
    private double radius = 5.0;

    [ObservableProperty]
    private List<Category> categories = [];

    [ObservableProperty]
    private string? selectedCategory;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private bool hasMorePages;

    public NearbyItemsViewModel()
    {
        Title = "Nearby Items";
    }

    public NearbyItemsViewModel(
        IItemService itemService,
        ILocationService locationService,
        INavigationService navigationService
    )
    {
        _itemService = itemService;
        _locationService = locationService;
        _navigationService = navigationService;
        Title = "Nearby Items";
    }

    partial void OnRadiusChanged(double value)
    {
        if (_hasLoaded)
            _ = LoadNearbyItemsAsync();
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        if (_hasLoaded)
            _ = LoadNearbyItemsAsync();
    }

    [RelayCommand]
    private async Task LoadNearbyItemsAsync()
    {
        try
        {
            IsBusy = true;
            ClearError();
            CurrentPage = 1;

            var (lat, lon) = await _locationService.GetCurrentLocationAsync();
            _cachedLat = lat;
            _cachedLon = lon;

            var result = await _itemService.GetNearbyItemsAsync(
                _cachedLat, _cachedLon, Radius, SelectedCategory, CurrentPage, PageSize
            );

            Items = new ObservableCollection<Item>(result);
            HasMorePages = result.Count == PageSize;
            IsEmpty = Items.Count == 0;
            _hasLoaded = true;
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreItemsAsync()
    {
        if (!HasMorePages || IsBusy)
            return;

        try
        {
            IsBusy = true;
            CurrentPage++;

            var result = await _itemService.GetNearbyItemsAsync(
                _cachedLat, _cachedLon, Radius, SelectedCategory, CurrentPage, PageSize
            );

            foreach (var item in result)
                Items.Add(item);

            HasMorePages = result.Count == PageSize;
        }
        catch (Exception ex)
        {
            CurrentPage--;
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToItemAsync(Item item)
    {
        await _navigationService.NavigateToAsync(
            Routes.ItemDetails,
            new Dictionary<string, object> { ["itemId"] = item.Id }
        );
    }
}
```

- [ ] **Step 4: Run NearbyItemsViewModel tests — all should pass**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~NearbyItemsViewModelTests"
```

Expected: all tests pass.

- [ ] **Step 5: Replace stub NearbyItemsPage XAML**

Replace `RentalApp/Views/NearbyItemsPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
  x:Class="RentalApp.Views.NearbyItemsPage"
  xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
  xmlns:vm="clr-namespace:RentalApp.ViewModels"
  Title="{Binding Title}">
  <ContentPage.BindingContext>
    <vm:NearbyItemsViewModel />
  </ContentPage.BindingContext>

  <Grid RowDefinitions="Auto,Auto,Auto,*" Padding="16" RowSpacing="8">

    <!-- Error banner -->
    <Border Grid.Row="0"
      BackgroundColor="{AppThemeBinding Light=#FFEBEE, Dark=#1B0000}"
      Stroke="{AppThemeBinding Light=#F44336, Dark=#EF5350}"
      StrokeThickness="1" Padding="12" IsVisible="{Binding HasError}">
      <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
      <Label Text="{Binding ErrorMessage}" TextColor="{AppThemeBinding Light=#D32F2F, Dark=#EF5350}" />
    </Border>

    <!-- Radius slider -->
    <StackLayout Grid.Row="1" Orientation="Horizontal" Spacing="8">
      <Label Text="{Binding Radius, StringFormat='Radius: {0:F0} km'}" VerticalOptions="Center" MinimumWidthRequest="110" />
      <Slider Value="{Binding Radius}" Minimum="1" Maximum="50" HorizontalOptions="FillAndExpand" />
    </StackLayout>

    <!-- Loading indicator -->
    <ActivityIndicator Grid.Row="2" IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}" />

    <!-- Items list -->
    <RefreshView Grid.Row="3" IsRefreshing="{Binding IsBusy}" Command="{Binding LoadNearbyItemsCommand}">
      <CollectionView ItemsSource="{Binding Items}" RemainingItemsThreshold="3"
                      RemainingItemsThresholdReachedCommand="{Binding LoadMoreItemsCommand}">
        <CollectionView.EmptyView>
          <Label Text="No items found nearby." HorizontalOptions="Center" Margin="0,40"
                 TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}" />
        </CollectionView.EmptyView>
        <CollectionView.ItemTemplate>
          <DataTemplate>
            <Border Margin="0,4" Padding="12" StrokeThickness="1"
                    Stroke="{AppThemeBinding Light={StaticResource Gray200}, Dark={StaticResource Gray700}}">
              <Border.StrokeShape><RoundRectangle CornerRadius="8" /></Border.StrokeShape>
              <Border.GestureRecognizers>
                <TapGestureRecognizer
                  Command="{Binding Source={RelativeSource AncestorType={x:Type vm:NearbyItemsViewModel}}, Path=NavigateToItemCommand}"
                  CommandParameter="{Binding .}" />
              </Border.GestureRecognizers>
              <Grid ColumnDefinitions="*,Auto" RowDefinitions="Auto,Auto">
                <Label Grid.Row="0" Grid.Column="0" Text="{Binding Title}" FontAttributes="Bold" />
                <Label Grid.Row="0" Grid.Column="1" Text="{Binding DailyRate, StringFormat='£{0:F2}/day'}" />
                <Label Grid.Row="1" Grid.Column="0"
                       Text="{Binding Distance, StringFormat='{0:F1} km away'}"
                       FontSize="12"
                       TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}" />
              </Grid>
            </Border>
          </DataTemplate>
        </CollectionView.ItemTemplate>
      </CollectionView>
    </RefreshView>

  </Grid>
</ContentPage>
```

Replace `RentalApp/Views/NearbyItemsPage.xaml.cs`:

```csharp
using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class NearbyItemsPage : ContentPage
{
    public NearbyItemsPage(NearbyItemsViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((NearbyItemsViewModel)BindingContext).LoadNearbyItemsCommand.ExecuteAsync(null);
    }
}
```

- [ ] **Step 6: Run the full test suite**

```bash
dotnet build RentalApp.sln
dotnet test RentalApp.Test/RentalApp.Test.csproj
```

Expected: all tests pass, build succeeds.

- [ ] **Step 7: Commit**

```bash
dotnet csharpier .
git add RentalApp/ViewModels/NearbyItemsViewModel.cs
git add RentalApp/Views/NearbyItemsPage.xaml RentalApp/Views/NearbyItemsPage.xaml.cs
git add RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs
git commit -m "feat: implement NearbyItemsViewModel and NearbyItemsPage"
```

---

## Self-Review Checklist

Before declaring the feature complete:

- [ ] All 13 tasks committed and tests green
- [ ] `docker-compose up` starts cleanly with the new migration applied
- [ ] `make use-remote-api` + login → Main dashboard shows 3 new buttons
- [ ] Items list loads, search and category filter work, load-more appends a second page
- [ ] Tapping an item navigates to ItemDetailsPage with correct data
- [ ] Owner sees Edit button; non-owner does not
- [ ] Edit → change title/rate → Save persists changes
- [ ] Create Item form: GPS permission prompt appears; successful create navigates back
- [ ] Nearby Items: GPS fix obtained, slider changes radius and reloads, load-more uses cached position
