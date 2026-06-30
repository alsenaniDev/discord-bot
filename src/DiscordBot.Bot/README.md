# DiscordBot.Bot

Discord.Net worker that connects to the Gateway and calls the .NET API.

- `Configuration/` — Token and API settings
- `Api/` — HTTP client for backend endpoints
- `Commands/` — Slash command handlers (/ping, /server, /setup)
- `Services/` — Gateway connection, welcome messages, command registration

Run:

```bash
dotnet run --project src/DiscordBot.Bot
```

Requires `Discord:Token` and matching `Api:ApiKey` with the API's `Bot:ApiKey`.
