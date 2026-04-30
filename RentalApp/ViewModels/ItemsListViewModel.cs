using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the browsable items list. Supports category filtering, text search, and infinite-scroll pagination.
/// </summary>
public partial class ItemsListViewModel : BaseViewModel
{
    private readonly IItemService _itemService;
    private readonly INavigationService _navigationService;
    private const int PageSize = 20;
    private static readonly Category AllItemsCategory = new(0, "All Items", string.Empty, 0);

    /// <summary>Guards against <see cref="OnSelectedCategoryItemChanged"/> re-triggering a load while <see cref="LoadItemsAsync"/> is restoring the picker selection.</summary>
    private bool _restoringCategory;

    /// <summary>Prevents property-change callbacks from firing a reload before the first <see cref="LoadItemsAsync"/> completes.</summary>
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
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private bool hasMorePages;

    /// <summary>
    /// Initialises a new instance of <see cref="ItemsListViewModel"/> for design-time support.
    /// </summary>
    public ItemsListViewModel()
    {
        Title = "Browse Items";
    }

    /// <summary>
    /// Initialises a new instance of <see cref="ItemsListViewModel"/> with the required services.
    /// </summary>
    /// <param name="itemService">Service used to fetch items and categories.</param>
    /// <param name="navigationService">Service used to navigate to item details and the create-item page.</param>
    public ItemsListViewModel(IItemService itemService, INavigationService navigationService)
    {
        _itemService = itemService;
        _navigationService = navigationService;
        Title = "Browse Items";
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        if (_hasLoaded)
            LoadItemsCommand.Execute(null);
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_hasLoaded)
            LoadItemsCommand.Execute(null);
    }

    /// <summary>Translates the UI picker selection into the slug used for API calls, skipping the synthetic "All Items" entry (Id == 0).</summary>
    partial void OnSelectedCategoryItemChanged(Category? value)
    {
        if (_restoringCategory)
            return;
        SelectedCategory = (value is null || value.Id == 0) ? null : value.Slug;
    }

    /// <summary>
    /// Resets to page 1 and loads items matching the current <see cref="SelectedCategory"/> and
    /// <see cref="SearchText"/> filters. Also refreshes the category picker list.
    /// </summary>
    [RelayCommand]
    private Task LoadItemsAsync() =>
        RunAsync(async () =>
        {
            CurrentPage = 1;
            var results = await _itemService.GetItemsAsync(
                SelectedCategory,
                SearchText,
                CurrentPage,
                PageSize
            );
            var cats = await _itemService.GetCategoriesAsync();

            Items = new ObservableCollection<Item>(results);
            Categories = cats;
            IsEmpty = results.Count == 0;
            HasMorePages = results.Count == PageSize;

            var all = new List<Category> { AllItemsCategory };
            all.AddRange(cats);
            FilterCategories = all;

            _restoringCategory = true;
            SelectedCategoryItem = string.IsNullOrEmpty(SelectedCategory)
                ? AllItemsCategory
                : all.FirstOrDefault(c => c.Slug == SelectedCategory) ?? AllItemsCategory;
            _restoringCategory = false;

            _hasLoaded = true;
        });

    /// <summary>
    /// Appends the next page of results to <see cref="Items"/>. Rolls back <see cref="CurrentPage"/>
    /// if the request fails so a retry will not skip a page.
    /// </summary>
    [RelayCommand]
    private Task LoadMoreItemsAsync() =>
        RunAsync(async () =>
        {
            if (!HasMorePages)
                return;
            CurrentPage++;
            try
            {
                var more = await _itemService.GetItemsAsync(
                    SelectedCategory,
                    SearchText,
                    CurrentPage,
                    PageSize
                );
                foreach (var item in more)
                    Items.Add(item);
                HasMorePages = more.Count == PageSize;
            }
            catch
            {
                CurrentPage--;
                throw;
            }
        });

    /// <summary>
    /// Navigates to the item details page, passing <paramref name="item"/>'s ID as a query parameter.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToItemAsync(Item item) =>
        await _navigationService.NavigateToAsync(
            Routes.ItemDetails,
            new Dictionary<string, object> { ["itemId"] = item.Id }
        );

    /// <summary>
    /// Navigates to the create-item page so the user can list a new rental.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToCreateItemAsync() =>
        await _navigationService.NavigateToAsync(Routes.CreateItem);
}
