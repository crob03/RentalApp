# UserProfilePage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `UserProfilePage` that shows user details and a paginated review list, accessible from the toolbar (own profile) and by tapping the owner name on `ItemDetailsPage` (other user profile).

**Architecture:** A single `UserProfileViewModel` extends `ReviewsViewModel` and implements `IQueryAttributable`. When navigated to without a `userId` query parameter (`_userId` defaults to `0`), it calls `GetCurrentUserAsync()` and shows email. With a positive `userId`, it calls `GetUserProfileAsync(userId)` and hides email. Reviews are loaded via the base-class pagination using `GetUserReviewsAsync`.

**Tech Stack:** .NET MAUI, C# 13, CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`), NSubstitute (tests), xUnit

---

## File Map

| Action | Path |
|--------|------|
| Create | `RentalApp/ViewModels/UserProfileViewModel.cs` |
| Create | `RentalApp/Views/UserProfilePage.xaml` |
| Create | `RentalApp/Views/UserProfilePage.xaml.cs` |
| Create | `RentalApp.Test/ViewModels/UserProfileViewModelTests.cs` |
| Modify | `RentalApp/Constants/Routes.cs` |
| Modify | `RentalApp/AppShell.xaml.cs` |
| Modify | `RentalApp/MauiProgram.cs` |
| Modify | `RentalApp/ViewModels/AuthenticatedViewModel.cs` |
| Modify | `RentalApp.Test/ViewModels/AuthenticatedViewModelTests.cs` |
| Modify | `RentalApp/ViewModels/ItemDetailsViewModel.cs` |
| Modify | `RentalApp.Test/ViewModels/ItemDetailsViewModelTests.cs` |
| Modify | `RentalApp/Views/ItemDetailsPage.xaml` |

---

## Task 1: Scaffolding — route constant, DI registration, and compilable stubs

**Files:**
- Modify: `RentalApp/Constants/Routes.cs`
- Modify: `RentalApp/AppShell.xaml.cs`
- Modify: `RentalApp/MauiProgram.cs`
- Create: `RentalApp/ViewModels/UserProfileViewModel.cs`
- Create: `RentalApp/Views/UserProfilePage.xaml`
- Create: `RentalApp/Views/UserProfilePage.xaml.cs`

- [ ] **Step 1: Add `UserProfile` constant to `Routes.cs`**

Add as the last constant in `RentalApp/Constants/Routes.cs`:

```csharp
/// <summary>The registered route name for the user profile page.</summary>
public const string UserProfile = "UserProfilePage";
```

- [ ] **Step 2: Register the route in `AppShell.xaml.cs`**

Add after the `Routes.CreateReview` line in `RentalApp/AppShell.xaml.cs`:

```csharp
Routing.RegisterRoute(Routes.UserProfile, typeof(UserProfilePage));
```

- [ ] **Step 3: Register ViewModel and Page in `MauiProgram.cs`**

Add after the `CreateReviewPage` lines in `RentalApp/MauiProgram.cs`:

```csharp
builder.Services.AddTransient<UserProfileViewModel>();
builder.Services.AddTransient<UserProfilePage>();
```

- [ ] **Step 4: Create stub `UserProfileViewModel.cs`**

Create `RentalApp/ViewModels/UserProfileViewModel.cs` with this content. The `LoadProfileAsync` body is intentionally empty — tests written in Task 2 will all fail against this stub, and Task 3 fills in the implementation.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Reviews;

namespace RentalApp.ViewModels;

public partial class UserProfileViewModel : ReviewsViewModel, IQueryAttributable
{
    private readonly IAuthService _authService;
    private readonly IReviewService _reviewService;
    private int _userId;
    private int _resolvedUserId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmail))]
    private string? email;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private int itemsListed;

    [ObservableProperty]
    private int rentalsCompleted;

    public bool ShowEmail => Email != null;

    public UserProfileViewModel(
        IAuthService authService,
        IReviewService reviewService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService
    )
        : base(tokenState, credentialStore, navigationService)
    {
        _authService = authService;
        _reviewService = reviewService;
        Title = "Profile";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("userId", out var id))
            _userId = Convert.ToInt32(id);
    }

    [RelayCommand]
    private Task LoadProfileAsync() => Task.CompletedTask;

    protected override Task<ReviewsResponse> FetchReviewsAsync(int page) =>
        Task.FromResult(new ReviewsResponse([], null, 0, page, ReviewPageSize, 0));
}
```

- [ ] **Step 5: Create stub `UserProfilePage.xaml`**

Create `RentalApp/Views/UserProfilePage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
  x:Class="RentalApp.Views.UserProfilePage"
  xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
  xmlns:vm="clr-namespace:RentalApp.ViewModels"
  x:DataType="vm:UserProfileViewModel"
  Title="{Binding Title}"
