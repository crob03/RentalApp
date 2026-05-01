# Pagination & UI Consistency Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce `ItemsSearchBaseViewModel` to eliminate duplicated pagination/category logic, replace infinite scroll with a "Load More" button, fix the double-spinner on refresh, and align XAML structure across `ItemsListPage` and `NearbyItemsPage`.

**Architecture:** A new abstract `ItemsSearchBaseViewModel : BaseViewModel` owns all shared observable properties, loading-state methods (`RunLoadAsync` / `RunLoadMoreAsync`), category picker logic, and both navigation commands. Both `ItemsListViewModel` and `NearbyItemsViewModel` extend it, retaining only their page-specific fields and commands. `BaseViewModel` is untouched.

**Tech Stack:** .NET 10, .NET MAUI, CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`, `ObservableObject`), xUnit, NSubstitute

---

## File Map

| Action | Path |
|--------|------|
| Create | `RentalApp/ViewModels/ItemsSearchBaseViewModel.cs` |
| Create | `RentalApp.Test/ViewModels/ItemsSearchBaseViewModelTests.cs` |
| Modify | `RentalApp/ViewModels/ItemsListViewModel.cs` |
| Modify | `RentalApp/ViewModels/NearbyItemsViewModel.cs` |
| Modify | `RentalApp/Views/ItemsListPage.xaml` |
| Modify | `RentalApp/Views/NearbyItemsPage.xaml` |
| Modify | `RentalApp.Test/ViewModels/ItemsListViewModelTests.cs` |
| Modify | `RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs` |

---

### Task 1: Create `ItemsSearchBaseViewModel`

**Files:**
- Create: `RentalApp/ViewModels/ItemsSearchBaseViewModel.cs`
- Create: `RentalApp.Test/ViewModels/ItemsSearchBaseViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `RentalApp.Test/ViewModels/ItemsSearchBaseViewModelTests.cs`:

