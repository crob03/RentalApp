# User Profile Page — Design Spec

**Date:** 2026-05-04
**Status:** Approved

---

## Overview

Implement a `UserProfilePage` that displays a user's name, stats (average rating, items listed, rentals completed), and a paginated list of reviews they have received. The page is accessible from two entry points:

1. The profile toolbar icon on any authenticated page — shows the current user's own profile (including email).
2. Tapping the owner name on `ItemDetailsPage` — shows another user's public profile (no email).

Both entry points use the same page and ViewModel. The distinction is made via a `userId` query parameter: absent or `0` means "own profile"; a positive integer means another user's profile.

---

## Architecture

### New files

| File | Purpose |
|------|---------|
| `RentalApp/ViewModels/UserProfileViewModel.cs` | ViewModel extending `ReviewsViewModel` |
| `RentalApp/Views/UserProfilePage.xaml` | XAML view |
| `RentalApp/Views/UserProfilePage.xaml.cs` | Code-behind |
| `RentalApp.Test/ViewModels/UserProfileViewModelTests.cs` | Integration tests |

### Modified files

| File | Change |
|------|--------|
| `RentalApp/Constants/Routes.cs` | Add `UserProfile = "UserProfilePage"` |
| `RentalApp/AppShell.xaml.cs` | Register `UserProfilePage` route |
| `RentalApp/MauiProgram.cs` | Register `UserProfileViewModel` and `UserProfilePage` as Transient |
| `RentalApp/ViewModels/AuthenticatedViewModel.cs` | Update `NavigateToProfileAsync` to navigate to `Routes.UserProfile` |
| `RentalApp/ViewModels/ItemDetailsViewModel.cs` | Add `ViewOwnerProfileCommand` |
| `RentalApp/Views/ItemDetailsPage.xaml` | Make owner name tappable, bind to `ViewOwnerProfileCommand` |

---

## UserProfileViewModel

### Inheritance

```
BaseViewModel
  └── AuthenticatedViewModel
        └── ReviewsViewModel
              └── UserProfileViewModel  ← new
```

### Constructor dependencies

- `IAuthService authService`
- `IReviewService reviewService`
- `AuthTokenState tokenState` (passed to base)
- `ICredentialStore credentialStore` (passed to base)
- `INavigationService navigationService` (passed to base)

### Fields

| Field | Type | Purpose |
|-------|------|---------|
| `_userId` | `int` | Set by `ApplyQueryAttributes`; defaults to `0` (self mode) |
| `_resolvedUserId` | `int` | Set during load; equals `currentUser.Id` in self mode, `_userId` otherwise. Used by `FetchReviewsAsync`. |

### Observable properties

| Property | Type | Notes |
|----------|------|-------|
| `DisplayName` | `string` | Full name: `$"{FirstName} {LastName}"` |
| `Email` | `string?` | Populated only in self mode; `null` for other users |
| `ItemsListed` | `int` | From profile response |
| `RentalsCompleted` | `int` | From profile response |

`AverageRating` and `TotalReviews` are inherited from `ReviewsViewModel` and populated by `LoadReviewsCommand`.

### Computed property

```csharp
public bool ShowEmail => Email != null;
```

Controls visibility of the email row in the view.

### IQueryAttributable

```csharp
public void ApplyQueryAttributes(IDictionary<string, object> query)
{
    if (query.TryGetValue("userId", out var id))
        _userId = Convert.ToInt32(id);
}
```

### LoadProfileCommand

Wraps in `RunAsync`. Branches on `_userId`:

**Self mode (`_userId <= 0`):**
- Calls `IAuthService.GetCurrentUserAsync()`
- Sets `_resolvedUserId = response.Id`
- Sets `Email = response.Email`
- Populates `DisplayName`, `ItemsListed`, `RentalsCompleted`, `AverageRating`

**Other user (`_userId > 0`):**
- Calls `IAuthService.GetUserProfileAsync(_userId)`
- Sets `_resolvedUserId = _userId`
- Leaves `Email = null`
- Populates `DisplayName`, `ItemsListed`, `RentalsCompleted`, `AverageRating`

