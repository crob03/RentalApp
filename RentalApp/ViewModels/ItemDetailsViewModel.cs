using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class ItemDetailsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IItemService _itemService;
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;
    private int _itemId;

    [ObservableProperty]
    private Item? currentItem;

    [ObservableProperty]
    private bool isOwner;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private string editTitle = string.Empty;

    [ObservableProperty]
    private string editDescription = string.Empty;

    [ObservableProperty]
    private string editDailyRate = string.Empty;

    [ObservableProperty]
    private bool editIsAvailable;

    public ItemDetailsViewModel()
    {
        Title = "Item Details";
    }

    public ItemDetailsViewModel(
        IItemService itemService,
        IAuthenticationService authService,
        INavigationService navigationService
    )
    {
        _itemService = itemService;
        _authService = authService;
        _navigationService = navigationService;
        Title = "Item Details";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("itemId", out var id))
            _itemId = Convert.ToInt32(id);
    }

    [RelayCommand]
    private Task LoadItemAsync() =>
        RunAsync(async () =>
        {
            CurrentItem = await _itemService.GetItemAsync(_itemId);
            IsOwner = CurrentItem?.OwnerId == _authService.CurrentUser?.Id;
        });

    [RelayCommand]
    private void ToggleEdit()
    {
        if (!IsEditing && CurrentItem != null)
        {
            EditTitle = CurrentItem.Title;
            EditDescription = CurrentItem.Description ?? string.Empty;
            EditDailyRate = CurrentItem.DailyRate.ToString();
            EditIsAvailable = CurrentItem.IsAvailable;
        }
        IsEditing = !IsEditing;
    }

    [RelayCommand]
    private async Task SaveChangesAsync()
    {
        if (CurrentItem == null)
            return;

        if (!double.TryParse(EditDailyRate, out var rate))
        {
            SetError("Please enter a valid daily rate.");
            return;
        }

        await RunAsync(async () =>
        {
            CurrentItem = await _itemService.UpdateItemAsync(
                CurrentItem.Id,
                EditTitle,
                EditDescription,
                rate,
                EditIsAvailable
            );
            IsEditing = false;
        });
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ClearError();
    }
}
