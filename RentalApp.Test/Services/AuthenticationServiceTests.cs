using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class AuthenticationServiceTests
{
    private readonly IApiService _api = Substitute.For<IApiService>();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();

    private AuthenticationService CreateSut() => new(_api, _credentialStore);

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        var profile = new User(
            1,
            "Jane",
            "Doe",
            0.0,
            0,
            0,
            "jane@example.com",
            DateTime.UtcNow,
            null
        );
        _api.GetCurrentUserAsync().Returns(profile);

        var sut = CreateSut();
        var result = await sut.LoginAsync("jane@example.com", "pass");

        Assert.True(result.IsSuccess);
        Assert.Equal(profile, sut.CurrentUser);
        Assert.True(sut.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_ApiThrows_ReturnsFailure()
    {
        _api.LoginAsync(Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new UnauthorizedAccessException("bad credentials"));

        var sut = CreateSut();
        var result = await sut.LoginAsync("jane@example.com", "wrong");

        Assert.False(result.IsSuccess);
        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_RememberMe_SavesCredentials()
    {
        _api.GetCurrentUserAsync()
            .Returns(
                new User(1, "Jane", "Doe", 0.0, 0, 0, "jane@example.com", DateTime.UtcNow, null)
            );

        var sut = CreateSut();
        await sut.LoginAsync("jane@example.com", "pass", rememberMe: true);

        await _credentialStore.Received(1).SaveAsync("jane@example.com", "pass");
    }

    [Fact]
    public async Task LoginAsync_RememberMeFalse_DoesNotSaveCredentials()
    {
        _api.GetCurrentUserAsync()
            .Returns(
                new User(1, "Jane", "Doe", 0.0, 0, 0, "jane@example.com", DateTime.UtcNow, null)
            );

        var sut = CreateSut();
        await sut.LoginAsync("jane@example.com", "pass", rememberMe: false);

        await _credentialStore.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_Success_FiresAuthenticationStateChangedWithTrue()
    {
        _api.GetCurrentUserAsync()
            .Returns(
                new User(1, "Jane", "Doe", 0.0, 0, 0, "jane@example.com", DateTime.UtcNow, null)
            );

        var sut = CreateSut();
        bool? firedWith = null;
        sut.AuthenticationStateChanged += (_, v) => firedWith = v;

        await sut.LoginAsync("jane@example.com", "pass");

        Assert.True(firedWith);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsSuccess()
    {
        var sut = CreateSut();
        var result = await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "pass");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RegisterAsync_ApiThrows_ReturnsFailure()
    {
        _api.RegisterAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>()
            )
            .ThrowsAsync(new InvalidOperationException("email taken"));

        var sut = CreateSut();
        var result = await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "pass");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LogoutAsync_ClearsCurrentUser()
    {
        _api.GetCurrentUserAsync()
            .Returns(
                new User(1, "Jane", "Doe", 0.0, 0, 0, "jane@example.com", DateTime.UtcNow, null)
            );
        var sut = CreateSut();
        await sut.LoginAsync("jane@example.com", "pass");

        await sut.LogoutAsync();

        Assert.Null(sut.CurrentUser);
        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public async Task LogoutAsync_FiresAuthenticationStateChangedWithFalse()
    {
        var sut = CreateSut();
        bool? firedWith = null;
        sut.AuthenticationStateChanged += (_, v) => firedWith = v;

        await sut.LogoutAsync();

        Assert.False(firedWith);
    }

    [Fact]
    public async Task LogoutAsync_ClearsPersistedCredentials()
    {
        var sut = CreateSut();
        await sut.LogoutAsync();

        await _credentialStore.Received(1).ClearAsync();
    }
}
