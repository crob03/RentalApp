using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class LoadingViewModelTests
{
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private LoadingViewModel CreateSut() =>
        new(_credentialStore, _authService, _tokenState, _navigationService);

    private static LoginResponse FakeLogin() => new("eyJ...", DateTime.UtcNow.AddHours(1), 1);

    [Fact]
    public async Task InitializeAsync_NoSavedCredentials_NavigatesToLogin()
    {
        _credentialStore.GetAsync().Returns((ValueTuple<string, string>?)null);

        await CreateSut().InitializeAsync();

        await _navigationService.Received(1).NavigateToAsync(Routes.Login);
        await _authService.DidNotReceive().LoginAsync(Arg.Any<LoginRequest>());
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_LoginSucceeds_NavigatesToMain()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "Password1!"));
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());

        await CreateSut().InitializeAsync();

        await _navigationService.Received(1).NavigateToAsync(Routes.Main);
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_LoginSucceeds_SetsToken()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "Password1!"));
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());

        await CreateSut().InitializeAsync();

        Assert.Equal("eyJ...", _tokenState.CurrentToken);
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_LoginFails_NavigatesToLogin()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "expired"));
        _authService
            .LoginAsync(Arg.Any<LoginRequest>())
            .ThrowsAsync(new HttpRequestException("Invalid credentials"));

        await CreateSut().InitializeAsync();

        await _navigationService.Received(1).NavigateToAsync(Routes.Login);
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_UsesStoredEmailAndPassword()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "Password1!"));
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());

        await CreateSut().InitializeAsync();

        await _authService
            .Received(1)
            .LoginAsync(
                Arg.Is<LoginRequest>(r =>
                    r.Email == "jane@example.com" && r.Password == "Password1!"
                )
            );
    }
}
