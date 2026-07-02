# Discord Bot Platform — Ubiquitous Language

**Document ID:** UL-001  
**Status:** Official — canonical business vocabulary  
**Owner:** Domain Architecture  
**Last updated:** 2026-07-02  
**Authority:** Equal to [Product Blueprint](./product-blueprint.md) for **naming and meaning**; blueprint wins on product scope, this document wins on **term definitions**

---

## Purpose

This document defines the **official language of the Discord Bot Platform**. Every ADR, domain blueprint, API name, database table, dashboard label, progress report, and code comment that describes business behavior **must use these terms with these meanings**.

A new developer should read this before writing product-facing code or documentation.

This is **not** a quick glossary. `/docs/architecture/glossary.md` remains a short index; **this document is the specification**.

---

## How to read term entries

Each term is marked:

| Marker | Meaning |
|--------|---------|
| **Live** | Implemented in code or UI today |
| **Planned** | Official vocabulary for roadmap work; not yet in code |
| **Conceptual** | DDD / architecture vocabulary applied to this platform |

When Live code uses a legacy name, the **official term** in this document still governs new work and docs.

---

## Domain language principles

1. **Business terminology wins over technical shortcuts.** Say **Guild**, not "tenant row," in product discussions.
2. **One concept = one official name.** Do not invent synonyms in docs, UI, or API responses.
3. **One official name = one meaning.** If a word already means something here, extend it — do not reuse it for something else.
4. **Qualify ambiguous words.** Never write **Role** alone. Never write **Log** alone when you mean **Log Entry**.
5. **Separate state from action.** **Ticket Assignment** is state; **Claim** is an action that may create assignment.
6. **Separate product packaging from authorization.** **Module** is what the guild buys and enables; **Capability** (Permission) is what a user may do.
7. **Discord vs platform.** Prefix with **Discord** when referring to Discord-native objects (Discord Role, Discord Channel). Prefix with **Guild** or **Platform** for our entities.
8. **Planned terms are still official.** Use **Ticket Timeline** in ticket specs even before the table exists — do not substitute **Conversation**.
9. **Legacy names are deprecated, not alternate.** `GuildStaff`, `ModerationPermissionRole`, and bare **Staff** as a data model name are historical only.
10. **Changes to this document require explicit revision.** Add a row to Revision History; significant renames require ADR.

---

## Naming rules

### Prefer

| Use | Instead of | Why |
|-----|------------|-----|
| **Capability** / **Permission** | Feature, Function | "Feature" is overloaded (module, flag, UI page). A Permission is an atomic authorized action. |
| **Module** | Feature pack, Plugin (today) | Module is the catalog unit with plan gating (`ModuleKeys`). |
| **Guild** | Server, Tenant (in product copy) | **Guild** matches domain entity and Discord API. "Server" is acceptable in Discord-facing embeds only. |
| **Dashboard** | Admin panel, Web UI | Distinguishes guild-scoped UI from **Platform Administration**. |
| **Log Entry** | Log, Event log row | **Log** alone is ambiguous (application logging, Discord audit log, module name). |
| **Activity Log** | Audit log (module name) | **Logs** module = activity log product surface. **Audit Log** = compliance-oriented view/export (planned). |
| **Permission Role** | Staff role, Staff record | **Staff page** is UI; persisted model is **GuildPermissionRole**. |
| **Upgrade Request** | Plan change, Billing ticket | Manual workflow entity is `PlanUpgradeRequest`. |
| **Synchronization** | Sync job, Refresh | Official noun for Discord → platform resource copy. |
| **Ticket Timeline** | Conversation, Chat history | Conversation implies informal chat; Timeline is ordered business record. |
| **Ticket Assignment** | Claim (as noun) | Claim is verb; Assignment is persisted state. |
| **Guild Settings** | Configuration (user-facing) | **Settings** is the dashboard page; **GuildSettings** is the entity. |
| **Server Profile** | Guild profile (UI) | Official product name for bot-managed embed metadata (`/server`). |

### Avoid in specifications

| Avoid | Use instead |
|-------|-------------|
| Feature | Module or Capability |
| Staff (as entity name) | Permission Role / Guild Staff Member (persona) |
| Role | Discord Role / Permission Role / Platform Administrator |
| Server (in API/entity names) | Guild |
| Audit log (for Logs module today) | Activity Log / Log Entry |
| Transcript (for 8-message embed) | Archive preview — until full Transcript exists |
| Customer | Guild Owner (unless platform billing context) |
| User (ambiguous) | Dashboard User / Guild Member / Guild Owner |

---

## Consistency rules

| Official term | Not equivalent | Rule |
|---------------|----------------|------|
| **Ticket Timeline** | Discord message list | Timeline includes dashboard replies, system events, and (planned) internal notes in business order. |
| **Archive** | Transcript | Archive = Discord channel artifact on close. Transcript = complete persisted record. |
| **Claim** | Ticket Assignment | Claim is an action; may set Assignment. |
| **Assign** | Ticket Assignment | Assign is an action performed by someone with authority; result is Assignment state. |
| **Module** | Capability | Module must be enabled for guild before Capability checks matter. |
| **Guild Member** | Guild Staff Member | Every staff member is a guild member; not every guild member is staff. |
| **Warning** | Moderation Case | Warning is a specific discipline type; Case is broader record (kick, clear, etc.). |
| **Synchronization** | Log Entry | Sync updates resource cache; it may emit a Log Entry when complete. |
| **Upgrade Request** | Subscription | Subscription is active entitlement; Upgrade Request is pending approval workflow. |
| **Command Panel** | Reaction Role | Both use buttons; Command Panel is general entry UX; Reaction Role assigns roles. |
| **Auto Reply Trigger** | Workflow Trigger (planned) | Auto Reply Trigger is keyword rule today; Workflow Trigger is future automation engine. |

---

## Forbidden terminology

These usages are **not allowed** in official docs, ADRs, or new UI/API names:

| Forbidden | Required qualification |
|-----------|------------------------|
| **Role** (alone) | **Discord Role**, **Permission Role**, or **Platform Administrator** |
| **Staff** (as database entity) | **Permission Role** (`GuildPermissionRole`) |
| **Feature** (product spec) | **Module** or **Capability** |
| **Log** (alone) | **Log Entry**, **Activity Log**, **Application Log**, or **Discord Audit Log** (external) |
| **Server** (entity/API) | **Guild** — exception: `/servers` route and Discord copy |
| **Conversation** (ticket domain) | **Ticket Timeline** |
| **Customer** (guild ops docs) | **Guild Owner** or **Guild** |
| **Moderation Permission Role** | Removed — use **Permission Role** with moderation Capabilities |
| **GuildStaff** | Removed — use **Permission Role** |
| **AccessTickets** (new docs) | **ViewTickets** (legacy API alias still accepted in code) |
| **Feature flag** (product) | **Module** enablement or **Capability** — unless Platform Administration toggle (planned) |