>
  <Label Text="User Profile" />
</ContentPage>
```

- [ ] **Step 6: Create `UserProfilePage.xaml.cs`**

Create `RentalApp/Views/UserProfilePage.xaml.cs`:

```csharp
using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class UserProfilePage : ContentPage
{
    private UserProfileViewModel ViewModel => (UserProfileViewModel)BindingContext;

    public UserProfilePage(UserProfileViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadProfileCommand.ExecuteAsync(null);
    }
}
```

- [ ] **Step 7: Build to verify scaffolding compiles**

```bash
dotnet build RentalApp.sln
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 8: Format**

```bash
dotnet csharpier .
```

- [ ] **Step 9: Commit**

```bash
git add RentalApp/Constants/Routes.cs RentalApp/AppShell.xaml.cs RentalApp/MauiProgram.cs RentalApp/ViewModels/UserProfileViewModel.cs RentalApp/Views/UserProfilePage.xaml RentalApp/Views/UserProfilePage.xaml.cs
git commit -m "feat: scaffold UserProfilePage route, DI registration, and stub ViewModel"
```

---

## Task 2: Write failing `UserProfileViewModel` tests

**Files:**
- Create: `RentalApp.Test/ViewModels/UserProfileViewModelTests.cs`

- [ ] **Step 1: Create the test file**

Create `RentalApp.Test/ViewModels/UserProfileViewModelTests.cs`:

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Reviews;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class UserProfileViewModelTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IReviewService _reviewService = Substitute.For<IReviewService>();
    private readonly INavigationService _nav = Substitute.For<INavigationService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();

    private UserProfileViewModel CreateSut()
    {
        _reviewService
            .GetUserReviewsAsync(Arg.Any<int>(), Arg.Any<GetReviewsRequest>())
            .Returns(new ReviewsResponse([], null, 0, 1, 10, 0));
        return new(_authService, _reviewService, _tokenState, _credentialStore, _nav);
    }

    private static CurrentUserResponse MakeCurrentUser(int id = 1) =>
        new(id, "alice@example.com", "Alice", "Smith", 4.5, 3, 7, DateTime.UtcNow);

    private static UserProfileResponse MakeUserProfile(int id = 2) =>
        new(id, "Bob", "Jones", 3.8, 5, 2, []);

    // ── Self mode (no userId supplied) ───────────────────────────────

    [Fact]
    public async Task LoadProfileCommand_SelfMode_SetsDisplayName()
    {
        _authService.GetCurrentUserAsync().Returns(MakeCurrentUser());
        var sut = CreateSut();

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Equal("Alice Smith", sut.DisplayName);
    }

    [Fact]
    public async Task LoadProfileCommand_SelfMode_SetsEmail()
    {
        _authService.GetCurrentUserAsync().Returns(MakeCurrentUser());
        var sut = CreateSut();

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Equal("alice@example.com", sut.Email);
        Assert.True(sut.ShowEmail);
    }

    [Fact]
    public async Task LoadProfileCommand_SelfMode_SetsStats()
    {
        _authService.GetCurrentUserAsync().Returns(MakeCurrentUser());
        var sut = CreateSut();

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Equal(3, sut.ItemsListed);
        Assert.Equal(7, sut.RentalsCompleted);
    }

    // ── Other user mode (userId supplied) ────────────────────────────

    [Fact]
    public async Task LoadProfileCommand_OtherUser_SetsDisplayName()
    {
        _authService.GetUserProfileAsync(2).Returns(MakeUserProfile(2));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["userId"] = 2 });

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Equal("Bob Jones", sut.DisplayName);
    }

    [Fact]
    public async Task LoadProfileCommand_OtherUser_EmailIsNullAndShowEmailIsFalse()
    {
        _authService.GetUserProfileAsync(2).Returns(MakeUserProfile(2));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["userId"] = 2 });

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Null(sut.Email);
        Assert.False(sut.ShowEmail);
    }

    [Fact]
    public async Task LoadProfileCommand_OtherUser_SetsStats()
    {
        _authService.GetUserProfileAsync(2).Returns(MakeUserProfile(2));
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["userId"] = 2 });

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.Equal(5, sut.ItemsListed);
        Assert.Equal(2, sut.RentalsCompleted);
    }

    // ── Review routing — resolvedUserId ──────────────────────────────

    [Fact]
    public async Task LoadReviewsCommand_AfterSelfModeLoad_UsesResolvedUserIdNotZero()
    {
        _authService.GetCurrentUserAsync().Returns(MakeCurrentUser(id: 42));
        var sut = CreateSut();
        await sut.LoadProfileCommand.ExecuteAsync(null);

        await sut.LoadReviewsCommand.ExecuteAsync(null);

        await _reviewService
            .Received()
            .GetUserReviewsAsync(42, Arg.Any<GetReviewsRequest>());
        await _reviewService
            .DidNotReceive()
            .GetUserReviewsAsync(0, Arg.Any<GetReviewsRequest>());
    }

    // ── Error handling ────────────────────────────────────────────────

    [Fact]
    public async Task LoadProfileCommand_ServiceThrows_SetsError()
    {
        _authService
            .GetCurrentUserAsync()
            .ThrowsAsync(new InvalidOperationException("Auth error"));
        var sut = CreateSut();

        await sut.LoadProfileCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Auth error", sut.ErrorMessage);
        Assert.False(sut.IsBusy);
    }
}
```

- [ ] **Step 2: Run tests to confirm failures**

```bash
dotnet test --filter "FullyQualifiedName~UserProfileViewModelTests"
```

Expected: 8 tests, all FAIL (the stub `LoadProfileAsync` returns `Task.CompletedTask` without setting any properties).

---

## Task 3: Implement `UserProfileViewModel`

**Files:**
- Modify: `RentalApp/ViewModels/UserProfileViewModel.cs`

- [ ] **Step 1: Replace the stub `LoadProfileAsync` and `FetchReviewsAsync` with full implementations**

Replace the entire content of `RentalApp/ViewModels/UserProfileViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.Services.Reviews;

