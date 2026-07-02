# Glossary

**Canonical vocabulary:** [/docs/blueprint/ubiquitous-language.md](../blueprint/ubiquitous-language.md) (UL-001)

This file is a **quick reference index**. Definitions, rules, examples, forbidden terms, and naming conventions live in the Ubiquitous Language specification. When this glossary and UL-001 disagree, **UL-001 wins**.

Project-specific terms used across code, docs, and dashboard.

---

## Platform core

| Term | Definition |
|------|------------|
| **Platform** | The Discord Bot SaaS product — API, bot worker, dashboard, and database as one system |
| **Tenant** | A single Discord server (guild) using the bot; isolated by `GuildId` |
| **Guild** | Domain entity representing a Discord server where the bot is installed |
| **Discord Guild ID** | Discord snowflake string identifying a server externally |
| **Multi-tenant** | One deployment serves many guilds with isolated settings |

## Discord concepts

| Term | Definition |
|------|------------|
| **Discord Role** | Native Discord role assigned to members; used as permission assignment primitive |
| **Discord Channel** | Synced channel metadata stored in `DiscordChannels` |
| **Discord Resource** | Cached Discord channel, role, or member metadata from synchronization |
| **Snowflake** | Discord's 64-bit ID format (stored as string) |
| **Slash Command** | Discord application command (e.g. `/warn`, `/ticket`) |
| **Interaction** | Discord gateway event for commands, buttons, selects, modals |
| **Gateway Intent** | Discord permission for bot to receive event types (e.g. GuildMembers) |

## Users and access

| Term | Definition |
|------|------------|
| **Dashboard User** | Person who logged in via Discord OAuth; stored in `Users` table |
| **Guild Owner** | Discord user who owns the server (`Guild.OwnerDiscordUserId`); full permissions |
| **Guild Member** | Any person in the Discord server; cached in `DiscordGuildMembers` |
| **Guild Staff Member** | Guild Member with dashboard/bot access via Permission Role mapping |
| **Platform Administrator** | Operator in `PlatformAdmins` table; manages all guilds and billing |
| **Permission Role** | `GuildPermissionRole` — maps a Discord Role to a permission set |
| **Permission** / **Capability** | Atomic authorized action (`GuildPermissions` flag today) |
| **Owner Permissions** | Full bitmask granted to owner and platform admin |

## Modules and plans

| Term | Definition |
|------|------------|
| **Module** | Platform feature unit (tickets, moderation, etc.) in `Modules` catalog |
| **Guild Module** | Per-guild enable/disable toggle in `GuildModules` |
| **Module Guard** | Bot service checking module enabled via API before running feature |
| **Subscription Plan** | Catalog tier (free, basic, pro, premium) defining allowed modules |
| **Subscription** | Active plan assignment for a guild (`GuildSubscription`) |
| **Allowed Modules** | JSON list on plan or `"*"` for all modules |
| **Upgrade Request** | Owner-initiated manual plan change (`PlanUpgradeRequest`) awaiting admin approval |

## Tickets

| Term | Definition |
|------|------------|
| **Ticket** | Support request in a dedicated Discord channel; tracked in `Tickets` |
| **Ticket Number** | Sequential identifier per guild |
| **Ticket Status** | `Open` or `Closed` |
| **Ticket Timeline** | Official term for ordered ticket history (planned unified; partial today) |
| **Timeline Event** | One entry on the Ticket Timeline |
| **Ticket Outbound Message** | Dashboard staff reply queued for bot delivery |
| **Ticket Assignment** | Staff ownership state (planned) |
| **Claim** | Action to take ticket ownership (planned) — not a noun for assignment |
| **Transcript** | Complete persisted ticket record (planned) |
| **Archive** | Discord channel embed summary on close — not full transcript |
| **Internal Note** | Staff-only ticket note (planned) |
| **Command Panel** | Discord message with buttons for ticket creation |

## Moderation

| Term | Definition |
|------|------------|
| **Moderation** | Module for warn, kick, clear, cases |
| **Warning** | Moderation warning record against a user |
| **Moderation Case** | General moderation action log entry (kick, clear, etc.) |

## Logging

