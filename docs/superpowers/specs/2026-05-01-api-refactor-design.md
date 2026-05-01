# API Service & Data Transfer Refactor — Design Spec

**Date:** 2026-05-01
**Branch:** feature/refactor-data-transfer-operations
**Status:** Approved for implementation

---

## Problem Statement

Four pain points in the current service layer:

1. **Local/Remote API misalignment** — `LocalApiService` hardcodes zeros for `AverageRating`, `ItemsListed`, and `RentalsCompleted`, always returns `Reviews = null`, and manages session state differently to `RemoteApiService`.
2. **Duplicated data shape declarations** — `RemoteApiService` defines 13 private sealed records (`ItemListDto`, `NearbyItemDto`, `ItemDetailDto`, etc.). The shared `Item` application model has 18 fields, most nullable, to serve four different endpoint shapes.
3. **`LogoutAsync` on the wrong abstraction** — `IApiService.LogoutAsync()` only exists to let each implementation clear its own private session state. There is no `/auth/logout` endpoint in the API — the API is stateless JWT. This is an operational concern masquerading as a data transfer operation.
4. **`IItemService` misaligned with standards** — compared to `IAuthenticationService` (state management, events, result types), `IItemService` is a thin validation wrapper that throws `ArgumentException` directly and does not follow the established service pattern.

---

## Goals

- `IApiService` is the single abstraction for all data operations. ViewModels and services have no knowledge of whether they are talking to a remote API or a local database.
- `IApiService` response types are derived directly from the OpenAPI spec (`openapi.json`).
- `LocalApiService` and `RemoteApiService` are fully interchangeable — same method signatures, same return types.
- Session state is unified — a single mechanism works for both implementations.
- Item validation follows the same pattern as registration validation.

---

## Non-Goals

- Implementing rental or review support in `LocalApiService` — the local DB does not yet have `Rental` or `Review` entities. These are declared on `IApiService` now and will be implemented when those entities land.
- Changes to the MAUI UI layer (Views, XAML) — this refactor is service and ViewModel-layer only.
- Auto-generating types from `openapi.json` — types are hand-authored for control over naming and MVVM binding compatibility.

---

## Solution Structure

A fourth project is introduced. All other projects keep their place.

```
RentalApp.sln
├── RentalApp              (MAUI UI — ViewModels, Views, Services, Http)
├── RentalApp.Contracts    (NEW — request/response records, zero dependencies)
├── RentalApp.Database     (EF Core entities, repositories, AppDbContext)
└── RentalApp.Migrations   (EF migrations only — unchanged)
```

**Project references:**
- `RentalApp` → `RentalApp.Contracts`
- `RentalApp.Database` → `RentalApp.Contracts`
- `RentalApp.Migrations` → no change

`RentalApp/Models/` is deleted in its entirety. ViewModels bind directly to `Contracts` response types.

---

## `RentalApp.Contracts`

A dependency-free class library of `record` types. Every endpoint in the OpenAPI spec has a corresponding request and response record. No EF Core, no MAUI, no HTTP references.

### Requests

```csharp
// Auth
record LoginRequest(string Email, string Password);
record RegisterRequest(string FirstName, string LastName, string Email, string Password);

// Items
record GetItemsRequest(string? Category = null, string? Search = null, int Page = 1, int PageSize = 20);
record GetNearbyItemsRequest(double Lat, double Lon, double Radius = 5.0, string? Category = null);
record CreateItemRequest(string Title, string? Description, double DailyRate, int CategoryId, double Latitude, double Longitude);
record UpdateItemRequest(string? Title, string? Description, double? DailyRate, bool? IsAvailable);

// Rentals
record GetRentalsRequest(string? Status = null);
record CreateRentalRequest(int ItemId, DateOnly StartDate, DateOnly EndDate);
record UpdateRentalStatusRequest(string Status);

// Reviews
record GetReviewsRequest(int Page = 1, int PageSize = 10);
record CreateReviewRequest(int RentalId, int Rating, string? Comment);
```