namespace RentalApp.ViewModels;

public partial class UserProfileViewModel : ReviewsViewModel, IQueryAttributable
{
    private readonly IAuthService _authService;
    private readonly IReviewService _reviewService;
    private int _userId;
    private int _resolvedUserId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmail))]
    private string? email;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private int itemsListed;

    [ObservableProperty]
    private int rentalsCompleted;

    public bool ShowEmail => Email != null;

    public UserProfileViewModel(
        IAuthService authService,
        IReviewService reviewService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService
    )
        : base(tokenState, credentialStore, navigationService)
    {
        _authService = authService;
        _reviewService = reviewService;
        Title = "Profile";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("userId", out var id))
            _userId = Convert.ToInt32(id);
    }

    [RelayCommand]
    private Task LoadProfileAsync() =>
        RunAsync(async () =>
        {
            if (_userId <= 0)
            {
                var own = await _authService.GetCurrentUserAsync();
                _resolvedUserId = own.Id;
                DisplayName = $"{own.FirstName} {own.LastName}";
                Email = own.Email;
                ItemsListed = own.ItemsListed;
                RentalsCompleted = own.RentalsCompleted;
                AverageRating = own.AverageRating;
            }
            else
            {
                _resolvedUserId = _userId;
                var profile = await _authService.GetUserProfileAsync(_userId);
                DisplayName = $"{profile.FirstName} {profile.LastName}";
                Email = null;
                ItemsListed = profile.ItemsListed;
                RentalsCompleted = profile.RentalsCompleted;
                AverageRating = profile.AverageRating;
            }
            _ = LoadReviewsCommand.ExecuteAsync(null);
        });

    protected override Task<ReviewsResponse> FetchReviewsAsync(int page) =>
        _reviewService.GetUserReviewsAsync(_resolvedUserId, new GetReviewsRequest(page, ReviewPageSize));
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test --filter "FullyQualifiedName~UserProfileViewModelTests"
```

Expected: 7 tests, all PASS.

- [ ] **Step 3: Run the full test suite to check for regressions**

```bash
dotnet test
```

Expected: all existing tests still pass.

- [ ] **Step 4: Format**

```bash
dotnet csharpier .
```

- [ ] **Step 5: Commit**

```bash
git add RentalApp/ViewModels/UserProfileViewModel.cs RentalApp.Test/ViewModels/UserProfileViewModelTests.cs
git commit -m "feat: implement UserProfileViewModel with self/other-user modes and tests"
```

---

## Task 4: Full `UserProfilePage` XAML layout

**Files:**
- Modify: `RentalApp/Views/UserProfilePage.xaml`

- [ ] **Step 1: Replace the stub XAML with the full layout**

Replace the content of `RentalApp/Views/UserProfilePage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
  x:Class="RentalApp.Views.UserProfilePage"
  xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
  xmlns:vm="clr-namespace:RentalApp.ViewModels"
  xmlns:responses="clr-namespace:RentalApp.Contracts.Responses"
  mc:Ignorable="d"
  x:DataType="vm:UserProfileViewModel"
  Title="{Binding Title}"
