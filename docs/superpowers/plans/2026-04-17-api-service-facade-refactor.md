# API Service Facade Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce `IApiService` as the single switchable data-transport interface, with `RemoteApiService` (HTTP) and `LocalApiService` (DB) as its two implementations, and collapse the duplicate auth logic into a single `AuthenticationService` that wraps it.

**Architecture:** `IApiService` is a pure data-transport facade returning purpose-built DTO types from `RentalApp.Models` — never EF entities. `LoginAsync` returns `Task` (not `Task<AuthToken>`) — the bearer token is a remote transport detail stored as a contained side effect inside `RemoteApiService`. A single `AuthenticationService` implements `IAuthenticationService`, owns all auth-state management (`CurrentUser`, events, `RememberMe` credential persistence), and delegates data operations to `IApiService`. Only `IApiService` is switched in `MauiProgram.cs`; everything above it is backend-agnostic.

**Tech Stack:** .NET 10 MAUI, C#, Entity Framework Core with Npgsql (PostgreSQL), xUnit, NSubstitute (to be added), BCrypt.Net.

---

## Key Design Decisions

**`IApiService.LoginAsync` returns `Task`.**
The bearer token is a remote transport detail. `RemoteApiService` stores it in `AuthTokenState` after login so `AuthRefreshHandler` can attach it to subsequent requests — this side effect is fully contained within the remote transport infrastructure and invisible above `IApiService`. `LocalApiService` has no token concept; it stores the authenticated user profile in memory after verifying credentials. Returning a mock token from the local implementation would be a leaky abstraction — the return value would always be ignored, meaning the interface would be lying about its contract.

**`RememberMe` credential persistence lives in `AuthenticationService`.**
Persisting credentials is auth-state management, not data transport. Neither `RemoteApiService` nor `LocalApiService` should know about the `rememberMe` flag. `AuthenticationService` is the right owner: the logic is identical regardless of backend and belongs alongside the other auth-state concerns it already manages.

**All `IApiService` return types are DTOs in `RentalApp.Models`.**
The remote API returns richer shaped responses (e.g. `itemsListed`, `reviews` on a user profile) that don't map to flat EF entities. Using DTOs means both implementations map to a shared contract without leaking persistence or HTTP concerns into the interface. EF entities stay strictly inside `LocalApiService` as a persistence detail.

---

## File Map

### Create
- `RentalApp/Models/UserProfile.cs` — DTO returned by auth and user profile methods
- `RentalApp/Models/AuthToken.cs` — internal to `RemoteApiService`; used only to deserialise the login response
- `RentalApp/Models/ApiDomainStubs.cs` — empty stub DTOs (`Item`, `Category`, `Rental`, `Review`, `CreateItemRequest`, `UpdateItemRequest`) required for `IApiService` to compile; replaced when those features are built
- `RentalApp/Services/RemoteApiService.cs` — `IApiService` backed by `IApiClient`; stores bearer token in `AuthTokenState` after login
- `RentalApp/Services/LocalApiService.cs` — `IApiService` backed by `AppDbContext`; stores authenticated `UserProfile` in memory after login
- `RentalApp/Services/AuthenticationService.cs` — single `IAuthenticationService`; owns auth state, `RememberMe` credential persistence, wraps `IApiService`
- `RentalApp.Test/Services/AuthenticationServiceTests.cs` — unit tests for `AuthenticationService` using a mocked `IApiService`
- `RentalApp.Test/Services/LocalApiServiceAuthTests.cs` — integration tests for `LocalApiService` auth methods against a real database

### Modify
- `RentalApp/Services/IApiService.cs` — add namespace; `LoginAsync` returns `Task`; all return types use `RentalApp.Models` DTOs
- `RentalApp/Services/IAuthenticationService.cs` — `CurrentUser` returns `UserProfile?` instead of `User?`
- `RentalApp/MauiProgram.cs` — switch only `IApiService`; register `AuthenticationService` unconditionally
- `RentalApp.Test/RentalApp.Test.csproj` — add NSubstitute; add `RentalApp.Database` project reference

