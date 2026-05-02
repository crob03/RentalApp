# IApiService Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split the monolithic `IApiService` into four focused domain services (`IAuthService`, `IItemService`, `IRentalService`, `IReviewService`) and remove `IAuthenticationService`, distributing its orchestration logic into ViewModels.

**Architecture:** Each domain service has a remote (HTTP via `IApiClient`) and local (PostgreSQL via EF Core) implementation. `AuthTokenState` gains the `AuthenticationStateChanged` event and becomes the single session authority. ViewModels own all orchestration: token setting, credential persistence, and logout confirmation. Old files are deleted only after all consuming code is migrated.

**Tech Stack:** .NET MAUI, CommunityToolkit.Mvvm, EF Core + Npgsql, NSubstitute, xUnit

---

## File Map

### Create
- `RentalApp/Services/RemoteServiceBase.cs` — shared `EnsureSuccessAsync` helper and `ApiErrorResponse` record
- `RentalApp/Services/IAuthService.cs`
- `RentalApp/Services/RemoteAuthService.cs`
- `RentalApp/Services/LocalAuthService.cs`
- `RentalApp/Services/IItemService.cs`
- `RentalApp/Services/RemoteItemService.cs`
- `RentalApp/Services/LocalItemService.cs`
- `RentalApp/Services/IRentalService.cs`
- `RentalApp/Services/RemoteRentalService.cs`
- `RentalApp/Services/LocalRentalService.cs`
- `RentalApp/Services/IReviewService.cs`
- `RentalApp/Services/RemoteReviewService.cs`
- `RentalApp/Services/LocalReviewService.cs`
- `RentalApp.Test/Services/RemoteAuthServiceTests.cs`
- `RentalApp.Test/Services/LocalAuthServiceTests.cs`
- `RentalApp.Test/Services/RemoteItemServiceTests.cs`
- `RentalApp.Test/Services/LocalItemServiceTests.cs`
- `RentalApp.Test/Services/RemoteRentalServiceTests.cs`
- `RentalApp.Test/Services/LocalRentalServiceTests.cs`
- `RentalApp.Test/Services/RemoteReviewServiceTests.cs`
- `RentalApp.Test/Services/LocalReviewServiceTests.cs`

### Modify
- `RentalApp/Http/AuthTokenState.cs` — add `AuthenticationStateChanged` event + `ClearToken()`
- `RentalApp/ViewModels/AppShellViewModel.cs` — remove `IAuthenticationService`, add `AuthTokenState` + `ICredentialStore`, absorb alert logic
- `RentalApp/ViewModels/LoginViewModel.cs` — remove `IAuthenticationService`, add `IAuthService` + `AuthTokenState`
- `RentalApp/ViewModels/LoadingViewModel.cs` — remove `IAuthenticationService`, add `IAuthService` + `AuthTokenState`
- `RentalApp/ViewModels/RegisterViewModel.cs` — remove `IAuthenticationService`, add `IAuthService`
- `RentalApp/ViewModels/MainViewModel.cs` — remove `IAuthenticationService` + `LogoutAsync`, add `IAuthService`, make user load async via `InitializeAsync()`
- `RentalApp/Views/MainPage.xaml` — remove `Command` from logout `ToolbarItem`, add `x:Name`
- `RentalApp/Views/MainPage.xaml.cs` — inject `AppShellViewModel`, wire logout command + call `InitializeAsync()` from `OnAppearing`
- `RentalApp/MauiProgram.cs` — replace `IApiService`/`IAuthenticationService` with four service pairs
- `RentalApp.Test/Http/AuthTokenStateTests.cs` — add tests for new event + `ClearToken`
- `RentalApp.Test/ViewModels/LoginViewModelTests.cs` — replace `IAuthenticationService` mock
- `RentalApp.Test/ViewModels/LoadingViewModelTests.cs` — replace `IAuthenticationService` mock
- `RentalApp.Test/ViewModels/RegisterViewModelTests.cs` — replace `IAuthenticationService` mock
- `RentalApp.Test/ViewModels/AppShellViewModelTests.cs` — replace `IAuthenticationService` mock, add logout tests
- `RentalApp.Test/ViewModels/MainViewModelTests.cs` — replace `IAuthenticationService` mock, make user load tests async

### Delete (Task 17)
- `RentalApp/Services/IApiService.cs`
- `RentalApp/Services/IAuthenticationService.cs`
- `RentalApp/Services/AuthenticationService.cs`
- `RentalApp/Services/AuthenticationResult.cs`
- `RentalApp/Services/RemoteApiService.cs`
- `RentalApp/Services/LocalApiService.cs`
- `RentalApp.Test/Services/AuthenticationServiceTests.cs`

---

## Task 1: Update AuthTokenState

**Files:**
- Modify: `RentalApp/Http/AuthTokenState.cs`
- Modify: `RentalApp.Test/Http/AuthTokenStateTests.cs`

- [ ] **Step 1: Add the failing tests**

Append to `RentalApp.Test/Http/AuthTokenStateTests.cs`:

```csharp
[Fact]
public void AuthenticationStateChanged_WhenTokenSet_RaisesWithTrue()
{
    var sut = new AuthTokenState();
    bool? raised = null;
    sut.AuthenticationStateChanged += (_, v) => raised = v;

    sut.CurrentToken = "eyJ...";

    Assert.True(raised);
}

[Fact]
public void AuthenticationStateChanged_WhenTokenCleared_RaisesWithFalse()
{
    var sut = new AuthTokenState { CurrentToken = "eyJ..." };
    bool? raised = null;
    sut.AuthenticationStateChanged += (_, v) => raised = v;

    sut.ClearToken();

    Assert.False(raised);
}

[Fact]
public void ClearToken_SetsCurrentTokenToNull()
{
    var sut = new AuthTokenState { CurrentToken = "eyJ..." };

    sut.ClearToken();

    Assert.Null(sut.CurrentToken);
}

[Fact]
public void HasSession_AfterClearToken_ReturnsFalse()
{
    var sut = new AuthTokenState { CurrentToken = "eyJ..." };

    sut.ClearToken();

    Assert.False(sut.HasSession);
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "AuthTokenStateTests" -v minimal
```
Expected: 4 failures — `AuthenticationStateChanged` and `ClearToken` do not exist yet.

- [ ] **Step 3: Replace `AuthTokenState.cs` with the new implementation**

```csharp
namespace RentalApp.Http;

public class AuthTokenState
{
    private string? _token;

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

    public bool HasSession => _token is not null;

    public void ClearToken() => CurrentToken = null;
}
```

- [ ] **Step 4: Run all AuthTokenState tests**

```bash
dotnet test RentalApp.Test --filter "AuthTokenStateTests" -v minimal
```
Expected: all 7 pass.

- [ ] **Step 5: Commit**

```bash
git add RentalApp/Http/AuthTokenState.cs RentalApp.Test/Http/AuthTokenStateTests.cs
git commit -m "feat: add AuthenticationStateChanged event and ClearToken to AuthTokenState"
```

---

## Task 2: Create RemoteServiceBase + IAuthService + RemoteAuthService

**Files:**
- Create: `RentalApp/Services/RemoteServiceBase.cs`
- Create: `RentalApp/Services/IAuthService.cs`
- Create: `RentalApp/Services/RemoteAuthService.cs`
- Create: `RentalApp.Test/Services/RemoteAuthServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `RentalApp.Test/Services/RemoteAuthServiceTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class RemoteAuthServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();

    private RemoteAuthService CreateSut() => new(_apiClient);

    [Fact]
    public async Task LoginAsync_SuccessResponse_ReturnsToken()
    {
        _apiClient
            .PostAsJsonAsync("auth/token", Arg.Any<object>())
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    token = "abc123",
                    expiresAt = DateTime.UtcNow.AddHours(1),
                    userId = 1,
                }),
            });

        var result = await CreateSut().LoginAsync(new LoginRequest("jane@example.com", "Password1!"));

        Assert.Equal("abc123", result.Token);
    }

    [Fact]
    public async Task LoginAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .PostAsJsonAsync("auth/token", Arg.Any<object>())
            .Returns(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = JsonContent.Create(new { error = "Unauthorized", message = "Invalid credentials" }),
            });

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateSut().LoginAsync(new LoginRequest("jane@example.com", "wrong"))
        );
    }

    [Fact]
    public async Task RegisterAsync_SuccessResponse_ReturnsResponse()
    {
        _apiClient
            .PostAsJsonAsync("auth/register", Arg.Any<object>())
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = 1,
                    email = "jane@example.com",
                    firstName = "Jane",
                    lastName = "Doe",
                    createdAt = DateTime.UtcNow,
                }),
            });

        var result = await CreateSut().RegisterAsync(new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!"));

        Assert.Equal("jane@example.com", result.Email);
    }

    [Fact]
    public async Task RegisterAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .PostAsJsonAsync("auth/register", Arg.Any<object>())
            .Returns(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = JsonContent.Create(new { error = "Conflict", message = "Email already registered" }),
            });

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateSut().RegisterAsync(new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!"))
        );
    }

    [Fact]
    public async Task GetCurrentUserAsync_SuccessResponse_ReturnsUser()
    {
        _apiClient
            .GetAsync("users/me")
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = 1,
                    email = "jane@example.com",
                    firstName = "Jane",
                    lastName = "Doe",
                    averageRating = (double?)null,
                    itemsListed = 0,
                    rentalsCompleted = 0,
                    createdAt = DateTime.UtcNow,
                }),
            });

        var result = await CreateSut().GetCurrentUserAsync();

        Assert.Equal("jane@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserProfileAsync_SuccessResponse_ReturnsProfile()
    {
        _apiClient
            .GetAsync("users/42/profile")
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = 42,
                    firstName = "Jane",
                    lastName = "Doe",
                    averageRating = (double?)null,
                    itemsListed = 0,
                    rentalsCompleted = 0,
                    reviews = Array.Empty<object>(),
                }),
            });

        var result = await CreateSut().GetUserProfileAsync(42);

        Assert.Equal(42, result.Id);
    }
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "RemoteAuthServiceTests" -v minimal
```
Expected: compile error — `IAuthService`, `RemoteAuthService` do not exist.

- [ ] **Step 3: Create `RemoteServiceBase.cs`**

```csharp
using System.Net.Http.Json;

