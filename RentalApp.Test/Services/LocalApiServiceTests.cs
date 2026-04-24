using Microsoft.EntityFrameworkCore;
using RentalApp.Services;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public class LocalApiServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public LocalApiServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private LocalApiService CreateSut() => new(_fixture.Context);

    // ── Register ───────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesUser()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();

        await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");

        var user = await _fixture.Context.Users.FirstOrDefaultAsync(u =>
            u.Email == "jane@example.com"
        );
        Assert.NotNull(user);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");

        var act = () => sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── Login ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_SetsCurrentUser()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");

        await sut.LoginAsync("jane@example.com", "Password1!");

        var user = await sut.GetCurrentUserAsync();
        Assert.Equal("jane@example.com", user.Email);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");

        var act = () => sut.LoginAsync("jane@example.com", "WrongPassword!");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        var sut = CreateSut();

        var act = () => sut.LoginAsync("nobody@example.com", "Password1!");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    // ── GetCurrentUser ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserAsync_BeforeLogin_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();

        var act = () => sut.GetCurrentUserAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task GetCurrentUserAsync_AfterLogin_ReturnsAuthenticatedUser()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");
        await sut.LoginAsync("jane@example.com", "Password1!");

        var user = await sut.GetCurrentUserAsync();

        Assert.Equal("jane@example.com", user.Email);
        Assert.Equal("Jane", user.FirstName);
    }

    // ── GetUser ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserAsync_ExistingUser_ReturnsProfile()
    {
        var sut = CreateSut();

        var user = await sut.GetUserAsync(1);

        Assert.Equal(1, user.Id);
        Assert.Equal("test@example.com", user.Email);
    }

    [Fact]
    public async Task GetUserAsync_NonExistentUser_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();

        var act = () => sut.GetUserAsync(999);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── Logout ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_AfterLogin_ClearsCurrentUser()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");
        await sut.LoginAsync("jane@example.com", "Password1!");

        await sut.LogoutAsync();

        var act = () => sut.GetCurrentUserAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }
}