### Delete
- `RentalApp/Services/ApiAuthenticationService.cs`
- `RentalApp/Services/LocalAuthenticationService.cs`

---

## Task 1: Add NSubstitute to the test project

**Files:**
- Modify: `RentalApp.Test/RentalApp.Test.csproj`

- [ ] **Step 1: Add NSubstitute and RentalApp.Database reference**

Open `RentalApp.Test/RentalApp.Test.csproj` and add to the first `<ItemGroup>`:

```xml
<PackageReference Include="NSubstitute" Version="5.3.0" />
<ProjectReference Include="../RentalApp.Database/RentalApp.Database.csproj" />
```

- [ ] **Step 2: Restore packages**

```bash
dotnet restore RentalApp.Test/RentalApp.Test.csproj
```

Expected: `Restore succeeded.`

- [ ] **Step 3: Commit**

```bash
git add RentalApp.Test/RentalApp.Test.csproj
git commit -m "test: add NSubstitute and Database project reference to test project"
```

---

## Task 2: Create DTO models in `RentalApp/Models/`

**Files:**
- Create: `RentalApp/Models/UserProfile.cs`
- Create: `RentalApp/Models/AuthToken.cs`
- Create: `RentalApp/Models/ApiDomainStubs.cs`

- [ ] **Step 1: Create `UserProfile`**

Create `RentalApp/Models/UserProfile.cs`:

```csharp
namespace RentalApp.Models;

public sealed record UserProfile(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    DateTime? CreatedAt
);
```

- [ ] **Step 2: Create `AuthToken`**

`AuthToken` is only used inside `RemoteApiService` to deserialise the login response. It is not part of `IApiService`'s public contract.

Create `RentalApp/Models/AuthToken.cs`:

```csharp
namespace RentalApp.Models;

public sealed record AuthToken(string Token, DateTime ExpiresAt, int UserId);
```

- [ ] **Step 3: Create stub domain types**

Create `RentalApp/Models/ApiDomainStubs.cs`:

```csharp
namespace RentalApp.Models;

// Placeholder DTOs — replaced when each feature is implemented
public sealed record Item;
public sealed record Category;
public sealed record Rental;
public sealed record Review;
public sealed record CreateItemRequest;
public sealed record UpdateItemRequest;
```

- [ ] **Step 4: Commit**

```bash
git add RentalApp/Models/
git commit -m "feat: add UserProfile DTO and domain stub types to RentalApp.Models"
```

---

## Task 3: Fix `IApiService` interface

**Files:**
- Modify: `RentalApp/Services/IApiService.cs`

- [ ] **Step 1: Replace the file contents**

```csharp
using RentalApp.Models;

namespace RentalApp.Services;

public interface IApiService
{
    // Authentication
    Task LoginAsync(string email, string password);
    Task RegisterAsync(string firstName, string lastName, string email, string password);
    Task<UserProfile> GetCurrentUserAsync();
    Task<UserProfile> GetUserProfileAsync(int userId);
    Task LogoutAsync();

    // Items
    Task<List<Item>> GetItemsAsync(string? category = null, string? search = null, int page = 1);
    Task<List<Item>> GetNearbyItemsAsync(double lat, double lon, double radius = 5.0, string? category = null);
    Task<Item> GetItemAsync(int id);
    Task<Item> CreateItemAsync(CreateItemRequest request);
    Task<Item> UpdateItemAsync(int id, UpdateItemRequest request);

    // Categories
    Task<List<Category>> GetCategoriesAsync();

    // Rentals
    Task<Rental> RequestRentalAsync(int itemId, DateTime startDate, DateTime endDate);
    Task<List<Rental>> GetIncomingRentalsAsync(string? status = null);
    Task<List<Rental>> GetOutgoingRentalsAsync(string? status = null);
    Task<Rental> GetRentalAsync(int id);
    Task UpdateRentalStatusAsync(int rentalId, string status);

    // Reviews
    Task<Review> CreateReviewAsync(int rentalId, int rating, string comment);
    Task<List<Review>> GetItemReviewsAsync(int itemId, int page = 1);
    Task<List<Review>> GetUserReviewsAsync(int userId, int page = 1);
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build RentalApp/RentalApp.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add RentalApp/Services/IApiService.cs
git commit -m "feat: update IApiService — LoginAsync returns Task, all return types use RentalApp.Models DTOs"
```

