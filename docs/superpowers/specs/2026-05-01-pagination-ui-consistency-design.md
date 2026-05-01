# Pagination & UI Consistency — Design Spec

**Date:** 2026-05-01
**Branch:** feature/improve-pagination
**Scope:** `ItemsListViewModel`, `NearbyItemsViewModel`, `ItemsListPage.xaml`, `NearbyItemsPage.xaml`

---

## Problem Statement

`ItemsListPage` and `NearbyItemsPage` have diverged in structure and behaviour despite being closely related:

- Both use infinite scroll (`RemainingItemsThreshold`) — replacing with an explicit "Load More" button.
- Both bind `IsBusy` to both `ActivityIndicator` and `RefreshView`, causing two simultaneous loading indicators when refreshing.
- `ItemsListPage` has no pull-to-refresh; `NearbyItemsPage` does.
- Empty state handling differs: `ItemsListPage` uses an overlapping `Label` driven by `IsEmpty`; `NearbyItemsPage` uses `CollectionView.EmptyView`. Both will use `CollectionView.EmptyView`, eliminating the need for `IsEmpty` entirely.
- Card markup differs: `ItemsListPage` wraps each card in an extra `<Grid>`; `NearbyItemsPage` does not.
- Error banner placement and margins are inconsistent between the two pages.
- Shared ViewModel logic (category picker state, pagination fields, `_restoringCategory` flag) is duplicated across both ViewModels.

---

## Architecture

### ViewModel Hierarchy

```
BaseViewModel                    ← unchanged
  └── ItemsSearchBaseViewModel        ← new
        ├── ItemsListViewModel   ← simplified
        └── NearbyItemsViewModel ← simplified
```

### `ItemsSearchBaseViewModel : BaseViewModel`

New `partial` class. Owns all state shared between the two paginated search pages. Receives `INavigationService` via constructor so it can own `NavigateToCreateItemCommand` directly.

**Observable properties:**
- `ObservableCollection<Item> Items`
- `List<Category> Categories`
- `List<Category> FilterCategories`
- `Category? SelectedCategoryItem`
- `string? SelectedCategory`
- `int CurrentPage`
- `bool HasMorePages`
- `bool IsLoading` — true during initial loads and filter/refresh-triggered reloads
- `bool IsLoadingMore` — true only during load-more operations

**Constants / statics:**
- `const int PageSize = 20`
- `static readonly Category AllItemsCategory = new(0, "All Items", string.Empty, 0)`

**Private flags:**
- `bool _restoringCategory` — guards against `OnSelectedCategoryItemChanged` re-triggering a load during category restore
- `bool _hasLoaded` — prevents property-change callbacks firing a reload before the first load completes

**Methods:**
- `RunLoadAsync(Func<Task>)` — sets `IsLoading`, clears error, restores in finally. Catches exceptions via `SetError`. Used by subclass full-load methods instead of `BaseViewModel.RunAsync`, keeping `IsBusy` unused by these ViewModels.
- `RunLoadMoreAsync(Func<Task>)` — guards on `HasMorePages`, increments `CurrentPage`, sets `IsLoadingMore`, clears error. On failure rolls back `CurrentPage` and surfaces the exception via `SetError`. Restores `IsLoadingMore` in finally. Subclasses provide only the fetch logic.
- `RestoreCategory(List<Category> all)` — the shared `_restoringCategory` block; sets `SelectedCategoryItem` from `SelectedCategory` without triggering a reload.
- `abstract Task ReloadAsync()` — called by `OnSelectedCategoryChanged`; each subclass implements it to invoke its own load command.
- `NavigateToCreateItemCommand` — navigates to `Routes.CreateItem`; shared so both pages get the FAB without duplication.

**Partial methods:**
- `OnSelectedCategoryItemChanged(Category? value)` — translates picker selection to `SelectedCategory` slug; skips synthetic "All Items" (Id == 0); respects `_restoringCategory`.
- `OnSelectedCategoryChanged(string? value)` — calls `ReloadAsync()` if `_hasLoaded`.

### `ItemsListViewModel : ItemsSearchBaseViewModel`

Retains only:
- `string SearchText`
- `OnSearchTextChanged` — calls `ReloadAsync()` if `_hasLoaded`
- `LoadItemsAsync` — calls `RunLoadAsync`; resets to page 1, fetches items + categories, calls `RestoreCategory`
- `LoadMoreItemsAsync` — calls `RunLoadMoreAsync` with only the fetch logic; page increment/rollback handled by base
- `override Task ReloadAsync()` — executes `LoadItemsCommand`

### `NearbyItemsViewModel : ItemsSearchBaseViewModel`

