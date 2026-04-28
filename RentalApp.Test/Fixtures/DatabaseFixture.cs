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
        await Context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE users RESTART IDENTITY CASCADE");
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
        Context.Users.Add(
            new User
            {
                Id = 1,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
            }
        );
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
            new Category
            {
                Id = 1,
                Name = "Tools",
                Slug = "tools",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new Category
            {
                Id = 2,
                Name = "Electronics",
                Slug = "electronics",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            }
        );

        // Item 1: ~0.5 km from origin, Item 2: ~1.5 km from origin, Item 3: ~20 km (excluded from 5 km search)
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