---

## Task 4: Update `IAuthenticationService`

**Files:**
- Modify: `RentalApp/Services/IAuthenticationService.cs`

`CurrentUser` previously returned the EF entity `User`. It now returns `UserProfile` from `RentalApp.Models`.

- [ ] **Step 1: Replace the file contents**

```csharp
using RentalApp.Models;

namespace RentalApp.Services;

public interface IAuthenticationService
{
    event EventHandler<bool>? AuthenticationStateChanged;

    bool IsAuthenticated { get; }

    UserProfile? CurrentUser { get; }

    Task<AuthenticationResult> LoginAsync(string email, string password, bool rememberMe = false);

    Task<AuthenticationResult> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    );

    Task LogoutAsync();
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build RentalApp/RentalApp.csproj
```

Expected: Build errors on `ApiAuthenticationService` and `LocalAuthenticationService` — both reference the old `User` type. These are deleted in Task 11; ignore for now.

- [ ] **Step 3: Commit**

```bash
git add RentalApp/Services/IAuthenticationService.cs
git commit -m "refactor: IAuthenticationService.CurrentUser returns UserProfile DTO"
```

---

## Task 5: Write failing tests for `AuthenticationService`

**Files:**
- Create: `RentalApp.Test/Services/AuthenticationServiceTests.cs`

- [ ] **Step 1: Create the test file**

```csharp
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Models;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class AuthenticationServiceTests
{
    private readonly IApiService _api = Substitute.For<IApiService>();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();
    private AuthenticationService CreateSut() => new(_api, _credentialStore);

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        var profile = new UserProfile(1, "Jane", "Doe", "jane@example.com", DateTime.UtcNow);
        _api.GetCurrentUserAsync().Returns(profile);

        var sut = CreateSut();
        var result = await sut.LoginAsync("jane@example.com", "pass");

        Assert.True(result.IsSuccess);
        Assert.Equal(profile, sut.CurrentUser);
        Assert.True(sut.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_ApiThrows_ReturnsFailure()
    {
        _api.LoginAsync(Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new UnauthorizedAccessException("bad credentials"));

        var sut = CreateSut();
        var result = await sut.LoginAsync("jane@example.com", "wrong");

        Assert.False(result.IsSuccess);
        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsync_RememberMe_SavesCredentials()
    {
        _api.GetCurrentUserAsync().Returns(
            new UserProfile(1, "Jane", "Doe", "jane@example.com", DateTime.UtcNow)
        );

        var sut = CreateSut();
        await sut.LoginAsync("jane@example.com", "pass", rememberMe: true);

        await _credentialStore.Received(1).SaveAsync("jane@example.com", "pass");
    }

    [Fact]
    public async Task LoginAsync_RememberMeFalse_DoesNotSaveCredentials()
    {
        _api.GetCurrentUserAsync().Returns(
            new UserProfile(1, "Jane", "Doe", "jane@example.com", DateTime.UtcNow)
        );

        var sut = CreateSut();
        await sut.LoginAsync("jane@example.com", "pass", rememberMe: false);

        await _credentialStore.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_Success_FiresAuthenticationStateChangedWithTrue()
    {
        _api.GetCurrentUserAsync().Returns(
            new UserProfile(1, "Jane", "Doe", "jane@example.com", DateTime.UtcNow)
        );

        var sut = CreateSut();
        bool? firedWith = null;
        sut.AuthenticationStateChanged += (_, v) => firedWith = v;

        await sut.LoginAsync("jane@example.com", "pass");

        Assert.True(firedWith);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsSuccess()
    {
        var sut = CreateSut();
        var result = await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "pass");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RegisterAsync_ApiThrows_ReturnsFailure()
    {
        _api.RegisterAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()
            )
            .ThrowsAsync(new InvalidOperationException("email taken"));

        var sut = CreateSut();
        var result = await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "pass");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LogoutAsync_ClearsCurrentUser()
    {
        _api.GetCurrentUserAsync().Returns(
            new UserProfile(1, "Jane", "Doe", "jane@example.com", DateTime.UtcNow)
        );
        var sut = CreateSut();
        await sut.LoginAsync("jane@example.com", "pass");

        await sut.LogoutAsync();

        Assert.Null(sut.CurrentUser);
        Assert.False(sut.IsAuthenticated);
    }

    [Fact]
    public async Task LogoutAsync_FiresAuthenticationStateChangedWithFalse()
    {
        var sut = CreateSut();
        bool? firedWith = null;
        sut.AuthenticationStateChanged += (_, v) => firedWith = v;

        await sut.LogoutAsync();

        Assert.False(firedWith);
    }

    [Fact]
    public async Task LogoutAsync_ClearsPersistedCredentials()
    {
        var sut = CreateSut();
        await sut.LogoutAsync();

        await _credentialStore.Received(1).ClearAsync();
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~AuthenticationServiceTests"
```

