using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Rentals;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class RentalsViewModelTests
{
    private readonly IRentalService _rentalService = Substitute.For<IRentalService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();

    private static RentalSummaryResponse MakeRental(
        int id = 1,
        string itemTitle = "Drill",
        string status = "Requested"
    ) =>
        new(
            id,
            ItemId: 10,
            ItemTitle: itemTitle,
            BorrowerId: 2,
            BorrowerName: "Bob Smith",
            OwnerId: 1,
            OwnerName: "Alice Jones",
            StartDate: new DateOnly(2026, 6, 1),
            EndDate: new DateOnly(2026, 6, 5),
            Status: status,
            TotalPrice: 40.0,
            CreatedAt: DateTime.UtcNow
        );

    private static RentalsListResponse MakeRentalsResponse(List<RentalSummaryResponse> rentals) =>
        new(rentals, rentals.Count);

    private RentalsViewModel CreateSut() =>
        new(_rentalService, _nav, _tokenState, _credentialStore);

    // ── LoadRentalsCommand ─────────────────────────────────────────────

    [Fact]
    public async Task LoadRentalsCommand_DefaultsToIncoming_CallsGetIncomingRentals()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([]));
        var sut = CreateSut();

        await sut.LoadRentalsCommand.ExecuteAsync(null);

        await _rentalService.Received(1).GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>());
        await _rentalService.DidNotReceive().GetOutgoingRentalsAsync(Arg.Any<GetRentalsRequest>());
    }

    [Fact]
    public async Task LoadRentalsCommand_WhenOutgoing_CallsGetOutgoingRentals()
    {
        _rentalService
            .GetOutgoingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([]));
        var sut = CreateSut();
        sut.IsIncoming = false;

        await sut.LoadRentalsCommand.ExecuteAsync(null);

        await _rentalService.Received(1).GetOutgoingRentalsAsync(Arg.Any<GetRentalsRequest>());
        await _rentalService.DidNotReceive().GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>());
    }

    [Fact]
    public async Task LoadRentalsCommand_Success_PopulatesRentals()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([MakeRental(1), MakeRental(2, "Ladder")]));
        var sut = CreateSut();

        await sut.LoadRentalsCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Rentals.Count);
    }

    [Fact]
    public async Task LoadRentalsCommand_IsBusyFalseAfterCompletion()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([]));
        var sut = CreateSut();

        await sut.LoadRentalsCommand.ExecuteAsync(null);

        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task LoadRentalsCommand_ServiceThrows_SetsError()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .ThrowsAsync(new Exception("network error"));
        var sut = CreateSut();

        await sut.LoadRentalsCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("network error", sut.ErrorMessage);
    }

    [Fact]
    public async Task LoadRentalsCommand_PassesSelectedStatusToRequest()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([MakeRental(status: "Requested")]));
        var sut = CreateSut();
        // Set before first load: _hasLoaded is false so no reload is triggered,
        // but SelectedStatus is already "Requested" when LoadRentalsCommand fires.
        sut.SelectedStatusItem = "Requested";

        await sut.LoadRentalsCommand.ExecuteAsync(null);

        await _rentalService
            .Received(1)
            .GetIncomingRentalsAsync(Arg.Is<GetRentalsRequest>(r => r.Status == "Requested"));
    }

    [Fact]
    public async Task LoadRentalsCommand_WhenAllSelected_PassesNullStatusToRequest()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([]));
        var sut = CreateSut();

        await sut.LoadRentalsCommand.ExecuteAsync(null);

        await _rentalService
            .Received(1)
            .GetIncomingRentalsAsync(Arg.Is<GetRentalsRequest>(r => r.Status == null));
    }

    // ── FilterStatuses ─────────────────────────────────────────────────

    [Fact]
    public async Task LoadRentalsCommand_PopulatesFilterStatusesFromResults()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(
                MakeRentalsResponse([
                    MakeRental(status: "Requested"),
                    MakeRental(2, status: "Approved"),
                ])
            );
        var sut = CreateSut();

        await sut.LoadRentalsCommand.ExecuteAsync(null);

        Assert.Contains("Requested", sut.FilterStatuses);
        Assert.Contains("Approved", sut.FilterStatuses);
    }

    [Fact]
    public async Task LoadRentalsCommand_AlwaysIncludesAllSentinel()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([MakeRental(status: "Requested")]));
        var sut = CreateSut();

        await sut.LoadRentalsCommand.ExecuteAsync(null);

        Assert.Equal(RentalsViewModel.AllStatuses, sut.FilterStatuses[0]);
    }

    [Fact]
    public async Task LoadRentalsCommand_DeduplicatesStatuses()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(
                MakeRentalsResponse([
                    MakeRental(1, status: "Requested"),
                    MakeRental(2, status: "Requested"),
                ])
            );
        var sut = CreateSut();

        await sut.LoadRentalsCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.FilterStatuses.Count); // "All" + "Requested"
    }

    [Fact]
    public async Task FilterStatuses_NotRebuiltWhenStatusFilterChanges()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Is<GetRentalsRequest>(r => r.Status == null))
            .Returns(
                MakeRentalsResponse([MakeRental(1, status: "Requested"), MakeRental(2, status: "Approved")])
            );
        _rentalService
            .GetIncomingRentalsAsync(Arg.Is<GetRentalsRequest>(r => r.Status == "Requested"))
            .Returns(MakeRentalsResponse([MakeRental(1, status: "Requested")]));
        var sut = CreateSut();
        await sut.LoadRentalsCommand.ExecuteAsync(null);

        sut.SelectedStatusItem = "Requested";
        await (sut.LoadRentalsCommand.ExecutionTask ?? Task.CompletedTask);

        Assert.Contains("Approved", sut.FilterStatuses);
        Assert.Contains("Requested", sut.FilterStatuses);
    }

    [Fact]
    public async Task FilterStatuses_RebuiltWhenDirectionChanges()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([MakeRental(status: "Requested")]));
        _rentalService
            .GetOutgoingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([MakeRental(status: "Approved")]));
        var sut = CreateSut();
        await sut.LoadRentalsCommand.ExecuteAsync(null);

        sut.IsIncoming = false;
        await (sut.LoadRentalsCommand.ExecutionTask ?? Task.CompletedTask);

        Assert.Contains("Approved", sut.FilterStatuses);
        Assert.DoesNotContain("Requested", sut.FilterStatuses);
    }

    // ── SelectedStatusItem ─────────────────────────────────────────────

    [Fact]
    public async Task SelectedStatusItem_SetToStatus_SetsSelectedStatus()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([MakeRental(status: "Requested")]));
        var sut = CreateSut();
        await sut.LoadRentalsCommand.ExecuteAsync(null);

        sut.SelectedStatusItem = "Requested";

        Assert.Equal("Requested", sut.SelectedStatus);
    }

    [Fact]
    public async Task SelectedStatusItem_SetToAll_ClearsSelectedStatus()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([MakeRental(status: "Requested")]));
        var sut = CreateSut();
        await sut.LoadRentalsCommand.ExecuteAsync(null);
        sut.SelectedStatusItem = "Requested";

        sut.SelectedStatusItem = RentalsViewModel.AllStatuses;

        Assert.Null(sut.SelectedStatus);
    }

    // ── Toggle / reload ────────────────────────────────────────────────

    [Fact]
    public async Task IsIncomingChanged_AfterLoad_TriggersReload()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([]));
        _rentalService
            .GetOutgoingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([]));
        var sut = CreateSut();
        await sut.LoadRentalsCommand.ExecuteAsync(null);

        sut.IsIncoming = false;
        await (sut.LoadRentalsCommand.ExecutionTask ?? Task.CompletedTask);

        await _rentalService.Received(1).GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>());
        await _rentalService.Received(1).GetOutgoingRentalsAsync(Arg.Any<GetRentalsRequest>());
    }

    [Fact]
    public async Task SelectedStatusChanged_AfterLoad_TriggersReload()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([MakeRental(status: "Requested")]));
        var sut = CreateSut();
        await sut.LoadRentalsCommand.ExecuteAsync(null);

        sut.SelectedStatusItem = "Requested";
        await (sut.LoadRentalsCommand.ExecutionTask ?? Task.CompletedTask);

        await _rentalService.Received(2).GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>());
    }

    [Fact]
    public async Task IsIncomingChanged_StaleStatusFilter_ResetsSelectionToAll()
    {
        _rentalService
            .GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([MakeRental(status: "Requested")]));
        _rentalService
            .GetOutgoingRentalsAsync(Arg.Any<GetRentalsRequest>())
            .Returns(MakeRentalsResponse([MakeRental(status: "Approved")]));
        var sut = CreateSut();
        await sut.LoadRentalsCommand.ExecuteAsync(null);
        sut.SelectedStatusItem = "Requested";
        await (sut.LoadRentalsCommand.ExecutionTask ?? Task.CompletedTask);

        sut.IsIncoming = false;
        await (sut.LoadRentalsCommand.ExecutionTask ?? Task.CompletedTask);

        Assert.Equal(RentalsViewModel.AllStatuses, sut.SelectedStatusItem);
        Assert.Null(sut.SelectedStatus);
    }

    [Fact]
    public void IsIncomingChanged_BeforeLoad_DoesNotTriggerReload()
    {
        var sut = CreateSut();

        sut.IsIncoming = false;

        _rentalService.DidNotReceive().GetIncomingRentalsAsync(Arg.Any<GetRentalsRequest>());
        _rentalService.DidNotReceive().GetOutgoingRentalsAsync(Arg.Any<GetRentalsRequest>());
    }

    // ── ShowIncomingCommand / ShowOutgoingCommand ──────────────────────

    [Fact]
    public void ShowIncomingCommand_SetsIsIncomingTrue()
    {
        var sut = CreateSut();
        sut.IsIncoming = false;

        sut.ShowIncomingCommand.Execute(null);

        Assert.True(sut.IsIncoming);
    }

    [Fact]
    public void ShowOutgoingCommand_SetsIsIncomingFalse()
    {
        var sut = CreateSut();

        sut.ShowOutgoingCommand.Execute(null);

        Assert.False(sut.IsIncoming);
    }
}
