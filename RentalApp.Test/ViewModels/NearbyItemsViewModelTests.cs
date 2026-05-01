using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class NearbyItemsViewModelTests
{
    private readonly IApiService _api = Substitute.For<IApiService>();
    private readonly ILocationService _locationService = Substitute.For<ILocationService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();

    private NearbyItemsViewModel CreateSut() => new(_api, _locationService, _nav);

    private static NearbyItemResponse MakeItem(int id) =>
        new(
            id,
            $"Item {id}",
            null,
            10.0,
            1,
            "Tools",
            1,
            "Owner",
            55.9,
            -3.2,
            0.5,
            true,
            null
        );

    private static CategoryResponse MakeCategory(
        int id = 1,
        string name = "Tools",
        string slug = "tools"
    ) => new(id, name, slug, 5);

    private static NearbyItemsResponse MakeNearbyResponse(List<NearbyItemResponse> items) =>
        new(items, new SearchLocationResponse(55.9533, -3.1883), 5.0, items.Count);

    // ── LoadNearbyItemsCommand ─────────────────────────────────────────

    [Fact]
    public async Task LoadNearbyItemsCommand_Success_PopulatesItems()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _api.GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>())
            .Returns(MakeNearbyResponse([MakeItem(1), MakeItem(2)]));
        _api.GetCategoriesAsync().Returns(new CategoriesResponse([]));
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Items.Count);
        Assert.False(sut.IsLoading);
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
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task LoadNearbyItemsCommand_MoreThanOnePage_SetsHasMorePagesTrue()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _api.GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>())
            .Returns(MakeNearbyResponse(Enumerable.Range(1, 21).Select(MakeItem).ToList()));
        _api.GetCategoriesAsync().Returns(new CategoriesResponse([]));
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.True(sut.HasMorePages);
    }

    // ── LoadMoreItemsCommand — uses cached GPS ─────────────────────────

    [Fact]
    public async Task LoadMoreItemsCommand_SlicesFromCacheWithoutApiCall()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _api.GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>())
            .Returns(MakeNearbyResponse(Enumerable.Range(1, 21).Select(MakeItem).ToList()));
        _api.GetCategoriesAsync().Returns(new CategoriesResponse([]));
        var sut = CreateSut();
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        await sut.LoadMoreItemsCommand.ExecuteAsync(null);

        Assert.Equal(21, sut.Items.Count);
        Assert.False(sut.HasMorePages);
        await _locationService.Received(1).GetCurrentLocationAsync();
        await _api
            .Received(1)
            .GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>());
    }

    // ── Radius change triggers reload ──────────────────────────────────

    [Fact]
    public async Task RadiusChange_AfterFirstLoad_TriggersReload()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _api.GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>())
            .Returns(MakeNearbyResponse([]));
        _api.GetCategoriesAsync().Returns(new CategoriesResponse([]));
        var sut = CreateSut();
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        sut.Radius = 10.0;

        await (sut.LoadNearbyItemsCommand.ExecutionTask ?? Task.CompletedTask);
        await _api
            .Received(2)
            .GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>());
    }

    // ── Category filter ────────────────────────────────────────────────

    [Fact]
    public async Task LoadNearbyItemsCommand_PopulatesFilterCategoriesWithAllItemsSentinel()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _api.GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>())
            .Returns(MakeNearbyResponse([]));
        _api.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
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
        _api.GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>())
            .Returns(MakeNearbyResponse([]));
        _api.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        Assert.Equal(0, sut.SelectedCategoryItem?.Id);
        Assert.Equal("All Items", sut.SelectedCategoryItem?.Name);
    }

    [Fact]
    public async Task SelectingCategory_UpdatesSelectedCategorySlug()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _api.GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>())
            .Returns(MakeNearbyResponse([]));
        _api.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        sut.SelectedCategoryItem = MakeCategory();

        Assert.Equal("tools", sut.SelectedCategory);
    }

    [Fact]
    public async Task SelectingAllItems_ClearsSelectedCategory()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _api.GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>())
            .Returns(MakeNearbyResponse([]));
        _api.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
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
        _api.GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>())
            .Returns(MakeNearbyResponse([]));
        _api.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        sut.SelectedCategoryItem = MakeCategory();
        await (sut.LoadNearbyItemsCommand.ExecutionTask ?? Task.CompletedTask);

        await _api
            .Received(2)
            .GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>());
    }

    [Fact]
    public async Task LoadNearbyItems_DoesNotTriggerExtraReload_WhenRestoringCategory()
    {
        _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
        _api.GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>())
            .Returns(MakeNearbyResponse([]));
        _api.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();

        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);
        await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

        await _api
            .Received(2)
            .GetNearbyItemsAsync(Arg.Any<GetNearbyItemsRequest>());
    }
}
