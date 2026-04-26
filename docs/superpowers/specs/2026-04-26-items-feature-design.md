# Items Feature Design

**Date:** 2026-04-26
**Branch:** feature/implmenent-item-create-update-search
**Status:** Approved — pending implementation

---

## Overview

Add item listing, viewing, creation, nearby search, and owner-editing to the RentalApp. Follows the existing MVVM + CommunityToolkit.Mvvm architecture, mirrors the authentication service pattern, and introduces a repository layer for local DB access.

---

## Architecture

```
Views (XAML)
  ItemsListPage · ItemDetailsPage · CreateItemPage · NearbyItemsPage
  MainPage (new nav buttons)
        │ binds to
ViewModels
  ItemsListViewModel · ItemDetailsViewModel
  CreateItemViewModel · NearbyItemsViewModel
  MainViewModel (4 new RelayCommands)
        │
  IItemService          ILocationService
  ItemService           LocationService
        │                     │
  IApiService           MAUI Geolocation API
  ├── RemoteApiService → HTTP API
  └── LocalApiService  → ItemRepository ──▶ AppDbContext (PostGIS)
                         (maps DB models    (returns DB entities)
                          to DTOs)
```

### Dependency direction

ViewModels depend on `IItemService` and `ILocationService`. `ItemService` depends on `IApiService`. `LocalApiService` delegates raw data access to `ItemRepository`, then maps the returned EF entities to `RentalApp.Models` DTOs before returning them. ViewModels never touch `IApiService` directly.

### Repository responsibility boundary

`ItemRepository` returns `RentalApp.Database.Models` EF entities — never DTOs. `LocalApiService` owns DTO construction and may combine results from multiple repositories. For example, when reviews are implemented, `LocalApiService.GetItemAsync` will call both `ItemRepository` and `ReviewRepository`, then assemble the full `RentalApp.Models.Item` DTO from both result sets.

---

## Components

### `IItemService` / `ItemService`

Domain service — mirrors the role of `IAuthenticationService` for the items domain.

```csharp
Task<List<Item>> GetItemsAsync(string? category, string? search, int page, int pageSize, int? ownerId = null);
Task<List<Item>> GetNearbyItemsAsync(double lat, double lon, double radius, string? category, int page, int pageSize);
Task<Item> GetItemAsync(int id);
Task<Item> CreateItemAsync(string title, string? description, double dailyRate, int categoryId, double lat, double lon);
Task<Item> UpdateItemAsync(int id, string? title, string? description, double? dailyRate, bool? isAvailable);
Task<List<Category>> GetCategoriesAsync();
```

**Validation rules (enforced before forwarding to `IApiService`):**
- `title`: 5–100 characters
- `description`: max 1000 characters (nullable)
- `dailyRate`: > 0 and ≤ 1000
- `categoryId`: > 0
- `page`: ≥ 1
- `pageSize`: 1–100

Throws `ArgumentException` on violation. All other exceptions propagate from `IApiService`.

### `IApiService` changes

`GetNearbyItemsAsync` gains `page` and `pageSize` parameters to match `GetItemsAsync`:

```csharp
Task<List<Item>> GetNearbyItemsAsync(
    double lat, double lon, double radius = 5.0,
    string? category = null, int page = 1, int pageSize = 20);
```

`GetItemsAsync` gains explicit `pageSize` and `ownerId` parameters:

```csharp
Task<List<Item>> GetItemsAsync(
    string? category = null, string? search = null,
    int page = 1, int pageSize = 20, int? ownerId = null);
```

### `ILocationService` / `LocationService`

GPS abstraction over MAUI's `Geolocation.Default`.

```csharp
Task<(double Lat, double Lon)> GetCurrentLocationAsync();
bool IsLocationAvailable { get; }
```

`GetCurrentLocationAsync` requests permission if needed, then fetches a single fix. Throws `InvalidOperationException` if permission is denied or location is unavailable.

### `ItemRepository`

Local database access layer. Used exclusively by `LocalApiService`. Returns `RentalApp.Database.Models.Item` EF entities — DTO mapping is not its responsibility.

Nearby search uses EF Core spatial (PostGIS via `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`):

```csharp
var origin = new Point(lon, lat) { SRID = 4326 };
var radiusMeters = radiusKm * 1000;
context.Items
    .Where(i => i.Location.IsWithinDistance(origin, radiusMeters))
    .OrderBy(i => i.Location.Distance(origin))
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
```

`GetItemsAsync` applies the same `.Skip((page - 1) * pageSize).Take(pageSize)` pattern, mimicking the remote API's pagination behaviour against the local database.

**Signatures:**

```csharp
Task<IEnumerable<Database.Models.Item>> GetItemsAsync(string? category, string? search, int page, int pageSize, int? ownerId = null);
Task<IEnumerable<Database.Models.Item>> GetNearbyItemsAsync(Point origin, double radiusMeters, string? category, int page, int pageSize);
Task<Database.Models.Item?> GetItemAsync(int id);
Task<Database.Models.Item> CreateItemAsync(string title, string? description, double dailyRate, int categoryId, Point location);
Task<Database.Models.Item> UpdateItemAsync(int id, string? title, string? description, double? dailyRate, bool? isAvailable);
Task<IEnumerable<Database.Models.Category>> GetCategoriesAsync();
```