### Responses

Each record maps to exactly one API endpoint shape — no nullable fields serving multiple contexts.

```csharp
// Auth
record LoginResponse(string Token, DateTime ExpiresAt, int UserId);
record RegisterResponse(int Id, string Email, string FirstName, string LastName, DateTime CreatedAt);

// Users
record CurrentUserResponse(int Id, string Email, string FirstName, string LastName,
    double? AverageRating, int ItemsListed, int RentalsCompleted, DateTime CreatedAt);
record UserProfileResponse(int Id, string FirstName, string LastName,
    double? AverageRating, int ItemsListed, int RentalsCompleted, List<ReviewResponse> Reviews);

// Items
record ItemsResponse(List<ItemSummaryResponse> Items, int TotalItems, int Page, int PageSize, int TotalPages);
record ItemSummaryResponse(int Id, string Title, string? Description, double DailyRate,
    int CategoryId, string Category, int OwnerId, string OwnerName,
    double? OwnerRating, bool IsAvailable, double? AverageRating, DateTime CreatedAt);

record NearbyItemsResponse(List<NearbyItemResponse> Items, SearchLocationResponse SearchLocation, double Radius, int TotalResults);
record NearbyItemResponse(int Id, string Title, string? Description, double DailyRate,
    int CategoryId, string Category, int OwnerId, string OwnerName,
    double Latitude, double Longitude, double Distance, bool IsAvailable, double? AverageRating);

record ItemDetailResponse(int Id, string Title, string? Description, double DailyRate,
    int CategoryId, string Category, int OwnerId, string OwnerName,
    double? OwnerRating, double? Latitude, double? Longitude, bool IsAvailable,
    double? AverageRating, int TotalReviews, DateTime CreatedAt, List<ItemReviewResponse> Reviews);
record CreateItemResponse(int Id, string Title, string? Description, double DailyRate,
    int CategoryId, string Category, int OwnerId, string OwnerName,
    double Latitude, double Longitude, bool IsAvailable, DateTime CreatedAt);
record UpdateItemResponse(int Id, string Title, string? Description, double DailyRate, bool IsAvailable);

// Categories
record CategoriesResponse(List<CategoryResponse> Categories);
record CategoryResponse(int Id, string Name, string Slug, int ItemCount);

// Rentals
record RentalsListResponse(List<RentalSummaryResponse> Rentals, int TotalRentals);
record RentalSummaryResponse(int Id, int ItemId, string ItemTitle, int BorrowerId,
    string BorrowerName, int OwnerId, string OwnerName,
    DateOnly StartDate, DateOnly EndDate, string Status, double TotalPrice, DateTime CreatedAt);
record RentalDetailResponse(int Id, int ItemId, string ItemTitle, string? ItemDescription,
    int BorrowerId, string BorrowerName, int OwnerId, string OwnerName,
    DateOnly StartDate, DateOnly EndDate, string Status, double TotalPrice, DateTime RequestedAt);
record UpdateRentalStatusResponse(int Id, string Status, DateTime UpdatedAt);

// Reviews
record ReviewResponse(int Id, int Rating, string? Comment, string ReviewerName, DateTime CreatedAt);
record ItemReviewResponse(int Id, int ReviewerId, string ReviewerName, int Rating, string? Comment, DateTime CreatedAt);
record ReviewsResponse(List<ReviewResponse> Reviews, double? AverageRating, int TotalReviews,
    int Page, int PageSize, int TotalPages);
record CreateReviewResponse(int Id, int RentalId, int ReviewerId, string ReviewerName,
    int Rating, string? Comment, DateTime CreatedAt);

// Shared
record SearchLocationResponse(double Latitude, double Longitude);
```

---

## `IApiService`

One method per API operation. Typed request in, typed response out.

