using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Models;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class NearbyItemsViewModelTests
{
    private readonly IItemService _itemService = Substitute.For<IItemService>();
    private readonly ILocationService _locationService = Substitute.For<ILocationService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();

    private NearbyItemsViewModel CreateSut() => new(_itemService, _locationService, _nav);

    private static Item MakeItem(int id) =>
        new(
            id,
            $"Item {id}",
            null,
            10.0,
            1,
            "Tools",
            1,
            "Owner",
            null,
            55.9,
            -3.2,
            0.5,
            true,
            null,
            null,
            null,
            null
        );

    private static Category MakeCategory(
        int id = 1,
        string name = "Tools",
        string slug = "tools"
    ) => new(id, name, slug, 5);

    // ── LoadNearbyItemsCommand ─────────────────────────────────────────

    [Fact]
    public async Task LoadNearbyItemsCommand_Success_PopulatesItems()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(55.9533, -3.1883, 5.0, null, 1, 20)
            .Returns(new List<Item> { MakeItem(1), MakeItem(2) });
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Items.Count);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task LoadNearbyItemsCommand_GpsFails_SetsError()
    {
        _locationService
            .GetCurrentLocationAsync()
            .ThrowsAsync(
                new InvalidOperationException(
                    "Location unavailable. Please enable GPS and try again."
                )
            );
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Contains("Location unavailable", sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }

    [Fact]
    public async Task LoadNearbyItemsCommand_EmptyResult_SetsIsEmptyTrue()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            )
            .Returns(new List<Item>());
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.True(sut.IsEmpty);
    }

    [Fact]
    public async Task LoadNearbyItemsCommand_FullPage_SetsHasMorePagesTrue()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                1,
                20
            )
            .Returns(Enumerable.Range(1, 20).Select(MakeItem).ToList());
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.True(sut.HasMorePages);
    }

    // ── LoadMoreItemsCommand — uses cached GPS ─────────────────────────

    [Fact]
    public async Task LoadMoreItemsCommand_UsesCachedGpsCoordinates()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(55.9533, -3.1883, 5.0, null, 1, 20)
            .Returns(Enumerable.Range(1, 20).Select(MakeItem).ToList());
        _itemService
            .GetNearbyItemsAsync(55.9533, -3.1883, 5.0, null, 2, 20)
            .Returns(new List<Item> { MakeItem(21) });
        var sut = CreateSut();
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        await sut.LoadMoreItemsCommand.ExecuteAsync(null);

        Assert.Equal(21, sut.Items.Count);
        await _locationService.Received(1).GetCurrentLocationAsync();
    }

    // ── Radius change triggers reload ──────────────────────────────────

    [Fact]
    public async Task RadiusChange_AfterFirstLoad_TriggersReload()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            )
            .Returns(new List<Item>());
        var sut = CreateSut();
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        sut.Radius = 10.0;

        await Task.Delay(50);
        await _itemService
            .Received(2)
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            );
    }

    // ── Category filter ────────────────────────────────────────────────

    [Fact]
    public async Task LoadNearbyItemsCommand_PopulatesFilterCategoriesWithAllItemsSentinel()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            )
            .Returns([]);
        _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.FilterCategories.Count);
        Assert.Equal(0, sut.FilterCategories[0].Id);
        Assert.Equal("All Items", sut.FilterCategories[0].Name);
        Assert.Equal("tools", sut.FilterCategories[1].Slug);
    }

    [Fact]
    public async Task LoadNearbyItemsCommand_SelectedCategoryItemDefaultsToAllItems()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            )
            .Returns([]);
        _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.Equal(0, sut.SelectedCategoryItem?.Id);
        Assert.Equal("All Items", sut.SelectedCategoryItem?.Name);
    }

    [Fact]
    public async Task SelectingCategory_UpdatesSelectedCategorySlug()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            )
            .Returns([]);
        _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
        var sut = CreateSut();
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        sut.SelectedCategoryItem = MakeCategory();

        Assert.Equal("tools", sut.SelectedCategory);
    }

    [Fact]
    public async Task SelectingAllItems_ClearsSelectedCategory()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            )
            .Returns([]);
        _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
        var sut = CreateSut();
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);
        sut.SelectedCategoryItem = MakeCategory();

        sut.SelectedCategoryItem = sut.FilterCategories[0];

        Assert.Null(sut.SelectedCategory);
    }

    [Fact]
    public async Task CategoryChange_AfterLoad_TriggersReload()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            )
            .Returns([]);
        _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
        var sut = CreateSut();
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        sut.SelectedCategoryItem = MakeCategory();
        await Task.Delay(50);

        await _itemService
            .Received(2)
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            );
    }

    [Fact]
    public async Task LoadNearbyItems_DoesNotTriggerExtraReload_WhenRestoringCategory()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _itemService
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            )
            .Returns([]);
        _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        await _itemService
            .Received(2)
            .GetNearbyItemsAsync(
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<double>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            );
    }
}
