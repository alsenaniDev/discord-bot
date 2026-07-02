# Folder Structure

Detailed folder layout for the monorepo. See also `solution-structure.md`.

## Repository root

```
discord bots/
├── DiscordBot.sln
├── README.md
├── docker-compose.yml
├── .env.example
├── .gitignore
├── deploy/railway/           # Production Docker + Railway
├── database/seeds/           # Manual SQL
├── docs/                     # All documentation
│   ├── architecture/         # This handbook
│   ├── adr/
│   ├── product/
│   ├── project-management/
│   ├── progress/
│   └── step-*.md             # Historical implementation guides
├── dashboard/DiscordBot.Dashboard/
└── src/
    ├── DiscordBot.Api/
    ├── DiscordBot.Bot/
    ├── DiscordBot.Domain/
    └── DiscordBot.Infrastructure/
```

## src/DiscordBot.Domain/

```
Domain/
├── Entities/           # One file per entity (~21 files)
├── Enums/              # GuildPermissions, LogEventType, TicketStatus, ...
├── Constants/          # ModuleKeys, PlanKeys, GuildPermissionDefaults
├── Extensions/         # Enum extension methods
└── Helpers/            # Shared pure functions
```

**Rule:** No subfolders beyond these categories unless entity count exceeds ~40.

## src/DiscordBot.Infrastructure/

```
Infrastructure/
├── Data/
│   ├── AppDbContext.cs
│   ├── Configurations/     # One *Configuration.cs per entity
│   ├── *Seeder.cs          # Hosted seed services
│   └── Migrations/         # EF migrations (never edit old migrations)
├── Services/               # Flat — one *Service.cs per domain area
├── Auth/                   # OAuth, JWT, auth codes
├── Options/                # IOptions binding classes
├── Models/                 # DTOs grouped by feature (flat folder)
└── DependencyInjection.cs
```

**Rule:** New business logic → new `*Service.cs` in Services/, not nested folders (matches current convention).

## src/DiscordBot.Api/

```
Api/
├── Controllers/            # One controller per route group
├── Extensions/             # Startup extensions (Auth, CORS)
├── Filters/                # BotApiKey, PlatformAdmin
├── Middleware/             # Exception, logging
├── Validation/             # Input validators
├── Models/                 # API-only models (minimal)
├── Program.cs
├── appsettings*.json
└── Properties/launchSettings.json
```

## src/DiscordBot.Bot/

```
Bot/
├── Api/                    # BotApiClient, ApiModels
├── Commands/               # One handler file per command group
├── Configuration/          # Options classes
├── Extensions/
├── Services/               # Hosted services, workers, guards, builders
├── UI/                     # DiscordCustomIds
├── Program.cs
└── appsettings*.json
```

## dashboard/DiscordBot.Dashboard/

```
Dashboard/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── guards/
│   │   │   ├── interceptors/
│   │   │   ├── models/
│   │   │   ├── services/
│   │   │   └── utils/
│   │   ├── features/       # One folder per page
│   │   │   ├── admin/
│   │   │   ├── auth/
│   │   │   ├── layout/
│   │   │   └── {feature}/
│   │   ├── shared/         # Reusable components
│   │   ├── app.module.ts
│   │   └── app-routing.module.ts
│   ├── assets/i18n/
│   ├── environments/
│   ├── index.html
│   └── styles.css
├── angular.json
├── package.json
└── vercel.json
```

## docs/

```
docs/
├── architecture/           # Handbook (canonical)
├── adr/                    # Decision records
├── product/                # Business docs
├── project-management/     # Backlog, debt, releases
├── progress/               # Task completion reports
└── step-*.md               # Historical (do not delete)
```

## Where to put new code

| Adding | Location |
|--------|----------|
| New entity | `Domain/Entities/` + `Infrastructure/Data/Configurations/` + migration |
| New API endpoint | Existing or new `Controllers/` + `Services/` + DTO in `Models/` |
| New bot command | `Bot/Commands/` + register in `SlashCommandRegistration.cs` + route in `DiscordBotHostedService` |
| New dashboard page | `features/{name}/` + route in `app-routing.module.ts` + i18n keys |
| New module | `ModuleKeys.cs` + seeder + bot guard + dashboard page |
| Architecture decision | `docs/adr/NNNN-title.md` |

## Related docs

- `solution-structure.md`, `coding-standards.md`
