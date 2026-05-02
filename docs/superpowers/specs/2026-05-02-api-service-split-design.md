# Design: Split IApiService into Domain Services

**Date:** 2026-05-02
**Branch:** refactor/split-api-to-services

---

## Overview

`IApiService` is a monolithic interface covering authentication, items, rentals, and reviews. This refactor splits it into four focused domain service interfaces, each with a remote (HTTP) and local (database) implementation. `IAuthenticationService` is removed entirely; its orchestration logic is distributed into the appropriate ViewModels. `AuthTokenState` becomes the single source of truth for session state and owns the `AuthenticationStateChanged` event.

---

## 1. Service Interface Split

### Deleted
- `IApiService` — replaced by four domain interfaces
- `RemoteApiService` — replaced by four `Remote*Service` classes
- `LocalApiService` — replaced by four `Local*Service` classes
- `IAuthenticationService` — removed entirely
- `AuthenticationService` — removed entirely
- `AuthenticationResult` — removed (only used by `AuthenticationService`)

### New Interfaces and Implementations

#### `IAuthService`
```
LoginAsync(LoginRequest) → LoginResponse
RegisterAsync(RegisterRequest) → RegisterResponse
GetCurrentUserAsync() → CurrentUserResponse
GetUserProfileAsync(int userId) → UserProfileResponse
```
Implementations: `RemoteAuthService` (uses `IApiClient`), `LocalAuthService` (uses `AppDbContext`, `AuthTokenState`)

#### `IItemService`
```
GetItemsAsync(GetItemsRequest) → ItemsResponse
GetNearbyItemsAsync(GetNearbyItemsRequest) → NearbyItemsResponse
GetItemAsync(int id) → ItemDetailResponse
CreateItemAsync(CreateItemRequest) → CreateItemResponse
UpdateItemAsync(int id, UpdateItemRequest) → UpdateItemResponse
GetCategoriesAsync() → CategoriesResponse
```
Implementations: `RemoteItemService` (uses `IApiClient`), `LocalItemService` (uses `AppDbContext`, `IItemRepository`, `ICategoryRepository`)

#### `IRentalService`
```
GetIncomingRentalsAsync(GetRentalsRequest) → RentalsListResponse
GetOutgoingRentalsAsync(GetRentalsRequest) → RentalsListResponse
GetRentalAsync(int id) → RentalDetailResponse
CreateRentalAsync(CreateRentalRequest) → RentalSummaryResponse
UpdateRentalStatusAsync(int id, UpdateRentalStatusRequest) → UpdateRentalStatusResponse
```
Implementations: `RemoteRentalService` (uses `IApiClient`), `LocalRentalService` (uses `AppDbContext`, throws `NotImplementedException`)

#### `IReviewService`
```
GetItemReviewsAsync(int itemId, GetReviewsRequest) → ReviewsResponse
GetUserReviewsAsync(int userId, GetReviewsRequest) → ReviewsResponse
CreateReviewAsync(CreateReviewRequest) → CreateReviewResponse
```
Implementations: `RemoteReviewService` (uses `IApiClient`), `LocalReviewService` (uses `AppDbContext`, throws `NotImplementedException`)

All new interfaces and implementations live in `RentalApp/Services/`.

---

## 2. `AuthTokenState` Changes

`AuthTokenState` gains the `AuthenticationStateChanged` event, moved from `IAuthenticationService`. The event fires `true` when a token is assigned, `false` when cleared.

```csharp
public event EventHandler<bool>? AuthenticationStateChanged;

public string? CurrentToken
{
    get => _token;
    set
    {
        _token = value;
        AuthenticationStateChanged?.Invoke(this, value is not null);
    }
}

public void ClearToken() => CurrentToken = null;
```

`AuthTokenState` remains a Singleton in DI. Any component needing session change notifications subscribes to `AuthTokenState.AuthenticationStateChanged` directly.

---

## 3. ViewModel Changes

### `LoginViewModel`
- **Replaces:** `IAuthenticationService` injection
- **Gains:** `IAuthService`, `AuthTokenState`, `ICredentialStore`
- `LoginAsync()`: calls `IAuthService.LoginAsync()` → sets `AuthTokenState.CurrentToken` → conditionally saves credentials via `ICredentialStore` based on `RememberMe`
- `InitializeAsync()`: reads saved credentials from `ICredentialStore` directly (behaviour unchanged)

### `LoadingViewModel`
- **Replaces:** `IAuthenticationService` injection
- **Gains:** `ICredentialStore`, `IAuthService`, `AuthTokenState`
- On startup: reads `ICredentialStore` for saved credentials → if found, calls `IAuthService.LoginAsync()` → sets `AuthTokenState.CurrentToken` → navigates to `Routes.Main`
- If no credentials or login fails: navigates to `Routes.Login`

### `RegisterViewModel`
- **Replaces:** `IAuthenticationService` injection
- **Gains:** `IAuthService`
- Calls `IAuthService.RegisterAsync()` directly. No other behavioural changes.

