using NSubstitute;
using RentalApp.Constants;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class LoginViewModelTests
{
    private readonly IAuthenticationService _authService = Substitute.For<IAuthenticationService>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();

    private LoginViewModel CreateSut() => new(_authService, _navigationService, _credentialStore);

    // ── LoginAsync — navigation ────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_SuccessfulAuth_NavigatesToMain()
    {
        _authService
            .LoginAsync("jane@example.com", "Password1!", false)
            .Returns(AuthenticationResult.Success());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";

        await sut.LoginCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Main);
    }

    [Fact]
    public async Task LoginAsync_FailedAuth_DoesNotNavigate()
    {
        _authService
            .LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(AuthenticationResult.Failure("Invalid credentials"));
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "wrong";

        await sut.LoginCommand.ExecuteAsync(null);

        await _navigationService.DidNotReceive().NavigateToAsync(Arg.Any<string>());
    }

    [Theory]
    [InlineData("", "Password1!")]
    [InlineData("jane@example.com", "")]
    [InlineData("", "")]
    public async Task LoginAsync_EmptyFields_DoesNotNavigate(string email, string password)
    {
        var sut = CreateSut();
        sut.Email = email;
        sut.Password = password;

        await sut.LoginCommand.ExecuteAsync(null);

        await _navigationService.DidNotReceive().NavigateToAsync(Arg.Any<string>());
    }

    // ── LoginAsync — error state ───────────────────────────────────────

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

    [Fact]
    public async Task LoginAsync_FailedAuth_SetsErrorFromService()
    {
        _authService
            .LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(AuthenticationResult.Failure("Invalid credentials"));
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "wrong";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Invalid credentials", sut.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_SuccessfulAuth_ClearsError()
    {
        _authService
            .LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(AuthenticationResult.Success());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.False(sut.HasError);
    }

    // ── NavigateToRegisterAsync ────────────────────────────────────────

    [Fact]
    public async Task NavigateToRegisterCommand_NavigatesToRegister()
    {
        var sut = CreateSut();

        await sut.NavigateToRegisterCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Register);
    }

    // ── LoginAsync — RememberMe ────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithRememberMeTrue_PassesRememberMeToService()
    {
        _authService
            .LoginAsync(Arg.Any<string>(), Arg.Any<string>(), true)
            .Returns(AuthenticationResult.Success());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";
        sut.RememberMe = true;

        await sut.LoginCommand.ExecuteAsync(null);

        await _authService.Received(1).LoginAsync("jane@example.com", "Password1!", true);
    }

    [Fact]
    public async Task LoginAsync_WithRememberMeFalse_PassesRememberMeToService()
    {
        _authService
            .LoginAsync(Arg.Any<string>(), Arg.Any<string>(), false)
            .Returns(AuthenticationResult.Success());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";
        sut.RememberMe = false;

        await sut.LoginCommand.ExecuteAsync(null);

        await _authService.Received(1).LoginAsync("jane@example.com", "Password1!", false);
    }

    // ── LoginCommand — CanExecute ──────────────────────────────────────

    [Fact]
    public void LoginCommand_WhenNotBusy_CanExecute()
    {
        var sut = CreateSut();

        Assert.True(sut.LoginCommand.CanExecute(null));
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
        Assert.Equal(string.Empty, sut.ErrorMessage);
    }
}
