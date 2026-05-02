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
| 12 | 2026-04-17 | Architecture | `RentalApp.Models` DTOs as the exclusive return types of `IApiService` — never EF entities *(superseded by Decision 14)* |
| 13 | 2026-04-30 | Architecture / Data Access | All EF Core entity configuration lives in `AppDbContext.OnModelCreating`; models carry only `[Required]` |
| 14 | 2026-05-01 | Architecture | `RentalApp.Contracts` project as the exclusive source of `IApiService` request/response record types *(supersedes Decision 12; superseded by Decision 16)* |
| 15 | 2026-05-01 | Architecture / MVVM | ViewModels may call `IApiService` directly for non-auth operations; `IItemService` is retired |
| 16 | 2026-05-02 | Architecture | Contracts folder collapsed into `RentalApp`; `RentalApp.Contracts` class library retired *(supersedes Decision 14)* |
| 17 | 2026-05-02 | Tooling / Dev Workflow | Release builds always use the remote API; SharedPreferences switching is debug-only |
| 18 | 2026-05-02 | Architecture | `IApiService` split into four domain service interfaces; `IAuthenticationService` removed with its orchestration logic distributed into ViewModels *(supersedes Decisions 10, 11, 15)* |

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

---

### Decision 13: Consolidate EF Core Entity Configuration in `AppDbContext.OnModelCreating`
**Date**: 2026-04-30
**Area**: Architecture / Data Access

**Decision**: All EF Core entity configuration — table names, primary keys, indexes, column constraints, and relationships — is expressed via the Fluent API in `AppDbContext.OnModelCreating`. EF-specific annotations (`[Table]`, `[PrimaryKey]`, `[MaxLength]`) are removed from model files. `[Required]` is retained on model properties as it serves dual purpose: EF NOT NULL constraint and model validation.

**Alternatives considered**:
- **Mixed annotations + Fluent API** — the prior state. Rejected because configuration was split across two places with no clear rule for what belonged where, making it easy to miss or duplicate settings (e.g. `[MaxLength]` was present on `Item` properties that were already constrained in `AppDbContext`).
- **`IEntityTypeConfiguration<T>` per entity** — the most scalable pattern; each entity gets its own configuration class, and `OnModelCreating` calls `modelBuilder.ApplyConfigurationsFromAssembly(...)`. Not adopted because the overhead of separate files per entity is unnecessary at the current scale of three entities, and `OnModelCreating` remains readable. This remains the preferred migration path if the model grows significantly.
- **All configuration via annotations** — cannot express all EF behaviour (relationships, custom column types such as `geography(Point, 4326)`, composite indexes). Not viable as a complete solution.

**Rationale**: `AppDbContext` is already the single place that configures indexes, max lengths, relationships, and column types. Extending it to also own table names and primary keys makes it the unambiguous source of truth for how EF sees each entity. Models become near-POCOs decorated only with validation annotations, with no dependency on `Microsoft.EntityFrameworkCore` or `System.ComponentModel.DataAnnotations.Schema`.

---

### Decision 14: `RentalApp.Contracts` as the Exclusive Source of `IApiService` Request/Response Types *(Supersedes Decision 12; superseded by Decision 16)*
**Date**: 2026-05-02
**Area**: Architecture

**Decision**: All `IApiService` method parameters and return types use records defined in the `RentalApp.Contracts` class library. Requests live under `RentalApp.Contracts/Requests/`; responses live under `RentalApp.Contracts/Responses/`. EF Core entities from `RentalApp.Database.Models` are never returned from `IApiService` or exposed to any layer above it. `RentalApp.Models` is deleted.

