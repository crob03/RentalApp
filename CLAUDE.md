# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET MAUI cross-platform rental application targeting Android, built with C# and .NET 10. It uses a three-project architecture with PostgreSQL as the database.

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

### Add a new EF Core migration
```bash
dotnet ef migrations add <MigrationName> --project RentalApp.Migrations
```

### Switch API target (Android device/emulator)
```bash
make use-remote-api   # Point app at remote API (default)
make use-local-api    # Point app at LocalAuthenticationService
```
Writes Android SharedPreferences via `adb` and restarts the app — no rebuild required.

## Architecture

Three projects in the solution:

### RentalApp (MAUI UI)
MVVM pattern using `CommunityToolkit.Mvvm`. Views are XAML pages in `Views/`, bound to ViewModels in `ViewModels/`. `BaseViewModel` provides `IsBusy`, `Title`, and `SetError(msg)`/`ClearError()` helpers — always use these for error state in ViewModels. Services in `Services/` handle authentication (`IAuthenticationService`), navigation (`INavigationService`), and credential persistence (`ICredentialStore`/`CredentialStore`). Dependency injection is configured in `MauiProgram.cs`. Static helpers (e.g. `RegistrationValidator`) live in `Helpers/` — pure, stateless utilities with no DI dependency.

**Service hierarchy**: `IApiService` is the low-level abstraction (raw HTTP via `RemoteApiService` targeting `https://set09102-api.b-davison.workers.dev/`, or local DB via `LocalApiService`). `IAuthenticationService` wraps it with domain logic. ViewModels depend only on `IAuthenticationService`, never on `IApiService` directly.

**Http layer**: `Http/` contains `IApiClient`/`ApiClient` (typed `HttpClient` wrapper) and `AuthTokenState` (singleton bearer token holder). `RemoteApiService` uses both. Switch to `LocalApiService` via `make use-local-api` for offline dev.

**Domain services**: `IItemService`/`ItemService` handles item CRUD with input validation before delegating to `IApiService`. `ILocationService`/`LocationService` wraps `IGeolocation` (device GPS) — both are registered in DI. ViewModels that need items or location inject these, not `IApiService` directly.

**TempPage**: Legacy post-login placeholder — still registered but superseded by `MainPage` as the authenticated landing screen. `LoadingPage` handles initial app startup before routing.

**DI lifetime gotcha**: `LoginViewModel`, `RegisterViewModel`, `TempViewModel`, and `AppShellViewModel` are registered as Singleton (state persists across navigations). `IAuthenticationService`, `ILocationService`, and `INavigationService` are also Singleton. `IItemService` is Transient — it holds no auth state and is cheap to construct. All other ViewModels and Pages are Transient.

**Shell navigation**: Root route is `//login` (`Routes.Login`). AppShell flyout is disabled — routing is entirely programmatic via `INavigationService`. Never call `Shell.Current` directly from ViewModels. Route name constants live in `Constants/Routes.cs`: `Login`, `Main`, `Temp`, `ItemsList`, `ItemDetails`, `CreateItem`, `NearbyItems`.

**Dual Item model gotcha**: Two `Item` classes exist — do not confuse them. `RentalApp.Models.Item` is a `record` (API response DTO, used by ViewModels). `RentalApp.Database.Models.Item` is an EF entity with a PostGIS `Point Location` column. The UI project never references the Database models directly.

### RentalApp.Database (Data Access Layer)
EntityFrameworkCore with Npgsql (PostgreSQL) and **NetTopologySuite** for PostGIS geography support. `AppDbContext` manages three entities: `User`, `Category`, and `Item`. `Item.Location` is stored as a PostGIS `geography(Point, 4326)` column — `UseNetTopologySuite()` must be present on the EF options (it is; don't remove it). Connection string is read from the `CONNECTION_STRING` environment variable, falling back to embedded `appsettings.json` in the assembly. Passwords are hashed with BCrypt.

**Repositories**: `IItemRepository`/`ItemRepository` and `ICategoryRepository`/`CategoryRepository` sit between `AppDbContext` and `LocalApiService`. They are registered as Singleton and injected into `LocalApiService` only — `RemoteApiService` has no dependency on them.

### RentalApp.Migrations (Migrations Library)
Class library housing EF Core migration files under `Migrations/`. Implements `IDesignTimeDbContextFactory<AppDbContext>` so `dotnet ef` can target this project directly without a separate startup project. Applied via `dotnet ef database update --project RentalApp.Migrations` (handled by docker-compose service ordering).

## Testing

- Tests live in `RentalApp.Test/`, mirroring the source structure: `ViewModels/`, `Services/`, `Http/`
- Integration tests use a **real PostgreSQL database** via `Fixtures/DatabaseFixture` — no mocking of `AppDbContext`
- `DatabaseFixture` implements xUnit's `IClassFixture<T>`: one DB per test class, torn down after
- Requires `CONNECTION_STRING` env var (falls back to `Host=localhost;Port=5432;Database=appdb_test;Username=app_user;Password=app_password`)
- Run the DB first: `docker-compose up db` before running tests locally

## Infrastructure

- **Database**: PostgreSQL 16 — credentials: `app_user`/`app_password`, database `appdb`, port 5432
- **Dev Container**: `.devcontainer/devcontainer.json` provisions .NET 10 SDK, Android SDK (build-tools 36.0.0), and Java JDK 21
- **Docker**: Multi-stage `Dockerfile` builds with MAUI Android workload; `docker-compose.yml` orchestrates `db`, `migrate`, and `app` services

# Additional Context

- @PROJECTPLAN.md for context around the projects current status, ways of working, and coding standards.