```csharp
using NSubstitute;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class ItemsSearchBaseViewModelTests
{
    private readonly INavigationService _nav = Substitute.For<INavigationService>();

    private sealed class TestableViewModel(INavigationService nav) : ItemsSearchBaseViewModel(nav)
    {
        public int ReloadCallCount { get; private set; }

        protected override Task ReloadAsync()
        {
            ReloadCallCount++;
            return Task.CompletedTask;
        }

        public Task DoLoadAsync(Func<Task> op) => RunLoadAsync(op);
        public Task DoLoadMoreAsync(Func<Task> op) => RunLoadMoreAsync(op);
        public void DoRestoreCategory(List<Category> all) => RestoreCategory(all);
    }

    private TestableViewModel CreateSut() => new(_nav);

    private static Category MakeCategory(int id = 1, string name = "Tools", string slug = "tools") =>
        new(id, name, slug, 5);

    private static Category AllItems => new(0, "All Items", string.Empty, 0);

    private static Item MakeItem(int id = 1) =>
        new(id, "Drill", null, 10.0, 1, "Tools", 1, "Alice", null, null, null, null, true, null, null, DateTime.UtcNow, null);

    // ── RunLoadAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task RunLoadAsync_SetsIsLoadingDuringExecution_AndClearsAfter()
    {
        var sut = CreateSut();
        bool wasLoading = false;
        await sut.DoLoadAsync(async () => { wasLoading = sut.IsLoading; });
        Assert.True(wasLoading);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task RunLoadAsync_WhenThrows_SetsError_AndClearsIsLoading()
    {
        var sut = CreateSut();
        await sut.DoLoadAsync(() => throw new Exception("boom"));
        Assert.True(sut.HasError);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.False(sut.IsLoading);
    }

    // ── RunLoadMoreAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RunLoadMoreAsync_WhenHasMorePages_SetsIsLoadingMoreAndIncrementsPage()
    {
        var sut = CreateSut();
        sut.HasMorePages = true;
        sut.CurrentPage = 1;
        bool wasLoadingMore = false;
        int pageInsideOp = 0;
        await sut.DoLoadMoreAsync(async () =>
        {
            wasLoadingMore = sut.IsLoadingMore;
            pageInsideOp = sut.CurrentPage;
        });
        Assert.True(wasLoadingMore);
        Assert.Equal(2, pageInsideOp);
        Assert.False(sut.IsLoadingMore);
    }

    [Fact]
    public async Task RunLoadMoreAsync_WhenHasMorePagesFalse_DoesNothing()
    {
        var sut = CreateSut();
        sut.HasMorePages = false;
        bool ran = false;
        await sut.DoLoadMoreAsync(async () => { ran = true; });
        Assert.False(ran);
        Assert.Equal(1, sut.CurrentPage);
    }

    [Fact]
    public async Task RunLoadMoreAsync_WhenThrows_RollsBackPage_AndSetsError()
    {
        var sut = CreateSut();
        sut.HasMorePages = true;
        sut.CurrentPage = 1;
        await sut.DoLoadMoreAsync(() => throw new Exception("fail"));
        Assert.Equal(1, sut.CurrentPage);
        Assert.True(sut.HasError);
        Assert.Equal("fail", sut.ErrorMessage);
        Assert.False(sut.IsLoadingMore);
    }

    // ── Category slug mapping ──────────────────────────────────────────

    [Fact]
    public void SelectingCategory_SetsSelectedCategorySlug()
    {
        var sut = CreateSut();
        sut.SelectedCategoryItem = MakeCategory();
        Assert.Equal("tools", sut.SelectedCategory);
    }

    [Fact]
    public void SelectingAllItemsCategory_ClearsSelectedCategory()
    {
        var sut = CreateSut();
        sut.SelectedCategoryItem = MakeCategory();
        sut.SelectedCategoryItem = AllItems;
        Assert.Null(sut.SelectedCategory);
    }

    // ── _hasLoaded guard ───────────────────────────────────────────────

    [Fact]
    public void CategoryChange_BeforeFirstLoad_DoesNotTriggerReload()
    {
        var sut = CreateSut();
        sut.SelectedCategoryItem = MakeCategory();
        Assert.Equal(0, sut.ReloadCallCount);
    }

    [Fact]
    public async Task CategoryChange_AfterFirstLoad_TriggersReload()
    {
        var sut = CreateSut();
        await sut.DoLoadAsync(async () => { });

        sut.SelectedCategoryItem = MakeCategory();

        Assert.Equal(1, sut.ReloadCallCount);
    }

    // ── RestoreCategory ────────────────────────────────────────────────

    [Fact]
    public async Task RestoreCategory_SetsSelectedCategoryItem_WithoutTriggeringReload()
    {
        var sut = CreateSut();
        sut.SelectedCategory = "tools"; // before first load — _hasLoaded still false
        await sut.DoLoadAsync(async () => { });
        var all = new List<Category> { AllItems, MakeCategory() };

        sut.DoRestoreCategory(all);

        Assert.Equal("tools", sut.SelectedCategoryItem?.Slug);
        Assert.Equal(0, sut.ReloadCallCount);
    }

    // ── Commands ───────────────────────────────────────────────────────

    [Fact]
    public async Task NavigateToCreateItemCommand_NavigatesToCreateItem()
    {
        var sut = CreateSut();
        await sut.NavigateToCreateItemCommand.ExecuteAsync(null);
        await _nav.Received(1).NavigateToAsync(Routes.CreateItem);
    }

    [Fact]
    public async Task NavigateToItemCommand_NavigatesToItemDetails_WithItemId()
    {
        var sut = CreateSut();
        await sut.NavigateToItemCommand.ExecuteAsync(MakeItem(42));
        await _nav.Received(1).NavigateToAsync(
            Routes.ItemDetails,
            Arg.Is<Dictionary<string, object>>(d => d.ContainsKey("itemId") && (int)d["itemId"] == 42)
        );
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test RentalApp.Test --filter "FullyQualifiedName~ItemsSearchBaseViewModelTests" --no-build 2>&1 | tail -5
```

Expected: compile error — `ItemsSearchBaseViewModel` does not exist yet.

- [ ] **Step 3: Implement `ItemsSearchBaseViewModel`**

Create `RentalApp/ViewModels/ItemsSearchBaseViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public abstract partial class ItemsSearchBaseViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    protected const int PageSize = 20;
    protected static readonly Category AllItemsCategory = new(0, "All Items", string.Empty, 0);

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
    private int currentPage = 1;

    [ObservableProperty]
    private bool hasMorePages;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isLoadingMore;

    protected ItemsSearchBaseViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    partial void OnSelectedCategoryItemChanged(Category? value)
    {
        if (_restoringCategory)
            return;
        SelectedCategory = (value is null || value.Id == 0) ? null : value.Slug;
    }

    partial void OnSelectedCategoryChanged(string? value) => TriggerReloadIfLoaded();

    protected void TriggerReloadIfLoaded()
    {
        if (_hasLoaded)
            _ = ReloadAsync();
    }

    protected abstract Task ReloadAsync();

    protected async Task RunLoadAsync(Func<Task> operation)
    {
        try
        {
            IsLoading = true;
            ClearError();
            await operation();
            _hasLoaded = true;
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task RunLoadMoreAsync(Func<Task> operation)
    {
        if (!HasMorePages)
            return;

        try
        {
            IsLoadingMore = true;
            ClearError();
            CurrentPage++;
            await operation();
        }
        catch (Exception ex)
        {
            CurrentPage--;
            SetError(ex.Message);
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    protected void RestoreCategory(List<Category> all)
    {
        _restoringCategory = true;
        SelectedCategoryItem = string.IsNullOrEmpty(SelectedCategory)
            ? AllItemsCategory
            : all.FirstOrDefault(c => c.Slug == SelectedCategory) ?? AllItemsCategory;
        _restoringCategory = false;
    }

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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test RentalApp.Test --filter "FullyQualifiedName~ItemsSearchBaseViewModelTests" 2>&1 | tail -5
```

