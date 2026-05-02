using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Helpers;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class ItemDetailsViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiService _api;
    private readonly IAuthenticationService _authService;
    private int _itemId;

    [ObservableProperty]
    private ItemDetailResponse? currentItem;

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

    public ItemDetailsViewModel(IApiService api, IAuthenticationService authService)
    {
        _api = api;
        _authService = authService;
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
            CurrentItem = await _api.GetItemAsync(_itemId);
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

        var error = ItemValidator.ValidateUpdate(EditTitle, EditDescription, EditDailyRate);
        if (error is not null)
        {
            SetError(error);
            return;
        }

        double.TryParse(EditDailyRate, out var parsed); // guaranteed to succeed after ValidateUpdate
        double? rate = parsed;

        await RunAsync(async () =>
        {
            await _api.UpdateItemAsync(
                CurrentItem.Id,
                new UpdateItemRequest(EditTitle, EditDescription, rate, EditIsAvailable)
            );
            CurrentItem = await _api.GetItemAsync(CurrentItem.Id);
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