Both branches fire `_ = LoadReviewsCommand.ExecuteAsync(null)` after profile data is set.

`AverageRating` is set eagerly from the profile response for an immediate value while the reviews request is in flight; `LoadReviewsCommand` overwrites it with the same server-sourced value.

### FetchReviewsAsync

```csharp
protected override Task<ReviewsResponse> FetchReviewsAsync(int page) =>
    _reviewService.GetUserReviewsAsync(_resolvedUserId, new GetReviewsRequest(page, ReviewPageSize));
```

---

## UserProfilePage

### Code-behind

Triggers `LoadProfileCommand` in `OnAppearing`, following the same pattern as `ItemDetailsPage`.

### Toolbar

Standard authenticated toolbar: Profile icon (`NavigateToProfileCommand`) and Logout (`LogoutCommand`), both inherited from `AuthenticatedViewModel`.

### Page body

`ScrollView` containing a `VerticalStackLayout`:

1. **Header block**
   - `DisplayName` — large bold label
   - `Email` label — `IsVisible="{Binding ShowEmail}"`

2. **Stats row**
   - Three equally-spaced cells, each with a value label above a descriptor label:
     - Average rating (★ prefix)
     - Items listed
     - Rentals completed

3. **Reviews section**
   - Section header: "Reviews ({TotalReviews})"
   - `ActivityIndicator` while `IsLoadingReviews`
   - `CollectionView` bound to `Reviews`; each cell shows: star rating, reviewer name, comment (if non-null), formatted date
   - "Load more" `Button` visible when `HasMoreReviewPages && !IsLoadingMoreReviews`, bound to `LoadMoreReviewsCommand`

4. **Error label** — bound to `ErrorMessage` / `HasError` from `BaseViewModel`

---

## Changes to existing files

### `AuthenticatedViewModel.cs`

`NavigateToProfileAsync` updated: `Routes.Temp` → `Routes.UserProfile`. No parameters passed (self mode is the default).

### `ItemDetailsViewModel.cs`

New command:

```csharp
[RelayCommand]
private async Task ViewOwnerProfileAsync()
{
    if (CurrentItem?.OwnerId is int ownerId)
        await NavigateToAsync(Routes.UserProfile, new Dictionary<string, object> { ["userId"] = ownerId });
}
```

### `ItemDetailsPage.xaml`

The owner name label gains a `TapGestureRecognizer` bound to `ViewOwnerProfileCommand` and `TextDecorations="Underline"` to signal it is tappable.

---

## Navigation summary

| Entry point | Parameters passed | Mode |
|-------------|------------------|------|
| `AuthenticatedViewModel.NavigateToProfileCommand` (toolbar) | none | Self (`_userId = 0`) |
| `ItemDetailsViewModel.ViewOwnerProfileCommand` | `userId = CurrentItem.OwnerId` | Other user |

---

## DI registration

Both registered as **Transient** in `MauiProgram.cs`, consistent with all other page/ViewModel pairs.

---

## Testing

File: `RentalApp.Test/ViewModels/UserProfileViewModelTests.cs`
Pattern: integration tests using `DatabaseFixture` (real PostgreSQL), following `ItemDetailsViewModelTests`.

| Test | Assertion |
|------|-----------|
| Own profile loads correctly | `DisplayName`, `Email`, `ItemsListed`, `RentalsCompleted` populated from `GetCurrentUserAsync()` |
| Own profile shows email | `ShowEmail == true`, `Email` is non-null |
| Other user profile loads correctly | `DisplayName`, `ItemsListed`, `RentalsCompleted` from `GetUserProfileAsync(userId)` |
| Other user profile hides email | `ShowEmail == false`, `Email == null` |
| Reviews load for correct user | `Reviews` populated and `TotalReviews > 0` after `LoadProfileCommand` (seeded data) |
| Self mode resolves correct userId | `FetchReviewsAsync` targets `currentUser.Id`, not `0` |