Expected: all 10 tests pass.

- [ ] **Step 5: Commit**

```bash
git add RentalApp/ViewModels/ItemsSearchBaseViewModel.cs \
        RentalApp.Test/ViewModels/ItemsSearchBaseViewModelTests.cs
git commit -m "feat: add ItemsSearchBaseViewModel with shared pagination and category logic"
```

---

### Task 2: Refactor `ItemsListViewModel`

**Files:**
- Modify: `RentalApp/ViewModels/ItemsListViewModel.cs`
- Modify: `RentalApp.Test/ViewModels/ItemsListViewModelTests.cs`

- [ ] **Step 1: Remove tests for behaviour that moved to the base class or was deleted**

In `RentalApp.Test/ViewModels/ItemsListViewModelTests.cs`, delete these three test methods entirely:

- `LoadItemsCommand_EmptyResult_SetsIsEmptyTrue` — `IsEmpty` property is removed; `CollectionView.EmptyView` handles this in the UI
- `NavigateToItemCommand_NavigatesToItemDetails` — command moved to `ItemsSearchBaseViewModel`; covered by `ItemsSearchBaseViewModelTests`
- `NavigateToCreateItemCommand_NavigatesToCreateItem` — command moved to `ItemsSearchBaseViewModel`; covered by `ItemsSearchBaseViewModelTests`

- [ ] **Step 2: Run remaining tests to confirm they still pass before touching the ViewModel**

```bash
dotnet test RentalApp.Test --filter "FullyQualifiedName~ItemsListViewModelTests" 2>&1 | tail -5
```

Expected: all remaining tests pass (ViewModel is unchanged at this point).

- [ ] **Step 3: Replace `ItemsListViewModel` with the refactored version**

Replace the entire contents of `RentalApp/ViewModels/ItemsListViewModel.cs`:

```csharp
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

    protected override Task ReloadAsync()
    {
        LoadItemsCommand.Execute(null);
        return Task.CompletedTask;
    }

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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test RentalApp.Test --filter "FullyQualifiedName~ItemsListViewModelTests" 2>&1 | tail -5
```

Expected: all remaining tests pass.

- [ ] **Step 5: Run the full test suite to catch any regressions**

```bash
dotnet test RentalApp.Test 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add RentalApp/ViewModels/ItemsListViewModel.cs \
        RentalApp.Test/ViewModels/ItemsListViewModelTests.cs
git commit -m "refactor: simplify ItemsListViewModel to extend ItemsSearchBaseViewModel"
```

---

### Task 3: Refactor `NearbyItemsViewModel`

**Files:**
- Modify: `RentalApp/ViewModels/NearbyItemsViewModel.cs`
- Modify: `RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs`

- [ ] **Step 1: Update `NearbyItemsViewModelTests` for removed and changed behaviour**

In `RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs`:

Delete this test entirely — `IsEmpty` property is removed:
- `LoadNearbyItemsCommand_EmptyResult_SetsIsEmptyTrue`

Update these two tests — replace `Assert.False(sut.IsBusy)` with `Assert.False(sut.IsLoading)`:

```csharp
[Fact]
public async Task LoadNearbyItemsCommand_Success_PopulatesItems()
{
    _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
    _itemService
        .GetNearbyItemsAsync(55.9533, -3.1883, 5.0, null, 1, 20)
        .Returns(new List<Item> { MakeItem(1), MakeItem(2) });
    var sut = CreateSut();

    await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

    Assert.Equal(2, sut.Items.Count);
    Assert.False(sut.IsLoading);
}

[Fact]
public async Task LoadNearbyItemsCommand_GpsFails_SetsError()
{
    _locationService
        .GetCurrentLocationAsync()
        .ThrowsAsync(
            new InvalidOperationException(
                "Location unavailable. Please enable GPS and try again."
            )
        );
    var sut = CreateSut();

    await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

    Assert.True(sut.HasError);
    Assert.Contains("Location unavailable", sut.ErrorMessage);
    Assert.False(sut.IsLoading);
}
```