**Convention:** operations with domain-specific optional filters or body fields use a request object. Operations with only a single path parameter (`id`, `userId`) pass it as a raw `int` — a request wrapper for a single ID adds no clarity.

```csharp
public interface IApiService
{
    // Auth
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<CurrentUserResponse> GetCurrentUserAsync();
    Task<UserProfileResponse> GetUserProfileAsync(int userId);

    // Items
    Task<ItemsResponse> GetItemsAsync(GetItemsRequest request);
    Task<NearbyItemsResponse> GetNearbyItemsAsync(GetNearbyItemsRequest request);
    Task<ItemDetailResponse> GetItemAsync(int id);
    Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request);
    Task<UpdateItemResponse> UpdateItemAsync(int id, UpdateItemRequest request);

    // Categories
    Task<CategoriesResponse> GetCategoriesAsync();

    // Rentals
    Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request);
    Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request);
    Task<RentalDetailResponse> GetRentalAsync(int id);
    Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request);
    Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(int id, UpdateRentalStatusRequest request);

    // Reviews
    Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request);
    Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request);
    Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request);
}
```

`LogoutAsync` is not on `IApiService` — it was never a data transfer operation and has no corresponding API endpoint.

---

## Session State — Unified `AuthTokenState`

`AuthTokenState` is the single session holder for both `RemoteApiService` and `LocalApiService`.

```csharp
public class AuthTokenState
{
    public string? CurrentToken { get; set; }
    public bool HasSession => CurrentToken is not null;
}
```

### Login flow

`RemoteApiService.LoginAsync()` returns a real JWT token from the API. `LocalApiService.LoginAsync()` verifies the BCrypt hash, then returns the authenticated user's ID as the token string. In both cases, `AuthenticationService` receives the `LoginResponse` and writes the token to `AuthTokenState`.

```
RemoteApiService → LoginResponse(Token: "eyJ...", ExpiresAt: actual,    UserId: 42)
LocalApiService  → LoginResponse(Token: "42",    ExpiresAt: MaxValue,   UserId: 42)
```

`AuthenticationService` always writes: `_tokenState.CurrentToken = response.Token`

### Token usage per class

| Class | Role |
|---|---|
| `AuthenticationService` | **Writes** — sets token after login, nulls it on logout |
| `ApiClient` | **Reads** — attaches as `Authorization: Bearer` header on every HTTP request |
| `LocalApiService` | **Reads** — parses as `int` to identify the current user for DB queries |
| `RemoteApiService` | No dependency — `ApiClient` handles token attachment transparently |

`RemoteApiService` no longer injects `AuthTokenState` directly.

### Logout

`LogoutAsync` is removed from `IApiService`. Session clearing is handled entirely in `AuthenticationService`:

```csharp
public async Task LogoutAsync()
{
    _tokenState.CurrentToken = null;
    await _credentialStore.ClearAsync();
    _currentUser = null;
    AuthenticationStateChanged?.Invoke(this, false);
}
```

`LocalApiService` becomes stateless — `_currentUser` field is removed. Every call that needs the current user parses the user ID from `AuthTokenState.CurrentToken` and queries the DB.

---

## `IItemService` Removal

`IItemService` and `ItemService` are deleted. ViewModels that currently inject `IItemService` inject `IApiService` directly.

### `ItemValidator` static helper

Follows the exact pattern of `RegistrationValidator` — a single static class, methods returning `string?` (first error) or `null` (valid).

