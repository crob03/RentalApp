using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the "List an Item" page. Captures item details from the user, resolves the
/// device's current location at submission time, and delegates creation to <see cref="IItemService"/>.
/// </summary>
public partial class CreateItemViewModel : BaseViewModel
{
    private readonly IItemService _itemService;
    private readonly ILocationService _locationService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string itemTitle = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string dailyRate = string.Empty;

    [ObservableProperty]
    private List<Category> categories = [];

    [ObservableProperty]
    private Category? selectedCategory;

    /// <summary>
    /// Initialises a new instance of <see cref="CreateItemViewModel"/> with the required services.
    /// </summary>
    /// <param name="itemService">Service used to fetch categories and submit the new listing.</param>
    /// <param name="locationService">Service used to capture the device's current location at submission time.</param>
    /// <param name="navigationService">Service used to navigate back after a successful creation.</param>
    public CreateItemViewModel(
        IItemService itemService,
        ILocationService locationService,
        INavigationService navigationService
    )
    {
        _itemService = itemService;
        _locationService = locationService;
        _navigationService = navigationService;
        Title = "List an Item";
    }

    /// <summary>
    /// Populates <see cref="Categories"/> from the item service.
    /// Intended to be called when the page first appears.
    /// </summary>
    [RelayCommand]
    private Task LoadCategoriesAsync() =>
        RunAsync(async () =>
        {
            Categories = await _itemService.GetCategoriesAsync();
        });

    /// <summary>
    /// Validates the form, resolves the device location, submits the new listing, and navigates
    /// back on success. Surfaces validation errors via <see cref="BaseViewModel.SetError"/>.
    /// </summary>
    [RelayCommand]
    private async Task CreateItemAsync()
    {
        ClearError();

        if (SelectedCategory == null)
        {
            SetError("Please select a category.");
            return;
        }

        if (!double.TryParse(DailyRate, out var rate))
        {
            SetError("Please enter a valid daily rate.");
            return;
        }

        await RunAsync(async () =>
        {
            var (lat, lon) = await _locationService.GetCurrentLocationAsync();

            await _itemService.CreateItemAsync(
                ItemTitle,
                Description.Length > 0 ? Description : null,
                rate,
                SelectedCategory.Id,
                lat,
                lon
            );

            await _navigationService.NavigateBackAsync();
        });
    }
}
