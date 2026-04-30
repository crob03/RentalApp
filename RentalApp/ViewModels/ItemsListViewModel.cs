using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class ItemsListViewModel : BaseViewModel
{
    private readonly IItemService _itemService;
    private readonly INavigationService _navigationService;
    private const int PageSize = 20;
    private static readonly Category AllItemsCategory = new(0, "All Items", string.Empty, 0);
    private bool _restoringCategory;
    private bool _hasLoaded;

    [ObservableProperty]
    private ObservableCollection<Item> items = [];

    [ObservableProperty]
    private List<Category> categories = [];

    [ObservableProperty]
    private List<Category> filterCategories = [AllItemsCategory];

    [ObservableProperty]
    private Category? selectedCategoryItem = AllItemsCategory;

    [ObservableProperty]
    private string? selectedCategory;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private bool hasMorePages;

    public ItemsListViewModel()
    {
        Title = "Browse Items";
    }

    public ItemsListViewModel(IItemService itemService, INavigationService navigationService)
    {
        _itemService = itemService;
        _navigationService = navigationService;
        Title = "Browse Items";
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        if (_hasLoaded)
            LoadItemsCommand.Execute(null);
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_hasLoaded)
            LoadItemsCommand.Execute(null);
    }

    partial void OnSelectedCategoryItemChanged(Category? value)
    {
        if (_restoringCategory)
            return;
        SelectedCategory = (value is null || value.Id == 0) ? null : value.Slug;
    }

    [RelayCommand]
    private Task LoadItemsAsync() =>
        RunAsync(async () =>
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
            IsEmpty = results.Count == 0;
            HasMorePages = results.Count == PageSize;

            var all = new List<Category> { AllItemsCategory };
            all.AddRange(cats);
            FilterCategories = all;

            _restoringCategory = true;
            SelectedCategoryItem = string.IsNullOrEmpty(SelectedCategory)
                ? AllItemsCategory
                : all.FirstOrDefault(c => c.Slug == SelectedCategory) ?? AllItemsCategory;
            _restoringCategory = false;

            _hasLoaded = true;
        });

    [RelayCommand]
    private Task LoadMoreItemsAsync() =>
        RunAsync(async () =>
        {
            if (!HasMorePages)
                return;
            CurrentPage++;
            try
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
            }
            catch
            {
                CurrentPage--;
                throw;
            }
        });

    [RelayCommand]
    private async Task NavigateToItemAsync(Item item) =>
        await _navigationService.NavigateToAsync(
            Routes.ItemDetails,
            new Dictionary<string, object> { ["itemId"] = item.Id }
        );

    [RelayCommand]
    private async Task NavigateToCreateItemAsync() =>
        await _navigationService.NavigateToAsync(Routes.CreateItem);
}
