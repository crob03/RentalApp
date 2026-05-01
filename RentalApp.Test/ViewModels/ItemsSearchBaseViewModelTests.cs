using NSubstitute;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class ItemsSearchBaseViewModelTests
{
    private readonly IItemService _itemService = Substitute.For<IItemService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();

    private sealed class TestableViewModel(IItemService items, INavigationService nav)
        : ItemsSearchBaseViewModel(items, nav)
    {
        public int ReloadCallCount { get; private set; }

        protected override Task ReloadAsync()
        {
            ReloadCallCount++;
            return Task.CompletedTask;
        }

        public Task DoLoadAsync(Func<Task> op) => RunLoadAsync(op);

        public Task DoLoadMoreAsync(Func<Task> op) => RunLoadMoreAsync(op);

        public void DoRestoreCategory(List<Category> all) => RestoreCategory(all);
    }

    private TestableViewModel CreateSut() => new(_itemService, _nav);

    private static Category MakeCategory(
        int id = 1,
        string name = "Tools",
        string slug = "tools"
    ) => new(id, name, slug, 5);

    private static Category AllItems => new(0, "All Items", string.Empty, 0);

    private static Item MakeItem(int id = 1) =>
        new(
            id,
            "Drill",
            null,
            10.0,
            1,
            "Tools",
            1,
            "Alice",
            null,
            null,
            null,
            null,
            true,
            null,
            null,
            DateTime.UtcNow,
            null
        );

    // ── RunLoadAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task RunLoadAsync_SetsIsLoadingDuringExecution_AndClearsAfter()
    {
        var sut = CreateSut();
        bool wasLoading = false;
        await sut.DoLoadAsync(async () =>
        {
            wasLoading = sut.IsLoading;
        });
        Assert.True(wasLoading);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task RunLoadAsync_WhenThrows_SetsError_AndClearsIsLoading()
    {
        var sut = CreateSut();
        await sut.DoLoadAsync(() => throw new Exception("boom"));
        Assert.True(sut.HasError);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.False(sut.IsLoading);
    }

    // ── RunLoadMoreAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RunLoadMoreAsync_WhenHasMorePages_SetsIsLoadingMoreAndIncrementsPage()
    {
        var sut = CreateSut();
        sut.HasMorePages = true;
        sut.CurrentPage = 1;
        bool wasLoadingMore = false;
        int pageInsideOp = 0;
        await sut.DoLoadMoreAsync(async () =>
        {
            wasLoadingMore = sut.IsLoadingMore;
            pageInsideOp = sut.CurrentPage;
        });
        Assert.True(wasLoadingMore);
        Assert.Equal(2, pageInsideOp);
        Assert.False(sut.IsLoadingMore);
    }

    [Fact]
    public async Task RunLoadMoreAsync_WhenHasMorePagesFalse_DoesNothing()
    {
        var sut = CreateSut();
        sut.HasMorePages = false;
        bool ran = false;
        await sut.DoLoadMoreAsync(async () =>
        {
            ran = true;
        });
        Assert.False(ran);
        Assert.Equal(1, sut.CurrentPage);
    }

    [Fact]
    public async Task RunLoadMoreAsync_WhenThrows_RollsBackPage_AndSetsError()
    {
        var sut = CreateSut();
        sut.HasMorePages = true;
        sut.CurrentPage = 1;
        await sut.DoLoadMoreAsync(() => throw new Exception("fail"));
        Assert.Equal(1, sut.CurrentPage);
        Assert.True(sut.HasError);
        Assert.Equal("fail", sut.ErrorMessage);
        Assert.False(sut.IsLoadingMore);
    }

    // ── Category slug mapping ──────────────────────────────────────────

    [Fact]
    public void SelectingCategory_SetsSelectedCategorySlug()
    {
        var sut = CreateSut();
        sut.SelectedCategoryItem = MakeCategory();
        Assert.Equal("tools", sut.SelectedCategory);
    }

    [Fact]
    public void SelectingAllItemsCategory_ClearsSelectedCategory()
    {
        var sut = CreateSut();
        sut.SelectedCategoryItem = MakeCategory();
        sut.SelectedCategoryItem = AllItems;
        Assert.Null(sut.SelectedCategory);
    }

    // ── _hasLoaded guard ───────────────────────────────────────────────

    [Fact]
    public void CategoryChange_BeforeFirstLoad_DoesNotTriggerReload()
    {
        var sut = CreateSut();
        sut.SelectedCategoryItem = MakeCategory();
        Assert.Equal(0, sut.ReloadCallCount);
    }

    [Fact]
    public async Task CategoryChange_AfterFirstLoad_TriggersReload()
    {
        var sut = CreateSut();
        await sut.DoLoadAsync(async () => { });

        sut.SelectedCategoryItem = MakeCategory();

        Assert.Equal(1, sut.ReloadCallCount);
    }

    // ── RestoreCategory ────────────────────────────────────────────────

    [Fact]
    public async Task RestoreCategory_SetsSelectedCategoryItem_WithoutTriggeringReload()
    {
        var sut = CreateSut();
        sut.SelectedCategory = "tools"; // before first load — _hasLoaded still false
        await sut.DoLoadAsync(async () => { });
        var all = new List<Category> { AllItems, MakeCategory() };

        sut.DoRestoreCategory(all);

        Assert.Equal("tools", sut.SelectedCategoryItem?.Slug);
        Assert.Equal(0, sut.ReloadCallCount);
    }

    // ── Commands ───────────────────────────────────────────────────────

    [Fact]
    public async Task NavigateToCreateItemCommand_NavigatesToCreateItem()
    {
        var sut = CreateSut();
        await sut.NavigateToCreateItemCommand.ExecuteAsync(null);
        await _nav.Received(1).NavigateToAsync(Routes.CreateItem);
    }

    [Fact]
    public async Task NavigateToItemCommand_NavigatesToItemDetails_WithItemId()
    {
        var sut = CreateSut();
        await sut.NavigateToItemCommand.ExecuteAsync(MakeItem(42));
        await _nav.Received(1)
            .NavigateToAsync(
                Routes.ItemDetails,
                Arg.Is<Dictionary<string, object>>(d =>
                    d.ContainsKey("itemId") && (int)d["itemId"] == 42
                )
            );
    }
}
