using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the item details page. Receives the target item ID via Shell query parameters
/// and supports inline editing for the item owner.
/// </summary>
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

    /// <summary>
    /// Initialises a new instance of <see cref="ItemDetailsViewModel"/> for design-time support.
    /// </summary>
    public ItemDetailsViewModel()
    {
        Title = "Item Details";
    }

    /// <summary>
    /// Initialises a new instance of <see cref="ItemDetailsViewModel"/> with the required services.
    /// </summary>
    /// <param name="itemService">Service used to fetch and update the item.</param>
    /// <param name="authService">Authentication service used to determine whether the current user owns the item.</param>
    /// <param name="navigationService">Navigation service (reserved for future back-navigation use).</param>
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

    /// <summary>
    /// Receives Shell navigation query parameters. Extracts <c>itemId</c> so
    /// <see cref="LoadItemAsync"/> can fetch the correct item on page load.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("itemId", out var id))
            _itemId = Convert.ToInt32(id);
    }

    /// <summary>
    /// Fetches the item identified by <c>_itemId</c> and sets <see cref="IsOwner"/> based on
    /// whether the authenticated user's ID matches the item's owner.
    /// </summary>
    [RelayCommand]
    private Task LoadItemAsync() =>
        RunAsync(async () =>
        {
            CurrentItem = await _itemService.GetItemAsync(_itemId);
            IsOwner = CurrentItem?.OwnerId == _authService.CurrentUser?.Id;
        });

    /// <summary>Enters edit mode, pre-populating the edit fields from <see cref="CurrentItem"/>. Toggling again exits edit mode without saving.</summary>
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

    /// <summary>
    /// Validates the edited daily rate, persists the changes via <see cref="IItemService.UpdateItemAsync"/>,
    /// and exits edit mode on success.
    /// </summary>
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

    /// <summary>
    /// Exits edit mode and clears any error message without persisting changes.
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ClearError();
    }
}
