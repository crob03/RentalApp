using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Database.States;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Repositories;

public class RentalRepositoryTests
    : IClassFixture<DatabaseFixture<RentalRepositoryTests>>,
        IAsyncLifetime
{
    private readonly DatabaseFixture<RentalRepositoryTests> _fixture;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    // Seeded by DatabaseFixture: User 1 (owner), User 2 (borrower), Item 1 (OwnerId=1, DailyRate=10)
    private const int OwnerId = 1;
    private const int BorrowerId = 2;
    private const int ItemId = 1;

    public RentalRepositoryTests(DatabaseFixture<RentalRepositoryTests> fixture)
    {
        _fixture = fixture;
        _contextFactory = fixture.ContextFactory;
    }

    public async Task InitializeAsync() => await _fixture.ResetRentalsAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private RentalRepository CreateSut() => new(_contextFactory);

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow.Date);

    [Fact]
    public async Task CreateRentalAsync_ReturnsHydratedRentalWithRequestedStatus()
    {
        var start = Today().AddDays(1);
        var end = Today().AddDays(3);

        var rental = await CreateSut().CreateRentalAsync(ItemId, OwnerId, BorrowerId, start, end);

        Assert.Equal(ItemId, rental.ItemId);
        Assert.Equal(OwnerId, rental.OwnerId);
        Assert.Equal(BorrowerId, rental.BorrowerId);
        Assert.Equal(start, rental.StartDate);
        Assert.Equal(end, rental.EndDate);
        Assert.Equal(RentalStatus.Requested, rental.Status);
        Assert.NotNull(rental.Item);
        Assert.NotNull(rental.Owner);
        Assert.NotNull(rental.Borrower);
    }

    [Fact]
    public async Task GetRentalAsync_ExistingId_ReturnsRental()
    {
        var start = Today().AddDays(1);
        var created = await CreateSut()
            .CreateRentalAsync(ItemId, OwnerId, BorrowerId, start, start.AddDays(2));

        var result = await CreateSut().GetRentalAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetRentalAsync_NonExistentId_ReturnsNull()
    {
        var result = await CreateSut().GetRentalAsync(9999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetIncomingRentalsAsync_ReturnsOnlyOwnerRentals()
    {
        var start = Today().AddDays(1);
        await CreateSut().CreateRentalAsync(ItemId, OwnerId, BorrowerId, start, start.AddDays(2));

        var results = await CreateSut().GetIncomingRentalsAsync(OwnerId);

        Assert.Single(results);
        Assert.All(results, r => Assert.Equal(OwnerId, r.OwnerId));
    }

    [Fact]
    public async Task GetOutgoingRentalsAsync_ReturnsOnlyBorrowerRentals()
    {
        var start = Today().AddDays(1);
        await CreateSut().CreateRentalAsync(ItemId, OwnerId, BorrowerId, start, start.AddDays(2));

        var results = await CreateSut().GetOutgoingRentalsAsync(BorrowerId);

        Assert.Single(results);
        Assert.All(results, r => Assert.Equal(BorrowerId, r.BorrowerId));
    }

    [Fact]
    public async Task UpdateRentalStatusAsync_UpdatesStatusAndUpdatedAt()
    {
        var start = Today().AddDays(1);
        var created = await CreateSut()
            .CreateRentalAsync(ItemId, OwnerId, BorrowerId, start, start.AddDays(2));

        var updated = await CreateSut().UpdateRentalStatusAsync(created.Id, RentalStatus.Approved);

        Assert.Equal(RentalStatus.Approved, updated.Status);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateRentalStatusAsync_NonExistentId_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().UpdateRentalStatusAsync(9999, RentalStatus.Approved)
        );
    }

    [Fact]
    public async Task HasOverlappingRentalAsync_OverlappingDates_ReturnsTrue()
    {
        var start = Today().AddDays(5);
        await CreateSut().CreateRentalAsync(ItemId, OwnerId, BorrowerId, start, start.AddDays(5));

        var overlaps = await CreateSut()
            .HasOverlappingRentalAsync(ItemId, start.AddDays(2), start.AddDays(7));

        Assert.True(overlaps);
    }

    [Fact]
    public async Task HasOverlappingRentalAsync_NonOverlappingDates_ReturnsFalse()
    {
        var start = Today().AddDays(1);
        await CreateSut().CreateRentalAsync(ItemId, OwnerId, BorrowerId, start, start.AddDays(3));

        var overlaps = await CreateSut()
            .HasOverlappingRentalAsync(ItemId, start.AddDays(3), start.AddDays(6));

        Assert.False(overlaps);
    }

    [Fact]
    public async Task HasOverlappingRentalAsync_SameDayTurnaround_ReturnsFalse()
    {
        // Existing rental ends on day 5; new rental starts on day 5 — allowed.
        var start = Today().AddDays(1);
        await CreateSut().CreateRentalAsync(ItemId, OwnerId, BorrowerId, start, start.AddDays(4));

        var overlaps = await CreateSut()
            .HasOverlappingRentalAsync(ItemId, start.AddDays(4), start.AddDays(7));

        Assert.False(overlaps);
    }
}
