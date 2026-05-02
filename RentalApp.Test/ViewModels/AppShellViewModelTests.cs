using NSubstitute;
using RentalApp.Constants;
using RentalApp.Http;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class AppShellViewModelTests
{
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private TestableAppShellViewModel CreateSut(bool confirmLogout = false) =>
        new(_tokenState, _credentialStore, _navigationService) { ConfirmResult = confirmLogout };

    private sealed class TestableAppShellViewModel : AppShellViewModel
    {
        public bool ConfirmResult { get; set; }

        public TestableAppShellViewModel(
            AuthTokenState tokenState,
            ICredentialStore credentialStore,
            INavigationService navigationService
        )
            : base(tokenState, credentialStore, navigationService) { }

        protected override Task<bool> ConfirmLogoutAsync() => Task.FromResult(ConfirmResult);
    }

    // ── LogoutCommand — CanExecute ─────────────────────────────────────

    [Fact]
    public void LogoutCommand_WhenSessionActive_CanExecute()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut();

        Assert.True(sut.LogoutCommand.CanExecute(null));
    }

    [Fact]
    public void LogoutCommand_WhenNoSession_CannotExecute()
    {
        var sut = CreateSut();

        Assert.False(sut.LogoutCommand.CanExecute(null));
    }

    // ── LogoutAsync — confirmed ────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_WhenConfirmed_ClearsToken()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut(confirmLogout: true);

        await sut.LogoutCommand.ExecuteAsync(null);

        Assert.Null(_tokenState.CurrentToken);
    }

    [Fact]
    public async Task LogoutAsync_WhenConfirmed_ClearsCredentials()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut(confirmLogout: true);

        await sut.LogoutCommand.ExecuteAsync(null);

        await _credentialStore.Received(1).ClearAsync();
    }

    [Fact]
    public async Task LogoutAsync_WhenConfirmed_NavigatesToLogin()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut(confirmLogout: true);

        await sut.LogoutCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Login);
    }

    // ── LogoutAsync — cancelled ────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_WhenCancelled_DoesNotClearToken()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut(confirmLogout: false);

        await sut.LogoutCommand.ExecuteAsync(null);

        Assert.Equal("eyJ...", _tokenState.CurrentToken);
    }

    [Fact]
    public async Task LogoutAsync_WhenCancelled_DoesNotNavigate()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut(confirmLogout: false);

        await sut.LogoutCommand.ExecuteAsync(null);

        await _navigationService.DidNotReceive().NavigateToAsync(Arg.Any<string>());
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
