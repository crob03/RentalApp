using NSubstitute;
using RentalApp.Constants;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class AuthenticatedViewModelTests
{
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private TestableAuthenticatedViewModel CreateSut(bool confirmLogout = false) =>
        new(_tokenState, _credentialStore, _navigationService) { ConfirmResult = confirmLogout };

    private sealed class TestableAuthenticatedViewModel : AuthenticatedViewModel
    {
        public bool ConfirmResult { get; set; }

        public TestableAuthenticatedViewModel(
            AuthTokenState tokenState,
            ICredentialStore credentialStore,
            INavigationService navigationService
        )
            : base(tokenState, credentialStore, navigationService) { }

        protected override Task<bool> ConfirmLogoutAsync() => Task.FromResult(ConfirmResult);
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
    public async Task NavigateToProfileCommand_NavigatesToUserProfile()
    {
        var sut = CreateSut();

        await sut.NavigateToProfileCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.UserProfile);
    }
}
