using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the "Browse Items" page.
/// Supports free-text search, category filtering, and server-side pagination via
/// <see cref="ItemsSearchBaseViewModel"/>.
/// </summary>
public partial class ItemsListViewModel : ItemsSearchBaseViewModel
{
    /// <summary>The current free-text search query; changes trigger a full reload.</summary>
    [ObservableProperty]
    private string searchText = string.Empty;

    /// <summary>
    /// Initialises a new instance of <see cref="ItemsListViewModel"/> with the required services.
    /// </summary>
    /// <param name="itemService">Service used to fetch items and categories.</param>
    /// <param name="navigationService">Service used to navigate to item details and the create-item page.</param>
    public ItemsListViewModel(IItemService itemService, INavigationService navigationService)
        : base(itemService, navigationService)
    {
        Title = "Browse Items";
    }

    partial void OnSearchTextChanged(string value) => _ = TriggerReloadIfLoaded();

    /// <inheritdoc/>
    protected override async Task ReloadAsync()
    {
        LoadItemsCommand.Cancel();
        await (LoadItemsCommand.ExecutionTask ?? Task.CompletedTask);
        await LoadItemsCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Resets to page 1, fetches a page of items matching the current search and category filter,
    /// and refreshes the category list.
    /// </summary>
    [RelayCommand]
    private Task LoadItemsAsync(CancellationToken ct) =>
        RunLoadAsync(async () =>
        {
            CurrentPage = 1;
            var results = await ItemService.GetItemsAsync(
                SelectedCategory,
                SearchText,
                CurrentPage,
                PageSize
            );
            ct.ThrowIfCancellationRequested();
            Items = new ObservableCollection<Item>(results);
            HasMorePages = results.Count == PageSize;
            await LoadCategoriesAsync();
        });

    /// <summary>
    /// Appends the next page of items to <see cref="ItemsSearchBaseViewModel.Items"/>.
    /// </summary>
    [RelayCommand]
    private Task LoadMoreItemsAsync() =>
        RunLoadMoreAsync(async () =>
        {
            var more = await ItemService.GetItemsAsync(
                SelectedCategory,
                SearchText,
                CurrentPage,
                PageSize
            );
            foreach (var item in more)
                Items.Add(item);
            HasMorePages = more.Count == PageSize;
        });
}
