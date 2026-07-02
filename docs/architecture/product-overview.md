# Product Overview

## What the Discord Bot Platform is

A **multi-tenant SaaS** that provides:

1. A **Discord bot** installed per server (guild)
2. A **web dashboard** for guild owners and staff
3. A **REST API** connecting dashboard and bot to shared PostgreSQL state
4. **Subscription plans** limiting which modules each guild may enable
5. **Platform admin tools** for operator-managed billing and support

Each Discord server using the bot is a **tenant**. Tenant boundary = `Guild` entity (`DiscordGuildId`).

## Primary user journeys

### Guild owner

1. Invite bot to Discord server
2. Run `/setup` or `/sync` in Discord (registers guild, syncs channels/roles/members)
3. Log into dashboard via Discord OAuth
4. Complete onboarding checklist (plan, modules, settings)
5. Configure welcome, tickets, moderation permissions, reaction roles, etc.
6. Map Discord roles to dashboard/bot permissions on **Staff** page
7. Submit plan upgrade request (manual approval by platform admin)

### Guild staff (role-based)

1. Log into dashboard with Discord account that has mapped Discord role
2. Access moderation, logs, or tickets pages based on `GuildPermissionRoles`
3. Use bot commands (`/warn`, `/kick`, ticket close) based on same permission model

### Platform admin

1. Log in (JWT + `PlatformAdmins` table)
2. View all guilds, users, upgrade requests
3. Approve/reject upgrade requests, extend/cancel subscriptions
4. CRUD subscription plans and pricing

## Feature modules (product surface)

| Module key | User-facing name | Bot | Dashboard |
|------------|------------------|-----|-------------|
| `welcome` | Welcome | Join message | Settings |
| `tickets` | Tickets | `/ticket`, panels, buttons | Tickets page |
| `moderation` | Moderation | `/warn`, `/kick`, `/clear`, `/warnings` | Moderation + settings |
| `logs` | Logs | Discord channel log delivery | Logs page |
| `auto-role` | Auto Role | Role on join | Settings |
| `reaction-roles` | Reaction Roles | Button panels | Reaction roles page |

Module **enablement** is separate from **authorization** (who may use a enabled module).

## Subscription tiers (seeded defaults)

| Plan | Monthly price | Modules |
|------|---------------|---------|
| Free | $0 | welcome, logs |
| Basic | $9.99 | + reaction-roles |
| Pro | $19.99 | + tickets, moderation |
| Premium | $29.99 | all (`*`) |

Owners cannot directly change plan via API (`PUT subscription` returns 403). They create **upgrade requests**; admins approve.

## Dashboard pages (guild-scoped)

| Route | Access guard | Purpose |
|-------|--------------|---------|
| `/servers` | Auth | Guild picker |
| `/guilds/:id/overview` | Owner | Status summary |
| `/guilds/:id/settings` | Owner | Bot feature settings |
| `/guilds/:id/modules` | Owner | Enable/disable modules |
| `/guilds/:id/subscription` | Owner | Plan info, upgrade requests |
| `/guilds/:id/staff` | Owner | Permission roles |
| `/guilds/:id/profile` | Owner | Server profile for `/server` embed |
| `/guilds/:id/tickets` | Moderation | Ticket list |
| `/guilds/:id/moderation` | Moderation | Warnings/cases |
| `/guilds/:id/moderation/settings` | Owner | Bot moderation permissions (same backend as staff) |
| `/guilds/:id/logs` | Moderation | Activity logs |
| `/guilds/:id/reaction-roles` | Owner | Reaction role panels |

## Languages

Dashboard i18n: **English** and **Arabic** (`src/assets/i18n/en.json`, `ar.json`).

## Assumption

Payment processing (Stripe, etc.) is **not integrated**. Subscriptions are managed manually via admin approval workflow.
