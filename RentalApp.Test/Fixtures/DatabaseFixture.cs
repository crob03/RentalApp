using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;

namespace RentalApp.Test.Fixtures;

/// <summary>
/// Creates a real PostgreSQL database for integration tests and tears it down when the test
/// class is done. Intended for use with xUnit's <see cref="IClassFixture{TFixture}"/>.
/// </summary>
/// <remarks>
/// The connection string is read from the <c>CONNECTION_STRING</c> environment variable,
/// matching the value injected by the CI pipeline. Falls back to a local dev database.
/// </remarks>
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
            .UseNpgsql(connectionString)
            .Options;

        Context = new AppDbContext(options);

        // Ensure a clean schema on every run, even if a previous run failed before DisposeAsync.
        await Context.Database.EnsureDeletedAsync();
        await Context.Database.EnsureCreatedAsync();
        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }

    /// <summary>
    /// Deletes all rows and re-seeds the database. Call at the start of tests that mutate data.
    /// </summary>
    public async Task ResetAsync()
    {
        // RESTART IDENTITY resets the sequence so auto-generated inserts don't collide with seeded Ids.
        await Context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE users RESTART IDENTITY CASCADE"
        );
        // Prevent stale tracked entities from the previous test interfering with the fresh seed.
        Context.ChangeTracker.Clear();
        await SeedAsync();
    }

    private async Task SeedAsync()
    {
        Context.Users.AddRange(
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

        // Explicit Id inserts bypass the sequence — advance it to avoid PK collisions on subsequent auto-generated inserts.
        await Context.Database.ExecuteSqlRawAsync(
            """SELECT setval(pg_get_serial_sequence('users', 'Id'), (SELECT MAX("Id") FROM users))"""
        );
    }
}
