using System.Net;
using NSubstitute;
using RentalApp.Constants;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.Test.Http;

public class ApiClientTests
{
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();
    private readonly AuthTokenState _tokenState = new();

    private ApiClient CreateSut(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpClient = new HttpClient(new FakeHandler(handler))
        {
            BaseAddress = new Uri("https://api.example.com/"),
        };
        return new ApiClient(httpClient, _tokenState, _navigationService);
    }

    // ── Bearer token attachment ────────────────────────────────────────

    [Fact]
    public async Task GetAsync_TokenPresent_AttachesBearerHeader()
    {
        _tokenState.CurrentToken = "my-token";
        HttpRequestMessage? captured = null;
        var sut = CreateSut(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await sut.GetAsync("items");

        Assert.Equal("Bearer", captured?.Headers.Authorization?.Scheme);
        Assert.Equal("my-token", captured?.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GetAsync_TokenAbsent_DoesNotAttachAuthorizationHeader()
    {
        _tokenState.CurrentToken = null;
        HttpRequestMessage? captured = null;
        var sut = CreateSut(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await sut.GetAsync("items");

        Assert.Null(captured?.Headers.Authorization);
    }

    // ── GetAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_SuccessResponse_ReturnsResponse()
    {
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var response = await sut.GetAsync("items");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAsync_NonAuthErrorResponse_ReturnsResponseUnmodified()
    {
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));

        var response = await sut.GetAsync("items");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAsync_401Response_NavigatesToLoginWithSessionExpiredFlag()
    {
        _tokenState.CurrentToken = "active-token";
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        await sut.GetAsync("items");

        await _navigationService
            .Received(1)
            .NavigateToAsync(
                Routes.Login,
                Arg.Is<Dictionary<string, object>>(d =>
                    d.ContainsKey("sessionExpired") && (bool)d["sessionExpired"]
                )
            );
    }

    [Fact]
    public async Task GetAsync_401Response_ReturnsOriginalResponse()
    {
        _tokenState.CurrentToken = "active-token";
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var response = await sut.GetAsync("items");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── PostAsJsonAsync ────────────────────────────────────────────────

    [Fact]
    public async Task PostAsJsonAsync_SuccessResponse_ReturnsResponse()
    {
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.Created));

        var response = await sut.PostAsJsonAsync("items", new { name = "Tent" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostAsJsonAsync_401Response_NavigatesToLoginWithSessionExpiredFlag()
    {
        _tokenState.CurrentToken = "active-token";
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        await sut.PostAsJsonAsync("items", new { name = "Tent" });

        await _navigationService
            .Received(1)
            .NavigateToAsync(
                Routes.Login,
                Arg.Is<Dictionary<string, object>>(d =>
                    d.ContainsKey("sessionExpired") && (bool)d["sessionExpired"]
                )
            );
    }

    [Fact]
    public async Task PostAsJsonAsync_401Response_ReturnsOriginalResponse()
    {
        _tokenState.CurrentToken = "active-token";
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var response = await sut.PostAsJsonAsync("items", new { name = "Tent" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAsJsonAsync_TokenAbsent_401ResponsePassesThroughWithoutNavigation()
    {
        _tokenState.CurrentToken = null;
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var response = await sut.PostAsJsonAsync(
            "auth/token",
            new { email = "a@b.com", password = "wrong" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _navigationService
            .DidNotReceive()
            .NavigateToAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>());
    }

    // ── PutAsJsonAsync ────────────────────────────────────────────────

    [Fact]
    public async Task PutAsJsonAsync_SuccessResponse_ReturnsResponse()
    {
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var response = await sut.PutAsJsonAsync("items/1", new { name = "Tent" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutAsJsonAsync_UsesHttpPutMethod()
    {
        HttpRequestMessage? captured = null;
        var sut = CreateSut(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await sut.PutAsJsonAsync("items/1", new { name = "Tent" });

        Assert.Equal(HttpMethod.Put, captured?.Method);
    }

    [Fact]
    public async Task PutAsJsonAsync_401Response_NavigatesToLoginWithSessionExpiredFlag()
    {
        _tokenState.CurrentToken = "active-token";
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        await sut.PutAsJsonAsync("items/1", new { name = "Tent" });

        await _navigationService
            .Received(1)
            .NavigateToAsync(
                Routes.Login,
                Arg.Is<Dictionary<string, object>>(d =>
                    d.ContainsKey("sessionExpired") && (bool)d["sessionExpired"]
                )
            );
    }

    [Fact]
    public async Task PutAsJsonAsync_401Response_ReturnsOriginalResponse()
    {
        _tokenState.CurrentToken = "active-token";
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var response = await sut.PutAsJsonAsync("items/1", new { name = "Tent" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutAsJsonAsync_TokenAbsent_401ResponsePassesThroughWithoutNavigation()
    {
        _tokenState.CurrentToken = null;
        var sut = CreateSut(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var response = await sut.PutAsJsonAsync("items/1", new { name = "Tent" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _navigationService
            .DidNotReceive()
            .NavigateToAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>());
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(respond(request));
    }
}
