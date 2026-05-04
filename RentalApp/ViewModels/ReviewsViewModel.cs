using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;

namespace RentalApp.ViewModels;

/// <summary>
/// Abstract base for view models that display a paginated list of reviews.
/// Subclasses implement <see cref="FetchReviewsAsync"/> to supply the data source.
/// </summary>
public abstract partial class ReviewsViewModel : AuthenticatedViewModel
{
    /// <summary>Default page size for all review-listing requests.</summary>
    protected const int ReviewPageSize = 10;

    /// <summary>The currently loaded page of reviews.</summary>
    [ObservableProperty]
    private ObservableCollection<ReviewResponse> reviews = [];

    /// <summary>Mean rating across all reviews, or <see langword="null"/> if there are none.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAverageRating))]
    private double? averageRating;

    /// <summary>Total number of reviews across all pages.</summary>
    [ObservableProperty]
    private int totalReviews;

    /// <summary>Indicates whether the initial reviews load is in progress.</summary>
    [ObservableProperty]
    private bool isLoadingReviews;

    /// <summary>Indicates whether a "load more" reviews fetch is in progress.</summary>
    [ObservableProperty]
    private bool isLoadingMoreReviews;

    /// <summary>The 1-based page number of the last successfully fetched reviews page.</summary>
    [ObservableProperty]
    private int currentReviewPage = 1;

    /// <summary>Indicates whether additional pages of reviews are available.</summary>
    [ObservableProperty]
    private bool hasMoreReviewPages;

    /// <summary>True when <see cref="AverageRating"/> has a value.</summary>
    public bool HasAverageRating => AverageRating.HasValue;

    /// <summary>
    /// Initialises the base with the required authentication and navigation dependencies.
    /// </summary>
    protected ReviewsViewModel(
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService
    )
        : base(tokenState, credentialStore, navigationService) { }

    /// <summary>
    /// Fetches a page of reviews from the data source. Implemented by subclasses to provide
    /// item reviews, user reviews, or any other review subject.
    /// </summary>
    protected abstract Task<ReviewsResponse> FetchReviewsAsync(int page);

    /// <summary>Resets to page 1 and loads the first page of reviews.</summary>
    [RelayCommand]
    private Task LoadReviewsAsync() =>
        RunLoadReviewsAsync(async () =>
        {
            CurrentReviewPage = 1;
            var response = await FetchReviewsAsync(1);
            Reviews = new ObservableCollection<ReviewResponse>(response.Reviews);
            AverageRating = response.AverageRating;
            TotalReviews = response.TotalReviews;
            HasMoreReviewPages = response.Page < response.TotalPages;
        });

    /// <summary>Appends the next page of reviews to <see cref="Reviews"/>.</summary>
    [RelayCommand]
    private Task LoadMoreReviewsAsync() =>
        RunLoadMoreReviewsAsync(async () =>
        {
            var response = await FetchReviewsAsync(CurrentReviewPage);
            foreach (var review in response.Reviews)
                Reviews.Add(review);
            AverageRating = response.AverageRating;
            TotalReviews = response.TotalReviews;
            HasMoreReviewPages = response.Page < response.TotalPages;
        });

    /// <summary>
    /// Executes an initial reviews load with <see cref="IsLoadingReviews"/> lifecycle management.
    /// Surfaces exceptions via <see cref="BaseViewModel.SetError"/>.
    /// </summary>
    protected async Task RunLoadReviewsAsync(Func<Task> operation)
    {
        try
        {
            IsLoadingReviews = true;
            ClearError();
            await operation();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsLoadingReviews = false;
        }
    }

    /// <summary>
    /// Appends the next page of reviews with <see cref="IsLoadingMoreReviews"/> lifecycle management.
    /// Does nothing if <see cref="HasMoreReviewPages"/> is <see langword="false"/>.
    /// Rolls back <see cref="CurrentReviewPage"/> on failure.
    /// </summary>
    protected async Task RunLoadMoreReviewsAsync(Func<Task> operation)
    {
        if (!HasMoreReviewPages)
            return;

        try
        {
            IsLoadingMoreReviews = true;
            ClearError();
            CurrentReviewPage++;
            await operation();
        }
        catch (Exception ex)
        {
            CurrentReviewPage--;
            SetError(ex.Message);
        }
        finally
        {
            IsLoadingMoreReviews = false;
        }
    }
}
