using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public abstract partial class ItemsSearchBaseViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    protected readonly IItemService _itemService;
    protected const int PageSize = 20;
    protected static readonly Category AllItemsCategory = new(0, "All Items", string.Empty, 0);

    private bool _restoringCategory;
    private bool _hasLoaded;

    [ObservableProperty]
    private ObservableCollection<Item> items = [];

    [ObservableProperty]
    private List<Category> categories = [];

    [ObservableProperty]
    private List<Category> filterCategories = [AllItemsCategory];

    [ObservableProperty]
    private Category? selectedCategoryItem = AllItemsCategory;

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

    protected void TriggerReloadIfLoaded()
    {
        if (_hasLoaded)
            _ = ReloadAsync();
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

    protected void RestoreCategory(List<Category> all)
    {
        _restoringCategory = true;
        SelectedCategoryItem = string.IsNullOrEmpty(SelectedCategory)
            ? AllItemsCategory
            : all.FirstOrDefault(c => c.Slug == SelectedCategory) ?? AllItemsCategory;
        _restoringCategory = false;
    }

    [RelayCommand]
    private async Task NavigateToItemAsync(Item item) =>
        await _navigationService.NavigateToAsync(
            Routes.ItemDetails,
            new Dictionary<string, object> { ["itemId"] = item.Id }
        );

    [RelayCommand]
    private async Task NavigateToCreateItemAsync() =>
        await _navigationService.NavigateToAsync(Routes.CreateItem);
}
