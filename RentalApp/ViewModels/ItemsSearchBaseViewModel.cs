using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// Abstract base class for item-listing view models.
/// Manages shared pagination state, category loading and restoration, and commands for
/// navigating to item details and the create-item flow.
/// </summary>
public abstract partial class ItemsSearchBaseViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    protected readonly IItemService _itemService;

    /// <summary>Maximum number of items fetched per page.</summary>
    protected const int PageSize = 20;

    /// <summary>
    /// Sentinel <see cref="Category"/> representing "all categories". Used as the default picker
    /// selection and to signal that no category filter should be applied.
    /// </summary>
    protected static readonly Category AllItemsCategory = new(0, "All Items", string.Empty, 0);

    private bool _restoringCategory;
    private bool _hasLoaded;

    /// <summary>The current page of items displayed in the list.</summary>
    [ObservableProperty]
    private ObservableCollection<Item> items = [];

    /// <summary>All categories returned by the item service, excluding the "All Items" sentinel.</summary>
    [ObservableProperty]
    private List<Category> categories = [];

    /// <summary>
    /// Category list used for the filter picker — <see cref="AllItemsCategory"/> prepended to
    /// <see cref="Categories"/>.
    /// </summary>
    [ObservableProperty]
    private List<Category> filterCategories = [AllItemsCategory];

    /// <summary>The category object currently selected in the filter picker.</summary>
    [ObservableProperty]
    private Category? selectedCategoryItem = AllItemsCategory;

    /// <summary>
    /// Slug of the currently active category filter; <see langword="null"/> means all categories.
    /// </summary>
    [ObservableProperty]
    private string? selectedCategory;

    /// <summary>1-based page number of the most recently loaded page.</summary>
    [ObservableProperty]
    private int currentPage = 1;

    /// <summary>
    /// Indicates whether additional pages are available beyond the current one.
    /// </summary>
    [ObservableProperty]
    private bool hasMorePages;

    /// <summary>Indicates whether an initial or full-refresh load is in progress.</summary>
    [ObservableProperty]
    private bool isLoading;

    /// <summary>Indicates whether an incremental "load more" operation is in progress.</summary>
    [ObservableProperty]
    private bool isLoadingMore;

    /// <summary>
    /// Initialises a new instance of <see cref="ItemsSearchBaseViewModel"/> with the required services.
    /// </summary>
    /// <param name="itemService">Service used to fetch items and categories.</param>
    /// <param name="navigationService">Service used to navigate to item details and the create-item page.</param>
    protected ItemsSearchBaseViewModel(
        IItemService itemService,
        INavigationService navigationService
    )
    {
        _itemService = itemService;
        _navigationService = navigationService;
    }

    /// <summary>
    /// Fetches categories from the item service, rebuilds <see cref="FilterCategories"/>, and
    /// restores the previously selected category without triggering a reload.
    /// </summary>
    protected async Task LoadCategoriesAsync()
    {
        var cats = await _itemService.GetCategoriesAsync() ?? [];
        var all = new List<Category> { AllItemsCategory };
        all.AddRange(cats);
        Categories = cats;
        FilterCategories = all;
        RestoreCategory(all);
    }

    partial void OnSelectedCategoryItemChanged(Category? value)
    {
        if (_restoringCategory)
            return;
        SelectedCategory = (value is null || value.Id == 0) ? null : value.Slug;
    }

    partial void OnSelectedCategoryChanged(string? value) => TriggerReloadIfLoaded();

    /// <summary>
    /// Calls <see cref="ReloadAsync"/> only after the first successful load has completed,
    /// preventing premature reloads during initialisation.
    /// </summary>
    protected void TriggerReloadIfLoaded()
    {
        if (_hasLoaded)
            _ = ReloadAsync();
    }

    /// <summary>
    /// Triggers the subclass-specific load command to refresh the item list from scratch.
    /// Called automatically when the category or other filter properties change.
    /// </summary>
    protected abstract Task ReloadAsync();

    /// <summary>
    /// Executes <paramref name="operation"/> with <see cref="IsLoading"/> lifecycle management:
    /// sets <see cref="IsLoading"/>, clears any existing error, marks the view model as loaded
    /// on success, and restores <see cref="IsLoading"/> in a finally block.
    /// </summary>
    /// <param name="operation">The async load operation to execute.</param>
    protected async Task RunLoadAsync(Func<Task> operation)
    {
        try
        {
            IsLoading = true;
            ClearError();
            await operation();
            _hasLoaded = true;
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
    /// Executes <paramref name="operation"/> for a paginated "load more" request.
    /// Increments <see cref="CurrentPage"/> before the call and rolls it back on failure
    /// so the state remains consistent.
    /// </summary>
    /// <param name="operation">The async operation that appends the next page to <see cref="Items"/>.</param>
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
    /// Restores <see cref="SelectedCategoryItem"/> from the current <see cref="SelectedCategory"/>
    /// slug without triggering a reload via the <c>OnSelectedCategoryItemChanged</c> partial callback.
    /// </summary>
    /// <param name="all">The full category list including <see cref="AllItemsCategory"/>.</param>
    protected void RestoreCategory(List<Category> all)
    {
        _restoringCategory = true;
        SelectedCategoryItem = string.IsNullOrEmpty(SelectedCategory)
            ? AllItemsCategory
            : all.FirstOrDefault(c => c.Slug == SelectedCategory) ?? AllItemsCategory;
        _restoringCategory = false;
    }

    /// <summary>
    /// Navigates to the item details page for the given <paramref name="item"/>.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToItemAsync(Item item) =>
        await _navigationService.NavigateToAsync(
            Routes.ItemDetails,
            new Dictionary<string, object> { ["itemId"] = item.Id }
        );

    /// <summary>
    /// Navigates to the create-item page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToCreateItemAsync() =>
        await _navigationService.NavigateToAsync(Routes.CreateItem);
}
