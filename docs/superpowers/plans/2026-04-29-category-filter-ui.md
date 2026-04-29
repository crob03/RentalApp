# Category Filter UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a category Picker dropdown to `ItemsListPage` and `NearbyItemsPage` so users can filter items by category, with "All Items" clearing the filter.

**Architecture:** Both ViewModels gain `FilterCategories` (AllItems sentinel + real categories) and `SelectedCategoryItem` (Picker binding) properties. A `_restoringCategory` guard flag prevents the post-load category restore from re-triggering the load command. Both XAML pages get a Picker inserted in the controls area, bound to these new properties.

**Tech Stack:** .NET MAUI, CommunityToolkit.Mvvm `[ObservableProperty]`, NSubstitute + xUnit

---

## File Map

| Action | File |
|--------|------|
| Modify | `RentalApp/ViewModels/ItemsListViewModel.cs` |
| Modify | `RentalApp/ViewModels/NearbyItemsViewModel.cs` |
| Modify | `RentalApp/Views/ItemsListPage.xaml` |
| Modify | `RentalApp/Views/NearbyItemsPage.xaml` |
| Modify | `RentalApp.Test/ViewModels/ItemsListViewModelTests.cs` |
| Modify | `RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs` |

---

### Task 1: Add category filter tests for ItemsListViewModel

**Files:**
- Modify: `RentalApp.Test/ViewModels/ItemsListViewModelTests.cs`

- [ ] **Step 1: Add 6 new tests to `ItemsListViewModelTests`**

Append after the existing `NavigateToCreateItemCommand_NavigatesToCreateItem` test:

```csharp
// ── Category filter ────────────────────────────────────────────────

[Fact]
public async Task LoadItemsCommand_PopulatesFilterCategoriesWithAllItemsSentinel()
{
    _itemService.GetItemsAsync().ReturnsForAnyArgs([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();

    await sut.LoadItemsCommand.ExecuteAsync(null);

    Assert.Equal(2, sut.FilterCategories.Count);
    Assert.Equal(0, sut.FilterCategories[0].Id);
    Assert.Equal("All Items", sut.FilterCategories[0].Name);
    Assert.Equal("tools", sut.FilterCategories[1].Slug);
}

[Fact]
public async Task LoadItemsCommand_SelectedCategoryItemDefaultsToAllItems()
{
    _itemService.GetItemsAsync().ReturnsForAnyArgs([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();

    await sut.LoadItemsCommand.ExecuteAsync(null);

    Assert.Equal(0, sut.SelectedCategoryItem?.Id);
    Assert.Equal("All Items", sut.SelectedCategoryItem?.Name);
}

[Fact]
public async Task SelectingCategory_UpdatesSelectedCategorySlug()
{
    _itemService.GetItemsAsync().ReturnsForAnyArgs([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();
    await sut.LoadItemsCommand.ExecuteAsync(null);

    sut.SelectedCategoryItem = MakeCategory();

    Assert.Equal("tools", sut.SelectedCategory);
}

[Fact]
public async Task SelectingAllItems_ClearsSelectedCategory()
{
    _itemService.GetItemsAsync().ReturnsForAnyArgs([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();
    await sut.LoadItemsCommand.ExecuteAsync(null);
    sut.SelectedCategoryItem = MakeCategory();

    sut.SelectedCategoryItem = sut.FilterCategories[0];

    Assert.Null(sut.SelectedCategory);
}

[Fact]
public async Task CategoryChange_AfterLoad_TriggersReload()
{
    _itemService.GetItemsAsync().ReturnsForAnyArgs([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();
    await sut.LoadItemsCommand.ExecuteAsync(null);

    sut.SelectedCategoryItem = MakeCategory();
    await Task.Delay(50);

    await _itemService.Received(2).GetItemsAsync(
        Arg.Any<string?>(),
        Arg.Any<string?>(),
        Arg.Any<int>(),
        Arg.Any<int>()
    );
}

[Fact]
public async Task LoadItems_DoesNotTriggerExtraReload_WhenRestoringCategory()
{
    _itemService.GetItemsAsync().ReturnsForAnyArgs([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();

    await sut.LoadItemsCommand.ExecuteAsync(null);
    await sut.LoadItemsCommand.ExecuteAsync(null);

    await _itemService.Received(2).GetItemsAsync(
        Arg.Any<string?>(),
        Arg.Any<string?>(),
        Arg.Any<int>(),
        Arg.Any<int>()
    );
}
```

