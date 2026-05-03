using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Helpers;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// Transient view model for the item-details page. Loads a single item by ID, determines ownership
/// by comparing against the current user, and manages an in-place edit flow.
/// </summary>
public partial class ItemDetailsViewModel : AuthenticatedViewModel, IQueryAttributable
{
    private readonly IItemService _itemService;
    private readonly IAuthService _authService;
    private int _itemId;

    /// <summary>The currently displayed item; <see langword="null"/> while loading.</summary>
    [ObservableProperty]
    private ItemDetailResponse? currentItem;

    /// <summary>Indicates whether the authenticated user is the owner of <see cref="CurrentItem"/>.</summary>
    [ObservableProperty]
    private bool isOwner;

    /// <summary>Indicates whether the inline edit form is currently shown.</summary>
    [ObservableProperty]
    private bool isEditing;

    /// <summary>Editable copy of the item title, pre-populated when entering edit mode.</summary>
    [ObservableProperty]
    private string editTitle = string.Empty;

    /// <summary>Editable copy of the item description, pre-populated when entering edit mode.</summary>
    [ObservableProperty]
    private string editDescription = string.Empty;

    /// <summary>Editable daily rate as a string; parsed to <see cref="double"/> on save.</summary>
    [ObservableProperty]
    private string editDailyRate = string.Empty;

    /// <summary>Editable availability flag, pre-populated when entering edit mode.</summary>
    [ObservableProperty]
    private bool editIsAvailable;

    /// <summary>
    /// Initialises the view model with item, authentication, and navigation dependencies.
    /// </summary>
    /// <param name="itemService">Used to fetch and update the item.</param>
    /// <param name="authService">Used to fetch the current user for ownership comparison.</param>
    /// <param name="navigationService">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    /// <param name="tokenState">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    /// <param name="credentialStore">Passed to <see cref="AuthenticatedViewModel"/>.</param>
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

    /// <summary>Receives the <c>itemId</c> query parameter set during Shell navigation.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("itemId", out var id))
            _itemId = Convert.ToInt32(id);
    }

    /// <summary>Fetches the item and determines whether the current user is the owner.</summary>
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

    /// <summary>
    /// Toggles the inline edit form. Pre-populates edit fields from <see cref="CurrentItem"/> when entering edit mode.
    /// </summary>
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

    /// <summary>Validates the edit form and persists changes via <see cref="IItemService.UpdateItemAsync"/>.</summary>
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

    /// <summary>Exits edit mode and clears any validation errors.</summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ClearError();
    }
}
