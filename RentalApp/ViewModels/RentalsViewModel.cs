using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Rentals;

namespace RentalApp.ViewModels;

/// <summary>
/// Transient view model for the rentals page. Lets the authenticated user toggle between
/// incoming (owner) and outgoing (borrower) rentals and filter by status.
/// </summary>
public partial class RentalsViewModel : AuthenticatedViewModel
{
    private readonly IRentalService _rentalService;
    private bool _hasLoaded;
    private bool _suppressReload;
    private bool _rebuildStatuses = true;

    /// <summary>Sentinel value used in <see cref="FilterStatuses"/> to represent "no filter".</summary>
    public const string AllStatuses = "All";

    /// <summary>
    /// When <see langword="true"/>, the list shows incoming rentals (user is owner).
    /// When <see langword="false"/>, it shows outgoing rentals (user is borrower).
    /// </summary>
    [ObservableProperty]
    private bool isIncoming = true;

    /// <summary>The currently loaded rentals.</summary>
    [ObservableProperty]
    private ObservableCollection<RentalSummaryResponse> rentals = [];

    /// <summary>
    /// Status values derived from the current result set, prepended with <see cref="AllStatuses"/>.
    /// Rebuilt after each load.
    /// </summary>
    [ObservableProperty]
    private List<string> filterStatuses = [AllStatuses];

    /// <summary>The Picker's bound selection. Defaults to <see cref="AllStatuses"/>.</summary>
    [ObservableProperty]
    private string selectedStatusItem = AllStatuses;

    /// <summary>
    /// The active status filter passed to the service. <see langword="null"/> means all statuses.
    /// Derived from <see cref="SelectedStatusItem"/>.
    /// </summary>
    [ObservableProperty]
    private string? selectedStatus;

    /// <summary>
    /// Initialises the view model with rental, navigation, and authentication dependencies.
    /// </summary>
    public RentalsViewModel(
        IRentalService rentalService,
        INavigationService navigationService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore
    )
        : base(tokenState, credentialStore, navigationService)
    {
        _rentalService = rentalService;
        Title = "My Rentals";
    }

    partial void OnIsIncomingChanged(bool value)
    {
        _rebuildStatuses = true;
        _ = TriggerReloadIfLoadedAsync();
    }

    partial void OnSelectedStatusItemChanged(string value)
    {
        if (_suppressReload)
            return;
        SelectedStatus = value == AllStatuses ? null : value;
    }

    partial void OnSelectedStatusChanged(string? value)
    {
        if (_suppressReload)
            return;
        _rebuildStatuses = false;
        _ = TriggerReloadIfLoadedAsync();
    }

    /// <summary>Sets the view to show incoming rentals (user is the item owner).</summary>
    [RelayCommand]
    private void ShowIncoming() => IsIncoming = true;

    /// <summary>Sets the view to show outgoing rentals (user is the borrower).</summary>
    [RelayCommand]
    private void ShowOutgoing() => IsIncoming = false;

    private async Task TriggerReloadIfLoadedAsync()
    {
        if (!_hasLoaded)
            return;
        LoadRentalsCommand.Cancel();
        await (LoadRentalsCommand.ExecutionTask ?? Task.CompletedTask);
        await LoadRentalsCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Fetches the current user's rentals based on <see cref="IsIncoming"/> and
    /// <see cref="SelectedStatus"/>. Rebuilds <see cref="FilterStatuses"/> from the results.
    /// Called from <c>RentalsPage.OnAppearing</c> and whenever direction or status changes.
    /// </summary>
    [RelayCommand]
    private Task LoadRentalsAsync(CancellationToken ct) =>
        RunAsync(async () =>
        {
            var request = new GetRentalsRequest(SelectedStatus);
            var response = IsIncoming
                ? await _rentalService.GetIncomingRentalsAsync(request)
                : await _rentalService.GetOutgoingRentalsAsync(request);
            ct.ThrowIfCancellationRequested();
            Rentals = new ObservableCollection<RentalSummaryResponse>(response.Rentals);
            if (_rebuildStatuses)
                RebuildFilterStatuses(response.Rentals);
            _rebuildStatuses = true;
            _hasLoaded = true;
        });

    /// <summary>
    /// Rebuilds <see cref="FilterStatuses"/> from the distinct status values in
    /// <paramref name="rentals"/>. Only called on initial load, direction change, or
    /// pull-to-refresh — never on status-filter-triggered reloads, so the picker options
    /// do not shrink to match the filtered result set. If the current
    /// <see cref="SelectedStatusItem"/> is no longer present in the new list, it is reset
    /// to <see cref="AllStatuses"/> without triggering a further reload.
    /// </summary>
    private void RebuildFilterStatuses(List<RentalSummaryResponse> rentals)
    {
        var statuses = new List<string> { AllStatuses };
        statuses.AddRange(rentals.Select(r => r.Status).Distinct().OrderBy(s => s));
        FilterStatuses = statuses;

        if (!FilterStatuses.Contains(SelectedStatusItem))
        {
            _suppressReload = true;
            SelectedStatusItem = AllStatuses;
            SelectedStatus = null;
            _suppressReload = false;
        }
    }
}
