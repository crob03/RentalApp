using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Items;
using RentalApp.Services.Navigation;

namespace RentalApp.ViewModels;

/// <summary>
/// Transient view model for the browseable items list. Extends <see cref="ItemsSearchBaseViewModel{T}"/>
/// with free-text search, server-side pagination, and category filtering.
/// </summary>
public partial class ItemsListViewModel : ItemsSearchBaseViewModel<ItemSummaryResponse>
{
    /// <summary>The current free-text search term; an empty string means no text filter.</summary>
    [ObservableProperty]
    private string searchText = string.Empty;

    /// <summary>
    /// Initialises the view model with item, navigation, and authentication dependencies.
    /// </summary>
    /// <param name="itemService">Used to fetch paginated items and categories.</param>
    /// <param name="navigationService">Passed to <see cref="ItemsSearchBaseViewModel{T}"/>.</param>
    /// <param name="tokenState">Passed to <see cref="ItemsSearchBaseViewModel{T}"/>.</param>
    /// <param name="credentialStore">Passed to <see cref="ItemsSearchBaseViewModel{T}"/>.</param>
    public ItemsListViewModel(
        IItemService itemService,
        INavigationService navigationService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore
    )
        : base(itemService, navigationService, tokenState, credentialStore)
    {
        Title = "Browse Items";
    }

    partial void OnSearchTextChanged(string value) => _ = TriggerReloadIfLoaded();

    protected override async Task ReloadAsync()
    {
        LoadItemsCommand.Cancel();
        await (LoadItemsCommand.ExecutionTask ?? Task.CompletedTask);
        await LoadItemsCommand.ExecuteAsync(null);
    }

    /// <summary>Loads the first page of items matching the current search and category filters.</summary>
    [RelayCommand]
    private Task LoadItemsAsync(CancellationToken ct) =>
        RunLoadAsync(async () =>
        {
            CurrentPage = 1;
            var response = await ItemService.GetItemsAsync(
                new GetItemsRequest(CurrentPage, PageSize, SelectedCategory, SearchText)
            );
            ct.ThrowIfCancellationRequested();
            Items = new ObservableCollection<ItemSummaryResponse>(response.Items);
            HasMorePages = CurrentPage < response.TotalPages;
            await LoadCategoriesAsync();
        });

    /// <summary>Appends the next page of items to <see cref="ItemsSearchBaseViewModel{T}.Items"/>.</summary>
    [RelayCommand]
    private Task LoadMoreItemsAsync() =>
        RunLoadMoreAsync(async () =>
        {
            var response = await ItemService.GetItemsAsync(
                new GetItemsRequest(CurrentPage, PageSize, SelectedCategory, SearchText)
            );
            foreach (var item in response.Items)
                Items.Add(item);
            HasMorePages = CurrentPage < response.TotalPages;
        });
}
