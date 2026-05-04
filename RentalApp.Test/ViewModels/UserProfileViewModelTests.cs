using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Reviews;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class UserProfileViewModelTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IReviewService _reviewService = Substitute.For<IReviewService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();

    private UserProfileViewModel CreateSut()
    {
        _reviewService
            .GetUserReviewsAsync(Arg.Any<int>(), Arg.Any<GetReviewsRequest>())
            .Returns(new ReviewsResponse([], null, 0, 1, 10, 0));
        return new(_authService, _reviewService, _tokenState, _credentialStore, _nav);
    }

    private static CurrentUserResponse MakeCurrentUser(int id = 1) =>
        new(id, "alice@example.com", "Alice", "Smith", 4.5, 3, 7, DateTime.UtcNow);

    private static UserProfileResponse MakeUserProfile(int id = 2) =>
        new(id, "Bob", "Jones", 3.8, 5, 2, []);

    // ── Self mode (no userId supplied) ───────────────────────────────

    [Fact]
    public async Task LoadProfileCommand_SelfMode_SetsDisplayName()
    {
        _authService.GetCurrentUserAsync().Returns(MakeCurrentUser());
        var sut = CreateSut();

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Equal("Alice Smith", sut.DisplayName);
    }

    [Fact]
    public async Task LoadProfileCommand_SelfMode_SetsEmail()
    {
        _authService.GetCurrentUserAsync().Returns(MakeCurrentUser());
        var sut = CreateSut();

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Equal("alice@example.com", sut.Email);
        Assert.True(sut.ShowEmail);
    }

    [Fact]
    public async Task LoadProfileCommand_SelfMode_SetsStats()
    {
        _authService.GetCurrentUserAsync().Returns(MakeCurrentUser());
        var sut = CreateSut();

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Equal(3, sut.ItemsListed);
        Assert.Equal(7, sut.RentalsCompleted);
    }

    // ── Other user mode (userId supplied) ────────────────────────────

    [Fact]
    public async Task LoadProfileCommand_OtherUser_SetsDisplayName()
    {
        _authService.GetUserProfileAsync(2).Returns(MakeUserProfile(2));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["userId"] = 2 });

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Equal("Bob Jones", sut.DisplayName);
    }

    [Fact]
    public async Task LoadProfileCommand_OtherUser_EmailIsNullAndShowEmailIsFalse()
    {
        _authService.GetUserProfileAsync(2).Returns(MakeUserProfile(2));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["userId"] = 2 });

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Null(sut.Email);
        Assert.False(sut.ShowEmail);
    }

    [Fact]
    public async Task LoadProfileCommand_OtherUser_SetsStats()
    {
        _authService.GetUserProfileAsync(2).Returns(MakeUserProfile(2));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["userId"] = 2 });

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Equal(5, sut.ItemsListed);
        Assert.Equal(2, sut.RentalsCompleted);
    }

    // ── Review routing — resolvedUserId ──────────────────────────────

    [Fact]
    public async Task LoadReviewsCommand_AfterSelfModeLoad_UsesResolvedUserIdNotZero()
    {
        _authService.GetCurrentUserAsync().Returns(MakeCurrentUser(id: 42));
        var sut = CreateSut();
        await sut.LoadProfileCommand.ExecuteAsync(null);

        await sut.LoadReviewsCommand.ExecuteAsync(null);

        await _reviewService.Received().GetUserReviewsAsync(42, Arg.Any<GetReviewsRequest>());
        await _reviewService.DidNotReceive().GetUserReviewsAsync(0, Arg.Any<GetReviewsRequest>());
    }

    // ── Error handling ────────────────────────────────────────────────

    [Fact]
    public async Task LoadProfileCommand_ServiceThrows_SetsError()
    {
        _authService.GetCurrentUserAsync().ThrowsAsync(new InvalidOperationException("Auth error"));
        var sut = CreateSut();

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Auth error", sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }
}
