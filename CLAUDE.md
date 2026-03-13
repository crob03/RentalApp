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

### Run database migrations manually
```bash
dotnet run --project RentalApp.Migrations/RentalApp.Migrations.csproj
```

### Add a new EF Core migration
```bash
dotnet ef migrations add <MigrationName> --project RentalApp.Database --startup-project RentalApp.Migrations
```

## Architecture

Three projects in the solution:

### RentalApp (MAUI UI)
MVVM pattern using `CommunityToolkit.Mvvm`. Views are XAML pages in `Views/`, bound to ViewModels in `ViewModels/`. `BaseViewModel` provides `IsBusy`, `Title`, and `SetError(msg)`/`ClearError()` helpers — always use these for error state in ViewModels. Services in `Services/` handle authentication (`IAuthenticationService`) and navigation (`INavigationService`). Dependency injection is configured in `MauiProgram.cs`.

**DI lifetime gotcha**: `LoginViewModel` and `RegisterViewModel` are registered as Singleton (state persists across navigations). All other ViewModels and Pages are Transient.

**Shell navigation**: Root route is `//login` (used by `NavigationService.NavigateToRootAsync()`). AppShell flyout is disabled — routing is entirely programmatic via `Shell.Current.GoToAsync(route)`.

### RentalApp.Database (Data Access Layer)
EntityFrameworkCore with Npgsql (PostgreSQL). `AppDbContext` manages `User`, `Role`, and `UserRole` entities. Connection string is read from the `CONNECTION_STRING` environment variable, falling back to embedded `appsettings.json` in the assembly. Passwords are hashed with BCrypt.

**Role system**: Use `RoleConstants` (`Admin`, `OrdinaryUser`, `SpecialUser`) instead of string literals. Registration auto-assigns the role where `IsDefault = true` — if no default role is seeded, the role assignment is silently skipped.

### RentalApp.Migrations (Migration Runner)
Console app that calls `context.Database.Migrate()` to apply pending migrations on startup. Run before the main app in production (handled by docker-compose service ordering).

## Infrastructure

- **Database**: PostgreSQL 16 — credentials: `app_user`/`app_password`, database `appdb`, port 5432
- **Dev Container**: `.devcontainer/devcontainer.json` provisions .NET 10 SDK, Android SDK (build-tools 36.0.0), and Java JDK 21
- **Docker**: Multi-stage `Dockerfile` builds with MAUI Android workload; `docker-compose.yml` orchestrates `db`, `migrate`, and `app` services

# Additional Context

- @PROJECTPLAN.md for context around the projects current status, ways of working, and coding standards.