`LocalApiService` maps `Point.Y → Latitude`, `Point.X → Longitude` when constructing `RentalApp.Models.Item` DTOs.

### DB Model changes

- `RentalApp.Database/Models/Item.cs`: Replace `Latitude`/`Longitude` doubles with `NetTopologySuite.Geometries.Point Location` (SRID 4326, `geography(Point, 4326)` column)
- `RentalApp.Database/Models/Item.cs`: Add `bool IsAvailable` property (currently missing)
- `AppDbContext.OnConfiguring`: Add `.UseNetTopologySuite()` to Npgsql options
- New migration: Enable `CREATE EXTENSION IF NOT EXISTS postgis`, drop old lat/lon columns, add `location geography(Point,4326)` column

**New NuGet packages (RentalApp.Database):**
- `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`
- `NetTopologySuite`

---

## Pages & ViewModels

### `ItemsListPage` / `ItemsListViewModel` (Transient)

Displays a filterable, searchable, paginated list of all available items.

**Shell query parameter:** `ownerId` (int, optional) — when present, filters items to only those owned by the specified user (used by `NavigateToMyListingsCommand` on the dashboard).

**Properties:** `ObservableCollection<Item> Items`, `List<Category> Categories`, `string? SelectedCategory`, `string SearchText`, `bool IsEmpty`, `int CurrentPage`, `bool HasMorePages`, `int? OwnerFilter`

**Commands:**
- `LoadItemsCommand` — loads page 1; resets `Items` and `CurrentPage`; called on page appear and when search/filter changes; applies `OwnerFilter` if set
- `LoadMoreItemsCommand` — increments `CurrentPage`, appends next page to `Items`; disabled when `!HasMorePages`
- `NavigateToItemCommand(Item)` — navigates to `ItemDetailsPage` passing `itemId`
- `NavigateToCreateItemCommand` — navigates to `CreateItemPage`

`HasMorePages` is set to `false` when a page returns fewer items than `pageSize` (fixed at 20).

### `ItemDetailsPage` / `ItemDetailsViewModel` (Transient)

Displays full item details. Owners see an inline edit mode.

**Shell query parameter:** `itemId` (int)

**Properties:** `Item? CurrentItem`, `bool IsOwner`, `bool IsEditing`, `string EditTitle`, `string EditDescription`, `string EditDailyRate`, `bool EditIsAvailable`

**Commands:**
- `LoadItemCommand` — fetches item by ID on page appear
- `ToggleEditCommand` — flips `IsEditing`; pre-populates edit fields from `CurrentItem`; owner-only
- `SaveChangesCommand` — calls `IItemService.UpdateItemAsync`, refreshes `CurrentItem`, exits edit mode
- `CancelEditCommand` — exits edit mode without saving

`IsOwner` is determined by comparing `CurrentItem.OwnerId` with `IAuthenticationService.CurrentUser?.Id`.

Edit fields are separate observable string properties (not bound directly to the immutable `Item` record).

### `CreateItemPage` / `CreateItemViewModel` (Transient)

Form for creating a new item listing.

**Properties:** `string Title`, `string Description`, `string DailyRate`, `List<Category> Categories`, `Category? SelectedCategory`

**Commands:**
- `LoadCategoriesCommand` — called on page appear
- `CreateItemCommand` — calls `ILocationService.GetCurrentLocationAsync()`, then `IItemService.CreateItemAsync`; navigates back on success

Location is always sourced from GPS — no manual coordinate entry.

### `NearbyItemsPage` / `NearbyItemsViewModel` (Transient)

Displays a paginated list of items within a user-adjustable radius of the device's current GPS position.

**Properties:** `ObservableCollection<Item> Items`, `double Radius` (default 5.0, range 1–50 km), `List<Category> Categories`, `string? SelectedCategory`, `bool IsEmpty`, `int CurrentPage`, `bool HasMorePages`

**Commands:**
- `LoadNearbyItemsCommand` — fetches current GPS location, caches it in `_cachedLat`/`_cachedLon`, resets to page 1, calls `IItemService.GetNearbyItemsAsync`. GPS is fetched fresh on every full reload only.
- `LoadMoreItemsCommand` — increments `CurrentPage`, appends next page using the cached GPS coordinates; disabled when `!HasMorePages`
- `NavigateToItemCommand(Item)` — navigates to `ItemDetailsPage`

Changing `Radius` or `SelectedCategory` triggers `LoadNearbyItemsCommand` (resets to page 1, re-fetches GPS).

### `MainViewModel` additions

Four new `RelayCommand`s navigating to item pages:
- `NavigateToItemsListCommand` → `Routes.ItemsList`
- `NavigateToNearbyItemsCommand` → `Routes.NearbyItems`
- `NavigateToCreateItemCommand` → `Routes.CreateItem`
- `NavigateToMyListingsCommand` → `Routes.ItemsList` with current user ID as query parameter

