using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class LoginViewModelTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private LoginViewModel CreateSut() =>
        new(_authService, _tokenState, _credentialStore, _navigationService);

    private static LoginResponse FakeLogin() => new("eyJ...", DateTime.UtcNow.AddHours(1), 1);

    // ── LoginAsync — success ───────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_SetsTokenOnAuthTokenState()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Equal("eyJ...", _tokenState.CurrentToken);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_NavigatesToMain()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";

        await sut.LoginCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Main);
    }

    [Fact]
    public async Task LoginAsync_RememberMeTrue_SavesCredentials()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";
        sut.RememberMe = true;

        await sut.LoginCommand.ExecuteAsync(null);

        await _credentialStore.Received(1).SaveAsync("jane@example.com", "Password1!");
    }

    [Fact]
    public async Task LoginAsync_RememberMeFalse_DoesNotSaveCredentials()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";
        sut.RememberMe = false;

        await sut.LoginCommand.ExecuteAsync(null);

        await _credentialStore.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    // ── LoginAsync — failure ───────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ServiceThrows_SetsError()
    {
        _authService
            .LoginAsync(Arg.Any<LoginRequest>())
            .ThrowsAsync(new HttpRequestException("Invalid credentials"));
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "wrong";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Invalid credentials", sut.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ServiceThrows_DoesNotNavigate()
    {
        _authService
            .LoginAsync(Arg.Any<LoginRequest>())
            .ThrowsAsync(new HttpRequestException("Invalid credentials"));
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "wrong";

        await sut.LoginCommand.ExecuteAsync(null);

        await _navigationService.DidNotReceive().NavigateToAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_ServiceThrows_DoesNotSetToken()
    {
        _authService
            .LoginAsync(Arg.Any<LoginRequest>())
            .ThrowsAsync(new HttpRequestException("Invalid credentials"));
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "wrong";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Null(_tokenState.CurrentToken);
    }

    // ── LoginAsync — empty fields ──────────────────────────────────────

    [Theory]
    [InlineData("", "Password1!")]
    [InlineData("jane@example.com", "")]
    [InlineData("", "")]
    public async Task LoginAsync_EmptyFields_DoesNotCallService(string email, string password)
    {
        var sut = CreateSut();
        sut.Email = email;
        sut.Password = password;

        await sut.LoginCommand.ExecuteAsync(null);

        await _authService.DidNotReceive().LoginAsync(Arg.Any<LoginRequest>());
    }

    [Fact]
    public async Task LoginAsync_EmptyFields_SetsError()
    {
        var sut = CreateSut();
        sut.Email = "";
        sut.Password = "";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Please enter both email and password", sut.ErrorMessage);
    }

    // ── LoginCommand — CanExecute ──────────────────────────────────────

    [Fact]
    public void LoginCommand_WhenNotBusy_CanExecute()
    {
        Assert.True(CreateSut().LoginCommand.CanExecute(null));
    }

    [Fact]
    public void LoginCommand_WhileIsBusy_CannotExecute()
    {
        var sut = CreateSut();
        sut.IsBusy = true;

        Assert.False(sut.LoginCommand.CanExecute(null));
    }

    // ── InitializeAsync ────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_NoSavedCredentials_FieldsRemainDefault()
    {
        _credentialStore.GetAsync().Returns((ValueTuple<string, string>?)null);
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal(string.Empty, sut.Email);
        Assert.Equal(string.Empty, sut.Password);
        Assert.False(sut.RememberMe);
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_PopulatesFieldsAndSetsRememberMe()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "Password1!"));
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal("jane@example.com", sut.Email);
        Assert.Equal("Password1!", sut.Password);
        Assert.True(sut.RememberMe);
    }

    // ── ApplyQueryAttributes ───────────────────────────────────────────

    [Fact]
    public void ApplyQueryAttributes_SessionExpiredTrue_SetsError()
    {
        var sut = CreateSut();

        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["sessionExpired"] = true });

        Assert.True(sut.HasError);
        Assert.Equal("Your session has expired. Please log in again.", sut.ErrorMessage);
    }

    [Fact]
    public void ApplyQueryAttributes_OtherQuery_ClearsAnyExistingError()
    {
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["sessionExpired"] = true });

        sut.ApplyQueryAttributes(new Dictionary<string, object>());

        Assert.False(sut.HasError);
    }

    // ── NavigateToRegisterAsync ────────────────────────────────────────

    [Fact]
    public async Task NavigateToRegisterCommand_NavigatesToRegister()
    {
        await CreateSut().NavigateToRegisterCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Register);
    }
}
