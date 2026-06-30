# Step 1 — Solution structure

## What we created

```
discord bots/
├── DiscordBot.sln                 ← ties all .NET projects together
├── src/
│   ├── DiscordBot.Api/            ← HTTP layer (controllers, Program.cs)
│   ├── DiscordBot.Domain/         ← entities & enums (pure C#)
│   └── DiscordBot.Infrastructure/ ← database + Discord.Net (added next)
└── dashboard/
    └── DiscordBot.Dashboard/      ← Angular UI
```

## Why three .NET projects?

| Layer | Analogy | Why separate |
|-------|---------|--------------|
| **Domain** | The rules of your game | No SQL, no Discord — just data shapes |
| **Infrastructure** | The tools (DB, Discord client) | Can swap SQL Server or library without touching API |
| **Api** | The front door | Controllers stay thin; logic lives in services |

This is **not** CQRS or microservices — just folders that stay organized as the project grows.

## Dependency direction

```
Api → Infrastructure → Domain
Api → Domain
```

Domain never references anything else. That keeps entities reusable and easy to test.

## Angular layout (prepared, not built yet)

```
src/app/
├── core/       ← auth, API client, guards (singleton services)
├── features/   ← one folder per page (auth, servers, settings, logs)
└── shared/     ← buttons, layout pieces used everywhere
```

## Verify

```bash
dotnet build DiscordBot.sln
cd dashboard/DiscordBot.Dashboard && npm start
```

Next step: **EF Core + SQL Server** to store guild settings.