namespace RentalApp.Services;

internal abstract class RemoteServiceBase
{
    protected static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        throw new HttpRequestException(
            error?.Message ?? $"Request failed with status {(int)response.StatusCode}"
        );
    }

    protected sealed record ApiErrorResponse(string Error, string Message);
}
```

- [ ] **Step 4: Create `IAuthService.cs`**

```csharp
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<CurrentUserResponse> GetCurrentUserAsync();
    Task<UserProfileResponse> GetUserProfileAsync(int userId);
}
```

- [ ] **Step 5: Create `RemoteAuthService.cs`**

```csharp
using System.Net.Http.Json;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;

namespace RentalApp.Services;

public class RemoteAuthService : RemoteServiceBase, IAuthService
{
    private readonly IApiClient _apiClient;

    public RemoteAuthService(IApiClient apiClient) => _apiClient = apiClient;

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "auth/token",
            new { email = request.Email, password = request.Password }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Empty token response from API");
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "auth/register",
            new
            {
                firstName = request.FirstName,
                lastName = request.LastName,
                email = request.Email,
                password = request.Password,
            }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RegisterResponse>()
            ?? throw new InvalidOperationException("Empty register response from API");
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync()
    {
        var response = await _apiClient.GetAsync("users/me");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CurrentUserResponse>()
            ?? throw new InvalidOperationException("Empty profile response from API");
    }

    public async Task<UserProfileResponse> GetUserProfileAsync(int userId)
    {
        var response = await _apiClient.GetAsync($"users/{userId}/profile");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<UserProfileResponse>()
            ?? throw new InvalidOperationException("Empty profile response from API");
    }
}
```

- [ ] **Step 6: Run passing tests**

```bash
dotnet test RentalApp.Test --filter "RemoteAuthServiceTests" -v minimal
```
Expected: all 6 pass.

- [ ] **Step 7: Commit**

```bash
git add RentalApp/Services/RemoteServiceBase.cs RentalApp/Services/IAuthService.cs RentalApp/Services/RemoteAuthService.cs RentalApp.Test/Services/RemoteAuthServiceTests.cs
git commit -m "feat: add IAuthService and RemoteAuthService"
```

---

## Task 3: Create LocalAuthService

**Files:**
- Create: `RentalApp/Services/LocalAuthService.cs`
- Create: `RentalApp.Test/Services/LocalAuthServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `RentalApp.Test/Services/LocalAuthServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Database.Data;
using RentalApp.Http;
using RentalApp.Services;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public class LocalAuthServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly AuthTokenState _tokenState = new();

    public LocalAuthServiceTests(DatabaseFixture fixture)
    {
        _contextFactory = fixture.ContextFactory;
    }

    private LocalAuthService CreateSut() => new(_contextFactory, _tokenState);

    private async Task<int> SeedUserAsync(string email = "jane@example.com", string password = "Password1!")
    {
        await using var ctx = _contextFactory.CreateDbContext();
        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        var user = new RentalApp.Database.Models.User
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, salt),
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenEqualToUserId()
    {
        var userId = await SeedUserAsync();

        var result = await CreateSut().LoginAsync(new LoginRequest("jane@example.com", "Password1!"));

        Assert.Equal(userId.ToString(), result.Token);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        await SeedUserAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CreateSut().LoginAsync(new LoginRequest("jane@example.com", "wrong"))
        );
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CreateSut().LoginAsync(new LoginRequest("nobody@example.com", "Password1!"))
        );
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsEmailAndName()
    {
        var result = await CreateSut().RegisterAsync(
            new RegisterRequest("Alice", "Smith", "alice@example.com", "Password1!")
        );

        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal("Alice", result.FirstName);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        await SeedUserAsync("dup@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSut().RegisterAsync(new RegisterRequest("X", "Y", "dup@example.com", "Password1!"))
        );
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithValidToken_ReturnsUser()
    {
        var userId = await SeedUserAsync("me@example.com");
        _tokenState.CurrentToken = userId.ToString();

        var result = await CreateSut().GetCurrentUserAsync();

        Assert.Equal("me@example.com", result.Email);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithNoSession_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSut().GetCurrentUserAsync()
        );
    }
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "LocalAuthServiceTests" -v minimal
```
Expected: compile error — `LocalAuthService` does not exist.

- [ ] **Step 3: Create `LocalAuthService.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Database.Data;
using RentalApp.Http;
using DbUser = RentalApp.Database.Models.User;

namespace RentalApp.Services;

public class LocalAuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly AuthTokenState _tokenState;

    public LocalAuthService(IDbContextFactory<AppDbContext> contextFactory, AuthTokenState tokenState)
    {
        _contextFactory = contextFactory;
        _tokenState = tokenState;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        await using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        return new LoginResponse(Token: user.Id.ToString(), ExpiresAt: DateTime.MaxValue, UserId: user.Id);
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        await using var context = _contextFactory.CreateDbContext();
        var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existing != null)
            throw new InvalidOperationException("User with this email already exists");

        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        var newUser = new DbUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, salt),
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Users.Add(newUser);
        await context.SaveChangesAsync();

        return new RegisterResponse(newUser.Id, newUser.Email, newUser.FirstName, newUser.LastName, newUser.CreatedAt ?? DateTime.UtcNow);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync()
    {
        if (!_tokenState.HasSession)
            throw new InvalidOperationException("No user is currently authenticated");

        var userId = int.Parse(_tokenState.CurrentToken!);
        await using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Authenticated user not found");

        var itemsListed = await context.Items.CountAsync(i => i.OwnerId == userId);

        return new CurrentUserResponse(user.Id, user.Email, user.FirstName, user.LastName,
            AverageRating: null, ItemsListed: itemsListed, RentalsCompleted: 0, user.CreatedAt ?? DateTime.UtcNow);
    }

    public async Task<UserProfileResponse> GetUserProfileAsync(int userId)
    {
        await using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        var itemsListed = await context.Items.CountAsync(i => i.OwnerId == userId);

        return new UserProfileResponse(user.Id, user.FirstName, user.LastName,
            AverageRating: null, ItemsListed: itemsListed, RentalsCompleted: 0, Reviews: []);
    }
}
```

- [ ] **Step 4: Run passing tests**

```bash
dotnet test RentalApp.Test --filter "LocalAuthServiceTests" -v minimal
```
Expected: all 7 pass (requires DB — run `docker-compose up db` first if not running).

- [ ] **Step 5: Commit**

```bash
git add RentalApp/Services/LocalAuthService.cs RentalApp.Test/Services/LocalAuthServiceTests.cs
git commit -m "feat: add LocalAuthService"
```

---

## Task 4: Create IItemService + RemoteItemService

