using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the nearby items page. Resolves the device location on first load then
/// re-uses the cached coordinates when the user changes radius or category, avoiding repeated GPS requests.
/// </summary>
public partial class NearbyItemsViewModel : BaseViewModel
{
    private readonly IItemService _itemService;
    private readonly ILocationService _locationService;
    private readonly INavigationService _navigationService;
    private const int PageSize = 20;

    /// <summary>Device coordinates captured during the most recent full load; reused by load-more and filter changes.</summary>
    private double _cachedLat;
    private double _cachedLon;

    /// <summary>Prevents property-change callbacks from firing a reload before the first <see cref="LoadNearbyItemsAsync"/> completes.</summary>
    private bool _hasLoaded;
    private static readonly Category AllItemsCategory = new(0, "All Items", string.Empty, 0);

    /// <summary>Guards against <see cref="OnSelectedCategoryItemChanged"/> re-triggering a load while <see cref="LoadNearbyItemsAsync"/> is restoring the picker selection.</summary>
    private bool _restoringCategory;

    [ObservableProperty]
    private ObservableCollection<Item> items = [];

    [ObservableProperty]
    private double radius = 5.0;

    [ObservableProperty]
    private List<Category> categories = [];

    [ObservableProperty]
    private List<Category> filterCategories = [AllItemsCategory];

    [ObservableProperty]
    private Category? selectedCategoryItem = AllItemsCategory;

    [ObservableProperty]
    private string? selectedCategory;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private bool hasMorePages;

    /// <summary>
    /// Initialises a new instance of <see cref="NearbyItemsViewModel"/> for design-time support.
    /// </summary>
    public NearbyItemsViewModel()
    {
        Title = "Nearby Items";
    }

    /// <summary>
    /// Initialises a new instance of <see cref="NearbyItemsViewModel"/> with the required services.
    /// </summary>
    /// <param name="itemService">Service used to fetch nearby items and categories.</param>
    /// <param name="locationService">Service used to resolve the device's GPS position on load.</param>
    /// <param name="navigationService">Service used to navigate to the item details page.</param>
    public NearbyItemsViewModel(
        IItemService itemService,
        ILocationService locationService,
        INavigationService navigationService
    )
    {
        _itemService = itemService;
        _locationService = locationService;
        _navigationService = navigationService;
        Title = "Nearby Items";
    }

    partial void OnRadiusChanged(double value)
    {
        if (_hasLoaded)
            LoadNearbyItemsCommand.Execute(null);
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        if (_hasLoaded)
            LoadNearbyItemsCommand.Execute(null);
    }

    /// <summary>Translates the UI picker selection into the slug used for API calls, skipping the synthetic "All Items" entry (Id == 0).</summary>
    partial void OnSelectedCategoryItemChanged(Category? value)
    {
        if (_restoringCategory)
            return;
        SelectedCategory = (value is null || value.Id == 0) ? null : value.Slug;
    }

    /// <summary>
    /// Resolves the device location, resets to page 1, and loads items within <see cref="Radius"/>
    /// kilometres. The resolved coordinates are cached in <c>_cachedLat</c>/<c>_cachedLon</c>
    /// for subsequent pagination and filter changes.
    /// </summary>
    [RelayCommand]
    private Task LoadNearbyItemsAsync() =>
        RunAsync(async () =>
        {
            CurrentPage = 1;

            var (lat, lon) = await _locationService.GetCurrentLocationAsync();
            _cachedLat = lat;
            _cachedLon = lon;

            var result = await _itemService.GetNearbyItemsAsync(
                _cachedLat,
                _cachedLon,
                Radius,
                SelectedCategory,
                CurrentPage,
                PageSize
            );
            var cats = await _itemService.GetCategoriesAsync() ?? [];

            Items = new ObservableCollection<Item>(result);
            HasMorePages = result.Count == PageSize;
            IsEmpty = Items.Count == 0;
            Categories = cats;
            _hasLoaded = true;

            var all = new List<Category> { AllItemsCategory };
            all.AddRange(cats);
            FilterCategories = all;

            _restoringCategory = true;
            SelectedCategoryItem = string.IsNullOrEmpty(SelectedCategory)
                ? AllItemsCategory
                : all.FirstOrDefault(c => c.Slug == SelectedCategory) ?? AllItemsCategory;
            _restoringCategory = false;
        });

    /// <summary>
    /// Appends the next page of nearby results using the cached coordinates from the most recent
    /// full load. Rolls back <see cref="CurrentPage"/> if the request fails.
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
                var result = await _itemService.GetNearbyItemsAsync(
                    _cachedLat,
                    _cachedLon,
                    Radius,
                    SelectedCategory,
                    CurrentPage,
                    PageSize
                );
                foreach (var item in result)
                    Items.Add(item);
                HasMorePages = result.Count == PageSize;
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
}