- [ ] **Step 2: Run remaining tests to confirm they still pass before touching the ViewModel**

```bash
dotnet test RentalApp.Test --filter "FullyQualifiedName~NearbyItemsViewModelTests" 2>&1 | tail -5
```

Expected: all remaining tests pass (ViewModel is unchanged at this point).

- [ ] **Step 3: Replace `NearbyItemsViewModel` with the refactored version**

Replace the entire contents of `RentalApp/ViewModels/NearbyItemsViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class NearbyItemsViewModel : ItemsSearchBaseViewModel
{
    private readonly IItemService _itemService;
    private readonly ILocationService _locationService;

    private double _cachedLat;
    private double _cachedLon;
    private bool _locationFetched;

    [ObservableProperty]
    private double radius = 5.0;

    public NearbyItemsViewModel(
        IItemService itemService,
        ILocationService locationService,
        INavigationService navigationService
    ) : base(navigationService)
    {
        _itemService = itemService;
        _locationService = locationService;
        Title = "Nearby Items";
    }

    partial void OnRadiusChanged(double value) => TriggerReloadIfLoaded();

    protected override Task ReloadAsync()
    {
        LoadNearbyItemsCommand.Execute(null);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task LoadNearbyItemsAsync() =>
        RunLoadAsync(async () =>
        {
            CurrentPage = 1;

            if (!_locationFetched)
            {
                var (lat, lon) = await _locationService.GetCurrentLocationAsync();
                _cachedLat = lat;
                _cachedLon = lon;
                _locationFetched = true;
            }

            var result = await _itemService.GetNearbyItemsAsync(
                _cachedLat,
                _cachedLon,
                Radius,
                SelectedCategory,
                CurrentPage,
                PageSize
            );
            var cats = await _itemService.GetCategoriesAsync() ?? [];

            Items = new ObservableCollection<Item>(result);
            HasMorePages = result.Count == PageSize;
            Categories = cats;

            var all = new List<Category> { AllItemsCategory };
            all.AddRange(cats);
            FilterCategories = all;
            RestoreCategory(all);
        });

    [RelayCommand]
    private Task LoadMoreItemsAsync() =>
        RunLoadMoreAsync(async () =>
        {
            var result = await _itemService.GetNearbyItemsAsync(
                _cachedLat,
                _cachedLon,
                Radius,
                SelectedCategory,
                CurrentPage,
                PageSize
            );
            foreach (var item in result)
                Items.Add(item);
            HasMorePages = result.Count == PageSize;
        });
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test RentalApp.Test --filter "FullyQualifiedName~NearbyItemsViewModelTests" 2>&1 | tail -5
```

Expected: all remaining tests pass.

- [ ] **Step 5: Run the full test suite to catch any regressions**

```bash
dotnet test RentalApp.Test 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add RentalApp/ViewModels/NearbyItemsViewModel.cs \
        RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs
git commit -m "refactor: simplify NearbyItemsViewModel to extend ItemsSearchBaseViewModel"
```

---

### Task 4: Update `ItemsListPage.xaml`

**Files:**
- Modify: `RentalApp/Views/ItemsListPage.xaml`

The project already has `InvertedBoolConverter` registered in `App.xaml` as `{StaticResource InvertedBoolConverter}`. The footer uses `IsVisible="{Binding HasMorePages}"` on the wrapper to hide everything when there are no more pages, then `IsVisible="{Binding IsLoadingMore, Converter={StaticResource InvertedBoolConverter}}"` on the button to swap it out for the spinner when loading.

- [ ] **Step 1: Replace `ItemsListPage.xaml` with the unified structure**

