using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Constants;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class MainViewModelTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();

    private static CurrentUserResponse MakeUser(
        string firstName = "Jane",
        string lastName = "Doe"
    ) => new(1, "jane@example.com", firstName, lastName, null, 0, 0, DateTime.UtcNow);

    private MainViewModel CreateSut() =>
        new(_authService, _navigationService, _tokenState, _credentialStore);

    // ── InitializeAsync ────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_LoadsUserAndSetsWelcomeMessage()
    {
        _authService.GetCurrentUserAsync().Returns(MakeUser());
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal("Welcome, Jane Doe!", sut.WelcomeMessage);
    }

    [Fact]
    public async Task InitializeAsync_SetsCurrentUser()
    {
        var user = MakeUser();
        _authService.GetCurrentUserAsync().Returns(user);
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal(user, sut.CurrentUser);
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_SetsHasError()
    {
        _authService.GetCurrentUserAsync().ThrowsAsync(new Exception("network error"));
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.True(sut.HasError);
        Assert.Equal("network error", sut.ErrorMessage);
    }

    // ── RefreshDataAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RefreshDataAsync_ReloadsUser()
    {
        _authService.GetCurrentUserAsync().Returns(MakeUser("Alice", "Smith"));
        var sut = CreateSut();

        await sut.RefreshDataCommand.ExecuteAsync(null);

        Assert.Equal("Welcome, Alice Smith!", sut.WelcomeMessage);
    }

    [Fact]
    public async Task RefreshDataAsync_IsBusyFalseAfterCompletion()
    {
        _authService.GetCurrentUserAsync().Returns(MakeUser());
        var sut = CreateSut();

        await sut.RefreshDataCommand.ExecuteAsync(null);

        Assert.False(sut.IsBusy);
    }

    // ── Navigation commands ────────────────────────────────────────────

    [Fact]
    public async Task NavigateToItemsListCommand_NavigatesToItemsList()
    {
        await CreateSut().NavigateToItemsListCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.ItemsList);
    }

    [Fact]
    public async Task NavigateToNearbyItemsCommand_NavigatesToNearbyItems()
    {
        await CreateSut().NavigateToNearbyItemsCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.NearbyItems);
    }

    [Fact]
    public async Task NavigateToCreateItemCommand_NavigatesToCreateItem()
    {
        await CreateSut().NavigateToCreateItemCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.CreateItem);
    }
}
