using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the "Nearby Items" page.
/// Fetches all matching items from the server in a single call and paginates client-side,
/// caching the device location so the GPS is only queried once per session.
/// </summary>
public partial class NearbyItemsViewModel : ItemsSearchBaseViewModel
{
    private readonly ILocationService _locationService;

    private double _cachedLat;
    private double _cachedLon;
    private bool _locationFetched;
    private List<Item> _allNearbyItems = [];

    /// <summary>Search radius in kilometres; changes trigger a full reload.</summary>
    [ObservableProperty]
    private double radius = 5.0;

    /// <summary>
    /// Initialises a new instance of <see cref="NearbyItemsViewModel"/> with the required services.
    /// </summary>
    /// <param name="itemService">Service used to fetch nearby items and categories.</param>
    /// <param name="locationService">Service used to obtain the device's current GPS coordinates.</param>
    /// <param name="navigationService">Service used to navigate to item details and the create-item page.</param>
    public NearbyItemsViewModel(
        IItemService itemService,
        ILocationService locationService,
        INavigationService navigationService
    )
        : base(itemService, navigationService)
    {
        _locationService = locationService;
        Title = "Nearby Items";
    }

    partial void OnRadiusChanged(double value) => TriggerReloadIfLoaded();

    /// <inheritdoc/>
    protected override Task ReloadAsync() => LoadNearbyItemsCommand.ExecuteAsync(null);

    /// <summary>
    /// Fetches all nearby items within <see cref="Radius"/> kilometres (using the cached device
    /// location, acquiring it once if not yet fetched), stores the full result set, and displays
    /// the first page. Also refreshes the category list.
    /// </summary>
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

            _allNearbyItems = await _itemService.GetNearbyItemsAsync(
                _cachedLat,
                _cachedLon,
                Radius,
                SelectedCategory,
                CurrentPage,
                PageSize
            );
            Items = new ObservableCollection<Item>(_allNearbyItems.Take(PageSize));
            HasMorePages = _allNearbyItems.Count > PageSize;
            await LoadCategoriesAsync();
        });

    /// <summary>
    /// Slices the next page from the cached <c>_allNearbyItems</c> result set and appends it to
    /// <see cref="ItemsSearchBaseViewModel.Items"/> without making a new network request.
    /// </summary>
    [RelayCommand]
    private Task LoadMoreItemsAsync() =>
        RunLoadMoreAsync(() =>
        {
            var next = _allNearbyItems.Skip(Items.Count).Take(PageSize).ToList();
            foreach (var item in next)
                Items.Add(item);
            HasMorePages = Items.Count < _allNearbyItems.Count;
            return Task.CompletedTask;
        });
}