Retains only:
- `double Radius`
- `OnRadiusChanged` — calls `ReloadAsync()` if `_hasLoaded`
- `double _cachedLat`, `double _cachedLon`, `bool _locationFetched` — GPS cache
- `LoadNearbyItemsAsync` — calls `RunLoadAsync`; resolves GPS on first load, resets to page 1, fetches items + categories, calls `RestoreCategory`
- `LoadMoreItemsAsync` — calls `RunLoadMoreAsync` with only the fetch logic; page increment/rollback handled by base
- `override Task ReloadAsync()` — executes `LoadNearbyItemsCommand`

---

## UI / XAML

Both pages adopt a unified structure. Differences (SearchBar vs Radius slider) are noted inline.

### Grid structure (both pages)

```
Row 0 — Error banner
Row 1 — Page-specific filter (SearchBar on ItemsList; Radius slider on NearbyItems)
Row 2 — Category Picker
Row 3 — RefreshView → CollectionView (with Footer and EmptyView)
         + ActivityIndicator overlaid in same row (centred, IsVisible=IsLoading)
```

Row 3 spans the full remaining height (`*`). The `ActivityIndicator` and `RefreshView` both occupy Row 3; when `IsLoading` is true the `ActivityIndicator` is visible and the `RefreshView` shows an empty list, giving a visually centred spinner over the content area.

### Loading states

| State | Indicator |
|-------|-----------|
| Initial load | `ActivityIndicator` overlaid in Row 3, centred, `IsVisible="{Binding IsLoading}"` |
| Pull-to-refresh | Native `RefreshView` spinner, `IsRefreshing="{Binding IsLoading}"` |
| Load more in progress | Small `ActivityIndicator` in `CollectionView.Footer` replaces the Load More button, `IsVisible="{Binding IsLoadingMore}"` |
| Idle, more pages available | "Load More" button in `CollectionView.Footer`, `IsVisible="{Binding HasMorePages}"` (hidden when `IsLoadingMore`) |
| Idle, no more pages | Footer hidden entirely |

`RefreshView` binds to `IsLoading`, not `IsBusy` — this prevents the double-spinner on refresh.

### Empty state

Both pages use `CollectionView.EmptyView` with a centred label. `ItemsListPage` drops its overlapping `Label`/`IsVisible` approach.

### Card markup

Both pages render item cards directly as `<Border>` with no outer wrapper `<Grid>`. `ItemsListPage` removes its redundant `<Grid Padding="12,8" ColumnDefinitions="*,Auto">` per-item wrapper.

`NearbyItemsPage` cards additionally show a distance label (`{0:F1} km away`) at Row 1, Column 1 — this is page-specific and unchanged.

### Error banner

- Placed at `Grid.Row="0"` on both pages
- Consistent styling: `Padding="12"`, `Margin="0,0,0,4"`, `CornerRadius="8"`
- `IsVisible="{Binding HasError}"`

### Filter controls

- Both `Picker` controls use `Margin="0,4"`
- `ItemsListPage` `SearchBar`: `Margin="0,4"`
- `NearbyItemsPage` Radius `StackLayout`: `Margin="0,4"`

### FAB (both pages)

Both pages show a "+" FAB at `Grid.Row="3"`, `HorizontalOptions="End"`, `VerticalOptions="End"`, overlaid on the `RefreshView`. Bound to `NavigateToCreateItemCommand` from `ItemsSearchBaseViewModel`.

---

## Testing

### New: `ItemsSearchBaseViewModelTests`

A concrete `TestableSearchViewModel` subclass is created inside the test project. It implements `ReloadAsync()` as a no-op (or records invocation count), allowing the base class to be tested in isolation:

- `OnSelectedCategoryChanged` calls `ReloadAsync()` only after `_hasLoaded` is true
- `OnSelectedCategoryItemChanged` correctly maps picker selection to `SelectedCategory` slug
- `OnSelectedCategoryItemChanged` is skipped when `_restoringCategory` is true
- `RestoreCategory` sets `SelectedCategoryItem` without triggering a reload
- `RunLoadAsync` sets and clears `IsLoading`
- `RunLoadAsync` surfaces exceptions via `SetError` and leaves `IsLoading = false`
- `RunLoadMoreAsync` sets and clears `IsLoadingMore`
- `RunLoadMoreAsync` surfaces exceptions via `SetError` and leaves `IsLoadingMore = false`

### Updated: existing ViewModel tests

Any assertions on `IsBusy` during load-more operations are updated to check `IsLoadingMore`. Assertions on shared properties (`HasMorePages`, `CurrentPage`, `IsEmpty`, etc.) remain valid — those properties are still on the ViewModel, just inherited.

### No new XAML tests

UI layout is verified manually by running the app.

---

## Out of Scope

- Changes to `BaseViewModel`
- Changes to any other ViewModel or page (Login, Register, Main, CreateItem, ItemDetails)
- Navigation logic changes
- API or service layer changes
