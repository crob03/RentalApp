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

## Architecture

Three projects in the solution:

### RentalApp (MAUI UI)
MVVM pattern using `CommunityToolkit.Mvvm`. Views are XAML pages in `Views/`, bound to ViewModels in `ViewModels/`. `BaseViewModel` provides `IsBusy`, `Title`, and `SetError(msg)`/`ClearError()` helpers — always use these for error state in ViewModels. Services in `Services/` handle authentication (`IAuthenticationService`) and navigation (`INavigationService`). Dependency injection is configured in `MauiProgram.cs`.

**DI lifetime gotcha**: `LoginViewModel`, `RegisterViewModel`, `TempViewModel`, and `AppShellViewModel` are registered as Singleton (state persists across navigations). All other ViewModels and Pages are Transient.

**Shell navigation**: Root route is `//login` (used by `NavigationService.NavigateToRootAsync()`). AppShell flyout is disabled — routing is entirely programmatic via `Shell.Current.GoToAsync(route)`.

### RentalApp.Database (Data Access Layer)
EntityFrameworkCore with Npgsql (PostgreSQL). `AppDbContext` manages the `User` entity. Connection string is read from the `CONNECTION_STRING` environment variable, falling back to embedded `appsettings.json` in the assembly. Passwords are hashed with BCrypt.

### RentalApp.Migrations (Migrations Library)
Class library housing EF Core migration files under `Migrations/`. Implements `IDesignTimeDbContextFactory<AppDbContext>` so `dotnet ef` can target this project directly without a separate startup project. Applied via `dotnet ef database update --project RentalApp.Migrations` (handled by docker-compose service ordering).

## Infrastructure

- **Database**: PostgreSQL 16 — credentials: `app_user`/`app_password`, database `appdb`, port 5432
- **Dev Container**: `.devcontainer/devcontainer.json` provisions .NET 10 SDK, Android SDK (build-tools 36.0.0), and Java JDK 21
- **Docker**: Multi-stage `Dockerfile` builds with MAUI Android workload; `docker-compose.yml` orchestrates `db`, `migrate`, and `app` services

# Additional Context

- @PROJECTPLAN.md for context around the projects current status, ways of working, and coding standards.