- [ ] **Step 2: Confirm the build fails** (`FilterCategories` and `SelectedCategoryItem` don't exist yet)

```bash
dotnet build RentalApp.Test
```

Expected: compilation errors referencing `FilterCategories` and `SelectedCategoryItem` on `ItemsListViewModel`.

---

### Task 2: Implement category filter in ItemsListViewModel

**Files:**
- Modify: `RentalApp/ViewModels/ItemsListViewModel.cs`

- [ ] **Step 1: Add static sentinel, guard flag, and two new observable properties**

Add directly after `private const int PageSize = 20;`:

```csharp
private static readonly Category AllItemsCategory = new(0, "All Items", string.Empty, 0);
private bool _restoringCategory;
```

Add directly after `[ObservableProperty] private List<Category> categories = [];`:

```csharp
[ObservableProperty]
private List<Category> filterCategories = [AllItemsCategory];

[ObservableProperty]
private Category? selectedCategoryItem = AllItemsCategory;
```

- [ ] **Step 2: Add the property watcher**

Add directly after `partial void OnSearchTextChanged(string value) => LoadItemsCommand.Execute(null);`:

```csharp
partial void OnSelectedCategoryItemChanged(Category? value)
{
    if (_restoringCategory)
        return;
    SelectedCategory = (value is null || value.Id == 0) ? null : value.Slug;
}
```

- [ ] **Step 3: Update `LoadItemsAsync` to build `FilterCategories` and restore the selected item**

Replace the entire `LoadItemsAsync` method with:

```csharp
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
    });
```

- [ ] **Step 4: Run the new tests and confirm they pass**

```bash
dotnet test RentalApp.Test --filter "FullyQualifiedName~ItemsListViewModelTests"
```

Expected: all tests pass, including the 6 new category filter tests.

- [ ] **Step 5: Commit**

```bash
git add RentalApp/ViewModels/ItemsListViewModel.cs RentalApp.Test/ViewModels/ItemsListViewModelTests.cs
git commit -m "feat: add category filter to ItemsListViewModel with tests"
```

---

### Task 3: Add Picker to ItemsListPage.xaml

**Files:**
- Modify: `RentalApp/Views/ItemsListPage.xaml`

- [ ] **Step 1: Replace the entire file with the updated XAML**

The Picker is inserted as the new Row 1. All subsequent row numbers shift up by one. The trailing unused `Auto` row definition is dropped.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
  x:Class="RentalApp.Views.ItemsListPage"
  xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
  xmlns:vm="clr-namespace:RentalApp.ViewModels"
  xmlns:models="clr-namespace:RentalApp.Models"
  Title="{Binding Title}"
  x:DataType="vm:ItemsListViewModel"
>
  <Grid RowDefinitions="Auto,Auto,Auto,Auto,*">
    <!-- Search bar -->
    <SearchBar
      Grid.Row="0"
      Placeholder="Search items…"
      Text="{Binding SearchText}"
      Margin="8,8,8,0"
    />

    <!-- Category filter -->
    <Picker
      Grid.Row="1"
      ItemsSource="{Binding FilterCategories}"
      SelectedItem="{Binding SelectedCategoryItem}"
      ItemDisplayBinding="{Binding Name}"
      Margin="8,4,8,0"
    />

    <!-- Error banner -->
    <Label
      Grid.Row="2"
      Text="{Binding ErrorMessage}"
      TextColor="Red"
      IsVisible="{Binding HasError}"
      Margin="8,4"
    />

    <!-- Loading indicator -->
    <ActivityIndicator
      Grid.Row="3"
      IsRunning="{Binding IsBusy}"
      IsVisible="{Binding IsBusy}"
      HorizontalOptions="Center"
      Margin="0,4"
    />

    <!-- Empty state -->
    <Label
      Grid.Row="4"
      Text="No items found."
      HorizontalOptions="Center"
      VerticalOptions="Center"
      IsVisible="{Binding IsEmpty}"
    />

    <!-- Items list -->
    <CollectionView
      Grid.Row="4"
      ItemsSource="{Binding Items}"
      SelectionMode="None"
      RemainingItemsThreshold="3"
      RemainingItemsThresholdReachedCommand="{Binding LoadMoreItemsCommand}"
    >
      <CollectionView.ItemTemplate>
        <DataTemplate x:DataType="models:Item">
          <Grid Padding="12,8" ColumnDefinitions="*,Auto">
            <VerticalStackLayout>
              <Label Text="{Binding Title}" FontAttributes="Bold" FontSize="15" />
              <Label Text="{Binding Category}" FontSize="12" TextColor="Gray" />
            </VerticalStackLayout>
            <Label
              Grid.Column="1"
              Text="{Binding DailyRate, StringFormat='£{0:F2}/day'}"
              VerticalOptions="Center"
              FontAttributes="Bold"
            />
            <Grid.GestureRecognizers>
              <TapGestureRecognizer
                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ItemsListViewModel}}, Path=NavigateToItemCommand}"
                CommandParameter="{Binding .}"
              />
            </Grid.GestureRecognizers>
          </Grid>
        </DataTemplate>
      </CollectionView.ItemTemplate>
    </CollectionView>

    <!-- FAB: create item -->
    <Button
      Grid.Row="4"
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

