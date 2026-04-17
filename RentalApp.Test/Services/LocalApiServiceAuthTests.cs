using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class LocalApiServiceAuthTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private LocalApiService _sut = null!;

    public async Task InitializeAsync()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=appdb_test;Username=app_user;Password=app_password";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _sut = new LocalApiService(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesUser()
    {
        await _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password123");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "jane@example.com");
        Assert.NotNull(user);
        Assert.Equal("Jane", user.FirstName);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        await _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password123");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password456")
        );
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_SetsCurrentUser()
    {
        await _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password123");
        await _sut.LoginAsync("jane@example.com", "password123");

        var profile = await _sut.GetCurrentUserAsync();
        Assert.Equal("jane@example.com", profile.Email);
        Assert.Equal("Jane", profile.FirstName);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        await _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password123");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.LoginAsync("jane@example.com", "wrongpassword")
        );
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.LoginAsync("nobody@example.com", "password")
        );
    }

    [Fact]
    public async Task GetCurrentUserAsync_BeforeLogin_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetCurrentUserAsync());
    }

    [Fact]
    public async Task LogoutAsync_ClearsCurrentUser()
    {
        await _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password123");
        await _sut.LoginAsync("jane@example.com", "password123");
        await _sut.LogoutAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetCurrentUserAsync());
    }
}