Replace the entire contents of `RentalApp/Views/ItemsListPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
  x:Class="RentalApp.Views.ItemsListPage"
  xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
  xmlns:vm="clr-namespace:RentalApp.ViewModels"
  xmlns:models="clr-namespace:RentalApp.Models"
  mc:Ignorable="d"
  x:DataType="vm:ItemsListViewModel"
  Title="{Binding Title}"
>
  <d:ContentPage.BindingContext>
    <vm:ItemsListViewModel />
  </d:ContentPage.BindingContext>
  <Grid RowDefinitions="Auto,Auto,Auto,*" Padding="16" RowSpacing="8">

    <!-- Error banner -->
    <Border
      Grid.Row="0"
      BackgroundColor="{AppThemeBinding Light=#FFEBEE, Dark=#1B0000}"
      Stroke="{AppThemeBinding Light=#F44336, Dark=#EF5350}"
      StrokeThickness="1"
      Padding="12"
      Margin="0,0,0,4"
      IsVisible="{Binding HasError}"
    >
      <Border.StrokeShape>
        <RoundRectangle CornerRadius="8" />
      </Border.StrokeShape>
      <Label
        Text="{Binding ErrorMessage}"
        TextColor="{AppThemeBinding Light=#D32F2F, Dark=#EF5350}"
      />
    </Border>

    <!-- Search bar -->
    <SearchBar
      Grid.Row="1"
      Placeholder="Search items…"
      Text="{Binding SearchText}"
      Margin="0,4"
    />

    <!-- Category filter -->
    <Picker
      Grid.Row="2"
      ItemsSource="{Binding FilterCategories}"
      SelectedItem="{Binding SelectedCategoryItem}"
      ItemDisplayBinding="{Binding Name}"
      Margin="0,4"
    />

    <!-- Items list with pull-to-refresh -->
    <RefreshView
      Grid.Row="3"
      IsRefreshing="{Binding IsLoading}"
      Command="{Binding LoadItemsCommand}"
    >
      <CollectionView
        ItemsSource="{Binding Items}"
        SelectionMode="None"
      >
        <CollectionView.EmptyView>
          <Label
            Text="No items found."
            HorizontalOptions="Center"
            Margin="0,40"
            TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
          />
        </CollectionView.EmptyView>
        <CollectionView.Footer>
          <StackLayout Padding="0,8" IsVisible="{Binding HasMorePages}">
            <Button
              Text="Load More"
              Command="{Binding LoadMoreItemsCommand}"
              IsVisible="{Binding IsLoadingMore, Converter={StaticResource InvertedBoolConverter}}"
              HorizontalOptions="Center"
            />
            <ActivityIndicator
              IsRunning="{Binding IsLoadingMore}"
              IsVisible="{Binding IsLoadingMore}"
              HorizontalOptions="Center"
            />
          </StackLayout>
        </CollectionView.Footer>
        <CollectionView.ItemTemplate>
          <DataTemplate x:DataType="models:Item">
            <Border
              Margin="0,4"
              Padding="12"
              StrokeThickness="1"
              Stroke="{AppThemeBinding Light={StaticResource Gray200}, Dark={StaticResource Gray700}}"
            >
              <Border.StrokeShape>
                <RoundRectangle CornerRadius="8" />
              </Border.StrokeShape>
              <Border.GestureRecognizers>
                <TapGestureRecognizer
                  Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ItemsListViewModel}}, Path=NavigateToItemCommand}"
                  CommandParameter="{Binding .}"
                />
              </Border.GestureRecognizers>
              <Grid ColumnDefinitions="*,Auto" RowDefinitions="Auto,Auto">
                <Label Grid.Row="0" Grid.Column="0" Text="{Binding Title}" FontAttributes="Bold" />
                <Label
                  Grid.Row="1"
                  Grid.Column="0"
                  Text="{Binding Category}"
                  FontSize="12"
                  TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
                />
                <Label
                  Grid.Row="0"
                  Grid.Column="1"
                  Text="{Binding DailyRate, StringFormat='£{0:F2}/day'}"
                />
              </Grid>
            </Border>
          </DataTemplate>
        </CollectionView.ItemTemplate>
      </CollectionView>
    </RefreshView>

    <!-- Initial load indicator (overlaid, centred) -->
    <ActivityIndicator
      Grid.Row="3"
      IsRunning="{Binding IsLoading}"
      IsVisible="{Binding IsLoading}"
      HorizontalOptions="Center"
      VerticalOptions="Center"
    />

    <!-- FAB: create item -->
    <Button
      Grid.Row="3"
      Text="+"
      Command="{Binding NavigateToCreateItemCommand}"
      HorizontalOptions="End"
      VerticalOptions="End"
      WidthRequest="56"
      HeightRequest="56"
      CornerRadius="28"
      Margin="16"
      FontSize="24"
    />

  </Grid>
</ContentPage>
```

- [ ] **Step 2: Run the full test suite**

```bash
dotnet test RentalApp.Test 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 3: Commit**

```bash
git add RentalApp/Views/ItemsListPage.xaml
git commit -m "feat: update ItemsListPage with load more button, pull-to-refresh, and unified layout"
```
