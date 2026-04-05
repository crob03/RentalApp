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
