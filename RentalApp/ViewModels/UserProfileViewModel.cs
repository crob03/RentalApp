using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Reviews;

namespace RentalApp.ViewModels;

public partial class UserProfileViewModel : ReviewsViewModel, IQueryAttributable
{
    private readonly IAuthService _authService;
    private readonly IReviewService _reviewService;
    private int _userId;
    private int _resolvedUserId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmail))]
    private string? email;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private int itemsListed;

    [ObservableProperty]
    private int rentalsCompleted;

    public bool ShowEmail => Email != null;

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

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("userId", out var id))
            _userId = Convert.ToInt32(id);
    }

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

    protected override Task<ReviewsResponse> FetchReviewsAsync(int page) =>
        _reviewService.GetUserReviewsAsync(
            _resolvedUserId,
            new GetReviewsRequest(page, ReviewPageSize)
        );
}
