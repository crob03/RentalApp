using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Helpers;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Reviews;

namespace RentalApp.ViewModels;

/// <summary>
/// Transient view model for the create-review page. Receives <c>rentalId</c> via query attributes
/// and submits a review via <see cref="IReviewService"/>.
/// </summary>
public partial class CreateReviewViewModel : AuthenticatedViewModel, IQueryAttributable
{
    private readonly IReviewService _reviewService;
    private int _rentalId;

    /// <summary>Star rating selected by the user (1–5). Defaults to 1.</summary>
    [ObservableProperty]
    private int rating = 1;

    /// <summary>Optional free-text comment, max 500 characters.</summary>
    [ObservableProperty]
    private string comment = string.Empty;

    /// <summary>
    /// Initialises the view model with review, navigation, and authentication dependencies.
    /// </summary>
    public CreateReviewViewModel(
        IReviewService reviewService,
        INavigationService navigationService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore
    )
        : base(tokenState, credentialStore, navigationService)
    {
        _reviewService = reviewService;
        Title = "Leave a Review";
    }

    /// <summary>Receives the <c>rentalId</c> query parameter set during Shell navigation.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("rentalId", out var id))
            _rentalId = Convert.ToInt32(id);
    }

    /// <summary>
    /// Validates the form and submits a review via <see cref="IReviewService.CreateReviewAsync"/>.
    /// Navigates back on success; surfaces errors via <see cref="BaseViewModel.SetError"/>.
    /// </summary>
    [RelayCommand]
    private async Task SubmitReviewAsync()
    {
        var error = ReviewValidator.Validate(Rating, Comment);
        if (error is not null)
        {
            SetError(error);
            return;
        }

        await RunAsync(async () =>
        {
            await _reviewService.CreateReviewAsync(
                new CreateReviewRequest(
                    _rentalId,
                    Rating,
                    string.IsNullOrWhiteSpace(Comment) ? null : Comment
                )
            );
            await NavigateBackAsync();
        });
    }
}
