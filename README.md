# TaskTracker

A small **.NET 8 + EF Core + SQLite** sample project, built as a learning playground for
modern testing scenarios. The use case is a minimal task-tracking API where users own
projects, projects contain tasks, and tasks can be tagged.

The project is intentionally structured to demonstrate:

- **Clean Architecture** layering (Domain / Application / Infrastructure / Api)
- **Minimal API** endpoints in ASP.NET Core
- **EF Core** with SQLite (no MSSQL required)
- A **testing pyramid** with xUnit, FluentAssertions, and `WebApplicationFactory`-based
  integration tests using SQLite in-memory

## Domain model

```
User  1───*  Project  1───*  TaskItem  *───*  Tag
```

- `User` — system user (unique email)
- `Project` — a group of tasks, owned by a user
- `TaskItem` — a unit of work with a status (`Open → InProgress → Done`)
- `Tag` — labels attached to tasks (many-to-many)

Business rules live inside the entities (`TaskItem.Start()`, `TaskItem.Complete()`, etc.),
not in services — so they can be unit-tested without any infrastructure.

## Solution layout

```
src/
  TaskTracker.Domain         # Entities, enums, domain exceptions (no dependencies)
  TaskTracker.Application    # DTOs, services, validation (depends on Domain)
  TaskTracker.Infrastructure # EF Core, AppDbContext, migrations (depends on Application)
  TaskTracker.Api            # Minimal API host + DI wiring
tests/
  TaskTracker.UnitTests        # Domain & service unit tests
  TaskTracker.IntegrationTests # End-to-end HTTP tests via WebApplicationFactory
```

## Tech stack

| Concern | Choice |
|---|---|
| Runtime | .NET 8 (pinned via `global.json` to SDK `8.0.418`) |
| API | ASP.NET Core Minimal API |
| ORM | EF Core 8 |
| Database | SQLite |
| Tests | xUnit + FluentAssertions + `Microsoft.AspNetCore.Mvc.Testing` |

## Getting started

### Prerequisites

- .NET 8 SDK (`8.0.418` or compatible)
- `dotnet-ef` global tool:

  ```powershell
  dotnet tool install --global dotnet-ef --version 8.0.10
  ```

### Build

```powershell
dotnet build TaskTracker.sln
```

### Apply migrations / create the SQLite database

```powershell
dotnet ef database update `
  --project src/TaskTracker.Infrastructure `
  --startup-project src/TaskTracker.Api
```

This produces `src/TaskTracker.Api/tasktracker.db`.

### Run the API

```powershell
dotnet run --project src/TaskTracker.Api
```

The API listens on the default ASP.NET Core ports. A health-check root endpoint
(`GET /`) returns `TaskTracker API up.`. Logs are written to console and to
`logs/log-<date>.txt` (daily rolling, 14-day retention).

### Run tests

```powershell
dotnet test
```

### End-to-end smoke test

A PowerShell smoke test exercises the full API surface across 12 scenarios
(happy path + 5 negative cases for 400/404/409). Start the API on
`http://localhost:5099` and run:

```powershell
dotnet run --project src/TaskTracker.Api --urls http://localhost:5099   # in one shell
./smoke-test.ps1                                                        # in another
```

## API surface

All endpoints return JSON. Errors are returned as `ProblemDetails`
(RFC 7807). `TaskItem.Status` is carried as a string (`Open` / `InProgress` / `Done`).

| Resource | Verb | Path |
|---|---|---|
| Users | `POST` | `/users` |
| Users | `GET` | `/users` |
| Users | `GET` | `/users/{id}` |
| Projects | `POST` | `/projects` |
| Projects | `GET` | `/projects/{id}` |
| Projects | `GET` | `/users/{ownerId}/projects` |
| Tasks | `POST` | `/projects/{projectId}/tasks` |
| Tasks | `GET` | `/projects/{projectId}/tasks?status=` |
| Tasks | `GET` | `/tasks/{id}` |
| Tasks | `PATCH` | `/tasks/{id}` |
| Tasks | `DELETE` | `/tasks/{id}` |
| Tasks | `POST` | `/tasks/{id}/start` |
| Tasks | `POST` | `/tasks/{id}/complete` |
| Tasks | `POST` | `/tasks/{id}/tags` |
| Tasks | `DELETE` | `/tasks/{id}/tags/{name}` |

## Project status

This is an in-progress learning project. Currently scaffolded:

- [x] Solution + Clean Architecture project skeleton
- [x] Domain entities with business rules
- [x] EF Core configuration + initial migration
- [x] Minimal API host wired to Infrastructure
- [x] Application services + DTOs + FluentValidation validators
- [x] Repository interfaces + `IUnitOfWork` abstraction in Application
- [x] Infrastructure: repository implementations + UnitOfWork
- [x] Minimal API endpoints (CRUD + status transitions) + Serilog + global exception handler
- [ ] Unit tests for domain rules
- [ ] Application service tests against SQLite in-memory
- [ ] Integration tests with `WebApplicationFactory`

## Architectural decisions

| Concern | Choice |
|---|---|
| Data access | Repository pattern — repo interfaces in Application, EF Core impls in Infrastructure |
| Validation | FluentValidation (validators registered via `AddValidatorsFromAssembly`) |
| Error model | Exception-based (`NotFoundException` / `ConflictException` / `ValidationException`) |
| `TaskStatus` over the wire | Carried as string in DTOs, parsed at the service boundary |
| Test data | Manual Builder pattern (no AutoFixture/Bogus) |
| Logging | Serilog (Console + rolling file sink) |
| Auth | None (out of scope for this learning iteration). |
