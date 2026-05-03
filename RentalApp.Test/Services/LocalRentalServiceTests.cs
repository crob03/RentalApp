using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Services.Auth;
using RentalApp.Services.Rentals;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public class LocalRentalServiceTests
    : IClassFixture<DatabaseFixture<LocalRentalServiceTests>>,
        IAsyncLifetime
{
    private readonly DatabaseFixture<LocalRentalServiceTests> _fixture;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly AuthTokenState _tokenState = new();

    // Seeded by DatabaseFixture: User 1 (owner of all items), User 2 (borrower), Item 1 (DailyRate=10)
    private const int OwnerId = 1;
    private const int BorrowerId = 2;
    private const int ItemId = 1;

    public LocalRentalServiceTests(DatabaseFixture<LocalRentalServiceTests> fixture)
    {
        _fixture = fixture;
        _contextFactory = fixture.ContextFactory;
    }

    public async Task InitializeAsync()
    {
        _tokenState.CurrentToken = null;
        await _fixture.ResetRentalsAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private LocalRentalService CreateSut() =>
        new(
            new RentalRepository(_contextFactory),
            new ItemRepository(_contextFactory),
            _tokenState
        );

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow.Date);

    // ── CreateRentalAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRentalAsync_ValidRequest_ReturnsRequestedStatus()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);

        var result = await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(3)));

        Assert.Equal("Requested", result.Status);
        Assert.Equal(ItemId, result.ItemId);
        Assert.Equal(BorrowerId, result.BorrowerId);
        Assert.Equal(OwnerId, result.OwnerId);
    }

    [Fact]
    public async Task CreateRentalAsync_StartDateInPast_Throws()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut()
                .CreateRentalAsync(
                    new CreateRentalRequest(ItemId, Today().AddDays(-1), Today().AddDays(2))
                )
        );
    }

    [Fact]
    public async Task CreateRentalAsync_StartDateNotBeforeEndDate_Throws()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().CreateRentalAsync(new CreateRentalRequest(ItemId, start, start))
        );
    }

    [Fact]
    public async Task CreateRentalAsync_OwnerRentingOwnItem_Throws()
    {
        _tokenState.CurrentToken = OwnerId.ToString();
        var start = Today().AddDays(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)))
        );
    }

    [Fact]
    public async Task CreateRentalAsync_OverlappingDates_Throws()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(5);
        await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(5)));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut()
                .CreateRentalAsync(
                    new CreateRentalRequest(ItemId, start.AddDays(2), start.AddDays(7))
                )
        );
    }

    // ── GetIncomingRentalsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetIncomingRentalsAsync_ReturnsOwnerRentals()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        _tokenState.CurrentToken = OwnerId.ToString();
        var result = await CreateSut().GetIncomingRentalsAsync(new GetRentalsRequest());

        Assert.Equal(1, result.TotalRentals);
        Assert.Single(result.Rentals);
    }

    [Fact]
    public async Task GetIncomingRentalsAsync_WithStatusFilter_ReturnsMatchingRentals()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        _tokenState.CurrentToken = OwnerId.ToString();
        var requested = await CreateSut()
            .GetIncomingRentalsAsync(new GetRentalsRequest("Requested"));
        var approved = await CreateSut().GetIncomingRentalsAsync(new GetRentalsRequest("Approved"));

        Assert.Single(requested.Rentals);
        Assert.Empty(approved.Rentals);
    }

    // ── GetOutgoingRentalsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetOutgoingRentalsAsync_ReturnsBorrowerRentals()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        var result = await CreateSut().GetOutgoingRentalsAsync(new GetRentalsRequest());

        Assert.Equal(1, result.TotalRentals);
        Assert.Equal(BorrowerId, result.Rentals[0].BorrowerId);
    }

    // ── GetRentalAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRentalAsync_Owner_ReturnsFullDetail()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        var created = await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        _tokenState.CurrentToken = OwnerId.ToString();
        var result = await CreateSut().GetRentalAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.NotNull(result.ItemTitle);
    }

    [Fact]
    public async Task GetRentalAsync_Borrower_ReturnsFullDetail()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        var created = await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        var result = await CreateSut().GetRentalAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetRentalAsync_UnrelatedUser_ThrowsUnauthorized()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        var created = await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        _tokenState.CurrentToken = "999";
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateSut().GetRentalAsync(created.Id)
        );
    }

    // ── UpdateRentalStatusAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateRentalStatusAsync_OwnerApproves_StatusBecomesApproved()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        var created = await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        _tokenState.CurrentToken = OwnerId.ToString();
        var result = await CreateSut()
            .UpdateRentalStatusAsync(created.Id, new UpdateRentalStatusRequest("Approved"));

        Assert.Equal("Approved", result.Status);
    }

    [Fact]
    public async Task UpdateRentalStatusAsync_BorrowerApproves_ThrowsUnauthorized()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        var created = await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateSut()
                .UpdateRentalStatusAsync(created.Id, new UpdateRentalStatusRequest("Approved"))
        );
    }

    [Fact]
    public async Task UpdateRentalStatusAsync_OwnerMarksReturned_ThrowsUnauthorized()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        var created = await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        // Advance to OutForRent via owner
        _tokenState.CurrentToken = OwnerId.ToString();
        await CreateSut()
            .UpdateRentalStatusAsync(created.Id, new UpdateRentalStatusRequest("Approved"));
        await CreateSut()
            .UpdateRentalStatusAsync(created.Id, new UpdateRentalStatusRequest("OutForRent"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateSut()
                .UpdateRentalStatusAsync(created.Id, new UpdateRentalStatusRequest("Returned"))
        );
    }

    [Fact]
    public async Task UpdateRentalStatusAsync_InvalidTransition_ThrowsInvalidOperation()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        var created = await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        _tokenState.CurrentToken = OwnerId.ToString();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut()
                .UpdateRentalStatusAsync(created.Id, new UpdateRentalStatusRequest("Completed"))
        );
    }

    [Fact]
    public async Task UpdateRentalStatusAsync_ManualOverdue_ThrowsInvalidOperation()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        var created = await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        _tokenState.CurrentToken = OwnerId.ToString();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut()
                .UpdateRentalStatusAsync(created.Id, new UpdateRentalStatusRequest("Overdue"))
        );
    }

    [Fact]
    public async Task GetRentalAsync_OutForRentPastEndDate_ReturnsOverdue()
    {
        _tokenState.CurrentToken = BorrowerId.ToString();
        var start = Today().AddDays(1);
        var created = await CreateSut()
            .CreateRentalAsync(new CreateRentalRequest(ItemId, start, start.AddDays(2)));

        // Manually set OutForRent with a past end date to simulate overdue scenario
        await _fixture.Context.Database.ExecuteSqlAsync(
            $"""UPDATE rentals SET "Status" = 'OutForRent', "EndDate" = {Today().AddDays(-1)} WHERE "Id" = {created.Id}"""
        );
        _fixture.Context.ChangeTracker.Clear();

        _tokenState.CurrentToken = OwnerId.ToString();
        var result = await CreateSut().GetRentalAsync(created.Id);

        Assert.Equal("Overdue", result.Status);
    }
}
