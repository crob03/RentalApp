using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

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

    public CreateItemViewModel()
    {
        Title = "List an Item";
    }

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

    [RelayCommand]
    private Task LoadCategoriesAsync() =>
        RunAsync(async () =>
        {
            Categories = await _itemService.GetCategoriesAsync();
        });

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