**Alternatives considered**:
- **Retain `RentalApp.Models` in the UI project** — the prior approach (Decision 12). Rejected because it allowed `RemoteApiService` and `LocalApiService` to diverge — each implementation could define or interpret data objects differently with no enforcement of alignment.
- **Inline record types per service file** — no shared project; each implementation defines its own local types. Rejected for the same reason: duplicated definitions with no single point of truth, meaning shape mismatches between the two implementations go undetected until runtime.
- **`RentalApp.Database` as the home for shared types** — `RentalApp` already references `RentalApp.Database`, so types defined there are accessible. Rejected because a data access library carrying transport contract types is a layering violation; it would mix persistence concerns with API shape concerns in a single project.

**Rationale**: Without a shared contracts project, `RemoteApiService` and `LocalApiService` each had their own interpretation of what a request or response looked like, with no mechanism to keep them in sync. `RentalApp.Contracts` is the single source of truth for all data objects crossing the `IApiService` boundary — both implementations must satisfy the same record shapes, so drift between the local and remote paths is caught at compile time rather than at runtime. Registering `IApiService` as the single DI switch point only works correctly when both implementations speak the same type language; `RentalApp.Contracts` enforces that.

---

### Decision 15: ViewModels May Call `IApiService` Directly; `IItemService` Retired
**Date**: 2026-05-01
**Area**: Architecture / MVVM

**Decision**: The rule from Decision 10 — *"ViewModels → Service → IApiService"* — no longer applies to item operations. ViewModels that perform item CRUD (`ItemsListViewModel`, `NearbyItemsViewModel`, `ItemDetailsViewModel`, `CreateItemViewModel`, and their base `ItemsSearchBaseViewModel`) inject and call `IApiService` directly. `IItemService` and `ItemService` are deleted. Authentication operations remain exclusively behind `IAuthenticationService`; ViewModels must never call `IApiService.LoginAsync` or `IApiService.RegisterAsync` directly.

**Alternatives considered**:
- **Retain `IItemService` as a thin pass-through** — `ItemService` delegated directly to `IApiService` with input validation. Rejected because the validation responsibility is better placed in the `ItemValidator` helper (now in `Helpers/`), and a service that only validates and delegates adds a layer with no domain logic of its own.
- **Introduce a new `IItemService` with richer domain logic** — items have lifecycle state (available/unavailable), rental associations, and review aggregates; a stateful service could own these transitions. Rejected as premature given current requirements; this remains the preferred migration path if item domain logic grows in complexity.
- **Keep the existing `IItemService` and extend `IApiService` to use Contracts types** — the refactoring cost of updating `IItemService`'s signatures to match the new Contracts types is equivalent to removing it, and the resulting thin wrapper provides no architectural benefit.

**Rationale**: `IItemService` existed primarily to provide a stable type boundary between ViewModels and the transport layer. With `RentalApp.Contracts` now serving as that stable boundary (Decision 14), the service layer for items has no remaining responsibility that justifies its existence. Auth operations retain a dedicated service (`IAuthenticationService`) because they carry genuine domain logic — credential persistence, session state, `LoggedInUser` population, and the `OnLoginChanged` event — that must not leak into ViewModels or the transport layer.

---

### Decision 16: Contracts Folder Collapsed into `RentalApp`; `RentalApp.Contracts` Class Library Retired *(Supersedes Decision 14)*
**Date**: 2026-05-02
**Area**: Architecture

**Decision**: The `RentalApp.Contracts` class library is removed. Request/response records now live in a `Contracts/` folder inside `RentalApp` under the `RentalApp.Contracts` namespace — requests in `Contracts/Requests/`, responses in `Contracts/Responses/`. `RentalApp.Database` does not reference these types; only `RentalApp` and `RentalApp.Test` (via its project reference to `RentalApp`) consume them.

**Alternatives considered**:
- **Retain `RentalApp.Contracts` as a separate class library** — the prior approach (Decision 14). Rejected because the separate project added cognitive overhead (an extra entry in the solution, an extra project reference to manage) without delivering meaningful architectural benefit at the current scale of three projects and a single consumer.

