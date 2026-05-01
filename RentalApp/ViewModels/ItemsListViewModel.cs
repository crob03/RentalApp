using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class ItemsListViewModel : ItemsSearchBaseViewModel
{
    private readonly IItemService _itemService;

    [ObservableProperty]
    private string searchText = string.Empty;

    public ItemsListViewModel(IItemService itemService, INavigationService navigationService)
        : base(navigationService)
    {
        _itemService = itemService;
        Title = "Browse Items";
    }

    partial void OnSearchTextChanged(string value) => TriggerReloadIfLoaded();

    protected override Task ReloadAsync() => LoadItemsCommand.ExecuteAsync(null);

    [RelayCommand]
    private Task LoadItemsAsync() =>
        RunLoadAsync(async () =>
        {
            CurrentPage = 1;
            var results = await _itemService.GetItemsAsync(
                SelectedCategory,
                SearchText,
                CurrentPage,
                PageSize
            );
            var cats = await _itemService.GetCategoriesAsync();

            Items = new ObservableCollection<Item>(results);
            Categories = cats;
            HasMorePages = results.Count == PageSize;

            var all = new List<Category> { AllItemsCategory };
            all.AddRange(cats);
            FilterCategories = all;
            RestoreCategory(all);
        });

    [RelayCommand]
    private Task LoadMoreItemsAsync() =>
        RunLoadMoreAsync(async () =>
        {
            var more = await _itemService.GetItemsAsync(
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
