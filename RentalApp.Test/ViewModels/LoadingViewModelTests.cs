using NSubstitute;
using RentalApp.Constants;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class LoadingViewModelTests
{
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();
    private readonly IAuthenticationService _authService = Substitute.For<IAuthenticationService>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private LoadingViewModel CreateSut() => new(_credentialStore, _authService, _navigationService);

    // ── InitializeAsync ────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_NoSavedCredentials_NavigatesToLogin()
    {
        _credentialStore.GetAsync().Returns((ValueTuple<string, string>?)null);
        var sut = CreateSut();

        await sut.InitializeAsync();

        await _navigationService.Received(1).NavigateToAsync(Routes.Login);
        await _authService.DidNotReceive().LoginAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_LoginSucceeds_NavigatesToMain()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "Password1!"));
        _authService
            .LoginAsync("jane@example.com", "Password1!")
            .Returns(AuthenticationResult.Success());
        var sut = CreateSut();

        await sut.InitializeAsync();

        await _navigationService.Received(1).NavigateToAsync(Routes.Main);
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_LoginFails_NavigatesToLogin()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "expired"));
        _authService
            .LoginAsync("jane@example.com", "expired")
            .Returns(AuthenticationResult.Failure("Invalid credentials"));
        var sut = CreateSut();

        await sut.InitializeAsync();

        await _navigationService.Received(1).NavigateToAsync(Routes.Login);
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_UsesStoredEmailAndPassword()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "Password1!"));
        _authService
            .LoginAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(AuthenticationResult.Success());
        var sut = CreateSut();

        await sut.InitializeAsync();

        await _authService.Received(1).LoginAsync("jane@example.com", "Password1!");
    }
}