---

# Core platform

---

## Platform

**Status:** Live · **Conceptual anchor**

### Business definition

The **Platform** is the complete Discord Bot SaaS product operated for many communities: bot worker, REST API, web Dashboard, PostgreSQL database, and Platform Administration tools as one system.

### Technical definition

The deployed system comprising `DiscordBot.Api`, `DiscordBot.Bot`, `DiscordBot.Dashboard`, and `DiscordBot.Infrastructure` persistence — typically Railway (API, Bot, DB) + Vercel (Dashboard).

### Rules

- One Platform deployment serves many Guilds (**Multi-Tenant**).
- External actors: Guild Owners, Guild Staff Members, Guild Members (Discord only), Platform Administrators.

### Related terms

Guild, Tenant, Multi-Tenant, Dashboard, Bot, API, Platform Administrator

### Example

"We deploy the Platform to Railway" — not "we deploy the bot repo" when referring to the whole product.

---

## Tenant

**Status:** Live (conceptual)

### Business definition

A **Tenant** is one paying or registered customer boundary: a single Discord server using the Platform with isolated data.

### Technical definition

Tenant isolation is implemented via **`GuildId`** (internal UUID) and **`DiscordGuildId`** (Discord snowflake). All guild-scoped tables carry `GuildId`.

### Rules

- One Tenant = one Guild in v1.
- Cross-tenant queries are forbidden.
- Platform Administration operates across tenants with explicit authorization.

### Related terms

Guild, Multi-Tenant, Guild Owner

### Example

Guild A's tickets must never appear in Guild B's Dashboard — tenant isolation.

---

## Multi-Tenant

**Status:** Live

### Business definition

**Multi-Tenant** means one Platform installation serves many independent Guilds, each believing it is the only customer of its data slice.

### Technical definition

Shared database, shared bot application ID, row-level isolation by `GuildId`. Not separate databases per guild in v1.

### Rules

- Every guild-scoped service method must filter by `GuildId`.
- Subscription, settings, permissions, and modules are per-tenant.

### Related terms

Tenant, Guild, Platform

### Example

The `Tickets` table holds tickets for all guilds; queries always include `WHERE GuildId = @guildId`.

---

## Guild

**Status:** Live

### Business definition

A **Guild** is a Discord server where the Platform bot is installed and registered. It is the unit of configuration, billing, and isolation.

### Technical definition

Entity: `Guild` (`Guilds` table). Key fields: `DiscordGuildId`, `Name`, `OwnerDiscordUserId`, `IsActive`, `Settings`, `Subscription`.

### Rules

- A Guild exists in the Platform only after registration (`/setup` or bot join API).
- `IsActive = false` when bot leaves; data retained but operations stop.
- One Guild has at most one active **Guild Subscription** and one **Guild Settings** row.

### Related terms

Tenant, Guild Owner, Guild Member, Discord Resource, Synchronization

### Example

"Acme Community" Discord server → one `Guild` row with `DiscordGuildId` snowflake.

---

## Guild Owner

**Status:** Live

### Business definition

The **Guild Owner** is the Discord user who owns the server. They have full authority over guild configuration, billing requests, and staff delegation.

### Technical definition

Stored as `Guild.OwnerDiscordUserId`. Receives `GuildPermissionDefaults.OwnerPermissions` in `GuildPermissionResolver` without requiring a **Permission Role** mapping.

### Rules