Expected: Build error — `AuthenticationService` does not exist yet.

---

## Task 6: Implement `AuthenticationService`

**Files:**
- Create: `RentalApp/Services/AuthenticationService.cs`

- [ ] **Step 1: Create the file**

```csharp
using RentalApp.Models;

namespace RentalApp.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IApiService _api;
    private readonly ICredentialStore _credentialStore;
    private UserProfile? _currentUser;

    public event EventHandler<bool>? AuthenticationStateChanged;
    public bool IsAuthenticated => _currentUser != null;
    public UserProfile? CurrentUser => _currentUser;

    public AuthenticationService(IApiService api, ICredentialStore credentialStore)
    {
        _api = api;
        _credentialStore = credentialStore;
    }

    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        bool rememberMe = false
    )
    {
        try
        {
            await _api.LoginAsync(email, password);

            if (rememberMe)
                await _credentialStore.SaveAsync(email, password);

            _currentUser = await _api.GetCurrentUserAsync();
            AuthenticationStateChanged?.Invoke(this, true);
            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            return AuthenticationResult.Failure(ex.Message);
        }
    }

    public async Task<AuthenticationResult> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    )
    {
        try
        {
            await _api.RegisterAsync(firstName, lastName, email, password);
            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            return AuthenticationResult.Failure(ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        await _api.LogoutAsync();
        await _credentialStore.ClearAsync();
        AuthenticationStateChanged?.Invoke(this, false);
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~AuthenticationServiceTests"
```

Expected: All 10 tests PASS.

- [ ] **Step 3: Commit**

```bash
git add RentalApp/Services/AuthenticationService.cs RentalApp.Test/Services/AuthenticationServiceTests.cs
git commit -m "feat: implement AuthenticationService wrapping IApiService"
```

---

## Task 7: Write failing tests for `LocalApiService` auth

**Files:**
- Create: `RentalApp.Test/Services/LocalApiServiceAuthTests.cs`

These are integration tests against a real PostgreSQL database. Run `docker-compose up db` before executing. The tests create and destroy their own isolated database on each run.

- [ ] **Step 1: Create the test file**