**Files:**
- Create: `RentalApp/Services/IItemService.cs`
- Create: `RentalApp/Services/RemoteItemService.cs`
- Create: `RentalApp.Test/Services/RemoteItemServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `RentalApp.Test/Services/RemoteItemServiceTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class RemoteItemServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();

    private RemoteItemService CreateSut() => new(_apiClient);

    [Fact]
    public async Task GetItemsAsync_SuccessResponse_ReturnsItems()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("items?")))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { items = Array.Empty<object>(), totalItems = 0, page = 1, pageSize = 20, totalPages = 0 }),
            });

        var result = await CreateSut().GetItemsAsync(new GetItemsRequest(1, 20, null, null));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetItemsAsync_WithCategoryAndSearch_BuildsCorrectQuery()
    {
        string? capturedUrl = null;
        _apiClient
            .GetAsync(Arg.Do<string>(u => capturedUrl = u))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { items = Array.Empty<object>(), totalItems = 0, page = 1, pageSize = 20, totalPages = 0 }),
            });

        await CreateSut().GetItemsAsync(new GetItemsRequest(1, 20, "tools", "drill"));

        Assert.Contains("category=tools", capturedUrl);
        Assert.Contains("search=drill", capturedUrl);
    }

    [Fact]
    public async Task GetItemAsync_SuccessResponse_ReturnsItem()
    {
        _apiClient
            .GetAsync("items/5")
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = 5, title = "Drill", description = "A drill", dailyRate = 10.0m,
                    categoryId = 1, categoryName = "Tools", ownerId = 1, ownerName = "Jane Doe",
                    ownerRating = (double?)null, latitude = 55.0, longitude = -3.0,
                    isAvailable = true, averageRating = (double?)null, totalReviews = 0,
                    createdAt = DateTime.UtcNow, reviews = Array.Empty<object>(),
                }),
            });

        var result = await CreateSut().GetItemAsync(5);

        Assert.Equal(5, result.Id);
    }

    [Fact]
    public async Task GetCategoriesAsync_SuccessResponse_ReturnsCategories()
    {
        _apiClient
            .GetAsync("categories")
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { categories = Array.Empty<object>() }),
            });

        var result = await CreateSut().GetCategoriesAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetItemsAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .GetAsync(Arg.Any<string>())
            .Returns(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = JsonContent.Create(new { error = "Error", message = "Server error" }),
            });

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateSut().GetItemsAsync(new GetItemsRequest(1, 20, null, null))
        );
    }
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "RemoteItemServiceTests" -v minimal
```
Expected: compile error — `IItemService`, `RemoteItemService` do not exist.

- [ ] **Step 3: Create `IItemService.cs`**

```csharp
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public interface IItemService
{
    Task<ItemsResponse> GetItemsAsync(GetItemsRequest request);
    Task<NearbyItemsResponse> GetNearbyItemsAsync(GetNearbyItemsRequest request);
    Task<ItemDetailResponse> GetItemAsync(int id);
    Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request);
    Task<UpdateItemResponse> UpdateItemAsync(int id, UpdateItemRequest request);
    Task<CategoriesResponse> GetCategoriesAsync();
}
```

- [ ] **Step 4: Create `RemoteItemService.cs`**

```csharp
using System.Net.Http.Json;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using static System.FormattableString;

namespace RentalApp.Services;

public class RemoteItemService : RemoteServiceBase, IItemService
{
    private readonly IApiClient _apiClient;

    public RemoteItemService(IApiClient apiClient) => _apiClient = apiClient;

    public async Task<ItemsResponse> GetItemsAsync(GetItemsRequest request)
    {
        var query = Invariant($"items?page={request.Page}&pageSize={request.PageSize}");
        if (request.Category != null)
            query += $"&category={Uri.EscapeDataString(request.Category)}";
        if (!string.IsNullOrEmpty(request.Search))
            query += $"&search={Uri.EscapeDataString(request.Search)}";

        var response = await _apiClient.GetAsync(query);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ItemsResponse>()
            ?? throw new InvalidOperationException("Empty items response from API");
    }

    public async Task<NearbyItemsResponse> GetNearbyItemsAsync(GetNearbyItemsRequest request)
    {
        var query = Invariant($"items/nearby?lat={request.Lat}&lon={request.Lon}&radius={request.Radius}");
        if (request.Category != null)
            query += $"&category={Uri.EscapeDataString(request.Category)}";

        var response = await _apiClient.GetAsync(query);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<NearbyItemsResponse>()
            ?? throw new InvalidOperationException("Empty nearby items response from API");
    }

    public async Task<ItemDetailResponse> GetItemAsync(int id)
    {
        var response = await _apiClient.GetAsync($"items/{id}");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ItemDetailResponse>()
            ?? throw new InvalidOperationException("Empty item response from API");
    }

    public async Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "items",
            new
            {
                title = request.Title,
                description = request.Description,
                dailyRate = request.DailyRate,
                categoryId = request.CategoryId,
                latitude = request.Latitude,
                longitude = request.Longitude,
            }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CreateItemResponse>()
            ?? throw new InvalidOperationException("Empty create item response from API");
    }

    public async Task<UpdateItemResponse> UpdateItemAsync(int id, UpdateItemRequest request)
    {
        var response = await _apiClient.PutAsJsonAsync(
            $"items/{id}",
            new
            {
                title = request.Title,
                description = request.Description,
                dailyRate = request.DailyRate,
                isAvailable = request.IsAvailable,
            }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<UpdateItemResponse>()
            ?? throw new InvalidOperationException("Empty update item response from API");
    }

    public async Task<CategoriesResponse> GetCategoriesAsync()
    {
        var response = await _apiClient.GetAsync("categories");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CategoriesResponse>()
            ?? throw new InvalidOperationException("Empty categories response from API");
    }
}
```

- [ ] **Step 5: Run passing tests**

```bash
dotnet test RentalApp.Test --filter "RemoteItemServiceTests" -v minimal
```
Expected: all 5 pass.

- [ ] **Step 6: Commit**

```bash
git add RentalApp/Services/IItemService.cs RentalApp/Services/RemoteItemService.cs RentalApp.Test/Services/RemoteItemServiceTests.cs
git commit -m "feat: add IItemService and RemoteItemService"
```

---

## Task 5: Create LocalItemService

**Files:**
- Create: `RentalApp/Services/LocalItemService.cs`
- Create: `RentalApp.Test/Services/LocalItemServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `RentalApp.Test/Services/LocalItemServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Http;
using RentalApp.Services;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public class LocalItemServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly AuthTokenState _tokenState = new();

    public LocalItemServiceTests(DatabaseFixture fixture)
    {
        _contextFactory = fixture.ContextFactory;
    }

    private LocalItemService CreateSut()
    {
        var itemRepo = new ItemRepository(_contextFactory);
        var catRepo = new CategoryRepository(_contextFactory);
        return new LocalItemService(_contextFactory, itemRepo, catRepo, _tokenState);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsCategories()
    {
        var result = await CreateSut().GetCategoriesAsync();
        Assert.NotNull(result.Categories);
    }

    [Fact]
    public async Task GetItemsAsync_ReturnsItemsResponse()
    {
        var result = await CreateSut().GetItemsAsync(new GetItemsRequest(1, 20, null, null));
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateItemAsync_NoSession_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSut().CreateItemAsync(new CreateItemRequest("Title", "Desc", 10m, 1, 55.0, -3.0))
        );
    }
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "LocalItemServiceTests" -v minimal
```
Expected: compile error — `LocalItemService` does not exist.

- [ ] **Step 3: Create `LocalItemService.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Http;
using GeoFactory = NetTopologySuite.Geometries.GeometryFactory;
using GeoPoint = NetTopologySuite.Geometries.Point;
using NtsCoordinate = NetTopologySuite.Geometries.Coordinate;
using NtsPrecisionModel = NetTopologySuite.Geometries.PrecisionModel;

namespace RentalApp.Services;

public class LocalItemService : IItemService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IItemRepository _itemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly AuthTokenState _tokenState;

    private static readonly GeoFactory _geoFactory = new(new NtsPrecisionModel(), 4326);

    public LocalItemService(
        IDbContextFactory<AppDbContext> contextFactory,
        IItemRepository itemRepository,
        ICategoryRepository categoryRepository,
        AuthTokenState tokenState)
    {
        _contextFactory = contextFactory;
        _itemRepository = itemRepository;
        _categoryRepository = categoryRepository;
        _tokenState = tokenState;
    }

    public async Task<ItemsResponse> GetItemsAsync(GetItemsRequest request)
    {
        var totalItems = await _itemRepository.CountItemsAsync(request.Category, request.Search);
        var dbItems = await _itemRepository.GetItemsAsync(request.Category, request.Search, request.Page, request.PageSize);
        var items = dbItems.Select(ToItemSummary).ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalItems / request.PageSize) : 0;
        return new ItemsResponse(items, TotalItems: totalItems, Page: request.Page, PageSize: request.PageSize, TotalPages: totalPages);
    }

    public async Task<NearbyItemsResponse> GetNearbyItemsAsync(GetNearbyItemsRequest request)
    {
        var origin = _geoFactory.CreatePoint(new NtsCoordinate(request.Lon, request.Lat));
        var radiusMeters = request.Radius * 1000;

        var dbItems = await _itemRepository.GetNearbyItemsAsync(origin, radiusMeters, request.Category, 1, int.MaxValue);
        var items = dbItems.Select(i => ToNearbyItem(i, origin)).ToList();

        return new NearbyItemsResponse(items, new SearchLocationResponse(request.Lat, request.Lon), request.Radius, items.Count);
    }

    public async Task<ItemDetailResponse> GetItemAsync(int id)
    {
        var dbItem = await _itemRepository.GetItemAsync(id)
            ?? throw new InvalidOperationException($"Item {id} not found.");
        return ToItemDetail(dbItem);
    }

    public async Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request)
    {
        if (!_tokenState.HasSession)
            throw new InvalidOperationException("No user is currently authenticated");

        var ownerId = int.Parse(_tokenState.CurrentToken!);
        var location = _geoFactory.CreatePoint(new NtsCoordinate(request.Longitude, request.Latitude));
        var dbItem = await _itemRepository.CreateItemAsync(
            request.Title, request.Description, request.DailyRate,
            request.CategoryId, ownerId, location
        );

        return new CreateItemResponse(
            dbItem.Id, dbItem.Title, dbItem.Description, dbItem.DailyRate,
            dbItem.CategoryId, dbItem.Category.Name, dbItem.OwnerId,
            $"{dbItem.Owner.FirstName} {dbItem.Owner.LastName}",
            request.Latitude, request.Longitude, dbItem.IsAvailable, dbItem.CreatedAt ?? DateTime.UtcNow
        );
    }

    public async Task<UpdateItemResponse> UpdateItemAsync(int id, UpdateItemRequest request)
    {
        var dbItem = await _itemRepository.UpdateItemAsync(id, request.Title, request.Description, request.DailyRate, request.IsAvailable);
        return new UpdateItemResponse(dbItem.Id, dbItem.Title, dbItem.Description ?? string.Empty, dbItem.DailyRate, dbItem.IsAvailable);
    }

    public async Task<CategoriesResponse> GetCategoriesAsync()
    {
        var results = await _categoryRepository.GetAllAsync();
        var categories = results.Select(r => new CategoryResponse(r.Category.Id, r.Category.Name, r.Category.Slug, r.ItemCount)).ToList();
        return new CategoriesResponse(categories);
    }

    private static ItemSummaryResponse ToItemSummary(Database.Models.Item i) =>
        new(i.Id, i.Title, i.Description, i.DailyRate, i.CategoryId, i.Category.Name,
            i.OwnerId, $"{i.Owner.FirstName} {i.Owner.LastName}", OwnerRating: null,
            i.IsAvailable, AverageRating: null, i.CreatedAt ?? DateTime.UtcNow);

    private static NearbyItemResponse ToNearbyItem(Database.Models.Item i, GeoPoint origin) =>
        new(i.Id, i.Title, i.Description, i.DailyRate, i.CategoryId, i.Category.Name,
            i.OwnerId, $"{i.Owner.FirstName} {i.Owner.LastName}",
            Latitude: i.Location.Y, Longitude: i.Location.X,
            Distance: i.Location.Distance(origin) / 1000.0, i.IsAvailable, AverageRating: null);

    private static ItemDetailResponse ToItemDetail(Database.Models.Item i) =>
        new(i.Id, i.Title, i.Description, i.DailyRate, i.CategoryId, i.Category.Name,
            i.OwnerId, $"{i.Owner.FirstName} {i.Owner.LastName}", OwnerRating: null,
            Latitude: i.Location.Y, Longitude: i.Location.X, i.IsAvailable,
            AverageRating: null, TotalReviews: 0, i.CreatedAt ?? DateTime.UtcNow, Reviews: []);
}
```

