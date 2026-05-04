using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Reviews;

namespace RentalApp.ViewModels;

/// <summary>
/// Transient view model for the user-profile page. Supports two modes: self mode (no
/// <c>userId</c> query param — loads the authenticated user's own profile) and other-user
/// mode (<c>userId</c> supplied — loads a public profile without exposing the email address).
/// Extends <see cref="ReviewsViewModel"/> to provide paginated reviews for the displayed user.
/// </summary>
public partial class UserProfileViewModel : ReviewsViewModel, IQueryAttributable
{
    private readonly IAuthService _authService;
    private readonly IReviewService _reviewService;
    private int _userId;
    private int _resolvedUserId;

    /// <summary>Email address; non-null only in self mode.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmail))]
    private string? email;

    /// <summary>Full display name of the profile user.</summary>
    [ObservableProperty]
    private string displayName = string.Empty;

    /// <summary>Number of items the user has listed.</summary>
    [ObservableProperty]
    private int itemsListed;

    /// <summary>Number of rentals the user has completed as a borrower.</summary>
    [ObservableProperty]
    private int rentalsCompleted;

    /// <summary>True when <see cref="Email"/> is non-null (i.e. viewing own profile).</summary>
    public bool ShowEmail => Email != null;

    /// <summary>
    /// Initialises the view model with authentication, review, and navigation dependencies.
    /// </summary>
    /// <param name="authService">Used to fetch the current user or a public user profile.</param>
    /// <param name="reviewService">Used to page through the user's reviews.</param>
    /// <param name="tokenState">Passed to <see cref="ReviewsViewModel"/>.</param>
    /// <param name="credentialStore">Passed to <see cref="ReviewsViewModel"/>.</param>
    /// <param name="navigationService">Passed to <see cref="ReviewsViewModel"/>.</param>
    public UserProfileViewModel(
        IAuthService authService,
        IReviewService reviewService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService
    )
        : base(tokenState, credentialStore, navigationService)
    {
        _authService = authService;
        _reviewService = reviewService;
        Title = "Profile";
    }

    /// <summary>Receives the optional <c>userId</c> query parameter set during Shell navigation.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("userId", out var id))
            _userId = Convert.ToInt32(id);
    }

    /// <summary>
    /// Loads the profile for the current user (self mode) or a specific user (other-user mode),
    /// then fires <see cref="ReviewsViewModel.LoadReviewsCommand"/> to populate the review list.
    /// </summary>
    [RelayCommand]
    private Task LoadProfileAsync() =>
        RunAsync(async () =>
        {
            if (_userId <= 0)
            {
                var own = await _authService.GetCurrentUserAsync();
                _resolvedUserId = own.Id;
                DisplayName = $"{own.FirstName} {own.LastName}";
                Email = own.Email;
                ItemsListed = own.ItemsListed;
                RentalsCompleted = own.RentalsCompleted;
                AverageRating = own.AverageRating;
            }
            else
            {
                _resolvedUserId = _userId;
                var profile = await _authService.GetUserProfileAsync(_userId);
                DisplayName = $"{profile.FirstName} {profile.LastName}";
                Email = null;
                ItemsListed = profile.ItemsListed;
                RentalsCompleted = profile.RentalsCompleted;
                AverageRating = profile.AverageRating;
            }
            _ = LoadReviewsCommand.ExecuteAsync(null);
        });

    /// <inheritdoc/>
    protected override Task<ReviewsResponse> FetchReviewsAsync(int page) =>
        _reviewService.GetUserReviewsAsync(
            _resolvedUserId,
            new GetReviewsRequest(page, ReviewPageSize)
        );
}
