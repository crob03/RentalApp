using NSubstitute;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class MainViewModelTests
{
    private readonly IAuthenticationService _authService = Substitute.For<IAuthenticationService>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private static User MakeUser(string firstName = "Jane", string lastName = "Doe") =>
        new(1, firstName, lastName, null, 0, 0, "jane@example.com", null, null);

    private MainViewModel CreateSut() => new(_authService, _navigationService);

    // ── Constructor — data loading ─────────────────────────────────────

    [Fact]
    public void Constructor_WithCurrentUser_SetsWelcomeMessage()
    {
        _authService.CurrentUser.Returns(MakeUser());

        var sut = CreateSut();

        Assert.Equal("Welcome, Jane Doe!", sut.WelcomeMessage);
    }

    [Fact]
    public void Constructor_WithCurrentUser_SetsCurrentUser()
    {
        var user = MakeUser();
        _authService.CurrentUser.Returns(user);

        var sut = CreateSut();

        Assert.Equal(user, sut.CurrentUser);
    }

    [Fact]
    public void Constructor_WithNoCurrentUser_WelcomeMessageIsEmpty()
    {
        _authService.CurrentUser.Returns((User?)null);

        var sut = CreateSut();

        Assert.Equal(string.Empty, sut.WelcomeMessage);
    }

    // ── RefreshDataAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RefreshDataAsync_UpdatesCurrentUserAndWelcomeMessage()
    {
        _authService.CurrentUser.Returns((User?)null);
        var sut = CreateSut();
        _authService.CurrentUser.Returns(MakeUser("Alice", "Smith"));

        await sut.RefreshDataCommand.ExecuteAsync(null);

        Assert.Equal("Welcome, Alice Smith!", sut.WelcomeMessage);
    }

    [Fact]
    public async Task RefreshDataAsync_IsBusyFalseAfterCompletion()
    {
        var sut = CreateSut();

        await sut.RefreshDataCommand.ExecuteAsync(null);

        Assert.False(sut.IsBusy);
    }

    // ── Navigation commands ────────────────────────────────────────────

    [Fact]
    public async Task NavigateToProfileCommand_NavigatesToTemp()
    {
        var sut = CreateSut();

        await sut.NavigateToProfileCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Temp);
    }

    [Fact]
    public async Task NavigateToSettingsCommand_NavigatesToTemp()
    {
        var sut = CreateSut();

        await sut.NavigateToSettingsCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Temp);
    }

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
}
