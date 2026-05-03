using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Helpers;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// Transient view model for the create-item page. Collects listing details, resolves the device
/// location via <see cref="ILocationService"/>, and submits a new item via <see cref="IItemService"/>.
/// </summary>
public partial class CreateItemViewModel : AuthenticatedViewModel
{
    private readonly IItemService _itemService;
    private readonly ILocationService _locationService;

    /// <summary>The title for the new listing.</summary>
    [ObservableProperty]
    private string itemTitle = string.Empty;

    /// <summary>Optional description for the new listing.</summary>
    [ObservableProperty]
    private string description = string.Empty;

    /// <summary>Daily rental rate as a string; parsed to <see cref="double"/> before submission.</summary>
    [ObservableProperty]
    private string dailyRate = string.Empty;

    /// <summary>All available categories, populated by <see cref="LoadCategoriesCommand"/>.</summary>
    [ObservableProperty]
    private List<CategoryResponse> categories = [];

    /// <summary>The category selected by the user; required for submission.</summary>
    [ObservableProperty]
    private CategoryResponse? selectedCategory;

    /// <summary>
    /// Initialises the view model with item, location, navigation, and authentication dependencies.
    /// </summary>
    /// <param name="itemService">Used to fetch categories and submit the new item.</param>
    /// <param name="locationService">Used to resolve the device's current GPS coordinates on submission.</param>
    /// <param name="navigationService">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    /// <param name="tokenState">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    /// <param name="credentialStore">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    public CreateItemViewModel(
        IItemService itemService,
        ILocationService locationService,
        INavigationService navigationService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore
    )
        : base(tokenState, credentialStore, navigationService)
    {
        _itemService = itemService;
        _locationService = locationService;
        Title = "List an Item";
    }

    /// <summary>Fetches and caches the available categories for the picker.</summary>
    [RelayCommand]
    private Task LoadCategoriesAsync() =>
        RunAsync(async () =>
        {
            var response = await _itemService.GetCategoriesAsync();
            Categories = response.Categories;
        });

    /// <summary>Validates the form, resolves device location, and submits the new item listing.</summary>
    [RelayCommand]
    private async Task CreateItemAsync()
    {
        if (!ValidateForm())
            return;

        var rate = double.Parse(DailyRate);

        await RunAsync(async () =>
        {
            var (lat, lon) = await _locationService.GetCurrentLocationAsync();
            await _itemService.CreateItemAsync(
                new CreateItemRequest(
                    ItemTitle,
                    Description.Length > 0 ? Description : null,
                    rate,
                    SelectedCategory!.Id,
                    lat,
                    lon
                )
            );
            await NavigateBackAsync();
        });
    }

    private bool ValidateForm()
    {
        var error = ItemValidator.ValidateCreate(
            ItemTitle,
            Description.Length > 0 ? Description : null,
            DailyRate,
            SelectedCategory?.Id ?? 0
        );
        if (error is not null)
        {
            SetError(error);
            return false;
        }
        return true;
    }
}