```csharp
using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class LocalApiServiceAuthTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private LocalApiService _sut = null!;

    public async Task InitializeAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=appdb_test;Username=app_user;Password=app_password";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _sut = new LocalApiService(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesUser()
    {
        await _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password123");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "jane@example.com");
        Assert.NotNull(user);
        Assert.Equal("Jane", user.FirstName);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        await _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password123");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password456")
        );
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_SetsCurrentUser()
    {
        await _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password123");
        await _sut.LoginAsync("jane@example.com", "password123");

        var profile = await _sut.GetCurrentUserAsync();
        Assert.Equal("jane@example.com", profile.Email);
        Assert.Equal("Jane", profile.FirstName);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        await _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password123");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync("jane@example.com", "wrongpassword")
        );
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync("nobody@example.com", "password")
        );
    }

    [Fact]
    public async Task GetCurrentUserAsync_BeforeLogin_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetCurrentUserAsync()
        );
    }

    [Fact]
    public async Task LogoutAsync_ClearsCurrentUser()
    {
        await _sut.RegisterAsync("Jane", "Doe", "jane@example.com", "password123");
        await _sut.LoginAsync("jane@example.com", "password123");
        await _sut.LogoutAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetCurrentUserAsync()
        );
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~LocalApiServiceAuthTests"
```

Expected: Build error — `LocalApiService` does not exist yet.

---

## Task 8: Implement `LocalApiService`

**Files:**
- Create: `RentalApp/Services/LocalApiService.cs`

- [ ] **Step 1: Create the file**

```csharp
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;
using RentalApp.Models;

namespace RentalApp.Services;

public class LocalApiService : IApiService
{
    private readonly AppDbContext _context;
    private UserProfile? _currentUser;

    public LocalApiService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        _currentUser = ToProfile(user);
    }

    public async Task RegisterAsync(string firstName, string lastName, string email, string password)
    {
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing != null)
            throw new InvalidOperationException("User with this email already exists");

        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        _context.Users.Add(new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, salt),
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();
    }

    public Task<UserProfile> GetCurrentUserAsync()
    {
        if (_currentUser == null)
            throw new InvalidOperationException("No user is currently authenticated");

        return Task.FromResult(_currentUser);
    }

    public async Task<UserProfile> GetUserProfileAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        return ToProfile(user);
    }

    public Task LogoutAsync()
    {
        _currentUser = null;
        return Task.CompletedTask;
    }

    private static UserProfile ToProfile(User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.CreatedAt);

    // ── Future domain methods ──────────────────────────────────────
    public Task<List<Item>> GetItemsAsync(string? category = null, string? search = null, int page = 1) => throw new NotImplementedException();
    public Task<List<Item>> GetNearbyItemsAsync(double lat, double lon, double radius = 5.0, string? category = null) => throw new NotImplementedException();
    public Task<Item> GetItemAsync(int id) => throw new NotImplementedException();
    public Task<Item> CreateItemAsync(CreateItemRequest request) => throw new NotImplementedException();
    public Task<Item> UpdateItemAsync(int id, UpdateItemRequest request) => throw new NotImplementedException();
    public Task<List<Category>> GetCategoriesAsync() => throw new NotImplementedException();
    public Task<Rental> RequestRentalAsync(int itemId, DateTime startDate, DateTime endDate) => throw new NotImplementedException();
    public Task<List<Rental>> GetIncomingRentalsAsync(string? status = null) => throw new NotImplementedException();
    public Task<List<Rental>> GetOutgoingRentalsAsync(string? status = null) => throw new NotImplementedException();
    public Task<Rental> GetRentalAsync(int id) => throw new NotImplementedException();
    public Task UpdateRentalStatusAsync(int rentalId, string status) => throw new NotImplementedException();
    public Task<Review> CreateReviewAsync(int rentalId, int rating, string comment) => throw new NotImplementedException();
    public Task<List<Review>> GetItemReviewsAsync(int itemId, int page = 1) => throw new NotImplementedException();
    public Task<List<Review>> GetUserReviewsAsync(int userId, int page = 1) => throw new NotImplementedException();
}
```