>
  <d:ContentPage.BindingContext>
    <vm:UserProfileViewModel />
  </d:ContentPage.BindingContext>

  <ContentPage.ToolbarItems>
    <ToolbarItem
      Text="Profile"
      Command="{Binding NavigateToProfileCommand}"
      IconImageSource="user.png"
    />
    <ToolbarItem Text="Logout" Command="{Binding LogoutCommand}" IconImageSource="logout.png" />
  </ContentPage.ToolbarItems>

  <ScrollView>
    <Grid Padding="16" RowSpacing="12" RowDefinitions="Auto,Auto,Auto,Auto,Auto">

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

      <!-- Loading indicator -->
      <ActivityIndicator Grid.Row="1" IsRunning="{Binding IsBusy}" IsVisible="{Binding IsBusy}" />

      <!-- Profile header -->
      <StackLayout Grid.Row="2" Spacing="4">
        <Label Text="{Binding DisplayName}" FontSize="24" FontAttributes="Bold" />
        <Label
          Text="{Binding Email}"
          FontSize="14"
          IsVisible="{Binding ShowEmail}"
          TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
        />
      </StackLayout>

      <!-- Stats row -->
      <Grid Grid.Row="3" ColumnDefinitions="*,*,*" ColumnSpacing="8">
        <StackLayout Grid.Column="0" HorizontalOptions="Center" Spacing="2">
          <Label
            Text="{Binding AverageRating, StringFormat='★ {0:F1}'}"
            HorizontalOptions="Center"
            FontSize="18"
            FontAttributes="Bold"
            IsVisible="{Binding HasAverageRating}"
          />
          <Label
            Text="—"
            HorizontalOptions="Center"
            FontSize="18"
            FontAttributes="Bold"
            IsVisible="{Binding HasAverageRating, Converter={StaticResource InvertedBoolConverter}}"
          />
          <Label
            Text="Rating"
            HorizontalOptions="Center"
            FontSize="12"
            TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
          />
        </StackLayout>

        <StackLayout Grid.Column="1" HorizontalOptions="Center" Spacing="2">
          <Label
            Text="{Binding ItemsListed}"
            HorizontalOptions="Center"
            FontSize="18"
            FontAttributes="Bold"
          />
          <Label
            Text="Items listed"
            HorizontalOptions="Center"
            FontSize="12"
            TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
          />
        </StackLayout>

        <StackLayout Grid.Column="2" HorizontalOptions="Center" Spacing="2">
          <Label
            Text="{Binding RentalsCompleted}"
            HorizontalOptions="Center"
            FontSize="18"
            FontAttributes="Bold"
          />
          <Label
            Text="Rentals"
            HorizontalOptions="Center"
            FontSize="12"
            TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
          />
        </StackLayout>
      </Grid>

      <!-- Reviews section -->
      <StackLayout Grid.Row="4" Spacing="8" Margin="0,8,0,0">
        <Label
          Text="{Binding TotalReviews, StringFormat='Reviews ({0})'}"
          FontSize="18"
          FontAttributes="Bold"
        />

        <Label
          Text="{Binding AverageRating, StringFormat='★ {0:F1} average'}"
          IsVisible="{Binding HasAverageRating}"
          FontSize="14"
        />

        <ActivityIndicator
          IsRunning="{Binding IsLoadingReviews}"
          IsVisible="{Binding IsLoadingReviews}"
          HorizontalOptions="Center"
        />

        <CollectionView ItemsSource="{Binding Reviews}">
          <CollectionView.EmptyView>
            <Label
              Text="No reviews yet."
              TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
            />
          </CollectionView.EmptyView>
          <CollectionView.ItemTemplate>
            <DataTemplate x:DataType="responses:ReviewResponse">
              <StackLayout Spacing="4" Padding="0,8">
                <Label Text="{Binding ReviewerName}" FontAttributes="Bold" FontSize="14" />
                <Label Text="{Binding Rating, StringFormat='★ {0}/5'}" FontSize="13" />
                <Label Text="{Binding Comment}" />
                <Label
                  Text="{Binding CreatedAt, StringFormat='{0:d}'}"
                  FontSize="12"
                  TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
                />
              </StackLayout>
            </DataTemplate>
          </CollectionView.ItemTemplate>
        </CollectionView>

        <ActivityIndicator
          IsRunning="{Binding IsLoadingMoreReviews}"
          IsVisible="{Binding IsLoadingMoreReviews}"
          HorizontalOptions="Center"
        />

        <Button
          Text="Load More Reviews"
          Command="{Binding LoadMoreReviewsCommand}"
          IsVisible="{Binding HasMoreReviewPages}"
          BackgroundColor="Transparent"
          TextColor="{AppThemeBinding Light={StaticResource Primary}, Dark={StaticResource PrimaryDark}}"
        />
      </StackLayout>

    </Grid>
  </ScrollView>
