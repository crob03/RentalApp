using NSubstitute;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class RegisterViewModelTests
{
    private readonly IAuthenticationService _authService = Substitute.For<IAuthenticationService>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private RegisterViewModel CreateSut() => new(_authService, _navigationService);

    private static RegisterViewModel WithValidForm(RegisterViewModel sut)
    {
        sut.FirstName = "Jane";
        sut.LastName = "Doe";
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";
        sut.ConfirmPassword = "Password1!";
        sut.AcceptTerms = true;
        return sut;
    }

    // ── RegisterAsync — navigation ─────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_SuccessfulRegistration_NavigatesBack()
    {
        _authService
            .RegisterAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            )
            .Returns(AuthenticationResult.Success());
        var sut = WithValidForm(CreateSut());

        await sut.RegisterCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateBackAsync();
    }

    [Fact]
    public async Task RegisterAsync_FailedRegistration_DoesNotNavigate()
    {
        _authService
            .RegisterAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            )
            .Returns(AuthenticationResult.Failure("Email already registered"));
        var sut = WithValidForm(CreateSut());

        await sut.RegisterCommand.ExecuteAsync(null);

        await _navigationService.DidNotReceive().NavigateBackAsync();
    }

    [Fact]
    public async Task RegisterAsync_InvalidForm_DoesNotNavigate()
    {
        var sut = CreateSut();
        // All fields left empty — form validation will fail before the service is called

        await sut.RegisterCommand.ExecuteAsync(null);

        await _navigationService.DidNotReceive().NavigateBackAsync();
        await _authService
            .DidNotReceive()
            .RegisterAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
    }

    // ── RegisterAsync — error state ────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_FailedRegistration_SetsErrorFromService()
    {
        _authService
            .RegisterAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            )
            .Returns(AuthenticationResult.Failure("Email already registered"));
        var sut = WithValidForm(CreateSut());

        await sut.RegisterCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Email already registered", sut.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_InvalidForm_SetsValidationError()
    {
        var sut = CreateSut();
        sut.FirstName = "";

        await sut.RegisterCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
    }

    [Fact]
    public async Task RegisterAsync_SuccessfulRegistration_ClearsError()
    {
        _authService
            .RegisterAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            )
            .Returns(AuthenticationResult.Success());
        var sut = WithValidForm(CreateSut());

        await sut.RegisterCommand.ExecuteAsync(null);

        Assert.False(sut.HasError);
    }

    // ── NavigateBackToLoginAsync ───────────────────────────────────────

    [Fact]
    public async Task NavigateBackToLoginCommand_NavigatesBack()
    {
        var sut = CreateSut();

        await sut.NavigateBackToLoginCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateBackAsync();
    }

    // ── RegisterCommand — CanExecute ───────────────────────────────────

    [Fact]
    public void RegisterCommand_WhenNotBusy_CanExecute()
    {
        var sut = CreateSut();

        Assert.True(sut.RegisterCommand.CanExecute(null));
    }

    [Fact]
    public void RegisterCommand_WhileIsBusy_CannotExecute()
    {
        var sut = CreateSut();
        sut.IsBusy = true;

        Assert.False(sut.RegisterCommand.CanExecute(null));
    }
}
