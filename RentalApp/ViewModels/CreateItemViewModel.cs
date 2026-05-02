using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Helpers;
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
    private List<CategoryResponse> categories = [];

    [ObservableProperty]
    private CategoryResponse? selectedCategory;

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
            var response = await _itemService.GetCategoriesAsync();
            Categories = response.Categories;
        });

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
            await _navigationService.NavigateBackAsync();
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
