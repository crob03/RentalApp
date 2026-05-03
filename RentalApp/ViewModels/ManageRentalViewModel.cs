using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Database.States;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Rentals;

namespace RentalApp.ViewModels;

/// <summary>
/// Transient view model for the manage-rental page. Loads a single rental by ID,
/// determines the current user's role, and exposes the state-machine-driven
/// transition buttons available to that role.
/// </summary>
public partial class ManageRentalViewModel : AuthenticatedViewModel, IQueryAttributable
{
    private readonly IRentalService _rentalService;
    private readonly IAuthService _authService;
    private int _rentalId;
    private CurrentUserResponse? _currentUser;

    /// <summary>The currently loaded rental; <see langword="null"/> while loading.</summary>
    [ObservableProperty]
    private RentalDetailResponse? currentRental;

    /// <summary>True when the owner can approve this rental.</summary>
    [ObservableProperty]
    private bool canApprove;

    /// <summary>True when the owner can reject this rental.</summary>
    [ObservableProperty]
    private bool canReject;

    /// <summary>True when the owner can mark this rental as out for rent.</summary>
    [ObservableProperty]
    private bool canMarkOutForRent;

    /// <summary>True when the borrower can mark this rental as returned.</summary>
    [ObservableProperty]
    private bool canMarkReturned;

    /// <summary>True when the owner can mark this rental as completed.</summary>
    [ObservableProperty]
    private bool canComplete;

    /// <summary>
    /// Initialises the view model with rental, authentication, and navigation dependencies.
    /// </summary>
    public ManageRentalViewModel(
        IRentalService rentalService,
        IAuthService authService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService
    )
        : base(tokenState, credentialStore, navigationService)
    {
        _rentalService = rentalService;
        _authService = authService;
        Title = "Manage Rental";
    }

    /// <summary>Receives the <c>rentalId</c> query parameter set during Shell navigation.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("rentalId", out var id))
            _rentalId = Convert.ToInt32(id);
    }

    /// <summary>Fetches the rental and refreshes available actions based on role and status.</summary>
    [RelayCommand]
    private Task LoadRentalAsync() =>
        RunAsync(async () =>
        {
            CurrentRental = await _rentalService.GetRentalAsync(_rentalId);
            try
            {
                _currentUser = await _authService.GetCurrentUserAsync();
            }
            catch
            {
                // Current user unavailable — no action buttons will be shown.
            }
            RefreshAvailableActions();
        });

    /// <summary>
    /// Transitions the rental to <paramref name="targetStatus"/> then reloads
    /// the rental in-place to reflect the new state.
    /// </summary>
    [RelayCommand]
    private Task UpdateStatusAsync(string targetStatus) =>
        RunAsync(async () =>
        {
            await _rentalService.UpdateRentalStatusAsync(
                _rentalId,
                new UpdateRentalStatusRequest(targetStatus)
            );
            CurrentRental = await _rentalService.GetRentalAsync(_rentalId);
            RefreshAvailableActions();
        });

    private void RefreshAvailableActions()
    {
        if (
            CurrentRental is null
            || _currentUser is null
            || !Enum.TryParse<RentalStatus>(CurrentRental.Status, ignoreCase: true, out var status)
        )
        {
            CanApprove = CanReject = CanMarkOutForRent = CanMarkReturned = CanComplete = false;
            return;
        }

        var isOwner = _currentUser.Id == CurrentRental.OwnerId;
        var state = RentalStateFactory.From(status);
        var transitions = isOwner ? state.OwnerTransitions : state.BorrowerTransitions;

        CanApprove = transitions.Contains(RentalStatus.Approved);
        CanReject = transitions.Contains(RentalStatus.Rejected);
        CanMarkOutForRent = transitions.Contains(RentalStatus.OutForRent);
        CanMarkReturned = transitions.Contains(RentalStatus.Returned);
        CanComplete = transitions.Contains(RentalStatus.Completed);
    }
}
