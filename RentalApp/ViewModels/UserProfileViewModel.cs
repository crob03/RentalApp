using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private Task LoadProfileAsync() => Task.CompletedTask;

    protected override Task<ReviewsResponse> FetchReviewsAsync(int page) =>
        Task.FromResult(new ReviewsResponse([], null, 0, page, ReviewPageSize, 0));
}