- [ ] **Step 4: Run passing tests**

```bash
dotnet test RentalApp.Test --filter "LocalItemServiceTests" -v minimal
```
Expected: all 3 pass.

- [ ] **Step 5: Commit**

```bash
git add RentalApp/Services/IItemService.cs RentalApp/Services/LocalItemService.cs RentalApp.Test/Services/LocalItemServiceTests.cs
git commit -m "feat: add LocalItemService"
```

---

## Task 6: Create IRentalService + RemoteRentalService + LocalRentalService

**Files:**
- Create: `RentalApp/Services/IRentalService.cs`
- Create: `RentalApp/Services/RemoteRentalService.cs`
- Create: `RentalApp/Services/LocalRentalService.cs`
- Create: `RentalApp.Test/Services/RemoteRentalServiceTests.cs`
- Create: `RentalApp.Test/Services/LocalRentalServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `RentalApp.Test/Services/RemoteRentalServiceTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class RemoteRentalServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();

    private RemoteRentalService CreateSut() => new(_apiClient);

    [Fact]
    public async Task GetIncomingRentalsAsync_SuccessResponse_ReturnsRentals()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("rentals/incoming")))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { rentals = Array.Empty<object>(), totalItems = 0, page = 1, pageSize = 20, totalPages = 0 }),
            });

        var result = await CreateSut().GetIncomingRentalsAsync(new GetRentalsRequest(null, 1, 20));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOutgoingRentalsAsync_SuccessResponse_ReturnsRentals()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("rentals/outgoing")))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { rentals = Array.Empty<object>(), totalItems = 0, page = 1, pageSize = 20, totalPages = 0 }),
            });

        var result = await CreateSut().GetOutgoingRentalsAsync(new GetRentalsRequest(null, 1, 20));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetRentalAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .GetAsync("rentals/99")
            .Returns(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent.Create(new { error = "NotFound", message = "Not found" }),
            });

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateSut().GetRentalAsync(99)
        );
    }
}
```

Create `RentalApp.Test/Services/LocalRentalServiceTests.cs`:

```csharp
using RentalApp.Contracts.Requests;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class LocalRentalServiceTests
{
    private LocalRentalService CreateSut() => new();

    [Fact]
    public async Task GetIncomingRentalsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => CreateSut().GetIncomingRentalsAsync(new GetRentalsRequest(null, 1, 20))
        );
    }

    [Fact]
    public async Task GetOutgoingRentalsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => CreateSut().GetOutgoingRentalsAsync(new GetRentalsRequest(null, 1, 20))
        );
    }

    [Fact]
    public async Task GetRentalAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() => CreateSut().GetRentalAsync(1));
    }

    [Fact]
    public async Task CreateRentalAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => CreateSut().CreateRentalAsync(new CreateRentalRequest(1, DateTime.Today, DateTime.Today.AddDays(1)))
        );
    }

    [Fact]
    public async Task UpdateRentalStatusAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => CreateSut().UpdateRentalStatusAsync(1, new UpdateRentalStatusRequest("approved"))
        );
    }
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "RemoteRentalServiceTests|LocalRentalServiceTests" -v minimal
```
Expected: compile error — these types do not exist yet.

- [ ] **Step 3: Create `IRentalService.cs`**

```csharp
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public interface IRentalService
{
    Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request);
    Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request);
    Task<RentalDetailResponse> GetRentalAsync(int id);
    Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request);
    Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(int id, UpdateRentalStatusRequest request);
}
```

- [ ] **Step 4: Create `RemoteRentalService.cs`**

```csharp
using System.Net.Http.Json;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;

namespace RentalApp.Services;

public class RemoteRentalService : RemoteServiceBase, IRentalService
{
    private readonly IApiClient _apiClient;

    public RemoteRentalService(IApiClient apiClient) => _apiClient = apiClient;

    public Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request) =>
        GetRentalsAsync("rentals/incoming", request.Status);

    public Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request) =>
        GetRentalsAsync("rentals/outgoing", request.Status);

    public async Task<RentalDetailResponse> GetRentalAsync(int id)
    {
        var response = await _apiClient.GetAsync($"rentals/{id}");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RentalDetailResponse>()
            ?? throw new InvalidOperationException("Empty rental response from API");
    }

    public async Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "rentals",
            new
            {
                itemId = request.ItemId,
                startDate = request.StartDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                endDate = request.EndDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RentalSummaryResponse>()
            ?? throw new InvalidOperationException("Empty create rental response from API");
    }

    public async Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(int id, UpdateRentalStatusRequest request)
    {
        var response = await _apiClient.PutAsJsonAsync($"rentals/{id}/status", new { status = request.Status });
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<UpdateRentalStatusResponse>()
            ?? throw new InvalidOperationException("Empty update status response from API");
    }

    private async Task<RentalsListResponse> GetRentalsAsync(string path, string? status)
    {
        var query = status != null ? $"{path}?status={Uri.EscapeDataString(status)}" : path;
        var response = await _apiClient.GetAsync(query);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RentalsListResponse>()
            ?? throw new InvalidOperationException("Empty rentals response from API");
    }
}
```

- [ ] **Step 5: Create `LocalRentalService.cs`**

```csharp
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public class LocalRentalService : IRentalService
{
    public Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    public Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    public Task<RentalDetailResponse> GetRentalAsync(int id) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    public Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    public Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(int id, UpdateRentalStatusRequest request) =>
        throw new NotImplementedException("Rental support requires local DB entities");
}
```

- [ ] **Step 6: Run passing tests**

```bash
dotnet test RentalApp.Test --filter "RemoteRentalServiceTests|LocalRentalServiceTests" -v minimal
```
Expected: all 8 pass.

- [ ] **Step 7: Commit**

```bash
git add RentalApp/Services/IRentalService.cs RentalApp/Services/RemoteRentalService.cs RentalApp/Services/LocalRentalService.cs RentalApp.Test/Services/RemoteRentalServiceTests.cs RentalApp.Test/Services/LocalRentalServiceTests.cs
git commit -m "feat: add IRentalService with Remote and Local implementations"
```

---

## Task 7: Create IReviewService + RemoteReviewService + LocalReviewService

**Files:**
- Create: `RentalApp/Services/IReviewService.cs`
- Create: `RentalApp/Services/RemoteReviewService.cs`
- Create: `RentalApp/Services/LocalReviewService.cs`
- Create: `RentalApp.Test/Services/RemoteReviewServiceTests.cs`
- Create: `RentalApp.Test/Services/LocalReviewServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `RentalApp.Test/Services/RemoteReviewServiceTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class RemoteReviewServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();

    private RemoteReviewService CreateSut() => new(_apiClient);

    [Fact]
    public async Task GetItemReviewsAsync_SuccessResponse_ReturnsReviews()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("items/5/reviews")))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { reviews = Array.Empty<object>(), totalItems = 0, page = 1, pageSize = 20, totalPages = 0 }),
            });

        var result = await CreateSut().GetItemReviewsAsync(5, new GetReviewsRequest(1, 20));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUserReviewsAsync_SuccessResponse_ReturnsReviews()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("users/3/reviews")))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { reviews = Array.Empty<object>(), totalItems = 0, page = 1, pageSize = 20, totalPages = 0 }),
            });

        var result = await CreateSut().GetUserReviewsAsync(3, new GetReviewsRequest(1, 20));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateReviewAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .PostAsJsonAsync("reviews", Arg.Any<object>())
            .Returns(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(new { error = "Bad", message = "Invalid" }),
            });

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateSut().CreateReviewAsync(new CreateReviewRequest(1, 5, "Great"))
        );
    }
}
```

