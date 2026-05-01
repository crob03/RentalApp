using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Database.Repositories;
using RentalApp.Http;
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

    private (LocalApiService Sut, AuthTokenState TokenState) CreateSut()
    {
        var tokenState = new AuthTokenState();
        var sut = new LocalApiService(
            _fixture.ContextFactory,
            new ItemRepository(_fixture.ContextFactory),
            new CategoryRepository(_fixture.ContextFactory),
            tokenState
        );
        return (sut, tokenState);
    }

    // ── Register ───────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesUser()
    {
        await _fixture.ResetAsync();
        var (sut, _) = CreateSut();

        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );

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
        var (sut, _) = CreateSut();
        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );

        var act = () =>
            sut.RegisterAsync(new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!"));

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── Login ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_SetsCurrentUser()
    {
        await _fixture.ResetAsync();
        var (sut, tokenState) = CreateSut();
        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );

        var response = await sut.LoginAsync(new LoginRequest("jane@example.com", "Password1!"));
        tokenState.CurrentToken = response.Token;

        var user = await sut.GetCurrentUserAsync();
        Assert.Equal("jane@example.com", user.Email);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        await _fixture.ResetAsync();
        var (sut, _) = CreateSut();
        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );

        var act = () => sut.LoginAsync(new LoginRequest("jane@example.com", "WrongPassword!"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        var (sut, _) = CreateSut();

        var act = () => sut.LoginAsync(new LoginRequest("nobody@example.com", "Password1!"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    // ── GetCurrentUser ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserAsync_BeforeLogin_ThrowsInvalidOperationException()
    {
        var (sut, _) = CreateSut();

        var act = () => sut.GetCurrentUserAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task GetCurrentUserAsync_AfterLogin_ReturnsAuthenticatedUser()
    {
        await _fixture.ResetAsync();
        var (sut, tokenState) = CreateSut();
        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );
        var loginResponse = await sut.LoginAsync(
            new LoginRequest("jane@example.com", "Password1!")
        );
        tokenState.CurrentToken = loginResponse.Token;

        var user = await sut.GetCurrentUserAsync();

        Assert.Equal("jane@example.com", user.Email);
        Assert.Equal("Jane", user.FirstName);
    }

    // ── GetUserProfile ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUserProfileAsync_ExistingUser_ReturnsProfile()
    {
        var (sut, _) = CreateSut();

        var profile = await sut.GetUserProfileAsync(1);

        Assert.Equal(1, profile.Id);
    }

    [Fact]
    public async Task GetUserProfileAsync_NonExistentUser_ThrowsInvalidOperationException()
    {
        var (sut, _) = CreateSut();

        var act = () => sut.GetUserProfileAsync(999);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── Logout ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserAsync_AfterTokenCleared_ThrowsInvalidOperationException()
    {
        await _fixture.ResetAsync();
        var (sut, tokenState) = CreateSut();
        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );
        var loginResponse = await sut.LoginAsync(
            new LoginRequest("jane@example.com", "Password1!")
        );
        tokenState.CurrentToken = loginResponse.Token;

        tokenState.CurrentToken = null;

        var act = () => sut.GetCurrentUserAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── GetItemsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetItemsAsync_NoFilter_ReturnsItems()
    {
        var (sut, _) = CreateSut();

        var response = await sut.GetItemsAsync(new GetItemsRequest());

        Assert.NotEmpty(response.Items);
    }

    [Fact]
    public async Task GetItemsAsync_CategoryFilter_ReturnsMappedDtos()
    {
        var (sut, _) = CreateSut();

        var response = await sut.GetItemsAsync(new GetItemsRequest(Category: "tools"));

        Assert.All(response.Items, i => Assert.Equal("Tools", i.Category));
    }

    [Fact]
    public async Task GetItemsAsync_MapsLocationToLatLon()
    {
        var (sut, _) = CreateSut();

        var response = await sut.GetItemsAsync(new GetItemsRequest());

        Assert.All(
            response.Items,
            i =>
            {
                Assert.NotNull(i.Description);
            }
        );
    }

    // ── GetNearbyItemsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetNearbyItemsAsync_WithinRadius_ReturnsNearbyItems()
    {
        var (sut, _) = CreateSut();

        var response = await sut.GetNearbyItemsAsync(
            new GetNearbyItemsRequest(Lat: 55.9533, Lon: -3.1883, Radius: 5.0)
        );

        Assert.Equal(2, response.Items.Count);
    }

    [Fact]
    public async Task GetNearbyItemsAsync_PopulatesDistance()
    {
        var (sut, _) = CreateSut();

        var response = await sut.GetNearbyItemsAsync(
            new GetNearbyItemsRequest(Lat: 55.9533, Lon: -3.1883, Radius: 5.0)
        );

        Assert.All(response.Items, i => Assert.True(i.Distance >= 0));
    }

    // ── GetItemAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetItemAsync_ExistingId_ReturnsMappedItem()
    {
        var (sut, _) = CreateSut();

        var item = await sut.GetItemAsync(1);

        Assert.Equal(1, item.Id);
        Assert.Equal("Test Drill", item.Title);
    }

    [Fact]
    public async Task GetItemAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var (sut, _) = CreateSut();

        var act = () => sut.GetItemAsync(999);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── CreateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CreateItemAsync_AuthenticatedUser_CreatesAndReturnsItem()
    {
        await _fixture.ResetAsync();
        var (sut, tokenState) = CreateSut();
        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );
        var loginResponse = await sut.LoginAsync(
            new LoginRequest("jane@example.com", "Password1!")
        );
        tokenState.CurrentToken = loginResponse.Token;

        var item = await sut.CreateItemAsync(
            new CreateItemRequest("My Drill", "desc", 10.0, 1, 55.9533, -3.1883)
        );

        Assert.True(item.Id > 0);
        Assert.Equal("My Drill", item.Title);
        Assert.True(item.IsAvailable);
    }

    [Fact]
    public async Task CreateItemAsync_NotAuthenticated_ThrowsInvalidOperationException()
    {
        var (sut, _) = CreateSut();

        var act = () =>
            sut.CreateItemAsync(new CreateItemRequest("Drill", null, 10.0, 1, 55.9533, -3.1883));

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── UpdateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItemAsync_ValidUpdate_ReturnsUpdatedItem()
    {
        await _fixture.ResetItemsAsync();
        var (sut, _) = CreateSut();

        var item = await sut.UpdateItemAsync(
            1,
            new UpdateItemRequest("Updated Title", null, null, null)
        );

        Assert.Equal("Updated Title", item.Title);
    }

    // ── GetCategoriesAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetCategoriesAsync_ReturnsAllCategories()
    {
        var (sut, _) = CreateSut();

        var categories = await sut.GetCategoriesAsync();

        Assert.Equal(2, categories.Categories.Count);
    }
}
