# RentalApp

A cross-platform rental application built with .NET MAUI targeting Android. Users can list items for rent, browse and request rentals, manage the rental lifecycle, and leave reviews — all backed by a PostgreSQL database with PostGIS for location-aware item search.

---

## Prerequisites

**Host machine**

| Dependency | Version |
|------------|---------|
| Docker + Docker Compose | any recent |
| ADB (Android Debug Bridge) | bundled with Android SDK |
| Android emulator | via Android Studio AVD Manager |

**Dev Container (provisioned automatically)**

| Dependency | Version |
|------------|---------|
| .NET SDK | 10 |
| Android SDK build-tools | 36.0.0 |
| JDK | 21 |

The recommended setup is the included **Dev Container** (`.devcontainer/`). Open the repo in VS Code and choose **Reopen in Container** — .NET, the Android SDK, and JDK are provisioned automatically.

Docker and the Android emulator run on the host machine and are accessed from within the container over the network.

---

## Setup

> **Note:** Docker commands must be run on the **host machine**, not inside the Dev Container.

### 1. Start the database (host machine)

```bash
docker-compose up db
```

This starts a PostGIS-enabled PostgreSQL 16 instance on port `5432` with:

- **User**: `app_user`
- **Password**: `app_password`
- **Database**: `appdb`

### 2. Apply migrations (inside Dev Container)

```bash
make migrate
```

### 3. (Optional) Seed development data (inside Dev Container)

```bash
make seed-db    # insert categories, users, items, rentals
make clear-db   # truncate all seeded tables and reset sequences
```

---

## Running the Application

The app is built inside the Dev Container and installed onto an emulator running on the host machine via ADB.

### 1. Start the emulator (host machine)

Launch an emulator from Android Studio's AVD Manager.

### 2. Expose ADB to the Dev Container (host machine)

The Dev Container connects to the host ADB server over TCP. Run these commands on the host each time you start a new session:

```bash
adb kill-server
adb -a -P 5037 nodaemon server start
```

This starts the ADB server in a mode that accepts connections from the container on port `5037`.

### 3. Build and install (inside Dev Container)

```bash
dotnet build -c Debug RentalApp.sln
make install
```

### 4. Select the API mode (inside Dev Container)

By default the app points at the remote API. Switch between modes at any time without rebuilding:

```bash
make use-remote-api   # connects to the hosted API (default)
make use-local-api    # connects to Local* services backed by the local database
```

---

## Running Tests

Integration tests require a running PostgreSQL instance. Start the database on the **host machine** first:

```bash
docker-compose up db
```

Then run all tests from inside the Dev Container:

```bash
dotnet test
```

Tests use a dedicated `appdb_test` database. The connection string defaults to:

```
Host=localhost;Port=5432;Database=appdb_test;Username=app_user;Password=app_password
```

Override it with the `CONNECTION_STRING` environment variable if needed.

---

## API

The hosted API is available at:

**`https://set09102-api.b-davison.workers.dev/`**

The app targets this endpoint by default. Use `make use-local-api` to switch to the repository-backed local services during offline development.

---

## Architecture

The solution is split into four projects with clearly separated responsibilities:

| Project | Responsibility |
|---------|----------------|
| `RentalApp` | MAUI UI — Views, ViewModels, Services, HTTP layer |
| `RentalApp.Database` | Data access — EF Core, Repositories, rental state machine |
| `RentalApp.Migrations` | EF Core migration files |
| `RentalApp.Test` | Unit and integration tests |

The UI layer follows **MVVM** using `CommunityToolkit.Mvvm`. ViewModels consume service interfaces (`IAuthService`, `IItemService`, `IRentalService`, `IReviewService`) injected via DI — each with a `Remote*` (HTTP) and `Local*` (repository-backed) implementation. The active implementation is selected at runtime by an Android SharedPreference.

For a full breakdown of the ViewModel hierarchy, service layer, HTTP layer, rental state machine, and database schema see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