Create `RentalApp.Test/Services/LocalReviewServiceTests.cs`:

```csharp
using RentalApp.Contracts.Requests;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class LocalReviewServiceTests
{
    private LocalReviewService CreateSut() => new();

    [Fact]
    public async Task GetItemReviewsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => CreateSut().GetItemReviewsAsync(1, new GetReviewsRequest(1, 20))
        );
    }

    [Fact]
    public async Task GetUserReviewsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => CreateSut().GetUserReviewsAsync(1, new GetReviewsRequest(1, 20))
        );
    }

    [Fact]
    public async Task CreateReviewAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(
            () => CreateSut().CreateReviewAsync(new CreateReviewRequest(1, 5, "Great"))
        );
    }
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "RemoteReviewServiceTests|LocalReviewServiceTests" -v minimal
```
Expected: compile error.

- [ ] **Step 3: Create `IReviewService.cs`**

```csharp
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public interface IReviewService
{
    Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request);
    Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request);
    Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request);
}
```

- [ ] **Step 4: Create `RemoteReviewService.cs`**

```csharp
using System.Net.Http.Json;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using static System.FormattableString;

namespace RentalApp.Services;

public class RemoteReviewService : RemoteServiceBase, IReviewService
{
    private readonly IApiClient _apiClient;

    public RemoteReviewService(IApiClient apiClient) => _apiClient = apiClient;

    public async Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request)
    {
        var response = await _apiClient.GetAsync(
            Invariant($"items/{itemId}/reviews?page={request.Page}&pageSize={request.PageSize}")
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ReviewsResponse>()
            ?? throw new InvalidOperationException("Empty reviews response from API");
    }

    public async Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request)
    {
        var response = await _apiClient.GetAsync(
            Invariant($"users/{userId}/reviews?page={request.Page}&pageSize={request.PageSize}")
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ReviewsResponse>()
            ?? throw new InvalidOperationException("Empty reviews response from API");
    }

    public async Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "reviews",
            new { rentalId = request.RentalId, rating = request.Rating, comment = request.Comment }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CreateReviewResponse>()
            ?? throw new InvalidOperationException("Empty create review response from API");
    }
}
```

- [ ] **Step 5: Create `LocalReviewService.cs`**

```csharp
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public class LocalReviewService : IReviewService
{
    public Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");

    public Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");

    public Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");
}
```

- [ ] **Step 6: Run passing tests**

```bash
dotnet test RentalApp.Test --filter "RemoteReviewServiceTests|LocalReviewServiceTests" -v minimal
```
Expected: all 6 pass.

- [ ] **Step 7: Commit**

```bash
git add RentalApp/Services/IReviewService.cs RentalApp/Services/RemoteReviewService.cs RentalApp/Services/LocalReviewService.cs RentalApp.Test/Services/RemoteReviewServiceTests.cs RentalApp.Test/Services/LocalReviewServiceTests.cs
git commit -m "feat: add IReviewService with Remote and Local implementations"
```

---

## Task 8: Update AppShellViewModel

**Files:**
- Modify: `RentalApp/ViewModels/AppShellViewModel.cs`
- Modify: `RentalApp.Test/ViewModels/AppShellViewModelTests.cs`

- [ ] **Step 1: Replace the test file**

Replace `RentalApp.Test/ViewModels/AppShellViewModelTests.cs` with:

```csharp
using NSubstitute;
using RentalApp.Constants;
using RentalApp.Http;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class AppShellViewModelTests
{
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private TestableAppShellViewModel CreateSut(bool confirmLogout = false) =>
        new(_tokenState, _credentialStore, _navigationService) { ConfirmResult = confirmLogout };

    private sealed class TestableAppShellViewModel : AppShellViewModel
    {
        public bool ConfirmResult { get; set; }
        public TestableAppShellViewModel(AuthTokenState tokenState, ICredentialStore credentialStore, INavigationService navigationService)
            : base(tokenState, credentialStore, navigationService) { }
        protected override Task<bool> ConfirmLogoutAsync() => Task.FromResult(ConfirmResult);
    }

    // ── LogoutCommand — CanExecute ─────────────────────────────────────

    [Fact]
    public void LogoutCommand_WhenSessionActive_CanExecute()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut();

        Assert.True(sut.LogoutCommand.CanExecute(null));
    }

    [Fact]
    public void LogoutCommand_WhenNoSession_CannotExecute()
    {
        var sut = CreateSut();

        Assert.False(sut.LogoutCommand.CanExecute(null));
    }

    // ── LogoutAsync — confirmed ────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_WhenConfirmed_ClearsToken()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut(confirmLogout: true);

        await sut.LogoutCommand.ExecuteAsync(null);

        Assert.Null(_tokenState.CurrentToken);
    }

    [Fact]
    public async Task LogoutAsync_WhenConfirmed_ClearsCredentials()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut(confirmLogout: true);

        await sut.LogoutCommand.ExecuteAsync(null);

        await _credentialStore.Received(1).ClearAsync();
    }

    [Fact]
    public async Task LogoutAsync_WhenConfirmed_NavigatesToLogin()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut(confirmLogout: true);

        await sut.LogoutCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Login);
    }

    // ── LogoutAsync — cancelled ────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_WhenCancelled_DoesNotClearToken()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut(confirmLogout: false);

        await sut.LogoutCommand.ExecuteAsync(null);

        Assert.Equal("eyJ...", _tokenState.CurrentToken);
    }

    [Fact]
    public async Task LogoutAsync_WhenCancelled_DoesNotNavigate()
    {
        _tokenState.CurrentToken = "eyJ...";
        var sut = CreateSut(confirmLogout: false);

        await sut.LogoutCommand.ExecuteAsync(null);

        await _navigationService.DidNotReceive().NavigateToAsync(Arg.Any<string>());
    }

    // ── Navigation commands ────────────────────────────────────────────

    [Fact]
    public async Task NavigateToProfileCommand_NavigatesToTemp()
    {
        var sut = CreateSut();

        await sut.NavigateToProfileCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Temp);
    }

    [Fact]
    public async Task NavigateToSettingsCommand_NavigatesToTemp()
    {
        var sut = CreateSut();

        await sut.NavigateToSettingsCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Temp);
    }
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "AppShellViewModelTests" -v minimal
```
Expected: compile errors — `AppShellViewModel` still has the old constructor signature.

- [ ] **Step 3: Replace `AppShellViewModel.cs`**

```csharp
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class AppShellViewModel : BaseViewModel
{
    private readonly AuthTokenState _tokenState;
    private readonly ICredentialStore _credentialStore;
    private readonly INavigationService _navigationService;

    public AppShellViewModel(
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService)
    {
        _tokenState = tokenState;
        _credentialStore = credentialStore;
        _navigationService = navigationService;
        _tokenState.AuthenticationStateChanged += OnAuthenticationStateChanged;
        Title = "RentalApp";
    }

    private bool CanExecuteAuthenticatedAction() => _tokenState.HasSession;

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        LogoutCommand.NotifyCanExecuteChanged();
        NavigateToProfileCommand.NotifyCanExecuteChanged();
        NavigateToSettingsCommand.NotifyCanExecuteChanged();
    }

    protected virtual Task<bool> ConfirmLogoutAsync() =>
        Application.Current?.Windows[0]?.Page?.DisplayAlertAsync(
            "Logout", "Are you sure you want to logout?", "Yes", "No"
        ) ?? Task.FromResult(false);

    [RelayCommand(CanExecute = nameof(CanExecuteAuthenticatedAction))]
    private async Task LogoutAsync()
    {
        if (!await ConfirmLogoutAsync())
            return;

        await _credentialStore.ClearAsync();
        _tokenState.ClearToken();
        await _navigationService.NavigateToAsync(Routes.Login);

        LogoutCommand.NotifyCanExecuteChanged();
        NavigateToProfileCommand.NotifyCanExecuteChanged();
        NavigateToSettingsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task NavigateToProfileAsync() =>
        await _navigationService.NavigateToAsync(Routes.Temp);

    [RelayCommand]
    private async Task NavigateToSettingsAsync() =>
        await _navigationService.NavigateToAsync(Routes.Temp);
}
```

