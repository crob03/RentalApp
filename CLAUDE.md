# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET MAUI cross-platform rental application targeting Android, built with C# and .NET 10. It uses a four-project solution (UI, Database, Migrations, Tests) with PostgreSQL as the database.

## Common Commands

### Build
```bash
dotnet build RentalApp.sln
```

### Run with Docker (full stack: DB + migrations + app)
```bash
docker-compose up
```

### Run tests
```bash
dotnet test
```

### Format code (CSharpier — non-negotiable, zero-config)
```bash
dotnet csharpier .
```

### Apply database migrations manually
```bash
dotnet tool restore
dotnet ef database update --project RentalApp.Migrations
```

### Seed / clear local database
```bash
make seed-db    # Insert development data (categories, users, items, rentals)
make clear-db   # Truncate all seeded tables and reset sequences
```

### Add a new EF Core migration
```bash
dotnet ef migrations add <MigrationName> --project RentalApp.Migrations
```

### Switch API target (Android device/emulator)
```bash
make use-remote-api   # Point app at remote API (default)
make use-local-api    # Point app at Local* services (LocalAuthService, LocalItemService, …)
```
Writes Android SharedPreferences via `adb` and restarts the app — no rebuild required.

## Architecture

Four projects in the solution:

### RentalApp (MAUI UI)
MVVM pattern using `CommunityToolkit.Mvvm`. Views are XAML pages in `Views/`, bound to ViewModels in `ViewModels/`. `BaseViewModel` provides `IsBusy`, `Title`, `SetError(msg)`/`ClearError()`, and `RunAsync(Func<Task>)` — a lifecycle wrapper that sets `IsBusy`, clears errors, and surfaces exceptions via `SetError`. Always use `RunAsync` for async ViewModel operations rather than writing try/catch boilerplate. Services in `Services/` handle authentication (`IAuthService`), navigation (`INavigationService`), and credential persistence (`ICredentialStore`/`CredentialStore`). Dependency injection is configured in `MauiProgram.cs`. Static helpers (e.g. `RegistrationValidator`, `ItemValidator`) live in `Helpers/` — pure, stateless utilities with no DI dependency.

**Contracts**: Request/response records live in `Contracts/` within this project (namespace `RentalApp.Contracts`). Requests under `Contracts/Requests/`, responses under `Contracts/Responses/`. `IItemListable` is the shared interface for item-listing types. `RentalApp.Database` does **not** reference these — only `RentalApp` and `RentalApp.Test` (via project reference to `RentalApp`) use them.

**Service hierarchy**: Four domain-specific service interfaces replace the retired `IApiService`: `IAuthService`, `IItemService`, `IRentalService`, and `IReviewService`. Each has a `Remote*` implementation (HTTP via `RemoteServiceBase`) and a `Local*` implementation (repository-backed). Auth ViewModels (`LoginViewModel`, `RegisterViewModel`) inject `IAuthService` directly. Item-related ViewModels inject `IItemService` directly. `LoginViewModel` owns token state and credential persistence — it injects `AuthTokenState` and `ICredentialStore` and sets the token itself after a successful login.

**Http layer**: `Http/` contains `IApiClient`/`ApiClient` (typed `HttpClient` wrapper) and `AuthTokenState` (singleton bearer token holder). All `Remote*` services extend `RemoteServiceBase` (shared error-handling helper). Switch to local services via `make use-local-api` for offline dev.

**Domain services**: `ILocationService`/`LocationService` wraps `IGeolocation` (device GPS) and is registered in DI. `LocalRentalService` is fully implemented (repository-backed). `LocalReviewService` still throws `NotImplementedException` — Review DB entities are not yet implemented.

**Item listing ViewModels**: `ItemsSearchBaseViewModel<TItem>` (abstract, generic, `where TItem : IItemListable`, extends `AuthenticatedViewModel`) is the required base for all item-listing pages. It provides shared pagination state (`IsLoading`/`IsLoadingMore`/`CurrentPage`/`HasMorePages`), category filtering, and `RunLoadAsync`/`RunLoadMoreAsync` lifecycle helpers. Subclasses implement `ReloadAsync()` — triggered automatically after first load when filters change. Use `_ = TriggerReloadIfLoaded()` in `partial void` property callbacks (which cannot be async) to make fire-and-forget explicit.

**NearbyItems pagination**: The nearby items API endpoint ignores `page`/`pageSize` and returns all results. `NearbyItemsViewModel` caches the full result in `_allNearbyItems` and slices client-side — do not add server-side paging calls here.

**TempPage**: Legacy post-login placeholder — still registered but superseded by `MainPage` as the authenticated landing screen. `LoadingPage` handles initial app startup before routing.

**Authenticated ViewModel hierarchy**: `AuthenticatedViewModel` (abstract, extends `BaseViewModel`) is the required base for all post-auth ViewModels. It provides `LogoutCommand`, `NavigateToProfileCommand`, and protected `NavigateToAsync`/`NavigateBackAsync` helpers — subclasses no longer need to hold their own `INavigationService` field. `ItemsSearchBaseViewModel` extends `AuthenticatedViewModel`. `AppShellViewModel` has been removed.

