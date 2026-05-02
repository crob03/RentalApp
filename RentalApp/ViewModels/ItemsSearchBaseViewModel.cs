// RentalApp/ViewModels/ItemsSearchBaseViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Contracts;
using RentalApp.Contracts.Responses;
using RentalApp.Services;

namespace RentalApp.ViewModels;

internal static class ItemsSearchDefaults
{
    internal static readonly CategoryResponse AllItemsCategory = new(
        0,
        "All Items",
        string.Empty,
        0
    );
}

public abstract partial class ItemsSearchBaseViewModel<TItem> : BaseViewModel
    where TItem : IItemListable
{
    private readonly INavigationService _navigationService;
    private readonly IItemService _itemService;

    protected IItemService ItemService => _itemService;
    protected const int PageSize = 20;

    private bool _restoringCategory;
    private bool _hasLoaded;

    [ObservableProperty]
    private ObservableCollection<TItem> items = [];

    [ObservableProperty]
    private List<CategoryResponse> categories = [];

    [ObservableProperty]
    private List<CategoryResponse> filterCategories = [ItemsSearchDefaults.AllItemsCategory];

    [ObservableProperty]
    private CategoryResponse? selectedCategoryItem = ItemsSearchDefaults.AllItemsCategory;

    [ObservableProperty]
    private string? selectedCategory;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private bool hasMorePages;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isLoadingMore;

    protected ItemsSearchBaseViewModel(
        IItemService itemService,
        INavigationService navigationService
    )
    {
        _itemService = itemService;
        _navigationService = navigationService;
    }

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

    protected async Task TriggerReloadIfLoaded()
    {
        if (_hasLoaded)
            await ReloadAsync();
    }

    protected abstract Task ReloadAsync();

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

    protected void RestoreCategory(List<CategoryResponse> all)
    {
        _restoringCategory = true;
        SelectedCategoryItem = string.IsNullOrEmpty(SelectedCategory)
            ? ItemsSearchDefaults.AllItemsCategory
            : all.FirstOrDefault(c => c.Slug == SelectedCategory)
                ?? ItemsSearchDefaults.AllItemsCategory;
        _restoringCategory = false;
    }

    [RelayCommand]
    private async Task NavigateToItemAsync(TItem item) =>
        await _navigationService.NavigateToAsync(
            Routes.ItemDetails,
            new Dictionary<string, object> { ["itemId"] = item.Id }
        );

    [RelayCommand]
    private async Task NavigateToCreateItemAsync() =>
        await _navigationService.NavigateToAsync(Routes.CreateItem);
}