**Rationale**: The original motivation for a dedicated contracts project was to enforce a compile-time boundary ensuring both `RemoteApiService` and `LocalApiService` used the same record shapes. That boundary still exists — both implementations are in `RentalApp` and must satisfy the same `IApiService` signatures — but it no longer requires a separate assembly to enforce it. A `Contracts/` folder in `RentalApp` provides the same organisational clarity with less structural overhead. The namespace (`RentalApp.Contracts`) is preserved so call sites read identically to the separate-project approach.

---

### Decision 17: Release Builds Always Use the Remote API; SharedPreferences Switching is Debug-Only
**Date**: 2026-05-02
**Area**: Tooling / Dev Workflow

**Decision**: In `MauiProgram.cs`, the `useSharedApi` flag is wrapped in a `#if DEBUG` / `#else` preprocessor block. Debug builds read the flag from Android SharedPreferences (as established in Decision 9), allowing `make use-local-api` / `make use-remote-api` to switch at runtime without a rebuild. Release builds use `const bool useSharedApi = true`, hardcoding the remote API path at compile time.

**Alternatives considered**:
- **Single runtime flag for all configurations** — the prior state. Rejected because a release build could ship with the flag set to `false` (e.g. from a developer's local SharedPreferences state), routing production traffic to a local database.
- **Separate build configurations (e.g. Release-Local)** — unnecessary complexity; the `DEBUG` symbol already cleanly separates the two cases with no extra configuration overhead.

**Rationale**: Release builds must always target the remote API. Using `const bool useSharedApi = true` in the `#else` branch means the compiler dead-code-eliminates the entire local-API registration block from the release binary — `LocalApiService` and its dependencies are not referenced, not compiled in, and cannot be reached at runtime, even via `adb` or SharedPreferences manipulation. The debug path is unchanged, so the `make use-local-api` workflow (Decision 9) continues to work normally during development.

---

### Decision 18: `IApiService` Split into Domain Services; `IAuthenticationService` Removed *(Supersedes Decisions 10, 11, 15)*
**Date**: 2026-05-02
**Area**: Architecture

**Decision**: The monolithic `IApiService` interface (20+ methods across auth, items, rentals, and reviews) is retired and replaced by four domain-scoped service interfaces: `IAuthService`, `IItemService`, `IRentalService`, and `IReviewService`. Each interface has symmetric `Remote*` and `Local*` implementations; a shared `RemoteServiceBase` provides the common `IApiClient` dependency for all remote implementations. `IAuthenticationService` and `AuthenticationService` are deleted entirely. The orchestration logic they previously owned — writing the bearer token on login, persisting credentials when Remember Me is set, clearing credentials and token on logout, and re-authenticating on startup — is distributed into the ViewModels that own those flows: `LoginViewModel`, `AppShellViewModel`, and `LoadingViewModel`. These ViewModels now inject `AuthTokenState` and `ICredentialStore` directly alongside their respective domain service. ViewModels for items, rentals, and reviews inject the appropriate domain service interface directly, consistent with the direction established in Decision 15.

**Alternatives considered**:
- **Retain `IAuthenticationService` as an orchestrator above `IAuthService`** — `IAuthenticationService` would delegate transport to `IAuthService` while continuing to own token writing, credential persistence, and the `OnLoginChanged` event. Rejected because `AuthenticationService` had become a thin pass-through with no logic of its own beyond wiring three collaborators (`IApiService`, `AuthTokenState`, `ICredentialStore`) — logic that is equally expressible in the two or three ViewModels that need it. Keeping the orchestrator added an indirection layer and a fourth constructor dependency with no encapsulation benefit.
- **Retain `IApiService` and decompose via sub-interfaces** — introduce `IAuthApi`, `IItemApi`, etc. as sub-interfaces of a master `IApiService`. Rejected because it still requires both `RemoteApiService` and `LocalApiService` to implement the full combined interface; the seam between domain groups in a single class creates friction as each group grows.
- **Push auth orchestration into a dedicated `AuthOrchestrator` service (not a transport service)** — a non-transport service owning token/credential lifecycle, injected by the three ViewModels. Rejected as over-engineering: the orchestration logic is five to ten lines spread across three already-existing ViewModels, not a complex domain concern that warrants its own abstraction.
- **Keep the monolithic `IApiService` with ViewModels calling it directly** — the state after Decision 15. Rejected because the interface had become a grab-bag of unrelated operations, making it impossible to register different implementations per domain group and difficult to isolate tests to a specific domain.

**Rationale**: A single `IApiService` interface spanning all domains violated the Interface Segregation Principle — every consumer that needed one group of methods was forced to depend on the full interface. Splitting by domain means each ViewModel declares exactly which concerns it has, constructor signatures become self-documenting, and tests inject only the fakes they need. Auth orchestration (token writing, credential persistence, session observation) moving into ViewModels makes the flow explicit and traceable: `LoginViewModel.LoginAsync` sets the token and optionally saves credentials in the same method that calls `IAuthService.LoginAsync`, so there is no hidden side-effect in a service layer. `AppShellViewModel` subscribes to `AuthTokenState.AuthenticationStateChanged` directly, removing the `OnLoginChanged` indirection that `IAuthenticationService` previously provided. The symmetric `Local*` / `Remote*` pair pattern from Decision 10 is preserved — only the granularity of the interface boundary changes.

### Decision 19: Services Must Use Repositories; Direct `AppDbContext` Access is Prohibited
**Date**: 2026-05-02
**Area**: Architecture / Data Access

**Decision**: Local service implementations (`Local*`) must interact with the database exclusively through repository interfaces (`IItemRepository`, `IUserRepository`, `ICategoryRepository`, etc.). Direct injection of `IDbContextFactory<AppDbContext>` or `AppDbContext` into services is prohibited. `AppDbContext` is an implementation detail of the `RentalApp.Database` project and must not leak into the service layer. Every entity that requires data access from a service must have a corresponding `I*Repository` / `*Repository` pair in `RentalApp.Database/Repositories/`. Repositories own `AppDbContext` lifetime management (`await using var context = _contextFactory.CreateDbContext()`) and are the sole location where EF Core LINQ queries are written.

**Alternatives considered**:
- **Allow services to inject `IDbContextFactory<AppDbContext>` directly** — the pre-existing approach in `LocalAuthService`, which injected the context factory and queried `context.Users` inline. Rejected because it exposes the ORM abstraction to the service layer, tightly couples services to EF Core, and makes service-layer unit testing dependent on a real database or complex context mocking.
- **Use a generic repository (`IRepository<T>`)** — a single interface parameterised by entity type, exposing `GetByIdAsync`, `AddAsync`, etc. Rejected because it forces all callers to work with raw entities and cannot express entity-specific query patterns (e.g. `GetNearbyItemsAsync`, `CountItemsByOwnerAsync`) without either leaking `IQueryable<T>` or adding a parallel set of extension points.
- **Use a Unit of Work pattern wrapping all repositories** — a `IUnitOfWork` interface exposing each repository as a property and managing a shared `AppDbContext` across them. Rejected as over-engineering for the current scale: each repository manages its own short-lived context, which is correct for the Singleton registration lifetime and avoids cross-repository transaction complexity that is not yet needed.

**Rationale**: Confining `AppDbContext` to the repository layer enforces a stable abstraction boundary: services describe *what* data they need through repository interfaces, and repositories describe *how* to fetch it using EF Core. This separation means service tests (e.g. `LocalAuthServiceTests`) can construct real repository instances against a test database without any mocking, while still being isolated from EF Core query internals. It also ensures that data-access concerns — query optimisation, eager loading, PostGIS function calls — are consolidated in one place rather than scattered across service implementations. The `IUserRepository` introduction in this session closed the last remaining violation, where `LocalAuthService` was querying `context.Users` and `context.Items` directly.