```csharp
public static class ItemValidator
{
    public static string? ValidateCreate(string? title, string? description, double? dailyRate, int categoryId)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required";
        if (title.Length < 5)
            return "Title must be at least 5 characters";
        if (title.Length > 100)
            return "Title must be 100 characters or fewer";
        if (description?.Length > 1000)
            return "Description must be 1000 characters or fewer";
        if (dailyRate is null or <= 0)
            return "Daily rate must be greater than zero";
        if (dailyRate > 1000)
            return "Daily rate cannot exceed £1000";
        if (categoryId <= 0)
            return "A category must be selected";
        return null;
    }

    public static string? ValidateUpdate(string? title, string? description, double? dailyRate)
    {
        if (title is not null && string.IsNullOrWhiteSpace(title))
            return "Title is required";
        if (title is not null && title.Length < 5)
            return "Title must be at least 5 characters";
        if (title is not null && title.Length > 100)
            return "Title must be 100 characters or fewer";
        if (description is not null && description.Length > 1000)
            return "Description must be 1000 characters or fewer";
        if (dailyRate is not null and <= 0)
            return "Daily rate must be greater than zero";
        if (dailyRate > 1000)
            return "Daily rate cannot exceed £1000";
        return null;
    }
}
```

ViewModels call it via a private `ValidateForm()` method, identical in shape to `RegisterViewModel.ValidateForm()`:

```csharp
private bool ValidateForm()
{
    var error = ItemValidator.ValidateCreate(Title, Description, DailyRate, SelectedCategoryId);
    if (error is not null)
    {
        SetError(error);
        return false;
    }
    return true;
}
```

---

## Rental & Review Extension Path

Rental and review methods are declared on `IApiService` now. `LocalApiService` throws `NotImplementedException` for these until the corresponding DB entities land.

When rental/review support is added to `LocalApiService`:
1. Add `Rental` and `Review` EF entities to `RentalApp.Database/Models/`
2. Add `IRentalRepository`, `IReviewRepository` and their implementations
3. Add an EF migration
4. Implement the rental/review methods in `LocalApiService`, mapping entities to `Contracts` response types
5. Replace the hardcoded zeros in `GetCurrentUserAsync` and `GetUserProfileAsync` with real DB aggregates

---

## Deletions

| File / Type | Reason |
|---|---|
| `RentalApp/Models/Item.cs` | Replaced by endpoint-specific Contracts records |
| `RentalApp/Models/User.cs` | Replaced by `CurrentUserResponse` / `UserProfileResponse` |
| `RentalApp/Models/Review.cs` | Replaced by `ReviewResponse` / `ItemReviewResponse` / `CreateReviewResponse` |
| `RentalApp/Models/Category.cs` | Replaced by `CategoryResponse` |
| `RentalApp/Models/Rental.cs` | Replaced by `RentalSummaryResponse` / `RentalDetailResponse` |
| `RentalApp/Services/IItemService.cs` | Abolished — ViewModels inject `IApiService` directly |
| `RentalApp/Services/ItemService.cs` | Abolished — validation moves to `ItemValidator` |
| `IApiService.LogoutAsync()` | Not an API operation — removed from interface |
| `LocalApiService._currentUser` | Replaced by `AuthTokenState`-based session |
| `RemoteApiService._tokenState` | `RemoteApiService` no longer needs `AuthTokenState` directly |

---

## Summary of Changes by Layer

| Layer | Change |
|---|---|
| **New project** | `RentalApp.Contracts` — typed request/response records |
| **`IApiService`** | Typed request/response objects; `LogoutAsync` removed; rental/review methods added |
| **`RemoteApiService`** | Maps HTTP responses to Contracts types; loses `AuthTokenState` dependency |
| **`LocalApiService`** | Maps EF entities to Contracts types; loses `_currentUser` field; reads user ID from `AuthTokenState` |
| **`AuthTokenState`** | Adds `HasSession` property; owned by `AuthenticationService` (write) and `ApiClient`/`LocalApiService` (read) |
| **`AuthenticationService`** | Writes token after login; clears on logout — no longer delegates logout to `IApiService` |
| **`RentalApp/Models/`** | Deleted |
| **`IItemService` / `ItemService`** | Deleted |
| **`Helpers/ItemValidator`** | New static validator matching `RegistrationValidator` pattern |
| **ViewModels** | Inject `IApiService`; call `ItemValidator.ValidateCreate/Update` via `ValidateForm()` |