</ContentPage>
```

- [ ] **Step 2: Build to verify XAML compiles**

```bash
dotnet build RentalApp.sln
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add RentalApp/Views/UserProfilePage.xaml
git commit -m "feat: add UserProfilePage XAML layout with stats and paginated reviews"
```

---

## Task 5: Update `AuthenticatedViewModel` to navigate to `UserProfilePage`

**Files:**
- Modify: `RentalApp.Test/ViewModels/AuthenticatedViewModelTests.cs`
- Modify: `RentalApp/ViewModels/AuthenticatedViewModel.cs`

- [ ] **Step 1: Update the existing test to assert `Routes.UserProfile`**

In `RentalApp.Test/ViewModels/AuthenticatedViewModelTests.cs`, rename and update the test at line 95–103 from:

```csharp
[Fact]
public async Task NavigateToProfileCommand_NavigatesToTemp()
{
    var sut = CreateSut();

    await sut.NavigateToProfileCommand.ExecuteAsync(null);

    await _navigationService.Received(1).NavigateToAsync(Routes.Temp);
}
```

to:

```csharp
[Fact]
public async Task NavigateToProfileCommand_NavigatesToUserProfile()
{
    var sut = CreateSut();

    await sut.NavigateToProfileCommand.ExecuteAsync(null);

    await _navigationService.Received(1).NavigateToAsync(Routes.UserProfile);
}
```

- [ ] **Step 2: Run the test to confirm it fails**

```bash
dotnet test --filter "DisplayName~NavigateToProfileCommand_NavigatesToUserProfile"
```

Expected: FAIL — the command still navigates to `Routes.Temp`.

- [ ] **Step 3: Update `AuthenticatedViewModel.cs`**

In `RentalApp/ViewModels/AuthenticatedViewModel.cs`, change line 73–74 from:

```csharp
private async Task NavigateToProfileAsync() =>
    await _navigationService.NavigateToAsync(Routes.Temp);
```

to:

```csharp
private async Task NavigateToProfileAsync() =>
    await _navigationService.NavigateToAsync(Routes.UserProfile);