- [ ] **Step 4: Run passing tests**

```bash
dotnet test RentalApp.Test --filter "AppShellViewModelTests" -v minimal
```
Expected: all 9 pass.

- [ ] **Step 5: Commit**

```bash
git add RentalApp/ViewModels/AppShellViewModel.cs RentalApp.Test/ViewModels/AppShellViewModelTests.cs
git commit -m "refactor: migrate AppShellViewModel from IAuthenticationService to AuthTokenState + ICredentialStore"
```

---

## Task 9: Update LoginViewModel

**Files:**
- Modify: `RentalApp/ViewModels/LoginViewModel.cs`
- Modify: `RentalApp.Test/ViewModels/LoginViewModelTests.cs`

- [ ] **Step 1: Replace the test file**

Replace `RentalApp.Test/ViewModels/LoginViewModelTests.cs` with:

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class LoginViewModelTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private LoginViewModel CreateSut() => new(_authService, _tokenState, _credentialStore, _navigationService);

    private static LoginResponse FakeLogin() => new("eyJ...", DateTime.UtcNow.AddHours(1), 1);

    // ── LoginAsync — success ───────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_SetsTokenOnAuthTokenState()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Equal("eyJ...", _tokenState.CurrentToken);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_NavigatesToMain()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";

        await sut.LoginCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Main);
    }

    [Fact]
    public async Task LoginAsync_RememberMeTrue_SavesCredentials()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";
        sut.RememberMe = true;

        await sut.LoginCommand.ExecuteAsync(null);

        await _credentialStore.Received(1).SaveAsync("jane@example.com", "Password1!");
    }

    [Fact]
    public async Task LoginAsync_RememberMeFalse_DoesNotSaveCredentials()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";
        sut.RememberMe = false;

        await sut.LoginCommand.ExecuteAsync(null);

        await _credentialStore.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    // ── LoginAsync — failure ───────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ServiceThrows_SetsError()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>())
            .ThrowsAsync(new HttpRequestException("Invalid credentials"));
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "wrong";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Invalid credentials", sut.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ServiceThrows_DoesNotNavigate()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>())
            .ThrowsAsync(new HttpRequestException("Invalid credentials"));
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "wrong";

        await sut.LoginCommand.ExecuteAsync(null);

        await _navigationService.DidNotReceive().NavigateToAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_ServiceThrows_DoesNotSetToken()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>())
            .ThrowsAsync(new HttpRequestException("Invalid credentials"));
        var sut = CreateSut();
        sut.Email = "jane@example.com";
        sut.Password = "wrong";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.Null(_tokenState.CurrentToken);
    }

    // ── LoginAsync — empty fields ──────────────────────────────────────

    [Theory]
    [InlineData("", "Password1!")]
    [InlineData("jane@example.com", "")]
    [InlineData("", "")]
    public async Task LoginAsync_EmptyFields_DoesNotCallService(string email, string password)
    {
        var sut = CreateSut();
        sut.Email = email;
        sut.Password = password;

        await sut.LoginCommand.ExecuteAsync(null);

        await _authService.DidNotReceive().LoginAsync(Arg.Any<LoginRequest>());
    }

    [Fact]
    public async Task LoginAsync_EmptyFields_SetsError()
    {
        var sut = CreateSut();
        sut.Email = "";
        sut.Password = "";

        await sut.LoginCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Please enter both email and password", sut.ErrorMessage);
    }

    // ── LoginCommand — CanExecute ──────────────────────────────────────

    [Fact]
    public void LoginCommand_WhenNotBusy_CanExecute()
    {
        Assert.True(CreateSut().LoginCommand.CanExecute(null));
    }

    [Fact]
    public void LoginCommand_WhileIsBusy_CannotExecute()
    {
        var sut = CreateSut();
        sut.IsBusy = true;

        Assert.False(sut.LoginCommand.CanExecute(null));
    }

    // ── InitializeAsync ────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_NoSavedCredentials_FieldsRemainDefault()
    {
        _credentialStore.GetAsync().Returns((ValueTuple<string, string>?)null);
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal(string.Empty, sut.Email);
        Assert.Equal(string.Empty, sut.Password);
        Assert.False(sut.RememberMe);
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_PopulatesFieldsAndSetsRememberMe()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "Password1!"));
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal("jane@example.com", sut.Email);
        Assert.Equal("Password1!", sut.Password);
        Assert.True(sut.RememberMe);
    }

    // ── ApplyQueryAttributes ───────────────────────────────────────────

    [Fact]
    public void ApplyQueryAttributes_SessionExpiredTrue_SetsError()
    {
        var sut = CreateSut();

        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["sessionExpired"] = true });

        Assert.True(sut.HasError);
        Assert.Equal("Your session has expired. Please log in again.", sut.ErrorMessage);
    }

    [Fact]
    public void ApplyQueryAttributes_OtherQuery_ClearsAnyExistingError()
    {
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["sessionExpired"] = true });

        sut.ApplyQueryAttributes(new Dictionary<string, object>());

        Assert.False(sut.HasError);
    }

    // ── NavigateToRegisterAsync ────────────────────────────────────────

    [Fact]
    public async Task NavigateToRegisterCommand_NavigatesToRegister()
    {
        await CreateSut().NavigateToRegisterCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Register);
    }
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "LoginViewModelTests" -v minimal
```
Expected: compile errors — `LoginViewModel` still uses `IAuthenticationService`.

- [ ] **Step 3: Replace `LoginViewModel.cs`**

```csharp
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class LoginViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IAuthService _authService;
    private readonly AuthTokenState _tokenState;
    private readonly ICredentialStore _credentialStore;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool rememberMe;

    public LoginViewModel(
        IAuthService authService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService)
    {
        _authService = authService;
        _tokenState = tokenState;
        _credentialStore = credentialStore;
        _navigationService = navigationService;
        Title = "Login";
    }

    public async Task InitializeAsync()
    {
        var credentials = await _credentialStore.GetAsync();
        if (credentials is null)
            return;

        Email = credentials.Value.Email;
        Password = credentials.Value.Password;
        RememberMe = true;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("sessionExpired", out var value) && value is true)
            SetError("Your session has expired. Please log in again.");
        else
            ClearError();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsBusy))
            LoginCommand.NotifyCanExecuteChanged();
    }

    private bool CanLogin() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("Please enter both email and password");
            return;
        }

        IsBusy = true;
        ClearError();

        try
        {
            var response = await _authService.LoginAsync(new LoginRequest(Email, Password));
            _tokenState.CurrentToken = response.Token;

            if (RememberMe)
                await _credentialStore.SaveAsync(Email, Password);

            await _navigationService.NavigateToAsync(Routes.Main);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToRegisterAsync() =>
        await _navigationService.NavigateToAsync(Routes.Register);
}
```

- [ ] **Step 4: Run passing tests**

```bash
dotnet test RentalApp.Test --filter "LoginViewModelTests" -v minimal
```
Expected: all 15 pass.

- [ ] **Step 5: Commit**

```bash
git add RentalApp/ViewModels/LoginViewModel.cs RentalApp.Test/ViewModels/LoginViewModelTests.cs
git commit -m "refactor: migrate LoginViewModel to own token-setting and credential-saving"
```

---

## Task 10: Update LoadingViewModel

**Files:**
- Modify: `RentalApp/ViewModels/LoadingViewModel.cs`
- Modify: `RentalApp.Test/ViewModels/LoadingViewModelTests.cs`

- [ ] **Step 1: Replace the test file**

Replace `RentalApp.Test/ViewModels/LoadingViewModelTests.cs` with:

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class LoadingViewModelTests
{
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private LoadingViewModel CreateSut() => new(_credentialStore, _authService, _tokenState, _navigationService);

    private static LoginResponse FakeLogin() => new("eyJ...", DateTime.UtcNow.AddHours(1), 1);

    [Fact]
    public async Task InitializeAsync_NoSavedCredentials_NavigatesToLogin()
    {
        _credentialStore.GetAsync().Returns((ValueTuple<string, string>?)null);

        await CreateSut().InitializeAsync();

        await _navigationService.Received(1).NavigateToAsync(Routes.Login);
        await _authService.DidNotReceive().LoginAsync(Arg.Any<LoginRequest>());
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_LoginSucceeds_NavigatesToMain()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "Password1!"));
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());

        await CreateSut().InitializeAsync();

        await _navigationService.Received(1).NavigateToAsync(Routes.Main);
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_LoginSucceeds_SetsToken()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "Password1!"));
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());

        await CreateSut().InitializeAsync();

        Assert.Equal("eyJ...", _tokenState.CurrentToken);
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_LoginFails_NavigatesToLogin()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "expired"));
        _authService.LoginAsync(Arg.Any<LoginRequest>())
            .ThrowsAsync(new HttpRequestException("Invalid credentials"));

        await CreateSut().InitializeAsync();

        await _navigationService.Received(1).NavigateToAsync(Routes.Login);
    }

    [Fact]
    public async Task InitializeAsync_SavedCredentials_UsesStoredEmailAndPassword()
    {
        _credentialStore.GetAsync().Returns(("jane@example.com", "Password1!"));
        _authService.LoginAsync(Arg.Any<LoginRequest>()).Returns(FakeLogin());

        await CreateSut().InitializeAsync();

        await _authService.Received(1).LoginAsync(
            Arg.Is<LoginRequest>(r => r.Email == "jane@example.com" && r.Password == "Password1!")
        );
    }
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "LoadingViewModelTests" -v minimal
```
Expected: compile errors — constructor signature mismatch.