- [ ] **Step 2: Build to confirm no XAML errors**

```bash
dotnet build RentalApp.sln
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add RentalApp/Views/ItemsListPage.xaml
git commit -m "feat: add category picker to ItemsListPage"
```

---

### Task 4: Add category filter tests for NearbyItemsViewModel

**Files:**
- Modify: `RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs`

- [ ] **Step 1: Add `MakeCategory` helper after `MakeItem`**

```csharp
private static Category MakeCategory(
    int id = 1,
    string name = "Tools",
    string slug = "tools"
) => new(id, name, slug, 5);
```

- [ ] **Step 2: Add 6 new tests after `RadiusChange_AfterFirstLoad_TriggersReload`**

```csharp
// ── Category filter ────────────────────────────────────────────────

[Fact]
public async Task LoadNearbyItemsCommand_PopulatesFilterCategoriesWithAllItemsSentinel()
{
    _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
    _itemService
        .GetNearbyItemsAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
        .Returns([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();

    await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

    Assert.Equal(2, sut.FilterCategories.Count);
    Assert.Equal(0, sut.FilterCategories[0].Id);
    Assert.Equal("All Items", sut.FilterCategories[0].Name);
    Assert.Equal("tools", sut.FilterCategories[1].Slug);
}

[Fact]
public async Task LoadNearbyItemsCommand_SelectedCategoryItemDefaultsToAllItems()
{
    _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
    _itemService
        .GetNearbyItemsAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
        .Returns([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();

    await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

    Assert.Equal(0, sut.SelectedCategoryItem?.Id);
    Assert.Equal("All Items", sut.SelectedCategoryItem?.Name);
}

[Fact]
public async Task SelectingCategory_UpdatesSelectedCategorySlug()
{
    _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
    _itemService
        .GetNearbyItemsAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
        .Returns([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();
    await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

    sut.SelectedCategoryItem = MakeCategory();

    Assert.Equal("tools", sut.SelectedCategory);
}

[Fact]
public async Task SelectingAllItems_ClearsSelectedCategory()
{
    _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
    _itemService
        .GetNearbyItemsAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
        .Returns([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();
    await sut.LoadNearbyItemsCommand.ExecuteAsync(null);
    sut.SelectedCategoryItem = MakeCategory();

    sut.SelectedCategoryItem = sut.FilterCategories[0];

    Assert.Null(sut.SelectedCategory);
}

[Fact]
public async Task CategoryChange_AfterLoad_TriggersReload()
{
    _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
    _itemService
        .GetNearbyItemsAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
        .Returns([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();
    await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

    sut.SelectedCategoryItem = MakeCategory();
    await Task.Delay(50);

    await _itemService.Received(2).GetNearbyItemsAsync(
        Arg.Any<double>(),
        Arg.Any<double>(),
        Arg.Any<double>(),
        Arg.Any<string?>(),
        Arg.Any<int>(),
        Arg.Any<int>()
    );
}

[Fact]
public async Task LoadNearbyItems_DoesNotTriggerExtraReload_WhenRestoringCategory()
{
    _locationService.GetCurrentLocationAsync().Returns((55.9533, -3.1883));
    _itemService
        .GetNearbyItemsAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>())
        .Returns([]);
    _itemService.GetCategoriesAsync().Returns([MakeCategory()]);
    var sut = CreateSut();

    await sut.LoadNearbyItemsCommand.ExecuteAsync(null);
    await sut.LoadNearbyItemsCommand.ExecuteAsync(null);

    await _itemService.Received(2).GetNearbyItemsAsync(
        Arg.Any<double>(),
        Arg.Any<double>(),
        Arg.Any<double>(),
        Arg.Any<string?>(),
        Arg.Any<int>(),
        Arg.Any<int>()
    );
}
```

