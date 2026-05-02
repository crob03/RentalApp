// RentalApp/ViewModels/ItemsSearchBaseViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Contracts;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// Sentinel category representing "no filter" (all items), prepended to the filter list.
/// </summary>
internal static class ItemsSearchDefaults
{
    internal static readonly CategoryResponse AllItemsCategory = new(
        0,
        "All Items",
        string.Empty,
        0
    );
}

/// <summary>
/// Abstract base for item-listing view models. Extends <see cref="AuthenticatedViewModel"/> and provides
/// shared pagination state, category filtering, and <see cref="RunLoadAsync"/>/<see cref="RunLoadMoreAsync"/>
/// lifecycle helpers. Subclasses implement <see cref="ReloadAsync"/>.
/// </summary>
public abstract partial class ItemsSearchBaseViewModel<TItem> : AuthenticatedViewModel
    where TItem : IItemListable
{
    private readonly IItemService _itemService;

    /// <summary>Exposes the injected <see cref="IItemService"/> to subclasses.</summary>
    protected IItemService ItemService => _itemService;

    /// <summary>Default page size used by all item-listing requests.</summary>
    protected const int PageSize = 20;

    private bool _restoringCategory;
    private bool _hasLoaded;

    /// <summary>The currently loaded page of items.</summary>
    [ObservableProperty]
    private ObservableCollection<TItem> items = [];

    /// <summary>All available categories, excluding the "All Items" sentinel.</summary>
    [ObservableProperty]
    private List<CategoryResponse> categories = [];

    /// <summary>Category list shown in the filter picker, prepended with the "All Items" sentinel.</summary>
    [ObservableProperty]
    private List<CategoryResponse> filterCategories = [ItemsSearchDefaults.AllItemsCategory];

    /// <summary>The category picker selection, including the "All Items" sentinel option.</summary>
    [ObservableProperty]
    private CategoryResponse? selectedCategoryItem = ItemsSearchDefaults.AllItemsCategory;

    /// <summary>The active category slug filter; <see langword="null"/> means all categories.</summary>
    [ObservableProperty]
    private string? selectedCategory;

    /// <summary>The 1-based page number of the last successfully fetched page.</summary>
    [ObservableProperty]
    private int currentPage = 1;

    /// <summary>Indicates whether additional pages of results are available.</summary>
    [ObservableProperty]
    private bool hasMorePages;

    /// <summary>Indicates whether the initial page load is in progress.</summary>
    [ObservableProperty]
    private bool isLoading;

    /// <summary>Indicates whether a "load more" page fetch is in progress.</summary>
    [ObservableProperty]
    private bool isLoadingMore;

    /// <summary>
    /// Initialises the view model with item, navigation, and authentication dependencies.
    /// </summary>
    /// <param name="itemService">Used to fetch items and categories.</param>
    /// <param name="navigationService">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    /// <param name="tokenState">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    /// <param name="credentialStore">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    protected ItemsSearchBaseViewModel(
        IItemService itemService,
        INavigationService navigationService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore
    )
        : base(tokenState, credentialStore, navigationService)
    {
        _itemService = itemService;
    }

    /// <summary>
    /// Fetches and caches the category list on first call; subsequent calls are no-ops.
    /// Populates <see cref="Categories"/>, <see cref="FilterCategories"/>, and restores the active selection.
    /// </summary>
    protected async Task LoadCategoriesAsync()
    {
        if (Categories.Count > 0)
            return;
        var response = await _itemService.GetCategoriesAsync();
        var cats = response.Categories;
        var all = new List<CategoryResponse> { ItemsSearchDefaults.AllItemsCategory };
        all.AddRange(cats);
        Categories = cats;
        FilterCategories = all;
        RestoreCategory(all);
    }

    partial void OnSelectedCategoryItemChanged(CategoryResponse? value)
    {
        if (_restoringCategory)
            return;
        SelectedCategory = (value is null || value.Id == 0) ? null : value.Slug;
    }

    partial void OnSelectedCategoryChanged(string? value) => _ = TriggerReloadIfLoaded();

    /// <summary>
    /// Triggers <see cref="ReloadAsync"/> if the first load has already completed.
    /// Fire-and-forget; callers must discard the returned <see cref="Task"/> with <c>_ = </c>.
    /// </summary>
    protected async Task TriggerReloadIfLoaded()
    {
        if (_hasLoaded)
            await ReloadAsync();
    }

    /// <summary>
    /// Called after filters change to reset to page 1 and reload results. Subclasses must cancel
    /// any in-flight <see cref="IAsyncRelayCommand"/> before re-executing it.
    /// </summary>
    protected abstract Task ReloadAsync();

    /// <summary>
    /// Executes an initial page-load <paramref name="operation"/> with <see cref="IsLoading"/> lifecycle management.
    /// Swallows <see cref="OperationCanceledException"/> silently; surfaces other exceptions via <see cref="BaseViewModel.SetError"/>.
    /// </summary>
    protected async Task RunLoadAsync(Func<Task> operation)
    {
        try
        {
            IsLoading = true;
            ClearError();
            await operation();
            _hasLoaded = true;
        }
        catch (OperationCanceledException)
        {
            // cancellation is expected; nothing to report
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Appends the next page of results via <paramref name="operation"/> with <see cref="IsLoadingMore"/> lifecycle management.
    /// Does nothing if <see cref="HasMorePages"/> is <see langword="false"/>. Rolls back
    /// <see cref="CurrentPage"/> on failure.
    /// </summary>
    protected async Task RunLoadMoreAsync(Func<Task> operation)
    {
        if (!HasMorePages)
            return;

        try
        {
            IsLoadingMore = true;
            ClearError();
            CurrentPage++;
            await operation();
        }
        catch (Exception ex)
        {
            CurrentPage--;
            SetError(ex.Message);
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    /// <summary>
    /// Restores <see cref="SelectedCategoryItem"/> from <see cref="SelectedCategory"/> without
    /// triggering the <c>OnSelectedCategoryItemChanged</c> callback (which would otherwise kick off a reload).
    /// </summary>
    protected void RestoreCategory(List<CategoryResponse> all)
    {
        _restoringCategory = true;
        SelectedCategoryItem = string.IsNullOrEmpty(SelectedCategory)
            ? ItemsSearchDefaults.AllItemsCategory
            : all.FirstOrDefault(c => c.Slug == SelectedCategory)
                ?? ItemsSearchDefaults.AllItemsCategory;
        _restoringCategory = false;
    }

    /// <summary>Navigates to the detail page for the selected item.</summary>
    [RelayCommand]
    private Task NavigateToItemAsync(TItem item) =>
        NavigateToAsync(
            Routes.ItemDetails,
            new Dictionary<string, object> { ["itemId"] = item.Id }
        );

    /// <summary>Navigates to the create-item page.</summary>
    [RelayCommand]
    private Task NavigateToCreateItemAsync() => NavigateToAsync(Routes.CreateItem);
}
