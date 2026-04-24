using NSubstitute;
using RentalApp.Constants;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class AppShellViewModelTests
{
    private readonly IAuthenticationService _authService = Substitute.For<IAuthenticationService>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private AppShellViewModel CreateSut() => new(_authService, _navigationService);

    // ── LogoutAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_CallsLogoutOnAuthService()
    {
        _authService.IsAuthenticated.Returns(true);
        var sut = CreateSut();

        await sut.LogoutCommand.ExecuteAsync(null);

        await _authService.Received(1).LogoutAsync();
    }

    [Fact]
    public async Task LogoutAsync_NavigatesToLoginPage()
    {
        _authService.IsAuthenticated.Returns(true);
        var sut = CreateSut();

        await sut.LogoutCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.LoginPage);
    }

    // ── LogoutCommand — CanExecute ─────────────────────────────────────

    [Fact]
    public void LogoutCommand_WhenAuthenticated_CanExecute()
    {
        _authService.IsAuthenticated.Returns(true);
        var sut = CreateSut();

        Assert.True(sut.LogoutCommand.CanExecute(null));
    }

    [Fact]
    public void LogoutCommand_WhenNotAuthenticated_CannotExecute()
    {
        _authService.IsAuthenticated.Returns(false);
        var sut = CreateSut();

        Assert.False(sut.LogoutCommand.CanExecute(null));
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
}
