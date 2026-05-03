using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Services.Auth;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public class LocalAuthServiceTests
    : IClassFixture<DatabaseFixture<LocalAuthServiceTests>>,
        IAsyncLifetime
{
    private readonly DatabaseFixture<LocalAuthServiceTests> _fixture;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly AuthTokenState _tokenState = new();

    public LocalAuthServiceTests(DatabaseFixture<LocalAuthServiceTests> fixture)
    {
        _fixture = fixture;
        _contextFactory = fixture.ContextFactory;
    }

    public async Task InitializeAsync()
    {
        _tokenState.CurrentToken = null;
        await _fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private LocalAuthService CreateSut() =>
        new(new UserRepository(_contextFactory), new ItemRepository(_contextFactory), _tokenState);

    private async Task<int> SeedUserAsync(
        string email = "jane@example.com",
        string password = "Password1!"
    )
    {
        await using var ctx = _contextFactory.CreateDbContext();
        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        var user = new RentalApp.Database.Models.User
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, salt),
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenEqualToUserId()
    {
        var userId = await SeedUserAsync();

        var result = await CreateSut()
            .LoginAsync(new LoginRequest("jane@example.com", "Password1!"));

        Assert.Equal(userId.ToString(), result.Token);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        await SeedUserAsync("wrongpass@example.com");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateSut().LoginAsync(new LoginRequest("wrongpass@example.com", "wrong"))
        );
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateSut().LoginAsync(new LoginRequest("nobody@example.com", "Password1!"))
        );
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsEmailAndName()
    {
        var result = await CreateSut()
            .RegisterAsync(
                new RegisterRequest("Alice", "Smith", "alice@example.com", "Password1!")
            );

        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal("Alice", result.FirstName);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        await SeedUserAsync("dup@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut()
                .RegisterAsync(new RegisterRequest("X", "Y", "dup@example.com", "Password1!"))
        );
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithValidToken_ReturnsUser()
    {
        var userId = await SeedUserAsync("me@example.com");
        _tokenState.CurrentToken = userId.ToString();

        var result = await CreateSut().GetCurrentUserAsync();

        Assert.Equal("me@example.com", result.Email);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithNoSession_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().GetCurrentUserAsync()
        );
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmailDifferentCase_ThrowsInvalidOperationException()
    {
        await CreateSut()
            .RegisterAsync(
                new RegisterRequest("Alice", "Smith", "Alice@example.com", "Password1!")
            );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut()
                .RegisterAsync(
                    new RegisterRequest("Alice", "Smith", "alice@example.com", "Password1!")
                )
        );
    }

    [Fact]
    public async Task LoginAsync_MixedCaseEmail_SucceedsWithCorrectPassword()
    {
        await SeedUserAsync("jane@example.com");

        var result = await CreateSut()
            .LoginAsync(new LoginRequest("JANE@EXAMPLE.COM", "Password1!"));

        Assert.NotNull(result.Token);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithExistingUser_ReturnsProfile()
    {
        var userId = await SeedUserAsync("profile@example.com");

        var result = await CreateSut().GetUserProfileAsync(userId);

        Assert.Equal(userId, result.Id);
        Assert.Equal("Jane", result.FirstName);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithNonExistentUser_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().GetUserProfileAsync(999)
        );
    }

    [Fact]
    public async Task GetCurrentUserAsync_CalledTwice_ReturnsSameInstance()
    {
        var userId = await SeedUserAsync("cached@example.com");
        _tokenState.CurrentToken = userId.ToString();
        var sut = CreateSut();

        var first = await sut.GetCurrentUserAsync();
        var second = await sut.GetCurrentUserAsync();

        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetCurrentUserAsync_AfterLogin_ReturnsFreshResult()
    {
        var userId = await SeedUserAsync("fresh@example.com");
        _tokenState.CurrentToken = userId.ToString();
        var sut = CreateSut();

        var first = await sut.GetCurrentUserAsync();
        await sut.LoginAsync(new LoginRequest("fresh@example.com", "Password1!"));
        var second = await sut.GetCurrentUserAsync();

        Assert.NotSame(first, second);
    }
}