### `AppShellViewModel`
- **Replaces:** `IAuthenticationService` injection
- **Gains:** `AuthTokenState`, `ICredentialStore`
- `LogoutAsync()` is updated to: show confirmation alert → if confirmed, clear saved credentials via `ICredentialStore` and call `AuthTokenState.ClearToken()` (which fires `AuthenticationStateChanged`) → navigate to `Routes.Login`
- Alert logic previously in `MainViewModel.LogoutAsync()` is absorbed here

### `MainViewModel`
- **Replaces:** `IAuthenticationService` injection
- **Gains:** `IAuthService`
- `LogoutAsync()` command is **deleted** (logout now lives in `AppShellViewModel`)
- `LoadUserData()` calls `IAuthService.GetCurrentUserAsync()` directly
- The logout toolbar item moves from `MainPage.xaml` to `AppShell.xaml`, where it binds naturally to `AppShellViewModel.LogoutCommand`

---

## 4. `MauiProgram.cs` Changes

The single `IApiService` conditional block is replaced by four conditional blocks, one per domain service. The `Preferences.Get("UseSharedApi", true)` flag drives both paths as before.

**Remote path:**
```
IAuthService   → RemoteAuthService   (IApiClient)
IItemService   → RemoteItemService   (IApiClient)
IRentalService → RemoteRentalService (IApiClient)
IReviewService → RemoteReviewService (IApiClient)
```

**Local path:**
```
IAuthService   → LocalAuthService   (AppDbContext, AuthTokenState)
IItemService   → LocalItemService   (AppDbContext, IItemRepository, ICategoryRepository)
IRentalService → LocalRentalService (AppDbContext)
IReviewService → LocalReviewService (AppDbContext)
```

`IAuthenticationService` registration is removed. Constructor parameters updated for:
- `AppShellViewModel`: gains `AuthTokenState`, `ICredentialStore`
- `LoginViewModel`: gains `IAuthService`, `AuthTokenState`, `ICredentialStore`
- `LoadingViewModel`: gains `IAuthService`, `AuthTokenState`, `ICredentialStore`
- `RegisterViewModel`: gains `IAuthService`
- `MainViewModel`: gains `IAuthService`

---

## 5. Test Changes

### Deleted
- `AuthenticationServiceTests.cs`

### Updated

| File | Change |
|------|--------|
| `LoginViewModelTests.cs` | Replace `IAuthenticationService` mock with `IAuthService`, `AuthTokenState`, `ICredentialStore` mocks. Add tests: token set on `AuthTokenState` after login; credentials saved when `RememberMe = true`; credentials not saved when `RememberMe = false`. |
| `LoadingViewModelTests.cs` | Replace `IAuthenticationService` mock with `ICredentialStore`, `IAuthService`, `AuthTokenState`. Tests: saved credentials trigger auto-login and token set; no credentials navigates to login; failed login navigates to login. |
| `MainViewModelTests.cs` | Replace `IAuthenticationService` mock with `IAuthService`. Remove `LogoutAsync` tests. Add test: `LoadUserData` calls `GetCurrentUserAsync()`. |
| `AppShellViewModelTests.cs` | Replace `IAuthenticationService` mock with `AuthTokenState`, `ICredentialStore`. Add tests: `LogoutAsync` shows alert; on confirm, token cleared and credentials cleared and navigation to login triggered; on cancel, nothing happens. |
| `RegisterViewModelTests.cs` | Replace `IAuthenticationService` mock with `IAuthService`. |

### New Test Files

| File | Tests |
|------|-------|
| `RemoteAuthServiceTests.cs` | HTTP calls for login, register, get current user, get user profile |
| `LocalAuthServiceTests.cs` | DB-backed login (BCrypt verify), register (BCrypt hash), get current user by token |
| `RemoteItemServiceTests.cs` | HTTP calls for all item and category endpoints |
| `LocalItemServiceTests.cs` | DB-backed item queries, nearby items (PostGIS), category listing |
| `RemoteRentalServiceTests.cs` | HTTP calls for all rental endpoints |
| `LocalRentalServiceTests.cs` | Confirms `NotImplementedException` for all methods |
| `RemoteReviewServiceTests.cs` | HTTP calls for all review endpoints |
| `LocalReviewServiceTests.cs` | Confirms `NotImplementedException` for all methods |

New service tests follow the same XUnit + NSubstitute pattern as the existing `RemoteApiServiceTests` and `LocalApiServiceTests`.

---

## Constraints

- `RentalApp.Database` does not reference `RentalApp.Contracts` — `Local*Service` classes must continue mapping DB models to contract types internally.
- `UseNetTopologySuite()` must remain on EF options for PostGIS support.
- All ViewModels that previously depended on `IAuthenticationService.CurrentUser` must call `IAuthService.GetCurrentUserAsync()` instead — there is no shared user cache.
- `LocalRentalService` and `LocalReviewService` throw `NotImplementedException` (DB entities not yet implemented), matching the behaviour of the current `LocalApiService`.
