using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Items;
using RentalApp.Services.Location;
using RentalApp.Services.Navigation;

namespace RentalApp.ViewModels;

/// <summary>
/// Transient view model for the nearby-items page. Extends <see cref="ItemsSearchBaseViewModel{T}"/>
/// with radius-based filtering and client-side pagination. The nearby-items endpoint returns all
/// matching items in a single call; results are cached in <c>_allNearbyItems</c> and sliced client-side.
/// </summary>
public partial class NearbyItemsViewModel : ItemsSearchBaseViewModel<NearbyItemResponse>
{
    private readonly ILocationService _locationService;

    private double _cachedLat;
    private double _cachedLon;
    private bool _locationFetched;
    private List<NearbyItemResponse> _allNearbyItems = [];

    /// <summary>Search radius in kilometres; changing the value triggers a reload.</summary>
    [ObservableProperty]
    private double radius = 5.0;

    /// <summary>
    /// Initialises the view model with item, location, navigation, and authentication dependencies.
    /// </summary>
    /// <param name="itemService">Used to fetch nearby items and categories.</param>
    /// <param name="locationService">Used to obtain the device's current GPS coordinates on first load.</param>
    /// <param name="navigationService">Passed to <see cref="ItemsSearchBaseViewModel{T}"/>.</param>
    /// <param name="tokenState">Passed to <see cref="ItemsSearchBaseViewModel{T}"/>.</param>
    /// <param name="credentialStore">Passed to <see cref="ItemsSearchBaseViewModel{T}"/>.</param>
    public NearbyItemsViewModel(
        IItemService itemService,
        ILocationService locationService,
        INavigationService navigationService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore
    )
        : base(itemService, navigationService, tokenState, credentialStore)
    {
        _locationService = locationService;
        Title = "Nearby Items";
    }

    partial void OnRadiusChanged(double value) => _ = TriggerReloadIfLoaded();

    protected override async Task ReloadAsync()
    {
        LoadNearbyItemsCommand.Cancel();
        await (LoadNearbyItemsCommand.ExecutionTask ?? Task.CompletedTask);
        await LoadNearbyItemsCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Fetches all nearby items from the API, caches the full result, and shows the first page.
    /// Device location is resolved once and cached for subsequent filter/radius changes.
    /// </summary>
    [RelayCommand]
    private Task LoadNearbyItemsAsync(CancellationToken ct) =>
        RunLoadAsync(async () =>
        {
            CurrentPage = 1;

            if (!_locationFetched)
            {
                var (lat, lon) = await _locationService.GetCurrentLocationAsync();
                ct.ThrowIfCancellationRequested();
                _cachedLat = lat;
                _cachedLon = lon;
                _locationFetched = true;
            }

            var response = await ItemService.GetNearbyItemsAsync(
                new GetNearbyItemsRequest(_cachedLat, _cachedLon, Radius, SelectedCategory)
            );
            ct.ThrowIfCancellationRequested();
            _allNearbyItems = response.Items;
            Items = new ObservableCollection<NearbyItemResponse>(_allNearbyItems.Take(PageSize));
            HasMorePages = _allNearbyItems.Count > PageSize;
            await LoadCategoriesAsync();
        });

    /// <summary>Slices the next page from the cached result set and appends it to <see cref="ItemsSearchBaseViewModel{T}.Items"/>.</summary>
    [RelayCommand]
    private Task LoadMoreItemsAsync() =>
        RunLoadMoreAsync(() =>
        {
            var next = _allNearbyItems.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            foreach (var item in next)
                Items.Add(item);
            HasMorePages = Items.Count < _allNearbyItems.Count;
            return Task.CompletedTask;
        });
}
