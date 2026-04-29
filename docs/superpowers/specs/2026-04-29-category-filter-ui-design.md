# Category Filter UI — Design Spec

**Date:** 2026-04-29
**Branch:** feature/implement-item-crud
**Scope:** `ItemsListPage` and `NearbyItemsPage` — UI only

---

## Problem

Both pages expose category filtering through the ViewModel (`SelectedCategory`, `Categories`, `OnSelectedCategoryChanged`) and the service layer (`GetItemsAsync`, `GetNearbyItemsAsync` both accept a `category` slug parameter). However, no UI control is present on either page, so users cannot filter by category.

---

## Solution

Add a MAUI `Picker` to each page that lets the user select a category. The first option is always "All Items", which clears the filter. Selecting any other category passes its slug to the API.

---

## ViewModel Changes (both `ItemsListViewModel` and `NearbyItemsViewModel`)

### New properties

```
FilterCategories: List<Category>
```
A static "All Items" sentinel (`Id=0, Name="All Items", Slug="", ItemCount=0`) prepended to the real category list. Used as the Picker's `ItemsSource`. Updated after each load.

```
SelectedCategoryItem: Category?
```
Two-way bound to the Picker's `SelectedItem`. Initialised to the AllItems sentinel. When changed by the user, sets `SelectedCategory` (the existing `string?` slug property used by the service calls).

### Guard flag: `_restoringCategory`

After each load, `FilterCategories` is rebuilt and `SelectedCategoryItem` is restored by matching `SelectedCategory`'s slug against the new list. Without a guard, this restore would fire `OnSelectedCategoryItemChanged` → update `SelectedCategory` → trigger `OnSelectedCategoryChanged` → trigger another load (infinite loop).

The flag suppresses `OnSelectedCategoryItemChanged` during the restore step only.

### Property watcher

```csharp
partial void OnSelectedCategoryItemChanged(Category? value)
{
    if (_restoringCategory) return;
    SelectedCategory = (value is null || value.Id == 0) ? null : value.Slug;
}
```

`SelectedCategory`'s existing watcher (`OnSelectedCategoryChanged` / `OnSelectedCategoryChanged`) already triggers the load command — no changes needed there.

### Load method additions (both ViewModels)

After fetching categories and assigning `Categories`:

```csharp
var all = new List<Category> { AllItemsCategory };
all.AddRange(cats);
FilterCategories = all;

_restoringCategory = true;
SelectedCategoryItem = string.IsNullOrEmpty(SelectedCategory)
    ? AllItemsCategory
    : all.FirstOrDefault(c => c.Slug == SelectedCategory) ?? AllItemsCategory;
_restoringCategory = false;
```

---

## XAML Changes

### `ItemsListPage.xaml`

Add one `Auto` row to the Grid and insert a Picker between the search bar (row 0) and the error banner (now row 2). Shift all subsequent rows down by one.

```xml
<Picker
  Grid.Row="1"
  ItemsSource="{Binding FilterCategories}"
  SelectedItem="{Binding SelectedCategoryItem}"
  ItemDisplayBinding="{Binding Name}"
  Margin="8,4,8,0"
/>
```

### `NearbyItemsPage.xaml`

Add one `Auto` row and insert a Picker after the radius slider (row 1) and before the loading indicator (now row 3). Shift subsequent rows down by one.

```xml
<Picker
  Grid.Row="2"
  ItemsSource="{Binding FilterCategories}"
  SelectedItem="{Binding SelectedCategoryItem}"
  ItemDisplayBinding="{Binding Name}"
  Margin="0,0,0,4"
/>
```

---

## What is not changing

- `IItemService`, `ItemService`, `IApiService`, `LocalApiService` — no changes
- `SelectedCategory: string?` — kept as the internal slug used by service calls
- `Categories: List<Category>` — kept, still updated on load
- Pagination, search, radius filtering — untouched
- Tests — no ViewModel logic changes that affect existing behaviour

---

## Test coverage

New unit tests added to the existing `ItemsListViewModelTests` and `NearbyItemsViewModelTests` classes. The project uses NSubstitute and xUnit. Tests follow the existing patterns in those files.

### `ItemsListViewModelTests` additions

- `LoadItemsCommand_PopulatesFilterCategoriesWithAllItemsSentinel` — after load, `FilterCategories[0]` is the "All Items" sentinel and the real categories follow
- `LoadItemsCommand_SelectedCategoryItemDefaultsToAllItems` — `SelectedCategoryItem` is the AllItems sentinel after initial load
- `SelectingCategory_UpdatesSelectedCategorySlug` — setting `SelectedCategoryItem` to a real category sets `SelectedCategory` to its slug
- `SelectingAllItems_ClearsSelectedCategory` — setting `SelectedCategoryItem` back to AllItems sets `SelectedCategory` to null
- `CategoryChange_AfterLoad_TriggersReload` — changing `SelectedCategoryItem` after initial load triggers a second service call (mirrors the existing `RadiusChange_AfterFirstLoad_TriggersReload` pattern)
- `LoadItems_DoesNotTriggerExtraReload_WhenRestoringCategory` — calling `LoadItemsCommand` twice only calls the service twice, not more (guard flag prevents restore from looping)

### `NearbyItemsViewModelTests` additions

Same set of tests as above, scoped to `NearbyItemsViewModel` and using `GetNearbyItemsAsync` instead of `GetItemsAsync`.
