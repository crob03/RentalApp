using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class RegisterViewModelTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
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

    private static RegisterResponse FakeRegister() =>
        new(1, "jane@example.com", "Jane", "Doe", DateTime.UtcNow);

    // ── RegisterAsync — navigation ─────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_Success_NavigatesBack()
    {
        _authService.RegisterAsync(Arg.Any<RegisterRequest>()).Returns(FakeRegister());
        var sut = WithValidForm(CreateSut());

        await sut.RegisterCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateBackAsync();
    }

    [Fact]
    public async Task RegisterAsync_ServiceThrows_DoesNotNavigate()
    {
        _authService.RegisterAsync(Arg.Any<RegisterRequest>())
            .ThrowsAsync(new HttpRequestException("Email already registered"));
        var sut = WithValidForm(CreateSut());

        await sut.RegisterCommand.ExecuteAsync(null);

        await _navigationService.DidNotReceive().NavigateBackAsync();
    }

    [Fact]
    public async Task RegisterAsync_InvalidForm_DoesNotCallService()
    {
        var sut = CreateSut();

        await sut.RegisterCommand.ExecuteAsync(null);

        await _authService.DidNotReceive().RegisterAsync(Arg.Any<RegisterRequest>());
    }

    // ── RegisterAsync — error state ────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ServiceThrows_SetsError()
    {
        _authService.RegisterAsync(Arg.Any<RegisterRequest>())
            .ThrowsAsync(new HttpRequestException("Email already registered"));
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
    public async Task RegisterAsync_Success_ClearsError()
    {
        _authService.RegisterAsync(Arg.Any<RegisterRequest>()).Returns(FakeRegister());
        var sut = WithValidForm(CreateSut());

        await sut.RegisterCommand.ExecuteAsync(null);

        Assert.False(sut.HasError);
    }

    // ── NavigateBackToLoginAsync ───────────────────────────────────────

    [Fact]
    public async Task NavigateBackToLoginCommand_NavigatesBack()
    {
        await CreateSut().NavigateBackToLoginCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateBackAsync();
    }

    // ── RegisterCommand — CanExecute ───────────────────────────────────

    [Fact]
    public void RegisterCommand_WhenNotBusy_CanExecute()
    {
        Assert.True(CreateSut().RegisterCommand.CanExecute(null));
    }

    [Fact]
    public void RegisterCommand_WhileIsBusy_CannotExecute()
    {
        var sut = CreateSut();
        sut.IsBusy = true;

        Assert.False(sut.RegisterCommand.CanExecute(null));
    }
}