---

## Navigation

New constants added to `Constants/Routes.cs`:

```csharp
public const string ItemsList   = "ItemsListPage";
public const string ItemDetails = "ItemDetailsPage";
public const string CreateItem  = "CreateItemPage";
public const string NearbyItems = "NearbyItemsPage";
```

All new pages registered as Transient in `MauiProgram.cs`. All new ViewModels registered as Transient.

New services registered:
- `IItemService` → `ItemService` (Transient)
- `ILocationService` → `LocationService` (Singleton — stateless GPS wrapper)

---

## Error Handling

All ViewModels follow the `BaseViewModel` pattern: catch in command handler → `SetError(message)` → `IsBusy = false` in `finally`.

| Source | Exception | User-facing message |
|--------|-----------|---------------------|
| `LocationService` — permission denied / GPS off | `InvalidOperationException` | "Location unavailable. Please enable GPS and try again." |
| `ItemService` — validation failure | `ArgumentException` | `ex.Message` (validation messages are user-readable) |
| `IApiService` — network/DB failure | `HttpRequestException` / `InvalidOperationException` | `ex.Message` |

`NearbyItemsViewModel` guards against concurrent calls by checking `IsBusy` before executing `LoadNearbyItemsCommand`.

---

## Testing

| Test file | Type | Covers |
|-----------|------|--------|
| `Repositories/ItemRepositoryTests.cs` | Integration (real DB) | CRUD operations, PostGIS nearby spatial query, pagination (Skip/Take) |
| `Services/LocalApiServiceTests.cs` | Integration (real DB) | Item methods delegating to `ItemRepository`; DTO mapping from DB entities |
| `Services/ItemServiceTests.cs` | Unit (mock `IApiService`) | All validation rules — boundary values for title length, rate range, page/pageSize |
| `Services/LocationServiceTests.cs` | Unit | Permission-denied path, GPS unavailable path |
| `ViewModels/ItemsListViewModelTests.cs` | Unit (mock `IItemService`) | Load, category filter, search, empty state, load-more appending pages |
| `ViewModels/ItemDetailsViewModelTests.cs` | Unit | `IsOwner` determination, edit/save/cancel flow, load failure |
| `ViewModels/CreateItemViewModelTests.cs` | Unit | Validation error display, GPS error display, success navigation |
| `ViewModels/NearbyItemsViewModelTests.cs` | Unit | GPS error handling, radius changes triggering reload, load-more |

`DatabaseFixture` extended with:
- `ResetItemsAsync()` — truncates items and categories, re-seeds
- Seed data: 2 categories, 3 items at known coordinates for spatial query assertions and pagination boundary tests

---

## Files Created / Modified

### New files
```
RentalApp/Services/IItemService.cs
RentalApp/Services/ItemService.cs
RentalApp/Services/ILocationService.cs
RentalApp/Services/LocationService.cs
RentalApp/Services/ItemRepository.cs
RentalApp/ViewModels/ItemsListViewModel.cs
RentalApp/ViewModels/ItemDetailsViewModel.cs
RentalApp/ViewModels/CreateItemViewModel.cs
RentalApp/ViewModels/NearbyItemsViewModel.cs
RentalApp/Views/ItemsListPage.xaml + .cs
RentalApp/Views/ItemDetailsPage.xaml + .cs
RentalApp/Views/CreateItemPage.xaml + .cs
RentalApp/Views/NearbyItemsPage.xaml + .cs
RentalApp.Migrations/Migrations/<timestamp>_AddPostGISItemLocation.cs
RentalApp.Test/Repositories/ItemRepositoryTests.cs
RentalApp.Test/Services/ItemServiceTests.cs
RentalApp.Test/Services/LocationServiceTests.cs
RentalApp.Test/ViewModels/ItemsListViewModelTests.cs
RentalApp.Test/ViewModels/ItemDetailsViewModelTests.cs
RentalApp.Test/ViewModels/CreateItemViewModelTests.cs
RentalApp.Test/ViewModels/NearbyItemsViewModelTests.cs
```

### Modified files
```
RentalApp/Services/IApiService.cs            — pageSize param on GetItemsAsync; page + pageSize on GetNearbyItemsAsync
RentalApp/Services/RemoteApiService.cs       — implement item methods
RentalApp/Services/LocalApiService.cs        — delegate to ItemRepository; map DB entities to DTOs
RentalApp/ViewModels/MainViewModel.cs        — 4 new nav RelayCommands
RentalApp/Constants/Routes.cs               — 4 new route constants
RentalApp/MauiProgram.cs                    — register new services, VMs, pages
RentalApp.Database/Models/Item.cs           — Point Location replaces Lat/Lon; add IsAvailable
RentalApp.Database/Data/AppDbContext.cs     — UseNetTopologySuite()
RentalApp.Test/Fixtures/DatabaseFixture.cs  — ResetItemsAsync + item/category seed data
```
