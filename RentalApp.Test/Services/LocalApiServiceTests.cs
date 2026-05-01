using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Repositories;
using RentalApp.Services;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public class LocalApiServiceTests : IClassFixture<DatabaseFixture<LocalApiServiceTests>>
{
    private readonly DatabaseFixture<LocalApiServiceTests> _fixture;

    public LocalApiServiceTests(DatabaseFixture<LocalApiServiceTests> fixture)
    {
        _fixture = fixture;
    }

    private LocalApiService CreateSut() =>
        new(
            _fixture.ContextFactory,
            new ItemRepository(_fixture.ContextFactory),
            new CategoryRepository(_fixture.ContextFactory)
        );

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

        Assert.All(
            items,
            i =>
            {
                Assert.NotNull(i.Latitude);
                Assert.NotNull(i.Longitude);
            }
        );
    }

    // ── GetNearbyItemsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetNearbyItemsAsync_WithinRadius_ReturnsNearbyItems()
    {
        var sut = CreateSut();

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
}
