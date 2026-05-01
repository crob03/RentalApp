using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class NearbyItemsViewModel : ItemsSearchBaseViewModel
{
    private readonly IItemService _itemService;
    private readonly ILocationService _locationService;

    private double _cachedLat;
    private double _cachedLon;
    private bool _locationFetched;

    [ObservableProperty]
    private double radius = 5.0;

    public NearbyItemsViewModel(
        IItemService itemService,
        ILocationService locationService,
        INavigationService navigationService
    )
        : base(navigationService)
    {
        _itemService = itemService;
        _locationService = locationService;
        Title = "Nearby Items";
    }

    partial void OnRadiusChanged(double value) => TriggerReloadIfLoaded();

    protected override Task ReloadAsync() => LoadNearbyItemsCommand.ExecuteAsync(null);

    [RelayCommand]
    private Task LoadNearbyItemsAsync() =>
        RunLoadAsync(async () =>
        {
            CurrentPage = 1;

            if (!_locationFetched)
            {
                var (lat, lon) = await _locationService.GetCurrentLocationAsync();
                _cachedLat = lat;
                _cachedLon = lon;
                _locationFetched = true;
            }

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
            Categories = cats;

            var all = new List<Category> { AllItemsCategory };
            all.AddRange(cats);
            FilterCategories = all;
            RestoreCategory(all);
        });

    [RelayCommand]
    private Task LoadMoreItemsAsync() =>
        RunLoadMoreAsync(async () =>
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
        });
}
