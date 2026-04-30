using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class NearbyItemsViewModel : BaseViewModel
{
    private readonly IItemService _itemService;
    private readonly ILocationService _locationService;
    private readonly INavigationService _navigationService;
    private const int PageSize = 20;

    private double _cachedLat;
    private double _cachedLon;
    private bool _hasLoaded;
    private static readonly Category AllItemsCategory = new(0, "All Items", string.Empty, 0);
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

    public NearbyItemsViewModel()
    {
        Title = "Nearby Items";
    }

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

    partial void OnSelectedCategoryItemChanged(Category? value)
    {
        if (_restoringCategory)
            return;
        SelectedCategory = (value is null || value.Id == 0) ? null : value.Slug;
    }

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

    [RelayCommand]
    private async Task NavigateToItemAsync(Item item) =>
        await _navigationService.NavigateToAsync(
            Routes.ItemDetails,
            new Dictionary<string, object> { ["itemId"] = item.Id }
        );
}
