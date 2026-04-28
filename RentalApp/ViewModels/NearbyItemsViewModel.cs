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

    [ObservableProperty]
    private ObservableCollection<Item> items = [];

    [ObservableProperty]
    private double radius = 5.0;

    [ObservableProperty]
    private List<Category> categories = [];

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
            _ = LoadNearbyItemsAsync();
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        if (_hasLoaded)
            _ = LoadNearbyItemsAsync();
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

            Items = new ObservableCollection<Item>(result);
            HasMorePages = result.Count == PageSize;
            IsEmpty = Items.Count == 0;
            _hasLoaded = true;
        });

    [RelayCommand]
    private async Task LoadMoreItemsAsync()
    {
        if (!HasMorePages || IsBusy)
            return;

        try
        {
            IsBusy = true;
            CurrentPage++;

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
        catch (Exception ex)
        {
            CurrentPage--;
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToItemAsync(Item item) =>
        await _navigationService.NavigateToAsync(
            Routes.ItemDetails,
            new Dictionary<string, object> { ["itemId"] = item.Id }
        );
}