```

- [ ] **Step 4: Run the test to confirm it passes**

```bash
dotnet test --filter "DisplayName~NavigateToProfileCommand_NavigatesToUserProfile"
```

Expected: PASS.

- [ ] **Step 5: Run the full suite to check for regressions**

```bash
dotnet test
```

Expected: all tests pass.

- [ ] **Step 6: Format and commit**

```bash
dotnet csharpier .
git add RentalApp/ViewModels/AuthenticatedViewModel.cs RentalApp.Test/ViewModels/AuthenticatedViewModelTests.cs
git commit -m "feat: update NavigateToProfileCommand to route to UserProfilePage"
```

---

## Task 6: `ViewOwnerProfileCommand` in `ItemDetailsViewModel` and tappable owner name

**Files:**
- Modify: `RentalApp.Test/ViewModels/ItemDetailsViewModelTests.cs`
- Modify: `RentalApp/ViewModels/ItemDetailsViewModel.cs`
- Modify: `RentalApp/Views/ItemDetailsPage.xaml`

- [ ] **Step 1: Add the failing test to `ItemDetailsViewModelTests.cs`**

Add `using RentalApp.Constants;` to the using block at the top of `RentalApp.Test/ViewModels/ItemDetailsViewModelTests.cs`:

```csharp
using RentalApp.Constants;
```

Then add this test at the end of the `ItemDetailsViewModelTests` class (after the existing `RequestRentalCommand` tests):

```csharp
// ── ViewOwnerProfileCommand ────────────────────────────────────────

[Fact]
public async Task ViewOwnerProfileCommand_NavigatesToUserProfileWithOwnerId()
{
    _itemService.GetItemAsync(1).Returns(MakeItem(1, ownerId: 7));
    _authService.GetCurrentUserAsync().Returns(MakeUser(99));
    var sut = CreateSut();
    sut.ApplyQueryAttributes(new Dictionary<string, object> { ["itemId"] = 1 });
    await sut.LoadItemCommand.ExecuteAsync(null);

    await sut.ViewOwnerProfileCommand.ExecuteAsync(null);

    await _nav
        .Received(1)
        .NavigateToAsync(
            Routes.UserProfile,
            Arg.Is<Dictionary<string, object>>(d => (int)d["userId"] == 7)
        );
}
```

- [ ] **Step 2: Run the test to confirm it fails**

```bash
dotnet test --filter "DisplayName~ViewOwnerProfileCommand_NavigatesToUserProfileWithOwnerId"
```

Expected: FAIL — `ViewOwnerProfileCommand` does not exist yet (compilation error or runtime failure).

- [ ] **Step 3: Add `ViewOwnerProfileCommand` to `ItemDetailsViewModel.cs`**

Add the following command method to `RentalApp/ViewModels/ItemDetailsViewModel.cs`, after the `CancelEditCommand` method (around line 189):

```csharp
/// <summary>Navigates to the owner's public profile.</summary>
[RelayCommand]
private async Task ViewOwnerProfileAsync()
{
    if (CurrentItem?.OwnerId is int ownerId)
        await NavigateToAsync(
            Routes.UserProfile,
            new Dictionary<string, object> { ["userId"] = ownerId }
        );
}
```

Also add `using RentalApp.Constants;` to the using block at the top of `ItemDetailsViewModel.cs`. Insert it after `using RentalApp.Contracts.Responses;`:

```csharp
using RentalApp.Constants;
```

- [ ] **Step 4: Run the test to confirm it passes**

```bash
dotnet test --filter "DisplayName~ViewOwnerProfileCommand_NavigatesToUserProfileWithOwnerId"
```

Expected: PASS.

- [ ] **Step 5: Run full test suite**

```bash
dotnet test
```

Expected: all tests pass.

- [ ] **Step 6: Make the owner name label tappable in `ItemDetailsPage.xaml`**

In `RentalApp/Views/ItemDetailsPage.xaml`, replace the owner name label (lines 74–78) from:

```xml
<Label
  Text="{Binding CurrentItem.OwnerName, StringFormat='Listed by {0}'}"
  FontSize="13"
  TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
/>
```

with:

```xml
<Label
  Text="{Binding CurrentItem.OwnerName, StringFormat='Listed by {0}'}"
  FontSize="13"
  TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource Gray400}}"
  TextDecorations="Underline"
>
  <Label.GestureRecognizers>
    <TapGestureRecognizer Command="{Binding ViewOwnerProfileCommand}" />
  </Label.GestureRecognizers>
</Label>
```

- [ ] **Step 7: Build to verify XAML compiles**

```bash
dotnet build RentalApp.sln
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 8: Format and commit**

```bash
dotnet csharpier .
git add RentalApp/ViewModels/ItemDetailsViewModel.cs RentalApp.Test/ViewModels/ItemDetailsViewModelTests.cs RentalApp/Views/ItemDetailsPage.xaml
git commit -m "feat: add ViewOwnerProfileCommand to ItemDetailsViewModel and tappable owner name"
```
