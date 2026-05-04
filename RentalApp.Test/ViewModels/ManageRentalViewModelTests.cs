using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Rentals;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class ManageRentalViewModelTests
{
    private readonly IRentalService _rentalService = Substitute.For<IRentalService>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();

    private static RentalDetailResponse MakeRental(
        int ownerId = 1,
        int borrowerId = 2,
        string status = "Requested"
    ) =>
        new(
            Id: 10,
            ItemId: 5,
            ItemTitle: "Drill",
            ItemDescription: "A powerful drill",
            BorrowerId: borrowerId,
            BorrowerName: "Bob Smith",
            OwnerId: ownerId,
            OwnerName: "Alice Jones",
            StartDate: new DateTime(2026, 6, 1),
            EndDate: new DateTime(2026, 6, 5),
            Status: status,
            TotalPrice: 40.0,
            RequestedAt: DateTime.UtcNow
        );

    private static CurrentUserResponse MakeUser(int id) =>
        new(
            Id: id,
            Email: "",
            FirstName: "",
            LastName: "",
            AverageRating: null,
            ItemsListed: 0,
            RentalsCompleted: 0,
            CreatedAt: DateTime.UtcNow
        );

    private ManageRentalViewModel CreateSut(int currentUserId = 1)
    {
        _authService.GetCurrentUserAsync().Returns(MakeUser(currentUserId));
        var sut = new ManageRentalViewModel(
            _rentalService,
            _authService,
            _tokenState,
            _credentialStore,
            _nav
        );
        sut.ApplyQueryAttributes(new Dictionary<string, object> { { "rentalId", 10 } });
        return sut;
    }

    // ── LoadRentalCommand ──────────────────────────────────────────────

    [Fact]
    public async Task LoadRentalCommand_PopulatesCurrentRental()
    {
        _rentalService.GetRentalAsync(10).Returns(MakeRental());
        var sut = CreateSut();

        await sut.LoadRentalCommand.ExecuteAsync(null);

        Assert.NotNull(sut.CurrentRental);
        Assert.Equal(10, sut.CurrentRental!.Id);
    }

    [Fact]
    public async Task LoadRentalCommand_ServiceThrows_SetsError()
    {
        _rentalService.GetRentalAsync(10).ThrowsAsync(new Exception("network error"));
        var sut = CreateSut();

        await sut.LoadRentalCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("network error", sut.ErrorMessage);
    }

    // ── Can* properties — owner role ───────────────────────────────────

    [Fact]
    public async Task LoadRentalCommand_WhenOwner_RequestedRental_SetsCanApproveAndCanReject()
    {
        _rentalService.GetRentalAsync(10).Returns(MakeRental(ownerId: 1, status: "Requested"));
        var sut = CreateSut(currentUserId: 1);

        await sut.LoadRentalCommand.ExecuteAsync(null);

        Assert.True(sut.CanApprove);
        Assert.True(sut.CanReject);
        Assert.False(sut.CanMarkOutForRent);
        Assert.False(sut.CanMarkReturned);
        Assert.False(sut.CanComplete);
    }

    [Fact]
    public async Task LoadRentalCommand_WhenOwner_ApprovedRental_SetsCanMarkOutForRent()
    {
        _rentalService.GetRentalAsync(10).Returns(MakeRental(ownerId: 1, status: "Approved"));
        var sut = CreateSut(currentUserId: 1);

        await sut.LoadRentalCommand.ExecuteAsync(null);

        Assert.True(sut.CanMarkOutForRent);
        Assert.False(sut.CanApprove);
        Assert.False(sut.CanReject);
    }

    [Fact]
    public async Task LoadRentalCommand_WhenOwner_ReturnedRental_SetsCanComplete()
    {
        _rentalService.GetRentalAsync(10).Returns(MakeRental(ownerId: 1, status: "Returned"));
        var sut = CreateSut(currentUserId: 1);

        await sut.LoadRentalCommand.ExecuteAsync(null);

        Assert.True(sut.CanComplete);
        Assert.False(sut.CanMarkReturned);
    }

    // ── Can* properties — borrower role ───────────────────────────────

    [Fact]
    public async Task LoadRentalCommand_WhenBorrower_RequestedRental_NoActionsAvailable()
    {
        _rentalService
            .GetRentalAsync(10)
            .Returns(MakeRental(ownerId: 1, borrowerId: 2, status: "Requested"));
        var sut = CreateSut(currentUserId: 2);

        await sut.LoadRentalCommand.ExecuteAsync(null);

        Assert.False(sut.CanApprove);
        Assert.False(sut.CanReject);
        Assert.False(sut.CanMarkOutForRent);
        Assert.False(sut.CanMarkReturned);
        Assert.False(sut.CanComplete);
    }

    [Theory]
    [InlineData("OutForRent")]
    [InlineData("Out for Rent")]
    [InlineData("Overdue")]
    public async Task LoadRentalCommand_WhenBorrower_ActiveRental_SetsCanMarkReturned(string status)
    {
        _rentalService
            .GetRentalAsync(10)
            .Returns(MakeRental(ownerId: 1, borrowerId: 2, status: status));
        var sut = CreateSut(currentUserId: 2);

        await sut.LoadRentalCommand.ExecuteAsync(null);

        Assert.True(sut.CanMarkReturned);
        Assert.False(sut.CanApprove);
        Assert.False(sut.CanReject);
        Assert.False(sut.CanMarkOutForRent);
        Assert.False(sut.CanComplete);
    }

    // ── Can* properties — terminal states ─────────────────────────────

    [Theory]
    [InlineData("Rejected")]
    [InlineData("Completed")]
    public async Task LoadRentalCommand_TerminalState_NoActionsAvailable(string status)
    {
        _rentalService.GetRentalAsync(10).Returns(MakeRental(ownerId: 1, status: status));
        var sut = CreateSut(currentUserId: 1);

        await sut.LoadRentalCommand.ExecuteAsync(null);

        Assert.False(sut.CanApprove);
        Assert.False(sut.CanReject);
        Assert.False(sut.CanMarkOutForRent);
        Assert.False(sut.CanMarkReturned);
        Assert.False(sut.CanComplete);
    }

    // ── UpdateStatusCommand ───────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusCommand_CallsServiceWithTargetStatus()
    {
        _rentalService
            .GetRentalAsync(10)
            .Returns(
                MakeRental(ownerId: 1, status: "Requested"),
                MakeRental(ownerId: 1, status: "Approved")
            );
        _rentalService
            .UpdateRentalStatusAsync(10, Arg.Any<UpdateRentalStatusRequest>())
            .Returns(new UpdateRentalStatusResponse(10, "Approved", DateTime.UtcNow));
        var sut = CreateSut(currentUserId: 1);
        await sut.LoadRentalCommand.ExecuteAsync(null);

        await sut.UpdateStatusCommand.ExecuteAsync("Approved");

        await _rentalService
            .Received(1)
            .UpdateRentalStatusAsync(
                10,
                Arg.Is<UpdateRentalStatusRequest>(r => r.Status == "Approved")
            );
    }

    [Fact]
    public async Task UpdateStatusCommand_OnSuccess_ReloadsRentalAndRefreshesCanProperties()
    {
        _rentalService
            .GetRentalAsync(10)
            .Returns(
                MakeRental(ownerId: 1, status: "Requested"),
                MakeRental(ownerId: 1, status: "Approved")
            );
        _rentalService
            .UpdateRentalStatusAsync(10, Arg.Any<UpdateRentalStatusRequest>())
            .Returns(new UpdateRentalStatusResponse(10, "Approved", DateTime.UtcNow));
        var sut = CreateSut(currentUserId: 1);
        await sut.LoadRentalCommand.ExecuteAsync(null);

        await sut.UpdateStatusCommand.ExecuteAsync("Approved");

        await _rentalService.Received(2).GetRentalAsync(10);
        Assert.Equal("Approved", sut.CurrentRental!.Status);
        Assert.True(sut.CanMarkOutForRent);
    }

    [Fact]
    public async Task UpdateStatusCommand_ServiceThrows_SetsError()
    {
        _rentalService.GetRentalAsync(10).Returns(MakeRental(ownerId: 1, status: "Requested"));
        _rentalService
            .UpdateRentalStatusAsync(10, Arg.Any<UpdateRentalStatusRequest>())
            .ThrowsAsync(new InvalidOperationException("Transition not allowed"));
        var sut = CreateSut(currentUserId: 1);
        await sut.LoadRentalCommand.ExecuteAsync(null);

        await sut.UpdateStatusCommand.ExecuteAsync("Approved");

        Assert.True(sut.HasError);
        Assert.Equal("Transition not allowed", sut.ErrorMessage);
    }

    // ── CanReview property ─────────────────────────────────────────────

    [Fact]
    public async Task LoadRentalCommand_WhenBorrowerAndCompleted_SetsCanReviewTrue()
    {
        _rentalService
            .GetRentalAsync(10)
            .Returns(MakeRental(ownerId: 1, borrowerId: 2, status: "Completed"));
        var sut = CreateSut(currentUserId: 2);

        await sut.LoadRentalCommand.ExecuteAsync(null);

        Assert.True(sut.CanReview);
    }

    [Fact]
    public async Task LoadRentalCommand_WhenOwnerAndCompleted_SetsCanReviewFalse()
    {
        _rentalService
            .GetRentalAsync(10)
            .Returns(MakeRental(ownerId: 1, borrowerId: 2, status: "Completed"));
        var sut = CreateSut(currentUserId: 1);

        await sut.LoadRentalCommand.ExecuteAsync(null);

        Assert.False(sut.CanReview);
    }

    [Fact]
    public async Task LoadRentalCommand_WhenBorrowerAndNotCompleted_SetsCanReviewFalse()
    {
        _rentalService
            .GetRentalAsync(10)
            .Returns(MakeRental(ownerId: 1, borrowerId: 2, status: "Returned"));
        var sut = CreateSut(currentUserId: 2);

        await sut.LoadRentalCommand.ExecuteAsync(null);

        Assert.False(sut.CanReview);
    }

    [Fact]
    public async Task NavigateToCreateReviewCommand_NavigatesToCreateReviewWithRentalId()
    {
        _rentalService
            .GetRentalAsync(10)
            .Returns(MakeRental(ownerId: 1, borrowerId: 2, status: "Completed"));
        var sut = CreateSut(currentUserId: 2);
        await sut.LoadRentalCommand.ExecuteAsync(null);

        await sut.NavigateToCreateReviewCommand.ExecuteAsync(null);

        await _nav.Received(1)
            .NavigateToAsync(
                Routes.CreateReview,
                Arg.Is<Dictionary<string, object>>(d =>
                    d.ContainsKey("rentalId") && (int)d["rentalId"] == 10
                )
            );
    }
}
