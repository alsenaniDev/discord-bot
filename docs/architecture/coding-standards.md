# Coding Standards

Official engineering standards for the Discord Bot Platform. All contributors and AI agents must follow these rules.

---

## General principles

1. **Read before write** — match surrounding code style.
2. **Minimal scope** — do not refactor unrelated code in feature tasks.
3. **No secrets in git** — use example files and env vars.
4. **Tenant isolation** — every guild query filters by `GuildId`.
5. **Bot never touches DB** — use API.

---

## C# standards

### Style

- File-scoped namespaces
- Primary constructors acceptable where project already uses them
- `var` when type is obvious from RHS
- Braces on all control flow blocks
- One public type per file (exceptions: small nested DTOs in same file as service)

### Naming

See `naming-conventions.md`.

### Async

- Suffix async methods with `Async`
- Accept `CancellationToken cancellationToken = default` on all public service methods
- **Never** `.Result` or `.Wait()` on async calls
- Prefer `await` throughout controller → service chain

### Nullable

- Nullable reference types enabled where project uses them
- Use `?` for optional returns and parameters
- Guard clauses for null early return

### Exceptions

- Use `InvalidOperationException` for business rule violations caught by controllers
- Do not swallow exceptions silently
- Unhandled exceptions reach `ExceptionHandlingMiddleware`

### Dependency injection

- Register services in `DependencyInjection.cs`
- Default lifetime: **Scoped** for services and DbContext
- Inject interfaces (`IGuildService`), not concrete classes in controllers
- Bot: register handlers and services in `Program.cs`

### Entity Framework

- Configurations in `Data/Configurations/{Entity}Configuration.cs`
- No lazy loading
- `AsNoTracking()` for read-only queries
- Migrations via CLI only — never hand-edit snapshot without migration
- Do not query without `GuildId` filter on tenant data

### DTO rules

- Entities never returned from API controllers
- DTOs in `Infrastructure/Models/`
- Request bodies use `*Request` suffix
- Use `init` properties on response DTOs where established
- `[Required]` or manual null checks in controllers/services

### Comments and XML docs

- XML docs on public API controllers and complex service interfaces
- Comments explain **why**, not what
- No commented-out code in commits

### Class and method size

| Limit | Guideline |
|-------|-----------|
| Class | Prefer < 400 lines; split handlers by feature |
| Method | Prefer < 50 lines; extract private methods |
| Nesting | Max 3 levels; use guard clauses |

Large files today (`GuildsController`, `GuildService`, `BotApiClient`) — **do not grow**; extract when touching.

---

## Angular / TypeScript standards

### Structure

- One component per feature folder (`.ts`, `.html`, `.css`)
- Services in `core/services/` — singleton `providedIn: 'root'`
- Models in `core/models/` — interfaces only, no logic

### Naming

- Components: `PascalCase` + `Component` suffix
- Files: kebab-case matching selector (`staff.component.ts`)
- Services: `PascalCase` + `Service` suffix

### Subscriptions

- Unsubscribe in `ngOnDestroy` OR use `async` pipe in templates
- Prefer `pipe(catchError(...))` on HTTP calls

### i18n

- **All user-visible strings** through translate pipe or `TranslateService`
- Add keys to **both** `en.json` and `ar.json`

### HTTP

- Use `GuildService` and domain services — do not call HttpClient directly from components
- Handle errors with `getApiErrorMessage` utility

### Types

- Avoid `any`
- Define interfaces for API responses in `core/models/`

---

## Git and commits

- Conventional commit style preferred: `feat:`, `fix:`, `docs:`, `refactor:`
- One logical change per commit
- Do not commit `*.local.json`, `.env`, JWT secrets

---

## Testing

**Not required for every task today** — test projects do not exist yet.

When tests are added:

- Unit test services with mocked DbContext
- Integration test API with Testcontainers PostgreSQL
- Do not test Angular components unless complex logic

---

## Code review checklist

- [ ] GuildId filtered on all tenant queries
- [ ] Authorization checked before mutation
- [ ] Module enabled check for feature code paths
- [ ] No secrets in diff
- [ ] i18n keys added (EN + AR)
- [ ] Handbook updated if architecture changed
- [ ] Migration included if schema changed

---

## Related docs

- `naming-conventions.md`, `folder-structure.md`
- `architecture-principles.md`
