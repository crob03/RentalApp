// RentalApp.Test/Services/AuthenticationServiceTests.cs
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class AuthenticationServiceTests
{
    private readonly IApiService _api = Substitute.For<IApiService>();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();
    private readonly AuthTokenState _tokenState = new();

    private AuthenticationService CreateSut() => new(_api, _credentialStore, _tokenState);

    private static CurrentUserResponse FakeUser() =>
        new(1, "jane@example.com", "Jane", "Doe", null, 0, 0, DateTime.UtcNow);

    private static LoginResponse FakeLoginResponse() =>
        new("eyJ...", DateTime.UtcNow.AddHours(1), 1);

    // ── Initial state ──────────────────────────────────────────────────

    [Fact]
    public void IsAuthenticated_BeforeLogin_ReturnsFalse()
    {
        var sut = CreateSut();
        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public void CurrentUser_BeforeLogin_ReturnsNull()
    {
        var sut = CreateSut();
        Assert.Null(sut.CurrentUser);
    }

    // ── LoginAsync — success ───────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLoginResponse());
        _api.GetCurrentUserAsync().Returns(FakeUser());
        var sut = CreateSut();

        var result = await sut.LoginAsync("jane@example.com", "Password1!");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_SetsCurrentUser()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLoginResponse());
        _api.GetCurrentUserAsync().Returns(FakeUser());
        var sut = CreateSut();

        await sut.LoginAsync("jane@example.com", "Password1!");

        Assert.NotNull(sut.CurrentUser);
        Assert.Equal("jane@example.com", sut.CurrentUser!.Email);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_SetsIsAuthenticated()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLoginResponse());
        _api.GetCurrentUserAsync().Returns(FakeUser());
        var sut = CreateSut();

        await sut.LoginAsync("jane@example.com", "Password1!");

        Assert.True(sut.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_WritesTokenToAuthTokenState()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLoginResponse());
        _api.GetCurrentUserAsync().Returns(FakeUser());
        var sut = CreateSut();

        await sut.LoginAsync("jane@example.com", "Password1!");

        Assert.Equal("eyJ...", _tokenState.CurrentToken);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_RaisesAuthenticationStateChangedWithTrue()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLoginResponse());
        _api.GetCurrentUserAsync().Returns(FakeUser());
        var sut = CreateSut();
        bool? raised = null;
        sut.AuthenticationStateChanged += (_, isAuthenticated) => raised = isAuthenticated;

        await sut.LoginAsync("jane@example.com", "Password1!");

        Assert.True(raised);
    }

    // ── LoginAsync — RememberMe ────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_RememberMeTrue_SavesCredentials()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLoginResponse());
        _api.GetCurrentUserAsync().Returns(FakeUser());
        var sut = CreateSut();

        await sut.LoginAsync("jane@example.com", "Password1!", rememberMe: true);

        await _credentialStore.Received(1).SaveAsync("jane@example.com", "Password1!");
    }

    [Fact]
    public async Task LoginAsync_RememberMeFalse_DoesNotSaveCredentials()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLoginResponse());
        _api.GetCurrentUserAsync().Returns(FakeUser());
        var sut = CreateSut();

        await sut.LoginAsync("jane@example.com", "Password1!", rememberMe: false);

        await _credentialStore.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    // ── LoginAsync — failure ───────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ApiThrows_ReturnsFailureWithMessage()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>())
            .ThrowsAsync(new HttpRequestException("Invalid credentials"));
        var sut = CreateSut();

        var result = await sut.LoginAsync("jane@example.com", "wrong");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid credentials", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ApiThrows_DoesNotSetCurrentUser()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>())
            .ThrowsAsync(new HttpRequestException("Invalid credentials"));
        var sut = CreateSut();

        await sut.LoginAsync("jane@example.com", "wrong");

        Assert.Null(sut.CurrentUser);
        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_ApiThrows_DoesNotRaiseAuthenticationStateChanged()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>())
            .ThrowsAsync(new HttpRequestException("Invalid credentials"));
        var sut = CreateSut();
        bool eventRaised = false;
        sut.AuthenticationStateChanged += (_, _) => eventRaised = true;

        await sut.LoginAsync("jane@example.com", "wrong");

        Assert.False(eventRaised);
    }

    // ── RegisterAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsSuccess()
    {
        var sut = CreateSut();

        var result = await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_DelegatesToApi()
    {
        var sut = CreateSut();

        await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");

        await _api.Received(1)
            .RegisterAsync(
                Arg.Is<RegisterRequest>(r =>
                    r.FirstName == "Jane"
                    && r.LastName == "Doe"
                    && r.Email == "jane@example.com"
                    && r.Password == "Password1!"
                )
            );
    }

    [Fact]
    public async Task RegisterAsync_ApiThrows_ReturnsFailureWithMessage()
    {
        _api.RegisterAsync(Arg.Any<RegisterRequest>())
            .ThrowsAsync(new HttpRequestException("Email already registered"));
        var sut = CreateSut();

        var result = await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");

        Assert.False(result.IsSuccess);
        Assert.Equal("Email already registered", result.ErrorMessage);
    }

    // ── LogoutAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_AfterLogin_ClearsCurrentUser()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLoginResponse());
        _api.GetCurrentUserAsync().Returns(FakeUser());
        var sut = CreateSut();
        await sut.LoginAsync("jane@example.com", "Password1!");

        await sut.LogoutAsync();

        Assert.Null(sut.CurrentUser);
        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public async Task LogoutAsync_ClearsTokenState()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLoginResponse());
        _api.GetCurrentUserAsync().Returns(FakeUser());
        var sut = CreateSut();
        await sut.LoginAsync("jane@example.com", "Password1!");

        await sut.LogoutAsync();

        Assert.Null(_tokenState.CurrentToken);
    }

    [Fact]
    public async Task LogoutAsync_RaisesAuthenticationStateChangedWithFalse()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLoginResponse());
        _api.GetCurrentUserAsync().Returns(FakeUser());
        var sut = CreateSut();
        await sut.LoginAsync("jane@example.com", "Password1!");
        bool? raised = null;
        sut.AuthenticationStateChanged += (_, isAuthenticated) => raised = isAuthenticated;

        await sut.LogoutAsync();

        Assert.False(raised);
    }

    [Fact]
    public async Task LogoutAsync_ClearsCredentialStore()
    {
        _api.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLoginResponse());
        _api.GetCurrentUserAsync().Returns(FakeUser());
        var sut = CreateSut();
        await sut.LoginAsync("jane@example.com", "Password1!");

        await sut.LogoutAsync();

        await _credentialStore.Received(1).ClearAsync();
    }
}