- Exactly one Guild Owner per Guild (Discord's model).
- Owner bypass is centralized in the resolver — do not duplicate in features.
- Dashboard routes marked `guildAccess: 'owner'` are owner-only (settings, modules, staff).

### Related terms

Guild Staff Member, Permission, Platform Administrator

### Example

Owner submits an **Upgrade Request**; staff cannot approve it — only **Platform Administrator** can.

---

## Guild Member

**Status:** Live

### Business definition

A **Guild Member** is any person in the Discord server, including the owner, moderators, support agents, and regular members.

### Technical definition

Entity: `DiscordGuildMember` — cached Discord user with `DiscordUserId`, display names, and `DiscordRoleIdsJson` from **Synchronization**.

### Rules

- Membership is Discord-native; Platform does not "invite" dashboard users separately.
- Permission resolution uses synced role IDs, or live role IDs from bot evaluate endpoints.
- Guild Members are not automatically Guild Staff Members.

### Related terms

Discord Role, Guild Staff Member, Synchronization

### Example

A member opens a **Ticket** — they are the **Ticket** owner (`OwnerDiscordUserId`), not necessarily staff.

---

## Guild Staff Member

**Status:** Live (persona) · **Planned** (roster entity Phase 3)

### Business definition

A **Guild Staff Member** is a Guild Member who operates the Platform on behalf of the owner: support, moderation, or configuration (within granted Capabilities).

### Technical definition

Today: any Discord user whose **Discord Roles** map to a **Permission Role** with non-empty **Permissions**, or who passes owner/admin bypass. Future: optional `GuildStaffMember` roster profile (Phase 3) — **not** an auth source.

### Rules

- Authorization comes from **Permission Role** mapping, not from a separate staff user table.
- Colloquial "staff" in UI (Staff page) configures **Permission Roles**.
- Support Team and moderators are subsets of Guild Staff Members.

### Related terms

Permission Role, Capability, Support Team, Dashboard User

### Example

A user with Support **Permission Role** can access **Tickets** in Dashboard if they hold **ViewTickets**.

---

## Dashboard

**Status:** Live

### Business definition

The **Dashboard** is the web application where Guild Owners and Guild Staff Members configure the Platform and perform operational work (tickets, moderation views, logs).

### Technical definition

Angular SPA: `DiscordBot.Dashboard`. Authenticates via Discord OAuth → JWT. All data via REST **API** — never direct database access.

### Rules

- Guild-scoped routes: `/guilds/:id/...`
- Platform Administration routes: `/admin/...`
- i18n required: English and Arabic for user-facing strings.

### Related terms

Dashboard User, API, Guild Owner, Platform Administrator

### Example

Owner enables **Module** on `/guilds/:id/modules`; agent closes **Ticket** on `/guilds/:id/tickets`.

---

## Dashboard User

**Status:** Live (supporting term)

### Business definition

A **Dashboard User** is a person who logged into the Dashboard via Discord OAuth.

### Technical definition

Entity: `User` (`Users` table) with `DiscordUserId`. JWT claim identifies them on API calls.

### Rules

- Same human may be Dashboard User for multiple Guilds.
- Being a Dashboard User does not grant access until **Permission Role** or owner/admin bypass applies per Guild.

### Related terms

Guild Member, Guild Owner, Platform Administrator

### Example

User logs in once, sees all Guilds they own or have **AccessDashboard** for.

---

## Bot

**Status:** Live

### Business definition

The **Bot** is the Discord application that runs slash commands, buttons, modals, and background delivery in Discord servers on behalf of the Platform.

### Technical definition

Project: `DiscordBot.Bot` (Discord.Net). Stateless regarding persistence — all writes via **API** with **Bot API Key**.

### Rules

- Bot never reads PostgreSQL directly.
- Bot checks **Module** enablement (`ModuleGuard`) before feature handlers.
- Bot checks **Capabilities** via API evaluate endpoints for commands and ticket close.

### Related terms

API, Worker, Module, Discord Resource

### Example

Bot receives `/ticket open`, creates Discord channel, POSTs **Ticket** to API.

---

## API

**Status:** Live

### Business definition

The **API** is the HTTP service that owns all business rules and persistence for the Platform. Dashboard and Bot are clients.

### Technical definition

Project: `DiscordBot.Api`. Routes: `/api/guilds/*` (JWT), `/api/bot/*` (API key), `/api/admin/*`, `/api/auth/*`.

### Rules

- Thin controllers; business logic in Infrastructure services.
- All guild operations validate tenant access before query.

### Related terms

Bot, Dashboard, Worker, Integration

### Example

`PATCH /api/guilds/{id}/tickets/{ticketId}/close` closes **Ticket** and queues channel cleanup.

---

## Platform Administrator

**Status:** Live

### Business definition

A **Platform Administrator** operates the SaaS business: plans, upgrade approvals, fleet visibility. Not a guild role.

### Technical definition

User listed in `PlatformAdmins` table + JWT. Access to `/admin/*` routes and admin API controllers. Receives owner-equivalent **Permissions** when resolving guild access.

### Rules

- Platform Administrator ≠ Guild Owner unless same person on a specific guild.
- Do not call this "Platform Role" in UI — use **Platform Administrator**.

### Related terms

Platform, Upgrade Request, Subscription Plan

### Example

Admin approves **Upgrade Request** at `/admin/upgrade-requests`.

---

# Modules, capabilities, and configuration

---

## Product Domain

**Status:** Conceptual

### Business definition

A **Product Domain** is a major business capability area of the Platform (Tickets, Moderation, Subscriptions, Authorization, etc.) documented in the Product Blueprint.

### Technical definition

Organizational boundary for specs (`/docs/tickets/`, handbook chapters). May span entities, services, API controllers, dashboard routes, and bot handlers.

### Rules

- Domains are not necessarily 1:1 with **Modules** (e.g. Authorization spans all modules).
- New domains require blueprint update; new **Modules** require catalog seeder + `ModuleKeys`.

### Related terms

Module, Aggregate, Product Blueprint

### Example

**Tickets** product domain includes Ticket entity, TicketService, ticket bot handlers, tickets dashboard page.

---

## Module

**Status:** Live

### Business definition

A **Module** is a sellable, enable/disable unit of product functionality (Welcome, Tickets, Moderation, Logs, Auto Role, Reaction Roles).

### Technical definition

Catalog: `Module` entity + `ModuleKeys` constants. Per guild: `GuildModule.IsEnabled`. Bot: `ModuleGuard`. Plans: **Allowed Modules** JSON on **Subscription Plan**.

### Rules

- **Module** must be allowed by **Subscription Plan** AND enabled by owner before use.
- Module enablement is checked before **Capability** (Permission) checks.
- Six modules exist today; future modules (Analytics, Automation) follow same pattern.

### Related terms

Capability, Subscription Plan, Guild Module, Product Domain

### Example

Tickets **Module** on Pro plan: owner toggles on → members may open **Tickets**.

---

## Guild Module

**Status:** Live (supporting term)

### Business definition

A **Guild Module** is the per-guild on/off switch for a catalog **Module**.

### Technical definition

Entity: `GuildModule` linking `GuildId` + `ModuleId` + `IsEnabled`.

### Rules

- Disabling module does not delete historical data (e.g. closed tickets remain).

### Related terms

Module, Subscription Plan

### Example

Owner disables Moderation module — `/warn` returns module-disabled message from bot.

---

## Capability

**Status:** Live (conceptual) · maps to **Permission**

### Business definition

A **Capability** is the smallest unit of authorized action a Guild Staff Member may perform (view tickets, reply to tickets, use warn command, clear logs).

### Technical definition

Implemented as `GuildPermissions` enum flags (Phase 2: string keys in permission catalog). Resolved via **Permission Role** mappings and OR-merge.

### Rules

- Prefer **Capability** in product/ADR language; **Permission** acceptable and used in code enum name.
- Do not say "feature access" — say Capability or Module access.

### Related terms

Permission, Permission Role, Module

### Example

**ReplyToTickets** Capability allows queueing **Ticket Outbound Message** — not the same as enabling Tickets Module alone.

---

## Permission

**Status:** Live

### Business definition

A **Permission** is the official code name for a **Capability** — an atomic allow/deny flag in the authorization model.

### Technical definition

`GuildPermissions` flags enum (20 flags today, ~32 bit ceiling). Stored as bitmask on **Permission Role**. Legacy aliases accepted on API input (`Warn` → `UseWarn`).

### Rules

- Effective permissions = OR of all matching **Permission Roles** for user's **Discord Roles**.
- Guild Owner and Platform Administrator receive full bitmask via defaults.

### Related terms

Capability, Permission Role, Policy

### Example

User with **CloseTickets** Permission may close tickets in Dashboard and (with evaluate) in Discord.

---

## Permission Role

**Status:** Live

### Business definition

A **Permission Role** maps one **Discord Role** to a set of **Permissions** for a Guild. Configured on the Staff page (and Moderation Settings adapter for bot-focused toggles).

### Technical definition

Entity: `GuildPermissionRole` (`GuildPermissionRoles` table). Fields: `Name` (admin label), `DiscordRoleId`, `Permissions` bitmask.

### Rules

- Unique per `(GuildId, DiscordRoleId)`.
- Name is for dashboard display only — authorization uses Discord Role membership.
- One Discord Role may have at most one Permission Role row per guild.

### Related terms

Discord Role, Guild Staff Member, Capability

### Example

Permission Role "Support Team" maps `@Support` Discord Role → ViewTickets | ReplyToTickets | CloseTickets.

---

## Discord Role

**Status:** Live

### Business definition

A **Discord Role** is a native Discord permission group assigned to members in a server. Used as the assignment primitive for **Permission Roles**.

### Technical definition

Synced to `DiscordRole` entity (`DiscordRoles` table). Referenced by snowflake string IDs in settings and Permission Roles.

### Rules

- Platform does not create Discord Roles automatically except via bot actions (e.g. reaction role assignment).
- Bot native actions still require Discord hierarchy (bot role position).

### Related terms

Permission Role, Guild Member, Reaction Role

### Example

`DiscordRoleId` on Permission Role must exist in guild — synced via **Synchronization**.

---

## Policy

**Status:** Conceptual · **Live** (implicit in services)

### Business definition

A **Policy** is a business rule that decides whether an operation is allowed, often combining Module, Subscription, Permission, and Discord native checks.

### Technical definition

Implemented as service-layer checks: `ModuleGuard`, `GuildPermissionResolver`, `SubscriptionService`, controller authorization — not a separate Policy entity.

### Rules

- Policies must fail closed (deny if uncertain).
- Document new policies in domain specs when non-obvious.

### Related terms

Permission, Module, Guild Owner

### Example

Ticket creation policy: Tickets Module enabled + user has no open Ticket + valid category in **Guild Settings**.

---

## Configuration

**Status:** Conceptual

### Business definition

**Configuration** is the collective tunable behavior of a Guild (channels, messages, toggles, templates) as opposed to transactional records (tickets, warnings).

### Technical definition

Primarily **`GuildSettings`** entity plus **Guild Module** toggles, **Permission Roles**, **Auto Reply** rules, **Reaction Role** panels, command panel JSON.

### Rules

- Owner-level Dashboard pages for most configuration.
- **ManageTickets** / **ManageSettings** Capabilities (future enforcement) gate configuration changes.

### Related terms

Guild Settings, Settings, Module

### Example

Changing **Ticket Archive Channel** is configuration; closing a ticket is an operation.

---

## Guild Settings

**Status:** Live

### Business definition

**Guild Settings** are per-guild bot behavior defaults: welcome channel, log channel, ticket category, message templates, command panel definition.

### Technical definition

Entity: `GuildSettings` — 1:1 with `Guild`. Updated via `PUT /api/guilds/{id}/settings`.

### Rules

- One row per guild; created on first settings update if missing.
- Distinct from **Server Profile** (marketing/embed metadata).

### Related terms

Configuration, Settings, Command Panel

### Example

`TicketStaffReplyPrefix` template in Guild Settings prefixes **Ticket Outbound Message** in Discord.

---

## Settings

**Status:** Live (UI term)

### Business definition

**Settings** is the Dashboard page where the Guild Owner configures **Guild Settings** across tabs (Welcome, Tickets, Logs, Command Panel, Auto Replies).

### Technical definition

Route: `/guilds/:id/settings`. Component: `SettingsComponent`. Guard: owner.

### Rules

- "Settings" refers to UI; "Guild Settings" refers to persisted entity — both valid with context.

### Related terms

Guild Settings, Configuration

### Example

Owner sets **Ticket Archive Channel** in Settings → Tickets tab.

---

## Server Profile

**Status:** Live

### Business definition

**Server Profile** is bot-managed marketing and identity text shown in the `/server` embed — not a Discord server rename.

### Technical definition

Fields on `Guild`: `DisplayName`, `Description`, `CommunityType`, `SupportMessage`, `RulesUrl`, `WebsiteUrl`. Dashboard: `/guilds/:id/profile`.

### Rules

- Do not call this "Guild Profile" in user-facing copy — **Server Profile** is official.
- Editing profile does not change Discord server name icon.

### Related terms

Guild, Bot, Configuration

### Example

`/server` embed shows Server Profile description and rules link.

---

# Subscriptions and billing

---

## Subscription

**Status:** Live

### Business definition

A **Subscription** is the active commercial entitlement of a Guild to a **Subscription Plan**, including module allowance and expiry.

### Technical definition

Entity: `GuildSubscription` — links `GuildId`, plan, status, expiry. Seeded default: Free plan.

### Rules

- One active subscription row per guild.
- Expiry may downgrade to Free (operational behavior).
- Owners cannot directly PATCH subscription — use **Upgrade Request**.

### Related terms

Subscription Plan, Upgrade Request, Module

### Example

Pro **Subscription** allows enabling Tickets and Moderation **Modules**.

---

## Subscription Plan

**Status:** Live

### Business definition

A **Subscription Plan** is a catalog tier (Free, Basic, Pro, Premium) defining price and **Allowed Modules**.

### Technical definition

Entity: `SubscriptionPlan` with `Key`, `MonthlyPrice`, `AllowedModulesJson` (`["welcome","logs"]` or `"*"`).

### Rules

- Plans are platform-wide catalog; Platform Administrator can CRUD via admin.
- Module gating uses plan allowance before guild toggle.

### Related terms

Subscription, Upgrade Request, Module

### Example

Premium plan `"*"` includes **Auto Role** module; Pro does not (default seed).

---

## Upgrade Request

**Status:** Live

### Business definition

An **Upgrade Request** is a Guild Owner's request to change **Subscription Plan** for a duration, pending **Platform Administrator** approval.

### Technical definition

Entity: `PlanUpgradeRequest` with status `Pending | Approved | Rejected`. API: owner creates; admin reviews.

### Rules

- Manual billing workflow until Stripe **Integration** (planned).
- Approved request updates **Guild Subscription** with new expiry.

### Related terms

Subscription, Platform Administrator, Subscription Plan

### Example

Owner requests Pro for 12 months → admin approves → subscription updated.

---

## Feature Flag

**Status:** Planned · **Not used in product language today**

### Business definition

Do **not** use "Feature Flag" for guild module toggles or permission bits. Reserve for future **Platform Administrator** runtime toggles (rollout, kill switch) if introduced.

### Technical definition

Not implemented. Today: **Guild Module** `IsEnabled` + **Subscription Plan** + **Permissions**.

### Rules

- In specs and UI copy, say **Module enabled** or **Capability granted**, not "feature flag."
- If platform-level flags are added, define them in ADR and revise this entry.

### Related terms

Module, Capability, Platform Administrator

### Example

Incorrect: "Tickets feature flag." Correct: "Tickets **Module** enabled for guild."

---

# Tickets domain

Reference: `/docs/tickets/`

---

## Ticket

**Status:** Live

### Business definition

A **Ticket** is a structured support request owned by a Guild Member, conducted primarily in a private Discord channel and tracked by the Platform.

### Technical definition

Entity: `Ticket` — `GuildId`, `TicketNumber`, `OwnerDiscordUserId`, `ChannelDiscordId`, **Ticket Status**, `ClosedAt`, `ChannelCleanupRequested`.

### Rules

- Belongs to exactly one **Guild**.
- Exactly one **Owner** ( opener ) per ticket record today.
- At most one **Open** ticket per owner per guild (business rule).
- Sequential **Ticket Number** per guild.

### Related terms

Ticket Status, Ticket Timeline, Archive, Support Team

### Example

Member opens ticket → `#ticket-42` channel + `Ticket` row with `TicketNumber = 42`.

---

## Ticket Status

**Status:** Live

### Business definition

**Ticket Status** describes where the ticket is in its lifecycle: accepting support (Open) or finished (Closed).

### Technical definition

Enum: `TicketStatus` — `Open`, `Closed` only today. Stored as string in DB.

### Rules

- Closed tickets have `ClosedAt` set.
- Reopen (planned) may extend enum or transition rules — document in ADR when implemented.

### Related terms

Ticket, Ticket Assignment

### Example

Dashboard badge shows Open until staff or owner closes ticket.

---

## Ticket Priority

**Status:** Planned

### Business definition

**Ticket Priority** indicates urgency for queue ordering and SLA rules (e.g. Low, Normal, High, Urgent).

### Technical definition

Not in schema. Planned field on `Ticket` (Phase 3 per ticket roadmap).

### Rules

- Priority is state on Ticket, not a separate entity in v1 design.
- Default priority assigned at creation when implemented.

### Related terms

Ticket, Support Team, Analytics

### Example

High priority ticket appears at top of support queue (planned).

---

## Ticket Participant

**Status:** Planned (conceptual) · **Partial live behavior**

### Business definition

A **Ticket Participant** is any person who contributes to a ticket: owner, support agent, moderator, or bot posting on behalf of Platform.

### Technical definition

Today: implicit — `OwnerDiscordUserId` on Ticket; staff visible only via Discord channel membership and **Ticket Outbound Message** sender. Future: explicit participant list when adding/removing users (roadmap).

### Rules

- Owner is always a participant.
- Participants ≠ Guild Staff Members necessarily (owner is member, not staff).

### Related terms

Ticket, Guild Staff Member, Ticket Timeline

### Example

Owner + two support agents chatting in ticket channel are participants; only owner is Owner.

---

## Ticket Timeline

**Status:** Planned (official term) · **Partial live**

### Business definition

The **Ticket Timeline** is the ordered, authoritative record of everything that happened on a ticket for business and compliance purposes.

### Technical definition

**Planned:** `TicketMessage` / timeline entities (CM-002+). **Today:** Discord channel messages (ephemeral) + **Ticket Outbound Message** queue rows + **Log Entry** events (`TicketOpened`, `TicketClosed`, `TicketArchived`) — not unified.

### Rules

- Use **Ticket Timeline** in all ticket specs — never **Conversation** as official noun.
- Timeline includes member messages, staff replies (Discord and Dashboard), and system events when implemented.
- Timeline persists after Discord channel deletion (planned requirement for v1).

### Related terms

Timeline Event, Transcript, Ticket Outbound Message, Log Entry

### Example

Support agent opens ticket detail page → reads Timeline (planned), not Discord scroll alone.

---

## Timeline Event

**Status:** Planned · **Partial live**

### Business definition

A **Timeline Event** is one entry on the **Ticket Timeline**: message, system notice, status change, assignment change, or internal note marker.

### Technical definition

Planned typed records with timestamp and author. Today: approximate equivalents are Discord messages (not stored), **Ticket Outbound Message**, and ticket-related **Log Entry** types.

### Rules

- Status changes (close, reopen) should appear as Timeline Events when implemented.
- Distinguish from **Log Entry** — Timeline is ticket-scoped narrative; Log Entry is platform activity audit.

### Related terms

Ticket Timeline, Internal Note, Log Entry

### Example

"Staff replied from dashboard" → Timeline Event with delivery status (planned).

---

## Ticket Outbound Message

**Status:** Live

### Business definition

A **Ticket Outbound Message** is a Dashboard-composed staff reply queued for the Bot to deliver into the ticket Discord channel.

### Technical definition

Entity: `TicketOutboundMessage` — `Content`, `SenderDiscordUserId`, `IsDelivered`, `DeliveredAt`. Processed by **Background Job** poll.

### Rules

- Requires open Ticket and **ReplyToTickets** Capability (enforcement planned granularly).
- Max 2000 characters.
- On delivery, becomes part of Timeline (planned linkage); today only queue record.

### Related terms

Ticket Timeline, Bot, Worker, Support Team

### Example

Agent types reply in Dashboard → Outbound Message queued → Bot posts in `#ticket-42` within ~30s.

---

## Ticket Assignment

**Status:** Planned

### Business definition

**Ticket Assignment** is the persisted state of which Guild Staff Member (or role queue) owns responsibility for handling a ticket.

### Technical definition

Planned: `AssignedToDiscordUserId`, timestamps on `Ticket` (CM-010). Not in schema today.

### Rules

- Assignment is state, not an action verb.
- A ticket may be unassigned, assigned to one primary agent, or assigned to a queue (future).
- Changing assignment writes Timeline Event + Log Entry when implemented.

### Related terms

Claim, Support Team, Ticket

### Example

"Ticket #42 assignment: @SupportLead" — state displayed on ticket detail (planned).

---

## Claim

**Status:** Planned (action)

### Business definition

**Claim** is the action by which a Guild Staff Member takes ownership of an unassigned ticket, setting **Ticket Assignment** to themselves.

### Technical definition

Planned API/bot command: `PATCH .../claim` (CM-010). Not implemented.

### Rules

- **Claim** is verb; do not use "claim" as noun for assignment state.
- May require **ViewTickets** + module-specific Capability (TBD in ticket spec).

### Related terms

Ticket Assignment, Support Team

### Example

Agent clicks Claim → Ticket Assignment set to agent's Discord user id.

---

## Internal Note

**Status:** Planned

### Business definition

An **Internal Note** is staff-only text on a ticket visible in Dashboard but never sent to Discord or the ticket owner.

### Technical definition

Planned: `TicketNote` entity (CM-011). Not implemented.

### Rules

- Never appears on public Timeline visible to owner (unless product decision changes — default: staff-only).
- Requires appropriate support Capability (planned).

### Related terms

Ticket Timeline, Support Team, Guild Staff Member

### Example

"Escalated to billing team — waiting on finance" as Internal Note.

---

## Transcript

**Status:** Planned (full) · **Partial misleading preview today**

### Business definition

A **Transcript** is the complete, durable text record of a **Ticket Timeline**, suitable for review, export, and compliance.

### Technical definition

Planned: persisted messages + export API (CM-002+). Today: **Archive** embed shows up to 8 messages — **not** a Transcript.

### Rules

- Do not call archive preview a Transcript in product copy until persistence ships.
- Transcript survives Discord channel deletion.

### Related terms

Archive, Ticket Timeline, Analytics

### Example

Owner downloads HTML Transcript of closed ticket for records (planned Phase 3).

---

## Archive

**Status:** Live (partial)

### Business definition

An **Archive** is a summary artifact posted to the configured Discord archive channel when a ticket closes — notification for moderators, not the system of record.

### Technical definition

`TicketArchiveService` posts embed to `GuildSettings.TicketArchiveChannelId` with preview text; writes **Log Entry** `TicketArchived`.

### Rules

- Archive ≠ Transcript until Timeline persistence exists.
- Archive failure must not block ticket close.

### Related terms

Transcript, Ticket, Log Entry

### Example

On close, embed in `#ticket-archive` with ticket number, owner, closer, message preview.

---

## Support Team

**Status:** Live (organizational persona) · not a separate entity

### Business definition

The **Support Team** is the group of Guild Staff Members responsible for handling **Tickets** — typically mapped via Support **Permission Role**.

### Technical definition

Not a database table. Realized as Discord Role(s) + **Permission Role** with ticket Capabilities (`ViewTickets`, `ReplyToTickets`, `CloseTickets`, `ManageTickets`).

### Rules

- Do not create "SupportTeam" entity without ADR (ticket teams/queues planned Phase 3).
- Support Team members should receive Discord channel access when ticket v1 staff overwrite ships (CM-008).

### Related terms

Guild Staff Member, Ticket, Permission Role

### Example

Guild configures Permission Role "Support" for `@Support` role with ticket Capabilities.

---

# Moderation domain

---

## Moderation

**Status:** Live (Module)

### Business definition

**Moderation** is the product domain for enforcing community rules through warnings, kicks, message clears, and case history.

### Technical definition

Module key: `moderation`. Commands: `/warn`, `/kick`, `/clear`, `/warnings`. Entities: `Warning`, `ModerationCase`. Dashboard: moderation pages (read-only actions today).

### Rules

- Requires Moderation **Module** + bot/mod Capabilities.
- Discord native permissions (KickMembers, etc.) are additional layer.

### Related terms

Warning, Moderation Case, Capability

### Example

Moderator runs `/warn` → **Warning** created + **Log Entry**.

---

## Warning

**Status:** Live

### Business definition

A **Warning** is a formal moderation strike recorded against a Guild Member with reason and moderator attribution.

### Technical definition

Entity: `Warning` — target, moderator, reason, guild scoped.

### Rules

- Distinct from **Moderation Case** type Warn (Case may aggregate actions).
- Visible via `/warnings` and dashboard moderation view.

### Related terms

Moderation Case, Moderation, Log Entry

### Example

`/warn @user spam` creates Warning row and `WarningCreated` Log Entry.

---

## Moderation Case

**Status:** Live

### Business definition

A **Moderation Case** is a persisted record of a moderation action (kick, clear, etc.) for audit and dashboard review.

### Technical definition

Entity: `ModerationCase` with `ModerationCaseType`, target, moderator, optional message count/channel.

### Rules

- Not every action also creates Warning — kicks create cases.
- Dashboard moderation page lists cases read-only.

### Related terms

Warning, Moderation, Log Entry

### Example

`/kick` creates ModerationCase type Kick + `MemberKicked` Log Entry.

---

## Reaction Role

**Status:** Live

### Business definition

A **Reaction Role** is a button panel in Discord letting Guild Members self-assign a **Discord Role**.

### Technical definition

Entity: `ReactionRole` — channel, message, role, button ids, active flag. Created via bot command; deactivated from dashboard.

### Rules

- Requires Reaction Roles **Module** + bot ManageRoles.
- Not the same as **Command Panel** (general actions).

### Related terms

Discord Role, Module, Command Panel

### Example

Button "Get Gamer Role" toggles role via interaction handler.

---

# Logging and audit

---

## Log Entry

**Status:** Live

### Business definition

A **Log Entry** is one recorded platform activity event in the guild's activity log — what the bot or dashboard did, not every Discord server event.

### Technical definition

Entity: `LogEntry` — `LogEventType`, message, actor/target/channel metadata, `MetadataJson`.

### Rules

- Logs **Module** enables Discord mirror to log channel via `DiscordLogDeliveryService` for subset of types.
- Not Discord's native Audit Log.
- Cap at 200 rows per query in dashboard (implementation limit).

### Related terms

Activity Log, Audit Log, LogEventType, Domain Event

### Example

`TicketOpened` Log Entry when ticket created via API.

---

## Activity Log

**Status:** Live (product surface name)

### Business definition

The **Activity Log** is the Dashboard and module name for viewing **Log Entries** — the guild's bot/platform action history.

### Technical definition

Route: `/guilds/:id/logs`. Module key: `logs`. Distinct from application logging (Serilog/console).

### Rules

- Marketing: "Activity Log" module — avoid calling it full server logging.
- **ClearLogs** Capability required to delete entries.

### Related terms

Log Entry, Audit Log, Logs Module

### Example

Owner filters Activity Log by `TicketClosed` type.

---

## Audit Log

**Status:** Planned (compliance-oriented)

### Business definition

An **Audit Log** is a compliance-grade export or view of tamper-evident records — permission changes, admin actions, moderation, tickets — for enterprise customers.

### Technical definition

**Not implemented as separate product.** Today **Activity Log** + **Log Entry** serve partial purpose. Planned: permission change audit, export, retention policies (Phase 2–3).

### Rules

- Do not equate Activity Log module with enterprise Audit Log promise yet.
- When implemented, Audit Log may aggregate Log Entries + Timeline Events + admin actions.

### Related terms

Log Entry, Activity Log, Platform Administrator

### Example

Enterprise customer exports Audit Log CSV for quarter (planned).

---

## Domain Event

**Status:** Conceptual · **Live** (as LogEventType + handlers)

### Business definition

A **Domain Event** is something significant that happened in the business domain (ticket opened, member warned) that other parts of the system may react to.

### Technical definition

Often persisted as **Log Entry** with `LogEventType`. Future **Workflow Trigger** may subscribe to domain events. Not event sourcing today.

### Rules

- Prefer existing `LogEventType` before adding new event names.
- New event types require migration + dashboard i18n + optional Discord delivery.

### Related terms

Log Entry, Workflow, Trigger

### Example

`TicketClosed` domain event → Log Entry + Archive + channel cleanup job.

---

# Automation (current and planned)

---

## Command Panel

**Status:** Live

### Business definition

A **Command Panel** is a Discord message with buttons (e.g. open ticket, help) giving members command-free entry points.

### Technical definition

Configured in **Guild Settings**: channel, message id, title, buttons JSON, refresh flag. Synced by **Background Job** via `CommandPanelSyncService`.

### Rules

- Distinct from ticket channel control buttons on individual tickets.
- Panel refresh requested when settings change.

### Related terms

Module, Ticket, Bot

### Example

`#support` channel posts panel with "Create Ticket" button → **Ticket** creation flow.

---

## Auto Reply

**Status:** Live

### Business definition

An **Auto Reply** is a rule that sends an automatic bot message when a message matches a keyword **Trigger** in scope.

### Technical definition

Entity: `AutoReplyRule` — `Trigger`, `Response`, `MatchMode`, `Scope` (`AllChannels` | `TicketChannelsOnly`), `Priority`.

### Rules

- Lower priority number evaluated first (implementation order).
- Ticket scope requires channel linked to **Ticket**.

### Related terms

Trigger, Automation, Ticket

### Example

Trigger "hours" → bot replies with support hours text in ticket channels only.

---

## Trigger

**Status:** Live (Auto Reply) · **Planned** (Workflow)

### Business definition

A **Trigger** is the condition that starts automated behavior. In Auto Reply: keyword match. In future Workflow: domain event, schedule, or rule.

### Technical definition

Auto Reply: `AutoReplyRule.Trigger` string. Workflow Trigger: not implemented.

### Rules

- In Auto Reply docs, say **Auto Reply Trigger** when ambiguity with Workflow Trigger.
- Auto Reply Trigger is not a Domain Event — it evaluates message content.

### Related terms

Auto Reply, Workflow, Action

### Example

Trigger `"refund"` with Contains match mode.

---

## Action

**Status:** Planned (Workflow) · **Live** (implicit bot actions)

### Business definition

An **Action** is a single automated step performed when a rule fires: send message, close ticket, assign moderator (planned).

### Technical definition

Workflow Action: not implemented. Today bot actions are hardcoded in handlers (send welcome, deliver outbound message).

### Rules

- Future Workflow Action catalog must respect Module and Permission policies.

### Related terms

Workflow, Trigger, Automation

### Example

Planned action: "When ticket inactive 48h → send warning message."

---

## Automation

**Status:** Live (partial) · **Planned** (platform)

### Business definition

**Automation** reduces repetitive operator work through rules without custom code — today Auto Reply + background delivery; tomorrow Workflow engine.

### Technical definition

Current: `AutoReplyRule`, **Ticket Outbound Message** worker, command panel sync. Planned: workflow builder (Phase 4 blueprint).

### Rules

- Do not market full automation until Workflow exists — say "Auto Reply rules" for current capability.

### Related terms

Workflow, Trigger, Action, Auto Reply

### Example

Automation roadmap adds triggers on **Log Entry** types (ticket opened → notify staff).

---

## Workflow

**Status:** Planned

### Business definition

A **Workflow** is a defined sequence of Trigger → Conditions → Actions operating on domain objects (tickets, moderation, welcome).

### Technical definition

Not implemented. Described in Product Blueprint Phase 4.

### Rules

- Workflow is official term for future automation builder — not "script" or "recipe."
- Must observability: log Workflow runs as **Log Entry** or separate run table (TBD ADR).

### Related terms

Trigger, Action, Automation, Domain Event

### Example

Workflow: on TicketOpened → if category Billing → assign Billing queue.

---

# Analytics and reporting

---

## Analytics

**Status:** Planned (module) · **Partial live** (counts)

### Business definition

**Analytics** is aggregated insight into guild operations: ticket volume, response times, moderation rates, module usage.

### Technical definition

Today: overview/admin counts (`openTickets`, fleet stats). Planned: Analytics **Module** with charts (Phase 4).

### Rules

- Do not call overview stat cards "Analytics module."
- Analytics reads from persisted domain data — requires Ticket Timeline for ticket metrics.

### Related terms

Report, Ticket, Moderation, Module

### Example

Analytics dashboard shows median first response time per week (planned).

---

## Report

**Status:** Planned

### Business definition

A **Report** is a generated export or scheduled summary (CSV, PDF, dashboard snapshot) derived from **Analytics** or **Audit Log** data.

### Technical definition

Not implemented. Ticket Transcript export is a specialized report (planned).

### Rules

- Distinguish Report (output artifact) from Analytics (capability/module).

### Related terms

Analytics, Audit Log, Transcript

### Example

Monthly moderation Report emailed to Guild Owner (planned).

---

# Integrations and infrastructure

---

## Integration

**Status:** Planned

### Business definition

An **Integration** connects the Platform to external systems (Stripe, webhooks, CRM) through documented **API** contracts.

### Technical definition

Today: only Discord OAuth + Discord Gateway + manual billing. Planned: Stripe webhooks, outbound webhooks (Phase 3–4).

### Rules

- External systems never write directly to database — through API only.

### Related terms

API, Platform, Workflow

### Example

Stripe Integration updates **Subscription** on payment success webhook (planned).

---

## Notification

**Status:** Planned · **Stub live**

### Business definition

A **Notification** informs a Dashboard User of something requiring attention (new ticket, failed delivery, upgrade approved).

### Technical definition

Dashboard notification bell is UI stub (`notificationsOpen` flag). No persistence or delivery pipeline.

### Rules

- Do not claim notifications work in beta docs.
- Future notifications reference domain objects (Ticket, Upgrade Request).

### Related terms

Dashboard User, Ticket, Upgrade Request

### Example

"Ticket #42 assigned to you" notification (planned).

---

## Worker

**Status:** Live

### Business definition

A **Worker** is a long-running bot process component that polls the **API** for work items and performs Discord-side effects.

### Technical definition

`GuildMaintenanceWorker` (HostedService): command panel sync, ticket cleanup, outbound messages. `GuildResourceSyncWorker`: resource sync.

### Rules

- Workers run inside Bot process in v1 — not separate deployable services.
- Poll interval 30 seconds today.

### Related terms

Background Job, Bot, API, Synchronization

### Example

Worker delivers pending **Ticket Outbound Message** to Discord channel.

---

## Background Job

**Status:** Live (conceptual)

### Business definition

A **Background Job** is a unit of deferred work executed outside the HTTP request or Discord interaction path (cleanup, delivery, sync).

### Technical definition

Implemented as worker poll loops + API pending queues (`ChannelCleanupRequested`, undelivered outbound messages, `ResourceSyncRequested`, `CommandPanelRefreshRequested`). No Hangfire/queue broker.

### Rules

- Jobs must be idempotent where possible (ack cleanup, ack message).
- Failed jobs log errors; retry on next poll (limited dead-letter planning CM-013).

### Related terms

Worker, API, Ticket Outbound Message

### Example

Dashboard closes ticket → cleanup job deletes Discord channel after archive.

---

## Discord Resource

**Status:** Live

### Business definition

A **Discord Resource** is cached metadata about Discord structure copied into the Platform for dropdowns, permission sync, and display names.

### Technical definition

Entities: `DiscordChannel`, `DiscordRole`, `DiscordGuildMember`. Populated by bot **Synchronization**.

### Rules

- Resources are snapshots — may be stale until next sync.
- Snowflake IDs stored as strings.

### Related terms

Synchronization, Guild, Discord Role

### Example

Ticket category dropdown reads synced Discord categories.

---

## Synchronization

**Status:** Live

### Business definition

**Synchronization** is the process of copying Discord channels, roles, and members from a guild into Platform **Discord Resources**.

### Technical definition

Bot: `ResourceSyncService` on `/sync` and worker when `ResourceSyncRequested`. API stores channels, roles, members. Emits `ResourceSyncCompleted` **Log Entry**.

### Rules

- Dashboard triggers sync; bot executes — not synchronous in HTTP request from dashboard in all paths.
- Member role IDs used for **Permission** resolution when cached.

### Related terms

Discord Resource, Bot, Worker, Guild Member

### Example

Owner adds new role in Discord → runs sync → role appears in Permission Role dropdown.

---

# DDD meta language

Applied consistently in specs and ADRs for this codebase (layered monolith, not full DDD tactical patterns everywhere).

---

## Entity

**Status:** Conceptual

### Business definition

An **Entity** is a domain object with continuous identity that persists over time (same ticket number, same guild).

### Technical definition

Classes inheriting `BaseEntity` with `Id` (Guid), `CreatedAt`, `UpdatedAt`. Examples: `Guild`, `Ticket`, `LogEntry`.

### Rules

- Entities live in `DiscordBot.Domain/Entities`.
- Equality by `Id`, not by attribute values.

### Related terms

Aggregate, Value Object

### Example

`Ticket` entity identity is `Id`; `TicketNumber` is business identifier scoped to guild.

---

## Value Object

**Status:** Conceptual · **Limited use**

### Business definition

A **Value Object** has no identity — defined entirely by its attributes (money amount, date range, template string).

### Technical definition

Often modeled as strings, enums, or DTOs — not separate tables. Examples: `TicketStatus`, message template with placeholders, `AllowedModulesJson`.

### Rules

- Prefer enums for closed sets (Status, LogEventType).
- Immutable value objects when introducing new typed wrappers.

### Related terms

Entity, Configuration

### Example

`TicketStatus.Closed` is value — no `TicketStatusId`.

---

## Aggregate

**Status:** Conceptual

### Business definition

An **Aggregate** is a cluster of **Entities** treated as one consistency boundary for business operations.

### Technical definition

Informal aggregates in this platform:

| Aggregate root | Contains |
|----------------|----------|
| **Guild** | Settings, Modules, Subscription (refs), Permission Roles |
| **Ticket** | Outbound Messages (today); Timeline messages (planned) |
| **Permission Role** | Permissions bitmask |

No explicit aggregate repository — `*Service` classes enforce boundaries.

### Rules

- Cross-aggregate references by Id only (`Ticket.GuildId`).
- Modify ticket through `TicketService`, not by reaching through Guild navigation in controllers.

### Related terms

Entity, Product Domain, Policy

### Example

Closing **Ticket** updates Ticket aggregate + writes **Log Entry** (separate aggregate) via service orchestration.

---

# Legacy and deprecated terms

| Deprecated term | Official replacement | Notes |
|-----------------|---------------------|-------|
| GuildStaff | Permission Role | Table removed |
| ModerationPermissionRole | Permission Role | Merged July 2026 |
| GuildStaffRole enum | Permission flags | Removed |
| AccessTickets | ViewTickets | Legacy alias in API |
| Staff (entity) | Permission Role | "Staff page" UI name OK |
| Feature | Module / Capability | Product language |
| Conversation (tickets) | Ticket Timeline | |
| Audit log (for Logs module) | Activity Log | Enterprise audit planned separately |

---

## Revision history

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-02 | UL-001 initial ubiquitous language specification |

---

## Related documents

| Document | Path |
|----------|------|
| Product Blueprint | [product-blueprint.md](./product-blueprint.md) |
| Architecture glossary (index) | [/docs/architecture/glossary.md](../architecture/glossary.md) |
| Permission system | [/docs/architecture/permission-system.md](../architecture/permission-system.md) |
| Ticket domain | [/docs/tickets/ticket-system-review.md](../tickets/ticket-system-review.md) |
| Product Blueprint tickets maturity | CM-001 progress report |
