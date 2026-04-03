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
| 5 | 2026-04-03 | UX / Auth | Auto-login the user immediately after successful registration |

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

### Decision 5: Auto-Login After Successful Registration
**Date**: 2026-04-03
**Area**: UX / Auth

**Decision**: After a successful registration, the user is logged in immediately — `_currentUser` is set, `AuthenticationStateChanged` is fired, and the app navigates directly to `MainPage`. The registration and login flows now share the same end-state.

**Alternatives considered**:
- **Redirect to login page after registration** — original behaviour. User must re-enter credentials immediately after creating an account.
- **Auto-login with a confirmation message** — navigate to `MainPage` but show a brief success notification first. Rejected to avoid reintroducing a `DisplayAlert` call in the ViewModel.

**Rationale**: The app currently has no email verification step, so there is no gate between account creation and a valid authenticated session. Requiring the user to log in manually immediately after registering adds friction with no security benefit in this context. Should email verification be introduced in future, this decision should be revisited — auto-login would need to be deferred until the address is confirmed.
