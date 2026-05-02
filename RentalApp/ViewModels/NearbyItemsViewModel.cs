using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class NearbyItemsViewModel : ItemsSearchBaseViewModel<NearbyItemResponse>
{
    private readonly ILocationService _locationService;

    private double _cachedLat;
    private double _cachedLon;
    private bool _locationFetched;
    private List<NearbyItemResponse> _allNearbyItems = [];

    [ObservableProperty]
    private double radius = 5.0;

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