- [ ] **Step 2: Run the integration tests**

Ensure the test database is reachable (`docker-compose up db`), then:

```bash
dotnet test RentalApp.Test/RentalApp.Test.csproj --filter "FullyQualifiedName~LocalApiServiceAuthTests"
```

Expected: All 7 tests PASS.

- [ ] **Step 3: Commit**

```bash
git add RentalApp/Services/LocalApiService.cs RentalApp.Test/Services/LocalApiServiceAuthTests.cs
git commit -m "feat: implement LocalApiService auth methods with integration tests"
```

---

## Task 9: Implement `RemoteApiService`

**Files:**
- Create: `RentalApp/Services/RemoteApiService.cs`

`LoginAsync` stores the bearer token in `AuthTokenState` as a side effect. This is transport-layer session state — fully contained within `RemoteApiService`/`AuthTokenState`/`AuthRefreshHandler` and invisible above `IApiService`.

- [ ] **Step 1: Create the file**

```csharp
using System.Net.Http.Json;
using RentalApp.Models;

namespace RentalApp.Services;

public class RemoteApiService : IApiService
{
    private readonly IApiClient _apiClient;
    private readonly AuthTokenState _tokenState;

    public RemoteApiService(IApiClient apiClient, AuthTokenState tokenState)
    {
        _apiClient = apiClient;
        _tokenState = tokenState;
    }

    public async Task LoginAsync(string email, string password)
    {
        var response = await _apiClient.PostAsJsonAsync("auth/token", new { email, password });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            throw new UnauthorizedAccessException(error?.Message ?? "Login failed");
        }

        var token = await response.Content.ReadFromJsonAsync<AuthToken>()
            ?? throw new InvalidOperationException("Empty token response from API");

        _tokenState.CurrentToken = token.Token;
    }

    public async Task RegisterAsync(string firstName, string lastName, string email, string password)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "auth/register",
            new { firstName, lastName, email, password }
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            throw new InvalidOperationException(error?.Message ?? "Registration failed");
        }
    }

    public async Task<UserProfile> GetCurrentUserAsync()
    {
        var response = await _apiClient.GetAsync("users/me");
        response.EnsureSuccessStatusCode();
        return await DeserialiseProfileAsync(response);
    }

    public async Task<UserProfile> GetUserProfileAsync(int userId)
    {
        var response = await _apiClient.GetAsync($"users/{userId}");
        response.EnsureSuccessStatusCode();
        return await DeserialiseProfileAsync(response);
    }

    public Task LogoutAsync()
    {
        _tokenState.CurrentToken = null;
        return Task.CompletedTask;
    }

    private static async Task<UserProfile> DeserialiseProfileAsync(HttpResponseMessage response)
    {
        var dto = await response.Content.ReadFromJsonAsync<UserProfileResponse>()
            ?? throw new InvalidOperationException("Empty profile response from API");

        return new UserProfile(dto.Id, dto.FirstName, dto.LastName, dto.Email, dto.CreatedAt);
    }

    // ── Future domain methods ──────────────────────────────────────
    public Task<List<Item>> GetItemsAsync(string? category = null, string? search = null, int page = 1) => throw new NotImplementedException();
    public Task<List<Item>> GetNearbyItemsAsync(double lat, double lon, double radius = 5.0, string? category = null) => throw new NotImplementedException();
    public Task<Item> GetItemAsync(int id) => throw new NotImplementedException();
    public Task<Item> CreateItemAsync(CreateItemRequest request) => throw new NotImplementedException();
    public Task<Item> UpdateItemAsync(int id, UpdateItemRequest request) => throw new NotImplementedException();
    public Task<List<Category>> GetCategoriesAsync() => throw new NotImplementedException();
    public Task<Rental> RequestRentalAsync(int itemId, DateTime startDate, DateTime endDate) => throw new NotImplementedException();
    public Task<List<Rental>> GetIncomingRentalsAsync(string? status = null) => throw new NotImplementedException();
    public Task<List<Rental>> GetOutgoingRentalsAsync(string? status = null) => throw new NotImplementedException();
    public Task<Rental> GetRentalAsync(int id) => throw new NotImplementedException();
    public Task UpdateRentalStatusAsync(int rentalId, string status) => throw new NotImplementedException();
    public Task<Review> CreateReviewAsync(int rentalId, int rating, string comment) => throw new NotImplementedException();
    public Task<List<Review>> GetItemReviewsAsync(int itemId, int page = 1) => throw new NotImplementedException();
    public Task<List<Review>> GetUserReviewsAsync(int userId, int page = 1) => throw new NotImplementedException();

    private record UserProfileResponse(int Id, string Email, string FirstName, string LastName, DateTime CreatedAt);
    private record ApiErrorResponse(string Error, string Message);
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build RentalApp/RentalApp.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add RentalApp/Services/RemoteApiService.cs
git commit -m "feat: implement RemoteApiService — stores bearer token in AuthTokenState after login"
```

