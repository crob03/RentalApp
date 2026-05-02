using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Helpers;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class ItemDetailsViewModel : AuthenticatedViewModel, IQueryAttributable
{
    private readonly IItemService _itemService;
    private readonly IAuthService _authService;
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

    public ItemDetailsViewModel(
        IItemService itemService,
        IAuthService authService,
        INavigationService navigationService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore
    )
        : base(tokenState, credentialStore, navigationService)
    {
        _itemService = itemService;
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
            CurrentItem = await _itemService.GetItemAsync(_itemId);
            if (CurrentItem != null)
            {
                try
                {
                    var currentUser = await _authService.GetCurrentUserAsync();
                    IsOwner = CurrentItem.OwnerId == currentUser.Id;
                }
                catch
                {
                    IsOwner = false;
                }
            }
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

        double? rate = EditDailyRate is not null ? double.Parse(EditDailyRate) : null;

        await RunAsync(async () =>
        {
            await _itemService.UpdateItemAsync(
                CurrentItem.Id,
                new UpdateItemRequest(EditTitle, EditDescription, rate, EditIsAvailable)
            );
            CurrentItem = await _itemService.GetItemAsync(CurrentItem.Id);
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
