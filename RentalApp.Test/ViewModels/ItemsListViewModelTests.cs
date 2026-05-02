using System.Collections.ObjectModel;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class ItemsListViewModelTests
{
    private readonly IItemService _itemService = Substitute.For<IItemService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();

    private static ItemSummaryResponse MakeItem(int id = 1, string title = "Drill") =>
        new(id, title, null, 10.0, 1, "Tools", 1, "Alice", null, true, null, DateTime.UtcNow);

    private static CategoryResponse MakeCategory(
        int id = 1,
        string name = "Tools",
        string slug = "tools"
    ) => new(id, name, slug, 5);

    private static ItemsResponse MakeItemsResponse(
        List<ItemSummaryResponse> items,
        int page = 1,
        int totalPages = 1
    ) => new(items, items.Count, page, 20, totalPages);

    private ItemsListViewModel CreateSut() => new(_itemService, _nav);

    // ── LoadItemsCommand ───────────────────────────────────────────────

    [Fact]
    public async Task LoadItemsCommand_Success_PopulatesItems()
    {
        _itemService
            .GetItemsAsync(Arg.Any<GetItemsRequest>())
            .Returns(MakeItemsResponse([MakeItem(1), MakeItem(2, "Ladder")]));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Items.Count);
    }

    [Fact]
    public async Task LoadItemsCommand_Success_ClearsExistingItems()
    {
        _itemService
            .GetItemsAsync(Arg.Any<GetItemsRequest>())
            .Returns(MakeItemsResponse([MakeItem()]));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();
        await sut.LoadItemsCommand.ExecuteAsync(null);

        _itemService
            .GetItemsAsync(Arg.Any<GetItemsRequest>())
            .Returns(MakeItemsResponse([MakeItem(2, "Ladder")]));
        await sut.LoadItemsCommand.ExecuteAsync(null);

        var item = Assert.Single(sut.Items);
        Assert.Equal("Ladder", item.Title);
    }

    [Fact]
    public async Task LoadItemsCommand_FullPage_SetsHasMorePagesTrue()
    {
        var fullPage = Enumerable.Range(1, 20).Select(i => MakeItem(i)).ToList();
        _itemService
            .GetItemsAsync(Arg.Any<GetItemsRequest>())
            .Returns(MakeItemsResponse(fullPage, totalPages: 2));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([]));
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.True(sut.HasMorePages);
    }

    [Fact]
    public async Task LoadItemsCommand_PartialPage_SetsHasMorePagesFalse()
    {
        _itemService
            .GetItemsAsync(Arg.Any<GetItemsRequest>())
            .Returns(MakeItemsResponse([MakeItem()]));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([]));
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.False(sut.HasMorePages);
    }

    [Fact]
    public async Task LoadItemsCommand_ServiceThrows_SetsError()
    {
        _itemService
            .GetItemsAsync(Arg.Any<GetItemsRequest>())
            .ThrowsAsync(new Exception("network error"));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([]));
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("network error", sut.ErrorMessage);
    }

    // ── LoadMoreItemsCommand ───────────────────────────────────────────

    [Fact]
    public async Task LoadMoreItemsCommand_AppendsToExistingItems()
    {
        var fullPage = Enumerable.Range(1, 20).Select(i => MakeItem(i)).ToList();
        _itemService
            .GetItemsAsync(Arg.Any<GetItemsRequest>())
            .Returns(MakeItemsResponse(fullPage, totalPages: 2));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([]));
        var sut = CreateSut();
        await sut.LoadItemsCommand.ExecuteAsync(null);

        _itemService
            .GetItemsAsync(Arg.Any<GetItemsRequest>())
            .Returns(MakeItemsResponse([MakeItem(21, "Ladder")]));
        await sut.LoadMoreItemsCommand.ExecuteAsync(null);

        Assert.Equal(21, sut.Items.Count);
    }

    // ── Category filter ────────────────────────────────────────────────

    [Fact]
    public async Task LoadItemsCommand_PopulatesFilterCategoriesWithAllItemsSentinel()
    {
        _itemService.GetItemsAsync(Arg.Any<GetItemsRequest>()).Returns(MakeItemsResponse([]));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.FilterCategories.Count);
        Assert.Equal(0, sut.FilterCategories[0].Id);
        Assert.Equal("All Items", sut.FilterCategories[0].Name);
        Assert.Equal("tools", sut.FilterCategories[1].Slug);
    }

    [Fact]
    public async Task LoadItemsCommand_SelectedCategoryItemDefaultsToAllItems()
    {
        _itemService.GetItemsAsync(Arg.Any<GetItemsRequest>()).Returns(MakeItemsResponse([]));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);

        Assert.Equal(0, sut.SelectedCategoryItem?.Id);
        Assert.Equal("All Items", sut.SelectedCategoryItem?.Name);
    }

    [Fact]
    public async Task SelectingCategory_UpdatesSelectedCategorySlug()
    {
        _itemService.GetItemsAsync(Arg.Any<GetItemsRequest>()).Returns(MakeItemsResponse([]));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();
        await sut.LoadItemsCommand.ExecuteAsync(null);

        sut.SelectedCategoryItem = MakeCategory();

        Assert.Equal("tools", sut.SelectedCategory);
    }

    [Fact]
    public async Task SelectingAllItems_ClearsSelectedCategory()
    {
        _itemService.GetItemsAsync(Arg.Any<GetItemsRequest>()).Returns(MakeItemsResponse([]));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();
        await sut.LoadItemsCommand.ExecuteAsync(null);
        sut.SelectedCategoryItem = MakeCategory();

        sut.SelectedCategoryItem = sut.FilterCategories[0];

        Assert.Null(sut.SelectedCategory);
    }

    [Fact]
    public async Task CategoryChange_AfterLoad_TriggersReload()
    {
        _itemService.GetItemsAsync(Arg.Any<GetItemsRequest>()).Returns(MakeItemsResponse([]));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();
        await sut.LoadItemsCommand.ExecuteAsync(null);

        sut.SelectedCategoryItem = MakeCategory();
        await (sut.LoadItemsCommand.ExecutionTask ?? Task.CompletedTask);

        await _itemService.Received(2).GetItemsAsync(Arg.Any<GetItemsRequest>());
    }

    [Fact]
    public async Task LoadItems_DoesNotTriggerExtraReload_WhenRestoringCategory()
    {
        _itemService.GetItemsAsync(Arg.Any<GetItemsRequest>()).Returns(MakeItemsResponse([]));
        _itemService.GetCategoriesAsync().Returns(new CategoriesResponse([MakeCategory()]));
        var sut = CreateSut();

        await sut.LoadItemsCommand.ExecuteAsync(null);
        await sut.LoadItemsCommand.ExecuteAsync(null);

        await _itemService.Received(2).GetItemsAsync(Arg.Any<GetItemsRequest>());
    }
}