- [ ] **Step 3: Replace `LoadingViewModel.cs`**

```csharp
using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public class LoadingViewModel : BaseViewModel
{
    private readonly ICredentialStore _credentialStore;
    private readonly IAuthService _authService;
    private readonly AuthTokenState _tokenState;
    private readonly INavigationService _navigationService;

    public LoadingViewModel(
        ICredentialStore credentialStore,
        IAuthService authService,
        AuthTokenState tokenState,
        INavigationService navigationService)
    {
        _credentialStore = credentialStore;
        _authService = authService;
        _tokenState = tokenState;
        _navigationService = navigationService;
    }

    public async Task InitializeAsync()
    {
        var credentials = await _credentialStore.GetAsync();

        if (credentials is null)
        {
            await _navigationService.NavigateToAsync(Routes.Login);
            return;
        }

        try
        {
            var response = await _authService.LoginAsync(
                new LoginRequest(credentials.Value.Email, credentials.Value.Password)
            );
            _tokenState.CurrentToken = response.Token;
            await _navigationService.NavigateToAsync(Routes.Main);
        }
        catch
        {
            await _navigationService.NavigateToAsync(Routes.Login);
        }
    }
}
```

- [ ] **Step 4: Run passing tests**

```bash
dotnet test RentalApp.Test --filter "LoadingViewModelTests" -v minimal
```
Expected: all 5 pass.

- [ ] **Step 5: Commit**

```bash
git add RentalApp/ViewModels/LoadingViewModel.cs RentalApp.Test/ViewModels/LoadingViewModelTests.cs
git commit -m "refactor: migrate LoadingViewModel auto-login to use IAuthService + AuthTokenState directly"
```

---

## Task 11: Update RegisterViewModel

**Files:**
- Modify: `RentalApp/ViewModels/RegisterViewModel.cs`
- Modify: `RentalApp.Test/ViewModels/RegisterViewModelTests.cs`

- [ ] **Step 1: Replace the test file**

Replace `RentalApp.Test/ViewModels/RegisterViewModelTests.cs` with:

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class RegisterViewModelTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private RegisterViewModel CreateSut() => new(_authService, _navigationService);

    private static RegisterViewModel WithValidForm(RegisterViewModel sut)
    {
        sut.FirstName = "Jane";
        sut.LastName = "Doe";
        sut.Email = "jane@example.com";
        sut.Password = "Password1!";
        sut.ConfirmPassword = "Password1!";
        sut.AcceptTerms = true;
        return sut;
    }

    private static RegisterResponse FakeRegister() =>
        new(1, "jane@example.com", "Jane", "Doe", DateTime.UtcNow);

    // ── RegisterAsync — navigation ─────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_Success_NavigatesBack()
    {
        _authService.RegisterAsync(Arg.Any<RegisterRequest>()).Returns(FakeRegister());
        var sut = WithValidForm(CreateSut());

        await sut.RegisterCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateBackAsync();
    }

    [Fact]
    public async Task RegisterAsync_ServiceThrows_DoesNotNavigate()
    {
        _authService.RegisterAsync(Arg.Any<RegisterRequest>())
            .ThrowsAsync(new HttpRequestException("Email already registered"));
        var sut = WithValidForm(CreateSut());

        await sut.RegisterCommand.ExecuteAsync(null);

        await _navigationService.DidNotReceive().NavigateBackAsync();
    }

    [Fact]
    public async Task RegisterAsync_InvalidForm_DoesNotCallService()
    {
        var sut = CreateSut();

        await sut.RegisterCommand.ExecuteAsync(null);

        await _authService.DidNotReceive().RegisterAsync(Arg.Any<RegisterRequest>());
    }

    // ── RegisterAsync — error state ────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ServiceThrows_SetsError()
    {
        _authService.RegisterAsync(Arg.Any<RegisterRequest>())
            .ThrowsAsync(new HttpRequestException("Email already registered"));
        var sut = WithValidForm(CreateSut());

        await sut.RegisterCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("Email already registered", sut.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_InvalidForm_SetsValidationError()
    {
        var sut = CreateSut();
        sut.FirstName = "";

        await sut.RegisterCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
    }

    [Fact]
    public async Task RegisterAsync_Success_ClearsError()
    {
        _authService.RegisterAsync(Arg.Any<RegisterRequest>()).Returns(FakeRegister());
        var sut = WithValidForm(CreateSut());

        await sut.RegisterCommand.ExecuteAsync(null);

        Assert.False(sut.HasError);
    }

    // ── NavigateBackToLoginAsync ───────────────────────────────────────

    [Fact]
    public async Task NavigateBackToLoginCommand_NavigatesBack()
    {
        await CreateSut().NavigateBackToLoginCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateBackAsync();
    }

    // ── RegisterCommand — CanExecute ───────────────────────────────────

    [Fact]
    public void RegisterCommand_WhenNotBusy_CanExecute()
    {
        Assert.True(CreateSut().RegisterCommand.CanExecute(null));
    }

    [Fact]
    public void RegisterCommand_WhileIsBusy_CannotExecute()
    {
        var sut = CreateSut();
        sut.IsBusy = true;

        Assert.False(sut.RegisterCommand.CanExecute(null));
    }
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "RegisterViewModelTests" -v minimal
```
Expected: compile errors.

- [ ] **Step 3: Replace `RegisterViewModel.cs`**

```csharp
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Helpers;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string lastName = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;
    [ObservableProperty] private bool acceptTerms;

    public RegisterViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Register";
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsBusy))
            RegisterCommand.NotifyCanExecuteChanged();
    }

    private bool CanRegister() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        if (!ValidateForm())
            return;

        IsBusy = true;
        ClearError();

        try
        {
            await _authService.RegisterAsync(new RegisterRequest(FirstName, LastName, Email, Password));
            await _navigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateBackToLoginAsync() =>
        await _navigationService.NavigateBackAsync();

    private bool ValidateForm()
    {
        var error = RegistrationValidator.Validate(FirstName, LastName, Email, Password, ConfirmPassword, AcceptTerms);
        if (error is not null)
        {
            SetError(error);
            return false;
        }
        return true;
    }
}
```

- [ ] **Step 4: Run passing tests**

```bash
dotnet test RentalApp.Test --filter "RegisterViewModelTests" -v minimal
```
Expected: all 9 pass.

- [ ] **Step 5: Commit**

```bash
git add RentalApp/ViewModels/RegisterViewModel.cs RentalApp.Test/ViewModels/RegisterViewModelTests.cs
git commit -m "refactor: migrate RegisterViewModel to use IAuthService directly"
```

---

## Task 12: Update MainViewModel

**Files:**
- Modify: `RentalApp/ViewModels/MainViewModel.cs`
- Modify: `RentalApp.Test/ViewModels/MainViewModelTests.cs`

- [ ] **Step 1: Replace the test file**

Replace `RentalApp.Test/ViewModels/MainViewModelTests.cs` with:

```csharp
using NSubstitute;
using RentalApp.Constants;
using RentalApp.Contracts.Responses;
using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class MainViewModelTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly INavigationService _navigationService = Substitute.For<INavigationService>();

    private static CurrentUserResponse MakeUser(string firstName = "Jane", string lastName = "Doe") =>
        new(1, "jane@example.com", firstName, lastName, null, 0, 0, DateTime.UtcNow);

    private MainViewModel CreateSut() => new(_authService, _navigationService);

    // ── InitializeAsync ────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_LoadsUserAndSetsWelcomeMessage()
    {
        _authService.GetCurrentUserAsync().Returns(MakeUser());
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal("Welcome, Jane Doe!", sut.WelcomeMessage);
    }

    [Fact]
    public async Task InitializeAsync_SetsCurrentUser()
    {
        var user = MakeUser();
        _authService.GetCurrentUserAsync().Returns(user);
        var sut = CreateSut();

        await sut.InitializeAsync();

        Assert.Equal(user, sut.CurrentUser);
    }

    // ── RefreshDataAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RefreshDataAsync_ReloadsUser()
    {
        _authService.GetCurrentUserAsync().Returns(MakeUser("Alice", "Smith"));
        var sut = CreateSut();

        await sut.RefreshDataCommand.ExecuteAsync(null);

        Assert.Equal("Welcome, Alice Smith!", sut.WelcomeMessage);
    }

    [Fact]
    public async Task RefreshDataAsync_IsBusyFalseAfterCompletion()
    {
        _authService.GetCurrentUserAsync().Returns(MakeUser());
        var sut = CreateSut();

        await sut.RefreshDataCommand.ExecuteAsync(null);

        Assert.False(sut.IsBusy);
    }

    // ── Navigation commands ────────────────────────────────────────────

    [Fact]
    public async Task NavigateToProfileCommand_NavigatesToTemp()
    {
        await CreateSut().NavigateToProfileCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.Temp);
    }

    [Fact]
    public async Task NavigateToItemsListCommand_NavigatesToItemsList()
    {
        await CreateSut().NavigateToItemsListCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.ItemsList);
    }

    [Fact]
    public async Task NavigateToNearbyItemsCommand_NavigatesToNearbyItems()
    {
        await CreateSut().NavigateToNearbyItemsCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.NearbyItems);
    }

    [Fact]
    public async Task NavigateToCreateItemCommand_NavigatesToCreateItem()
    {
        await CreateSut().NavigateToCreateItemCommand.ExecuteAsync(null);

        await _navigationService.Received(1).NavigateToAsync(Routes.CreateItem);
    }
}
```

- [ ] **Step 2: Run failing tests**

```bash
dotnet test RentalApp.Test --filter "MainViewModelTests" -v minimal
```
Expected: compile errors — constructor signature mismatch.

- [ ] **Step 3: Replace `MainViewModel.cs`**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Contracts.Responses;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private CurrentUserResponse? currentUser;

    [ObservableProperty]
    private string welcomeMessage = string.Empty;

    public MainViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Dashboard";
    }

    public async Task InitializeAsync()
    {
        CurrentUser = await _authService.GetCurrentUserAsync();
        WelcomeMessage = $"Welcome, {CurrentUser.FirstName} {CurrentUser.LastName}!";
    }

    [RelayCommand]
    private async Task NavigateToProfileAsync() =>
        await _navigationService.NavigateToAsync(Routes.Temp);

    [RelayCommand]
    private async Task NavigateToItemsListAsync() =>
        await _navigationService.NavigateToAsync(Routes.ItemsList);

    [RelayCommand]
    private async Task NavigateToNearbyItemsAsync() =>
        await _navigationService.NavigateToAsync(Routes.NearbyItems);

    [RelayCommand]
    private async Task NavigateToCreateItemAsync() =>
        await _navigationService.NavigateToAsync(Routes.CreateItem);

    [RelayCommand]
    private Task RefreshDataAsync() => RunAsync(InitializeAsync);
}
```

