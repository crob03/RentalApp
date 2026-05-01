// RentalApp.Test/Services/LocalApiServiceTests.cs
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
    private readonly AuthTokenState _tokenState = new();

    public LocalApiServiceTests(DatabaseFixture<LocalApiServiceTests> fixture)
    {
        _fixture = fixture;
    }

    private LocalApiService CreateSut() =>
        new(
            _fixture.ContextFactory,
            new ItemRepository(_fixture.ContextFactory),
            new CategoryRepository(_fixture.ContextFactory),
            _tokenState
        );

    // ── Register ───────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesUser()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();

        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );

        var user = await _fixture.Context.Users.FirstOrDefaultAsync(u =>
            u.Email == "jane@example.com"
        );
        Assert.NotNull(user);
        Assert.Equal("Jane", user.FirstName);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );

        var act = () =>
            sut.RegisterAsync(new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!"));

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── Login ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsUserIdAsToken()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        var reg = await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );

        var response = await sut.LoginAsync(new LoginRequest("jane@example.com", "Password1!"));

        Assert.Equal(reg.Id.ToString(), response.Token);
        Assert.Equal(reg.Id, response.UserId);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );

        var act = () => sut.LoginAsync(new LoginRequest("jane@example.com", "WrongPassword!"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        var sut = CreateSut();

        var act = () => sut.LoginAsync(new LoginRequest("nobody@example.com", "Password1!"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    // ── GetCurrentUser ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserAsync_NoSession_ThrowsInvalidOperationException()
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
        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );
        var loginResponse = await sut.LoginAsync(
            new LoginRequest("jane@example.com", "Password1!")
        );
        _tokenState.CurrentToken = loginResponse.Token;

        var user = await sut.GetCurrentUserAsync();

        Assert.Equal("jane@example.com", user.Email);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal(0, user.ItemsListed);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithItems_ReturnsCorrectItemsListedCount()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );
        var loginResponse = await sut.LoginAsync(
            new LoginRequest("jane@example.com", "Password1!")
        );
        _tokenState.CurrentToken = loginResponse.Token;
        await sut.CreateItemAsync(
            new CreateItemRequest("My Drill", null, 5.0, CategoryId: 1, 55.9533, -3.1883)
        );

        var user = await sut.GetCurrentUserAsync();

        Assert.Equal(1, user.ItemsListed);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithItems_ReturnsCorrectItemsListedCount()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );
        var loginResponse = await sut.LoginAsync(
            new LoginRequest("jane@example.com", "Password1!")
        );
        _tokenState.CurrentToken = loginResponse.Token;
        await sut.CreateItemAsync(
            new CreateItemRequest("My Drill", null, 5.0, CategoryId: 1, 55.9533, -3.1883)
        );

        var profile = await sut.GetUserProfileAsync(loginResponse.UserId);

        Assert.Equal(1, profile.ItemsListed);
    }

    // ── GetItems ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetItemsAsync_NoFilter_ReturnsItems()
    {
        var sut = CreateSut();

        var response = await sut.GetItemsAsync(new GetItemsRequest());

        Assert.NotEmpty(response.Items);
    }

    [Fact]
    public async Task GetItemsAsync_CategoryFilter_ReturnsMappedDtos()
    {
        var sut = CreateSut();

        var response = await sut.GetItemsAsync(new GetItemsRequest(Category: "tools"));

        Assert.All(response.Items, i => Assert.Equal("Tools", i.Category));
    }

    // ── GetNearbyItems ─────────────────────────────────────────────────

    [Fact]
    public async Task GetNearbyItemsAsync_WithinRadius_ReturnsNearbyItems()
    {
        var sut = CreateSut();

        var response = await sut.GetNearbyItemsAsync(
            new GetNearbyItemsRequest(55.9533, -3.1883, 5.0)
        );

        Assert.Equal(2, response.Items.Count);
    }

    [Fact]
    public async Task GetNearbyItemsAsync_PopulatesDistance()
    {
        var sut = CreateSut();

        var response = await sut.GetNearbyItemsAsync(
            new GetNearbyItemsRequest(55.9533, -3.1883, 5.0)
        );

        Assert.All(response.Items, i => Assert.True(i.Distance >= 0));
    }

    [Fact]
    public async Task GetNearbyItemsAsync_PopulatesSearchLocation()
    {
        var sut = CreateSut();

        var response = await sut.GetNearbyItemsAsync(
            new GetNearbyItemsRequest(55.9533, -3.1883, 5.0)
        );

        Assert.Equal(55.9533, response.SearchLocation.Latitude);
        Assert.Equal(-3.1883, response.SearchLocation.Longitude);
    }

    // ── GetItem ────────────────────────────────────────────────────────

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

    // ── CreateItem ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateItemAsync_AuthenticatedUser_CreatesAndReturnsItem()
    {
        await _fixture.ResetAsync();
        var sut = CreateSut();
        var reg = await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );
        _tokenState.CurrentToken = reg.Id.ToString();

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
        var sut = CreateSut();

        var act = () =>
            sut.CreateItemAsync(new CreateItemRequest("Drill", null, 10.0, 1, 55.9533, -3.1883));

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── UpdateItem ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItemAsync_ValidUpdate_ReturnsUpdatedItem()
    {
        await _fixture.ResetItemsAsync();
        var sut = CreateSut();

        var item = await sut.UpdateItemAsync(
            1,
            new UpdateItemRequest("Updated Title", null, null, null)
        );

        Assert.Equal("Updated Title", item.Title);
    }

    // ── GetCategories ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCategoriesAsync_ReturnsAllCategories()
    {
        var sut = CreateSut();

        var response = await sut.GetCategoriesAsync();

        Assert.Equal(2, response.Categories.Count);
    }
}
