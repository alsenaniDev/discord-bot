# Step 8 — MVP Stabilization

Hardening pass before new features. No new modules — only reliability, UX, docs, and secrets hygiene.

---

## API improvements

| Change | Why |
|--------|-----|
| `ExceptionHandlingMiddleware` | Catches unhandled errors → JSON Problem Details + log |
| `RequestLoggingMiddleware` | Logs `{Method} {Path} responded {StatusCode}` |
| `GuildSettingsValidator` | Server-side validation for snowflakes and required fields |
| Consistent error JSON | `{ message, errors[] }` on validation failures |
| `appsettings.*.local.json` support | Secrets stay out of committed files |
| Empty secrets in base `appsettings.json` | Prevents accidental commit of JWT/bot keys |

---

## Dashboard improvements

| Change | Why |
|--------|-----|
| `getApiErrorMessage()` | Reads API `message`, `errors`, `detail` fields |
| Loading spinners | Login, callback, servers, settings |
| Improved empty state | Step-by-step when no servers found |
| Form validators | Required message, snowflake IDs, conditional required fields |
| Field-level errors | Red text under invalid inputs |
| Alert components | Consistent success/error styling |
| Login redirect | Skip login page if JWT already in localStorage |

---

## Secrets & config

1. Copy `appsettings.Development.example.json` → `appsettings.Development.local.json`
2. `.local.json` files are **gitignored**
3. `.env.example` documents environment variable overrides
4. Base `appsettings.json` has **empty** `Jwt:Secret`, `Bot:ApiKey`, Discord credentials

---

## Files added/changed

```
src/DiscordBot.Api/
├── Middleware/ExceptionHandlingMiddleware.cs
├── Middleware/RequestLoggingMiddleware.cs
├── Validation/GuildSettingsValidator.cs
├── appsettings.Development.example.json
└── Program.cs

dashboard/.../src/app/core/utils/
├── api-error.util.ts
└── settings.validators.ts

.gitignore          ← local config patterns
.env.example        ← expanded
README.md           ← full setup + troubleshooting
```

---

## Verify locally

```bash
dotnet build DiscordBot.sln
cd dashboard/DiscordBot.Dashboard && npm run build
```

Follow README **Quick start** with `.local.json` files filled in.

**Step 8 complete.**