- [ ] **Step 3: Confirm the build fails** (`FilterCategories` and `SelectedCategoryItem` don't exist on `NearbyItemsViewModel` yet)

```bash
dotnet build RentalApp.Test
```

Expected: compilation errors on `NearbyItemsViewModel` references.

---

### Task 5: Implement category filter in NearbyItemsViewModel

**Files:**
- Modify: `RentalApp/ViewModels/NearbyItemsViewModel.cs`

- [ ] **Step 1: Add static sentinel, guard flag, and two new observable properties**

Add directly after `private bool _hasLoaded;`:

```csharp
private static readonly Category AllItemsCategory = new(0, "All Items", string.Empty, 0);
private bool _restoringCategory;
```

Add directly after `[ObservableProperty] private List<Category> categories = [];`:

```csharp
[ObservableProperty]
private List<Category> filterCategories = [AllItemsCategory];

[ObservableProperty]
private Category? selectedCategoryItem = AllItemsCategory;
```

- [ ] **Step 2: Add the property watcher**

Add directly after `partial void OnSelectedCategoryChanged`:

```csharp
partial void OnSelectedCategoryItemChanged(Category? value)
{
    if (_restoringCategory)
        return;
    SelectedCategory = (value is null || value.Id == 0) ? null : value.Slug;
}
```

- [ ] **Step 3: Update `LoadNearbyItemsAsync` to fetch categories, build `FilterCategories`, and restore the selected item**

Replace the entire `LoadNearbyItemsAsync` method with:

```csharp
[RelayCommand]
private Task LoadNearbyItemsAsync() =>
    RunAsync(async () =>
    {
        CurrentPage = 1;

        var (lat, lon) = await _locationService.GetCurrentLocationAsync();
        _cachedLat = lat;
        _cachedLon = lon;

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
        IsEmpty = Items.Count == 0;
        Categories = cats;
        _hasLoaded = true;

        var all = new List<Category> { AllItemsCategory };
        all.AddRange(cats);
        FilterCategories = all;

        _restoringCategory = true;
        SelectedCategoryItem = string.IsNullOrEmpty(SelectedCategory)
            ? AllItemsCategory
            : all.FirstOrDefault(c => c.Slug == SelectedCategory) ?? AllItemsCategory;
        _restoringCategory = false;
    });
```

Note: `?? []` on `GetCategoriesAsync()` is a guard so existing tests that don't set up this mock return an empty list rather than null.

- [ ] **Step 4: Run the new tests and confirm they all pass**

```bash
dotnet test RentalApp.Test --filter "FullyQualifiedName~NearbyItemsViewModelTests"
```

Expected: all tests pass, including the 6 new category filter tests.

- [ ] **Step 5: Commit**

```bash
git add RentalApp/ViewModels/NearbyItemsViewModel.cs RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs
git commit -m "feat: add category filter to NearbyItemsViewModel with tests"
```

---

### Task 6: Add Picker to NearbyItemsPage.xaml

**Files:**
- Modify: `RentalApp/Views/NearbyItemsPage.xaml`

- [ ] **Step 1: Replace the entire file with the updated XAML**

The Picker is inserted as new Row 2, between the radius slider and the loading indicator. `ActivityIndicator` moves to Row 3, `RefreshView` to Row 4.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
  x:Class="RentalApp.Views.NearbyItemsPage"
  xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
  xmlns:vm="clr-namespace:RentalApp.ViewModels"
  xmlns:models="clr-namespace:RentalApp.Models"
  Title="{Binding Title}"
  x:DataType="vm:NearbyItemsViewModel"
>
  <Grid RowDefinitions="Auto,Auto,Auto,Auto,*" Padding="16" RowSpacing="8">
    <!-- Error banner -->
    <Border
      Grid.Row="0"
      BackgroundColor="{AppThemeBinding Light=#FFEBEE, Dark=#1B0000}"
      Stroke="{AppThemeBinding Light=#F44336, Dark=#EF5350}"
      StrokeThickness="1"
      Padding="12"
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

    <!-- Radius slider -->
    <StackLayout Grid.Row="1" Orientation="Horizontal" Spacing="8">
      <Label
        Text="{Binding Radius, StringFormat='Radius: {0:F0} km'}"
        VerticalOptions="Center"
        MinimumWidthRequest="110"
      />
      <Slider Value="{Binding Radius}" Minimum="1" Maximum="50" HorizontalOptions="FillAndExpand" />
    </StackLayout>

    <!-- Category filter -->
    <Picker
      Grid.Row="2"
      ItemsSource="{Binding FilterCategories}"
      SelectedItem="{Binding SelectedCategoryItem}"
      ItemDisplayBinding="{Binding Name}"
    />

    <!-- Loading indicator -->
    <ActivityIndicator Grid.Row="3" IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}" />

    <!-- Items list -->
    <RefreshView
      Grid.Row="4"
      IsRefreshing="{Binding IsBusy}"
      Command="{Binding LoadNearbyItemsCommand}"
    >
      <CollectionView
        ItemsSource="{Binding Items}"
        RemainingItemsThreshold="3"
        RemainingItemsThresholdReachedCommand="{Binding LoadMoreItemsCommand}"
      >
        <CollectionView.EmptyView>
          <Label
            Text="No items found nearby."
            HorizontalOptions="Center"
            Margin="0,40"
            TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
          />
        </CollectionView.EmptyView>
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
                  Command="{Binding Source={RelativeSource AncestorType={x:Type vm:NearbyItemsViewModel}}, Path=NavigateToItemCommand}"
                  CommandParameter="{Binding .}"
                />
              </Border.GestureRecognizers>
              <Grid ColumnDefinitions="*,Auto" RowDefinitions="Auto,Auto">
                <Label Grid.Row="0" Grid.Column="0" Text="{Binding Title}" FontAttributes="Bold" />
                <Label
                  Grid.Row="0"
                  Grid.Column="1"
                  Text="{Binding DailyRate, StringFormat='£{0:F2}/day'}"
                />
                <Label
                  Grid.Row="1"
                  Grid.Column="0"
                  Text="{Binding Distance, StringFormat='{0:F1} km away'}"
                  FontSize="12"
                  TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
                />
              </Grid>
            </Border>
          </DataTemplate>
        </CollectionView.ItemTemplate>
      </CollectionView>
    </RefreshView>
  </Grid>
</ContentPage>
```

- [ ] **Step 2: Build and run the full test suite**

```bash
dotnet build RentalApp.sln && dotnet test RentalApp.Test
```

Expected: build succeeds, all tests pass.

- [ ] **Step 3: Commit**

```bash
git add RentalApp/Views/NearbyItemsPage.xaml
git commit -m "feat: add category picker to NearbyItemsPage"
```
