# Decisions

Architectural, design, and tooling decisions made during the project.
Each entry is an immutable record — superseding decisions add a new entry rather than editing an existing one.

---

## Decision Log

| # | Date | Area | Decision |
|---|------|------|----------|
| 1 | 2026-03-13 | Tooling | Use Claude Code for code implementation, test writing, and PR review |
| 2 | 2026-03-13 | Process | Log all significant AI interactions in `INTERACTIONS.md`; log decisions in `DECISIONS.md` |
| 3 | 2026-03-30 | Architecture | Store EF Core migrations in `RentalApp.Migrations` class library with `IDesignTimeDbContextFactory` |
| 4 | 2026-03-30 | Tooling | Use CSharpier as the opinionated formatter for `.cs` and XAML files |
| 5 | 2026-04-03 | UX / Auth | Auto-login the user immediately after successful registration *(superseded by Decision 6)* |
| 6 | 2026-04-05 | Auth | Token refresh via `DelegatingHandler` using credentials stored in `SecureStorage`; Remember Me controls persistence; auto-login on startup |
| 7 | 2026-04-05 | Architecture / MVVM | Use `OnAppearing` in code-behind to trigger ViewModel initialisation for startup routing |
| 8 | 2026-04-10 | Tooling | Use DocFX for API documentation generation |
| 9 | 2026-04-16 | Tooling / Dev Workflow | Runtime API switching via Android SharedPreferences written through `adb shell run-as` |
| 10 | 2026-04-17 | Architecture | `IApiService` facade with `RemoteApiService` and `LocalApiService` as symmetric implementations |
| 11 | 2026-04-17 | Architecture | `LoginAsync` returns `Task`; each implementation manages its own session state internally |
| 12 | 2026-04-17 | Architecture | `RentalApp.Models` DTOs as the exclusive return types of `IApiService` — never EF entities |

---

### Decision 1: Use Claude Code as Development Partner
**Date**: 2026-03-13
**Area**: Tooling / Process

**Decision**: Use Claude Code (claude-sonnet-4-6) throughout the project for writing code, writing tests, and reviewing pull requests.

**Alternatives considered**:
- Developer-only implementation (no AI assistance)
- AI used only for code review, not implementation

**Rationale**: Claude Code accelerates implementation while keeping the developer in control via PR review. Logging interactions in `INTERACTIONS.md` ensures traceability and encourages critical evaluation of AI suggestions.

---

### Decision 2: Separate Interaction and Decision Logs
**Date**: 2026-03-13
**Area**: Process

**Decision**: Maintain two separate log files — `INTERACTIONS.md` for AI conversation records and `DECISIONS.md` for architectural/design decisions.

**Alternatives considered**:
- A single log file for both
- No formal logging

**Rationale**: Separating them keeps each file focused. `INTERACTIONS.md` is a chronological conversation log; `DECISIONS.md` is a durable record of *what was decided and why*, useful for onboarding and retrospectives.

---

### Decision 3: Store Migrations in a Dedicated Class Library with IDesignTimeDbContextFactory
**Date**: 2026-03-30
**Area**: Architecture / Data Access

**Decision**: `RentalApp.Migrations` is a class library (not an executable) that houses EF Core migration files and implements `IDesignTimeDbContextFactory<AppDbContext>`. Migrations are applied via `dotnet ef database update --project RentalApp.Migrations`. Reference: [EF Core — Using a Separate Migrations Project](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects?tabs=dotnet-core-cli).

**Alternatives considered**:
- **Migrations in `RentalApp.Database`** — simplest option; `dotnet ef` targets the DbContext project directly. Rejected because it mixes schema migration concerns into the data access library.
- **`RentalApp.Migrations` as a thin executable runner** (`context.Database.Migrate()`) — original approach. Rejected because: the full .NET SDK is available in the Docker environment (making a compiled runner redundant), `dotnet-ef` is already registered in `dotnet-tools.json`, and the executable cannot be targeted directly by `dotnet ef migrations add`.
- **`dotnet ef database update` with a separate startup project** — viable, but requires maintaining an executable solely to provide a startup context for the CLI. Eliminated by using `IDesignTimeDbContextFactory` instead.
- **Third dedicated migrations project alongside an unchanged `RentalApp.Migrations`** — unnecessary extra project; repurposing the existing project achieves the same outcome.

