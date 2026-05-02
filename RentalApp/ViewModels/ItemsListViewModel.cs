using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class ItemsListViewModel : ItemsSearchBaseViewModel<ItemSummaryResponse>
{
    [ObservableProperty]
    private string searchText = string.Empty;

    public ItemsListViewModel(IItemService itemService, INavigationService navigationService)
        : base(itemService, navigationService)
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