| Term | Definition |
|------|------------|
| **Log Entry** | Platform event record in `LogEntries` |
| **Activity Log** | Dashboard/module name for viewing Log Entries |
| **Audit Log** | Compliance-oriented export/view (planned; not Logs module today) |
| **Domain Event** | Significant business occurrence, often stored as Log Entry |

## Configuration

| Term | Definition |
|------|------------|
| **Guild Settings** | 1:1 configuration entity (`GuildSettings`) |
| **Settings** | Dashboard settings page |
| **Server Profile** | Bot-managed embed metadata for `/server` |
| **Onboarding Checklist** | Dashboard guided setup steps |
| **Auto Reply** | Keyword-triggered bot response rule |
| **Trigger** | Auto Reply match condition (Workflow Trigger planned separately) |
| **Reaction Role** | Button panel for self-assigning Discord roles |

## Automation (planned)

| Term | Definition |
|------|------------|
| **Automation** | Rules reducing operator work (Auto Reply today; Workflow planned) |
| **Workflow** | Trigger → Actions automation engine (planned) |
| **Action** | Automated step when workflow fires (planned) |

## Analytics (planned)

| Term | Definition |
|------|------------|
| **Analytics** | Aggregated operational metrics module (partial counts today) |
| **Report** | Exported summary artifact (planned) |

## Infrastructure

| Term | Definition |
|------|------------|
| **API** | HTTP service owning persistence and business rules |
| **Bot** | Discord.Net worker; API client only for persistence |
| **Worker** | Hosted background poll loop in bot process |
| **Background Job** | Deferred work (cleanup, delivery, sync) |
| **Synchronization** | Discord → platform resource copy |
| **Integration** | External system connection via API (planned) |
| **Notification** | Dashboard user alert (planned; UI stub today) |

## Technical

| Term | Definition |
|------|------------|
| **Bot API Key** | Shared secret (`X-Bot-Api-Key`) authenticating bot → API calls |
| **JWT** | JSON Web Token for dashboard user sessions |
| **Auth Code** | One-time code exchanged for JWT after OAuth (not stored in URL) |
| **DTO** | Data Transfer Object — API request/response shape |
| **Resolver** | `GuildPermissionResolver` — computes effective permissions for a user |
| **Mapper** | `GuildPermissionMapper` — converts flags to dashboard/bot DTOs |
| **Seeder** | Hosted service inserting default catalog data on startup |
| **Migration** | EF Core database schema change script |

## DDD (conceptual)

| Term | Definition |
|------|------------|
| **Product Domain** | Major business area (Tickets, Moderation, etc.) |
| **Entity** | Persisted object with identity (`BaseEntity`) |
| **Value Object** | Attribute-defined object without identity |
| **Aggregate** | Consistency boundary cluster (Guild, Ticket, etc.) |
| **Policy** | Business rule enforcing allow/deny |

## Documentation

| Term | Definition |
|------|------------|
| **Product Blueprint** | `/docs/blueprint/product-blueprint.md` — PB-001 |
| **Ubiquitous Language** | `/docs/blueprint/ubiquitous-language.md` — UL-001 |
| **Handbook** | `/docs/architecture/` — canonical architecture docs |
| **Step guide** | `/docs/step-NN-*.md` — historical implementation log |
| **ADR** | Architecture Decision Record in `/docs/adr/` |
| **Progress report** | Task completion doc in `/docs/progress/` |

## Removed terms (historical)

| Term | Status |
|------|--------|
| **GuildStaff** | Removed — use Permission Role |
| **ModerationPermissionRole** | Removed — merged into Permission Role |
| **GuildStaffRole** | Removed — enum for legacy staff (Moderator/Manager) |
| **Feature** (product spec) | Deprecated — use Module or Capability |
| **Feature Flag** (guild features) | Not used — use Module enablement |
| **Conversation** (tickets) | Forbidden — use Ticket Timeline |

## Related docs

- [Ubiquitous Language](../blueprint/ubiquitous-language.md) — **canonical definitions**
- [Product Blueprint](../blueprint/product-blueprint.md)
- `product-overview.md`, `permission-system.md`, `module-system.md`