**Rationale**: A dedicated migrations class library cleanly separates schema migration concerns from both the DbContext and application layers, consistent with Microsoft's recommended pattern. `IDesignTimeDbContextFactory` makes the library self-sufficient for `dotnet ef` tooling — no separate startup project is needed — while `MigrationsAssembly("RentalApp.Migrations")` on `AppDbContext` ensures all consumers resolve migrations from the correct assembly.

---

### Decision 4: Use CSharpier as the Opinionated Formatter
**Date**: 2026-03-30
**Area**: Tooling

**Decision**: Use [CSharpier](https://csharpier.com/) as the sole opinionated formatter for all `.cs` and XAML files across the solution. Formatting is non-negotiable and not configurable per-developer — CSharpier's output is the standard.

**Alternatives considered**:
- **Visual Studio / Rider built-in formatters** — each IDE has its own defaults and per-developer settings, leading to formatting inconsistencies across the team.
- **EditorConfig + `dotnet format`** — more configurable than CSharpier, but requires maintaining rule sets and still leaves room for style disagreements.
- **No enforced formatter** — rejected outright; inconsistent formatting increases diff noise and code review friction.

**Rationale**: CSharpier is an opinionated, zero-configuration formatter (analogous to Prettier for JavaScript). By removing formatting choices from the developer, diffs reflect only meaningful code changes. It is provisioned in the dev container and can be run via `dotnet csharpier .`, ensuring a consistent baseline regardless of which IDE or OS a developer uses.

---

### Decision 5: Auto-Login After Registration *(Superseded by Decision 6)*
**Date**: 2026-04-03
**Area**: UX / Auth

**Decision**: After successful registration, automatically log the user in and navigate to `MainPage` without requiring them to return to the login screen.

**Rationale at the time**: Reduced friction for new users — registration and login were a single flow.

**Superseded because**: The introduction of Remember Me (Decision 6) requires the user to pass through the login screen so they can opt into credential persistence. Silent auto-login after registration bypasses this choice entirely.

---

### Decision 6: Token Refresh via DelegatingHandler with SecureStorage
**Date**: 2026-04-05
**Area**: Auth

**Decision**: Implement token refresh using a `DelegatingHandler` (`AuthRefreshHandler`) wrapping the `HttpClient`. If a request returns 401, the handler checks `ICredentialStore` for saved credentials — if present, it re-authenticates via `/auth/token`, updates `AuthTokenState`, and retries the original request once; if absent, it redirects to the login root. Credentials are saved to `SecureStorage` only when the user checks "Remember Me" at login, and are cleared on logout. On app startup, `App.OnStart` checks `ICredentialStore` and silently auto-logs in if credentials are present. Auto-login after registration (Decision 5) is removed so the user passes through the login screen and can make the Remember Me choice.

**Alternatives considered**:
- **Proper refresh token endpoint** — the preferred solution; a short-lived access token paired with a long-lived, rotatable refresh token avoids storing user credentials entirely. Rejected because the backend is not under our control and no `/auth/refresh` endpoint exists.
- **In-memory credential cache only** — credentials held in a private field for the lifetime of the process. Supports token refresh during a session but does not survive app restarts, making Remember Me impossible. Rejected in favour of `SecureStorage`.
- **Manual HttpClient wrapper** — intercepting responses in a custom wrapper class rather than a `DelegatingHandler`. Rejected because `DelegatingHandler` is the .NET-idiomatic middleware pattern for `HttpClient` pipelines and keeps retry logic transparent to all callers.

**Rationale**: The backend exposes only a credential-exchange endpoint (`/auth/token`), so re-authentication requires the original email and password. `SecureStorage` (Android Keystore / iOS Keychain) is the platform-endorsed store for sensitive credentials — encrypted at rest, scoped to the app, and clearable on logout. The `DelegatingHandler` pattern keeps all retry logic in one place and is transparent to callers. Storing credentials only when the user explicitly opts in (Remember Me) limits the exposure window and respects user intent.

---

### Decision 7: Use OnAppearing to Trigger ViewModel Initialisation
**Date**: 2026-04-05
**Area**: Architecture / MVVM

**Decision**: Use `OnAppearing` in the page code-behind to call a ViewModel initialisation method (e.g. `InitializeAsync()`), rather than wiring up initialisation through Shell navigation events or constructors. The code-behind only delegates — it contains no logic itself.

**Alternatives considered**:
- **Constructor initialisation** — cannot be async, so unsuitable for operations like SecureStorage reads or network calls that must complete before navigating.
- **`IQueryAttributable.ApplyQueryAttributes`** — Shell's mechanism for passing parameters to pages on navigation. Appropriate when data is passed *into* a page; not a natural fit for self-driven startup logic with no inbound parameters.
- **`Shell.Current.Navigating` / `Navigated` events** — places navigation lifecycle handling in the ViewModel directly, but requires the ViewModel to subscribe to and unsubscribe from Shell events, coupling it to the Shell infrastructure.

**Rationale**: `OnAppearing` calling a ViewModel method is a widely accepted MVVM compromise in MAUI — used in Microsoft's own MAUI samples — because there is no framework-provided async lifecycle hook that feeds into the ViewModel cleanly. Provided the code-behind only delegates and contains no logic, the separation of concerns is preserved. All service calls, navigation decisions, and state mutations remain in the ViewModel and are independently testable.

---

### Decision 8: Use DocFX for API Documentation
**Date**: 2026-04-10
**Area**: Tooling

**Decision**: Use [DocFX](https://dotnet.github.io/docfx/) for API documentation generation, configured via `docfx.json` and published to GitHub Pages via the documentation workflow.

**Alternatives considered**:
- **Doxygen** — broad multi-language support but no native understanding of C# XML doc comments; `<summary>`, `<param>`, `<returns>`, and `<see cref="..."/>` are not parsed as structured content.

**Rationale**: DocFX is purpose-built for .NET. It uses Roslyn to analyse C# source, meaning XML doc comments are rendered as structured API documentation — cross-references, parameter tables, return types, and inheritance hierarchies are all resolved correctly. As a .NET global tool it requires no additional CI dependencies beyond the .NET SDK already present on the runner.

---

### Decision 9: Runtime API Switching via adb SharedPreferences
**Date**: 2026-04-16
**Area**: Tooling / Dev Workflow

**Decision**: Control the `useSharedApi` flag at runtime (without a rebuild) by reading it from Android SharedPreferences via `Preferences.Default.Get("UseSharedApi", true)` in `MauiProgram.cs`. Two Makefile targets — `use-remote-api` and `use-local-api` — write the preference directly to the app's SharedPreferences file via `adb shell run-as`, then force-stop and restart the app.

**Alternatives considered**:
- **Hardcoded `bool` in source** — requires a code edit and rebuild to switch. Simple and transparent but slow for iterating between the two API paths during development.
- **`appsettings.json` (embedded resource)** — considered and rejected (Interaction 24): an embedded JSON file is compiled into the APK at build time, making it functionally identical to a hardcoded `bool`. Both require a rebuild to change.
- **Intent extras via `adb shell am start -e`** — passes a flag at launch without persisting it. Requires platform-specific MAUI code in `MainActivity.OnCreate` to read the extra and make it available at the composition root. Not persisted across restarts. More complex for less benefit.

**Rationale**: SharedPreferences is the only mechanism that is both writable without a rebuild (via `adb shell run-as` on debug builds) and persistent across app restarts. The `Preferences.Default` API is already part of MAUI and requires no additional dependencies. The Makefile targets encapsulate the `adb` command so the correct quoting and file path are not left to memory. Defaults to remote API (`true`) on a fresh install — no target needs to be run to use the primary path.

**Note**: Requires a debug build and an installed APK. The `run-as` command is restricted to debug/debuggable packages on Android.

---

### Decision 10: IApiService Facade and Layered Service Architecture
**Date**: 2026-04-17
**Area**: Architecture

**Decision**: Introduce a single `IApiService` interface as the data-transport layer, with two implementations — `RemoteApiService` (HTTP) and `LocalApiService` (direct DB via EF Core). Domain concerns are handled by dedicated service classes (e.g. `AuthenticationService`, future `RentalService`) that sit above `IApiService` and are consumed by ViewModels. The pattern is: **ViewModel → Service → IApiService**. Only `IApiService` is switched in the DI container; all services above it are registered unconditionally.

**Alternatives considered**:
- **ViewModels call `IApiService` directly** — eliminates the service layer. Rejected because domain logic (auth state, event firing, credential persistence, future rental state transitions) belongs in a testable service, not in a ViewModel.
- **Dual service implementations per feature** — the prior approach (`ApiAuthenticationService`, `LocalAuthenticationService`). Rejected because domain logic was duplicated across both, and every new feature would require two parallel implementations.
- **Feature flags inside a single implementation** — a single service with `if (useRemote)` branches. Rejected as it conflates transport concerns with domain logic and scales poorly.

**Rationale**: Separating data transport (`IApiService`) from domain logic (service layer) means each layer has a single responsibility and can evolve independently. Adding a new feature requires one service class and two transport implementations — not two full-stack duplicates. The ViewModel layer remains thin and focused on presentation state.

---

### Decision 11: `LoginAsync` Returns `Task`; Session State is an Implementation Detail
**Date**: 2026-04-17
**Area**: Architecture / Auth

**Decision**: `IApiService.LoginAsync` returns `Task` (not `Task<AuthToken>`). Each implementation manages its own session state internally — `RemoteApiService` stores the bearer token in `AuthTokenState`; `LocalApiService` holds the authenticated user in a private field. `AuthTokenState` mutation in `RemoteApiService.LoginAsync` is an accepted intentional side effect of the transport layer.

**Alternatives considered**:
- **`LoginAsync` returns `Task<AuthToken>`** — `AuthenticationService` receives the token and passes it to `AuthTokenState`. Rejected because `LocalApiService` has no token concept; it would be forced to fabricate a mock token, which is semantically incorrect and leaks a remote-API concern into the interface contract.
- **`LoginAsync` returns `Task<string>` (raw token string)** — same problem. The interface should not require implementations that have no token to manufacture one.

**Rationale**: The interface contract is symmetric only if it makes no assumptions about how session state is represented. Returning `Task` leaves each implementation free to use whatever internal mechanism is appropriate — bearer token, in-memory user reference, or future alternatives — without the interface encoding those details.

---

### Decision 12: `RentalApp.Models` DTOs as Exclusive `IApiService` Return Types
**Date**: 2026-04-17
**Area**: Architecture / Data Access

**Decision**: All `IApiService` method return types use records in `RentalApp.Models`. EF Core entities from `RentalApp.Database.Models` are never returned from `IApiService` or exposed to any layer above it.

**Alternatives considered**:
- **Return EF entities directly** — eliminates the mapping step in `LocalApiService`. Rejected because the remote API returns JSON that does not map 1:1 to entity shapes (different field sets, no navigation properties, computed fields like `AverageRating`). Using entities would couple the transport contract to the persistence schema and break when the two diverge.
- **Shared models used by both EF and the transport layer** — a single model class annotated for both JSON deserialisation and EF column mapping. Rejected as an anti-pattern; EF attributes (`[Column]`, `[Key]`, navigation properties) have no meaning in a transport context and vice versa.

**Rationale**: The transport layer and the persistence layer have different shapes, different nullability requirements, and different lifecycles. Keeping them separate means each can evolve independently. The `User` DTO, for example, carries `AverageRating`, `ItemsListed`, and `Reviews` — none of which exist on the EF `User` entity.