**Rental ViewModels**: `RentalsViewModel` (extends `AuthenticatedViewModel` directly — not `ItemsSearchBaseViewModel`) manages incoming/outgoing rental lists with a direction toggle and status filter. `ManageRentalViewModel` loads a single rental and exposes role-based transition buttons driven by `RentalStateFactory`. It implements `IQueryAttributable` to receive the `rentalId` query parameter — use this pattern whenever a ViewModel needs data passed via `NavigateToAsync(route, parameters)` calls.

**DI lifetime gotcha**: `LoginViewModel`, `RegisterViewModel`, and `TempViewModel` are registered as Singleton (state persists across navigations). `IAuthService`, `ILocationService`, and `INavigationService` are also Singleton. All other ViewModels and Pages are Transient.

**Shell navigation**: Root routes are `//login` (`Routes.Login`) and `//main` (`Routes.Main`) — both are declared as `ShellContent` items in `AppShell.xaml` and replace the navigation stack when navigated to. AppShell flyout is disabled — routing is entirely programmatic via `INavigationService`. Never call `Shell.Current` directly from ViewModels. Route name constants live in `Constants/Routes.cs`: `Login`, `Register`, `Main`, `Temp`, `ItemsList`, `ItemDetails`, `CreateItem`, `NearbyItems`, `Rentals`, `ManageRental`.

**Dual Item model gotcha**: Two `Item` classes exist — do not confuse them. `RentalApp.Contracts.Responses.ItemDetailResponse`/`ItemSummary` are the DTO records used by ViewModels. `RentalApp.Database.Models.Item` is an EF entity with a PostGIS `Point Location` column. The UI project never references the Database models directly.

### RentalApp.Database (Data Access Layer)
EntityFrameworkCore with Npgsql (PostgreSQL) and **NetTopologySuite** for PostGIS geography support. `AppDbContext` manages four entities: `User`, `Category`, `Item`, and `Rental`. `Item.Location` is stored as a PostGIS `geography(Point, 4326)` column — `UseNetTopologySuite()` must be present on the EF options (it is; don't remove it). Connection string is read from the `CONNECTION_STRING` environment variable, falling back to embedded `appsettings.json` in the assembly. Passwords are hashed with BCrypt.

**Repositories**: `IItemRepository`/`ItemRepository`, `ICategoryRepository`/`CategoryRepository`, `IUserRepository`/`UserRepository`, and `IRentalRepository`/`RentalRepository` sit between `AppDbContext` and the `Local*` services. They are registered as Singleton and injected into `LocalAuthService`, `LocalItemService`, and `LocalRentalService` only — Remote services have no dependency on them.

**Rental state machine**: `States/` contains `RentalStatus` (enum) plus a state-object pattern — `IRentalState`, `RentalStateFactory`, and one concrete class per status (`RequestedState`, `ApprovedState`, `RejectedState`, `OutForRentState`, `OverdueState`, `ReturnedState`, `CompletedState`). Transitions are enforced via `RentalStateFactory.From(rental.Status).TransitionTo(targetStatus, rental)`. Key constraints: `Overdue` is set automatically (never a valid target for callers); `Returned` can only be set by the borrower; `Approved`/`Rejected`/`OutForRent`/`Completed` are owner-only.

### RentalApp.Migrations (Migrations Library)
Class library housing EF Core migration files under `Migrations/`. Implements `IDesignTimeDbContextFactory<AppDbContext>` so `dotnet ef` can target this project directly without a separate startup project. Applied via `dotnet ef database update --project RentalApp.Migrations` (handled by docker-compose service ordering).

## Testing

- Tests live in `RentalApp.Test/`, mirroring the source structure: `ViewModels/`, `Services/`, `Http/`, `Repositories/`
- Integration tests use a **real PostgreSQL database** via `Fixtures/DatabaseFixture` — no mocking of `AppDbContext`
- To test abstract ViewModels, create a `private sealed TestableViewModel` inner class that exposes protected methods — see `ItemsSearchBaseViewModelTests` for the pattern.
- `DatabaseFixture` implements xUnit's `IClassFixture<T>`: one DB per test class, torn down after
- Requires `CONNECTION_STRING` env var (falls back to `Host=localhost;Port=5432;Database=appdb_test;Username=app_user;Password=app_password`)
- Run the DB first: `docker-compose up db` before running tests locally

## Infrastructure

- **Database**: PostgreSQL 16 — credentials: `app_user`/`app_password`, database `appdb`, port 5432
- **Dev Container**: `.devcontainer/devcontainer.json` provisions .NET 10 SDK, Android SDK (build-tools 36.0.0), and Java JDK 21
- **Docker**: Multi-stage `Dockerfile` builds with MAUI Android workload; `docker-compose.yml` orchestrates `db`, `migrate`, and `app` services

# Additional Context

- @PROJECTPLAN.md for context around the projects current status, ways of working, and coding standards.