- [ ] **Step 4: Run passing tests**

```bash
dotnet test RentalApp.Test --filter "MainViewModelTests" -v minimal
```
Expected: all 8 pass.

- [ ] **Step 5: Commit**

```bash
git add RentalApp/ViewModels/MainViewModel.cs RentalApp.Test/ViewModels/MainViewModelTests.cs
git commit -m "refactor: migrate MainViewModel to use IAuthService, remove LogoutAsync"
```

---

## Task 13: Update MainPage (XAML + code-behind)

**Files:**
- Modify: `RentalApp/Views/MainPage.xaml`
- Modify: `RentalApp/Views/MainPage.xaml.cs`

- [ ] **Step 1: Update `MainPage.xaml`**

Change the logout `ToolbarItem` — remove the `Command` binding and add `x:Name`:

```xml
<ToolbarItem x:Name="LogoutToolbarItem" Text="Logout" IconImageSource="logout.png" />
```

The full `ContentPage.ToolbarItems` section becomes:

```xml
<ContentPage.ToolbarItems>
  <ToolbarItem
    Text="Profile"
    Command="{Binding NavigateToProfileCommand}"
    IconImageSource="user.png"
  />
  <ToolbarItem x:Name="LogoutToolbarItem" Text="Logout" IconImageSource="logout.png" />
</ContentPage.ToolbarItems>
```

- [ ] **Step 2: Replace `MainPage.xaml.cs`**

```csharp
using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel, AppShellViewModel shellViewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
        LogoutToolbarItem.Command = shellViewModel.LogoutCommand;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
```

- [ ] **Step 3: Build the project**

```bash
dotnet build RentalApp/RentalApp.csproj
```
Expected: successful build — `AppShellViewModel` is resolved from DI, `LogoutToolbarItem` exists by name.

- [ ] **Step 4: Commit**

```bash
git add RentalApp/Views/MainPage.xaml RentalApp/Views/MainPage.xaml.cs
git commit -m "refactor: wire MainPage logout toolbar to AppShellViewModel.LogoutCommand via code-behind"
```

---

## Task 14: Update MauiProgram.cs

**Files:**
- Modify: `RentalApp/MauiProgram.cs`

- [ ] **Step 1: Replace the API service registration block and remove IAuthenticationService**

The updated `MauiProgram.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Http;
using RentalApp.Services;
using RentalApp.ViewModels;
using RentalApp.Views;

namespace RentalApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<ICredentialStore, CredentialStore>();
        builder.Services.AddSingleton<AuthTokenState>();

        bool useSharedApi = Preferences.Default.Get("UseSharedApi", true);

        if (useSharedApi)
        {
            var baseAddress = new Uri("https://set09102-api.b-davison.workers.dev/");
            builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = baseAddress });
            builder.Services.AddSingleton<IApiClient, ApiClient>();
            builder.Services.AddSingleton<IAuthService, RemoteAuthService>();
            builder.Services.AddSingleton<IItemService, RemoteItemService>();
            builder.Services.AddSingleton<IRentalService, RemoteRentalService>();
            builder.Services.AddSingleton<IReviewService, RemoteReviewService>();
        }
        else
        {
            builder.Services.AddDbContextFactory<AppDbContext>();
            builder.Services.AddSingleton<IItemRepository, ItemRepository>();
            builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();
            builder.Services.AddSingleton<IAuthService>(sp => new LocalAuthService(
                sp.GetRequiredService<IDbContextFactory<AppDbContext>>(),
                sp.GetRequiredService<AuthTokenState>()
            ));
            builder.Services.AddSingleton<IItemService>(sp => new LocalItemService(
                sp.GetRequiredService<IDbContextFactory<AppDbContext>>(),
                sp.GetRequiredService<IItemRepository>(),
                sp.GetRequiredService<ICategoryRepository>(),
                sp.GetRequiredService<AuthTokenState>()
            ));
            builder.Services.AddSingleton<IRentalService, LocalRentalService>();
            builder.Services.AddSingleton<IReviewService, LocalReviewService>();
        }

        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        builder.Services.AddSingleton<AppShellViewModel>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<App>();

        builder.Services.AddTransient<LoadingViewModel>();
        builder.Services.AddTransient<LoadingPage>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddSingleton<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddSingleton<TempViewModel>();
        builder.Services.AddTransient<TempPage>();

        builder.Services.AddTransient<ItemsListViewModel>();
        builder.Services.AddTransient<ItemsListPage>();
        builder.Services.AddTransient<ItemDetailsViewModel>();
        builder.Services.AddTransient<ItemDetailsPage>();
        builder.Services.AddTransient<CreateItemViewModel>();
        builder.Services.AddTransient<CreateItemPage>();
        builder.Services.AddTransient<NearbyItemsViewModel>();
        builder.Services.AddTransient<NearbyItemsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

- [ ] **Step 2: Build the project**

```bash
dotnet build RentalApp/RentalApp.csproj
```
Expected: successful build.

- [ ] **Step 3: Commit**

```bash
git add RentalApp/MauiProgram.cs
git commit -m "refactor: replace IApiService/IAuthenticationService registrations with four domain service pairs"
```

---

## Task 15: Delete old files

**Files to delete:**
- `RentalApp/Services/IApiService.cs`
- `RentalApp/Services/IAuthenticationService.cs`
- `RentalApp/Services/AuthenticationService.cs`
- `RentalApp/Services/AuthenticationResult.cs`
- `RentalApp/Services/RemoteApiService.cs`
- `RentalApp/Services/LocalApiService.cs`
- `RentalApp.Test/Services/AuthenticationServiceTests.cs`

- [ ] **Step 1: Delete the files**

```bash
git rm RentalApp/Services/IApiService.cs \
       RentalApp/Services/IAuthenticationService.cs \
       RentalApp/Services/AuthenticationService.cs \
       RentalApp/Services/AuthenticationResult.cs \
       RentalApp/Services/RemoteApiService.cs \
       RentalApp/Services/LocalApiService.cs \
       RentalApp.Test/Services/AuthenticationServiceTests.cs
```

- [ ] **Step 2: Build to confirm no dangling references**

```bash
dotnet build RentalApp.sln
```
Expected: successful build with no errors.

- [ ] **Step 3: Commit**

```bash
git commit -m "refactor: delete IApiService, IAuthenticationService, AuthenticationService and their implementations"
```

---

## Task 16: Final verification

- [ ] **Step 1: Run the full test suite**

```bash
dotnet test RentalApp.sln -v minimal
```
Expected: all tests pass, no skipped tests, no compile errors.

- [ ] **Step 2: Check for stale references to deleted types**

```bash
grep -r "IApiService\|IAuthenticationService\|AuthenticationService\|AuthenticationResult\|RemoteApiService\|LocalApiService" \
     --include="*.cs" RentalApp/ RentalApp.Test/
```
Expected: no output (zero matches).

- [ ] **Step 3: Commit if clean**

```bash
git add -A
git commit -m "refactor: complete IApiService split into domain services"
```
