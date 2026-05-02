using NetTopologySuite.Geometries;
using RentalApp.Database.Repositories;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Repositories;

public class ItemRepositoryTests
    : IClassFixture<DatabaseFixture<ItemRepositoryTests>>,
        IAsyncLifetime
{
    private readonly DatabaseFixture<ItemRepositoryTests> _fixture;
    private static readonly GeometryFactory Factory = new GeometryFactory(
        new PrecisionModel(),
        4326
    );

    public ItemRepositoryTests(DatabaseFixture<ItemRepositoryTests> fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetItemsAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private ItemRepository CreateSut() => new(_fixture.ContextFactory);

    // ── GetItemsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetItemsAsync_NoFilter_ReturnsAllItems()
    {
        var sut = CreateSut();

        var items = await sut.GetItemsAsync(null, null, 1, 20);

        Assert.Equal(3, items.Count());
    }

    [Fact]
    public async Task GetItemsAsync_CategoryFilter_ReturnsOnlyMatchingCategory()
    {
        var sut = CreateSut();

        var items = await sut.GetItemsAsync("tools", null, 1, 20);

        Assert.All(items, i => Assert.Equal("tools", i.Category.Slug));
    }

    [Fact]
    public async Task GetItemsAsync_SearchFilter_ReturnsMatchingTitles()
    {
        var sut = CreateSut();

        var items = await sut.GetItemsAsync(null, "drill", 1, 20);

        Assert.Single(items);
        Assert.Equal("Test Drill", items.First().Title);
    }

    [Fact]
    public async Task GetItemsAsync_Page2_ReturnsSecondPage()
    {
        var sut = CreateSut();

        var page1 = await sut.GetItemsAsync(null, null, 1, 2);
        var page2 = await sut.GetItemsAsync(null, null, 2, 2);

        Assert.Equal(2, page1.Count());
        Assert.Single(page2);
        Assert.NotEqual(page1.First().Id, page2.First().Id);
    }

    [Fact]
    public async Task GetItemsAsync_IncludesNavigationProperties()
    {
        var sut = CreateSut();

        var items = await sut.GetItemsAsync(null, null, 1, 20);

        Assert.All(
            items,
            i =>
            {
                Assert.NotNull(i.Category);
                Assert.NotNull(i.Owner);
            }
        );
    }

    // ── GetNearbyItemsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetNearbyItemsAsync_SmallRadius_ExcludesDistantItems()
    {
        var sut = CreateSut();
        var origin = Factory.CreatePoint(new Coordinate(-3.1883, 55.9533));

        var items = await sut.GetNearbyItemsAsync(origin, 5_000, null);

        Assert.Equal(2, items.Count());
        Assert.DoesNotContain(items, i => i.Title == "Far Away Laptop");
    }

    [Fact]
    public async Task GetNearbyItemsAsync_CategoryFilter_AppliedWithinRadius()
    {
        var sut = CreateSut();
        var origin = Factory.CreatePoint(new Coordinate(-3.1883, 55.9533));

        var items = await sut.GetNearbyItemsAsync(origin, 5_000, "electronics");

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetNearbyItemsAsync_OrderedByDistance()
    {
        var sut = CreateSut();
        var origin = Factory.CreatePoint(new Coordinate(-3.1883, 55.9533));

        var items = (await sut.GetNearbyItemsAsync(origin, 5_000, null)).ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal(1, items[0].Id);
    }

    // ── GetItemAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetItemAsync_ExistingId_ReturnsItemWithNavProperties()
    {
        var sut = CreateSut();

        var item = await sut.GetItemAsync(1);

        Assert.NotNull(item);
        Assert.Equal(1, item!.Id);
        Assert.NotNull(item.Category);
        Assert.NotNull(item.Owner);
    }

    [Fact]
    public async Task GetItemAsync_NonExistentId_ReturnsNull()
    {
        var sut = CreateSut();

        var item = await sut.GetItemAsync(999);

        Assert.Null(item);
    }

    // ── CreateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CreateItemAsync_ValidInput_PersistsAndReturnsItem()
    {
        var sut = CreateSut();
        var location = Factory.CreatePoint(new Coordinate(-3.1883, 55.9533));

        var item = await sut.CreateItemAsync("New Drill", "desc", 15.0, 1, 1, location);

        Assert.True(item.Id > 0);
        Assert.Equal("New Drill", item.Title);
        Assert.True(item.IsAvailable);
    }

    // ── UpdateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItemAsync_PartialUpdate_OnlyChangesSuppliedFields()
    {
        var sut = CreateSut();

        var updated = await sut.UpdateItemAsync(1, "Updated Title", null, null, null);

        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal(10.0, updated.DailyRate);
    }

    [Fact]
    public async Task UpdateItemAsync_NonExistentId_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();

        var act = () => sut.UpdateItemAsync(999, "X", null, null, null);

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── CountItemsByOwnerAsync ─────────────────────────────────────────

    [Fact]
    public async Task CountItemsByOwnerAsync_OwnerWithItems_ReturnsCorrectCount()
    {
        var sut = CreateSut();

        var count = await sut.CountItemsByOwnerAsync(1);

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task CountItemsByOwnerAsync_UnknownOwner_ReturnsZero()
    {
        var sut = CreateSut();

        var count = await sut.CountItemsByOwnerAsync(999);

        Assert.Equal(0, count);
    }

    // ── CountItemsByCategoryAsync ──────────────────────────────────────

    [Fact]
    public async Task CountItemsByCategoryAsync_ReturnsCountPerCategory()
    {
        var sut = CreateSut();

        var counts = await sut.CountItemsByCategoryAsync();

        Assert.Equal(2, counts[1]); // tools: 2 items
        Assert.Equal(1, counts[2]); // electronics: 1 item
    }

    [Fact]
    public async Task CountItemsByCategoryAsync_OnlyIncludesCategoriesWithItems()
    {
        var sut = CreateSut();

        var counts = await sut.CountItemsByCategoryAsync();

        Assert.All(counts.Values, count => Assert.True(count > 0));
    }
}