---

## Task 10: Update `MauiProgram.cs`

**Files:**
- Modify: `RentalApp/MauiProgram.cs`

- [ ] **Step 1: Replace the service registration block**

Replace the entire `if (useSharedApi) { ... } else { ... }` block and the `IAuthenticationService` registration line with:

```csharp
bool useSharedApi = Preferences.Default.Get("UseSharedApi", true);

if (useSharedApi)
{
    var baseAddress = new Uri("https://set09102-api.b-davison.workers.dev/");

    builder.Services.AddSingleton<AuthTokenState>();
    builder.Services.AddSingleton(sp => new AuthRefreshHandler(
        sp.GetRequiredService<AuthTokenState>(),
        sp.GetRequiredService<ICredentialStore>(),
        baseAddress,
        sp.GetRequiredService<ILogger<AuthRefreshHandler>>()
    )
    {
        InnerHandler = new HttpClientHandler(),
    });
    builder.Services.AddSingleton(sp => new HttpClient(
        sp.GetRequiredService<AuthRefreshHandler>()
    )
    {
        BaseAddress = baseAddress,
    });
    builder.Services.AddSingleton<IApiClient, ApiClient>();
    builder.Services.AddSingleton<IApiService>(sp => new RemoteApiService(
        sp.GetRequiredService<IApiClient>(),
        sp.GetRequiredService<AuthTokenState>()
    ));
}
else
{
    builder.Services.AddDbContext<AppDbContext>();
    builder.Services.AddSingleton<IApiService>(sp => new LocalApiService(
        sp.GetRequiredService<AppDbContext>()
    ));
}

builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
```

- [ ] **Step 2: Verify build**

```bash
dotnet build RentalApp.sln
```

Expected: Build errors on `ApiAuthenticationService` and `LocalAuthenticationService` — both are now unreferenced and deleted in Task 11.

- [ ] **Step 3: Commit**

```bash
git add RentalApp/MauiProgram.cs
git commit -m "refactor: switch only IApiService in DI; register AuthenticationService unconditionally"
```

---

## Task 11: Delete old authentication service implementations

**Files:**
- Delete: `RentalApp/Services/ApiAuthenticationService.cs`
- Delete: `RentalApp/Services/LocalAuthenticationService.cs`

- [ ] **Step 1: Delete the files**

```bash
git rm RentalApp/Services/ApiAuthenticationService.cs RentalApp/Services/LocalAuthenticationService.cs
```

- [ ] **Step 2: Final build and full test run**

```bash
dotnet build RentalApp.sln
dotnet test RentalApp.Test/RentalApp.Test.csproj
```

Expected: `Build succeeded.` All tests PASS.

- [ ] **Step 3: Commit**

```bash
git commit -m "refactor: remove ApiAuthenticationService and LocalAuthenticationService"
```
