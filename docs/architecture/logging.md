# Logging

## Platform activity logs (product feature)

**Module:** `logs`  
**Entity:** `LogEntry` → table `LogEntries`

### Purpose

Store guild-scoped activity events viewable in dashboard **Logs** page and optionally delivered to a Discord channel.

### Log types

**Enum:** `src/DiscordBot.Domain/Enums/LogEventType.cs`

Examples: ticket opened/closed, warnings, settings updated, member joined, etc.

Extensions: `LogEventTypeExtensions.cs` for display names.

### Creation paths

| Source | How |
|--------|-----|
| Bot actions | `POST /api/bot/logs` via `BotLogWriter` |
| API/dashboard actions | `LogService.CreateLogAsync` |
| Discord events | Bot handlers → API |

### Discord delivery

**Service:** `src/DiscordBot.Bot/Services/DiscordLogDeliveryService.cs`

Posts embed to guild's configured log channel when `logs` module enabled and `GuildSettings.LogChannelId` set.

### Dashboard

**Route:** `/guilds/:id/logs` (moderation access)

Filters: type, date range, search, user ID. Max **200** entries per request.

Clear all: `DELETE /api/guilds/{id}/logs` with body `{ "confirmation": "DELETE" }` — owner or `ClearLogs` permission.

---

## Application logging (diagnostics)

### API

**File:** `src/DiscordBot.Api/Program.cs`

```csharp
builder.Logging.AddConsole();
builder.Logging.AddDebug();
```

**Middleware:** `RequestLoggingMiddleware` — logs HTTP method, path, status code, duration.

**Middleware:** `ExceptionHandlingMiddleware` — logs unhandled exceptions, returns JSON 500.

### Bot

Standard .NET logging to console. Discord.Net log severity configured in `DiscordBotHostedService`.

No centralized log aggregation (Seq, Datadog) configured.

---

## Log data model

| Field | Purpose |
|-------|---------|
| `GuildId` | Tenant |
| `Type` | LogEventType enum |
| `Message` | Human-readable summary |
| `ActorDiscordUserId` | Who performed action |
| `TargetDiscordUserId` | Optional target |
| `ChannelDiscordId` | Optional channel |
| `MetadataJson` | Flexible extra data |

Indexes: `(GuildId, CreatedAt)`, `(GuildId, Type, CreatedAt)`.

---

## What is NOT logged today

- Permission role changes (audit gap)
- Admin subscription changes (partial via LogEntry in some paths — verify per endpoint)
- Failed authorization attempts
- Bot API key failures (may appear in middleware logs only)
- Structured correlation IDs across bot → API requests

---

## Future recommendations

- Structured logging (Serilog) with JSON output
- Correlation ID header (`X-Correlation-Id`) bot → API
- Log retention policy per plan tier
- Audit log table separate from activity LogEntry
- Export logs to S3 for compliance

## Related docs

- `module-system.md`, `database.md`
- `monitoring.md`
