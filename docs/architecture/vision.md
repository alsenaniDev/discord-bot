# Vision

## Long-term vision

Build the **default operations platform for Discord communities** — a commercial SaaS where server owners configure moderation, support, onboarding, and engagement from one dashboard, backed by a reliable bot that respects subscription tiers and role-based access.

The platform should scale from a single guild beta to **thousands of paying customers** without rewriting core architecture every year.

## What success looks like

- A guild owner can invite the bot, complete onboarding, choose a plan, enable modules, and configure staff permissions in under 30 minutes.
- Staff use the dashboard and Discord commands with **consistent permissions** — no duplicate configuration.
- Platform operators manage subscriptions, upgrade requests, and customer guilds from an admin area.
- New modules (analytics, automation, marketplace plugins) can be added without breaking existing guilds.
- The system runs reliably on managed cloud infrastructure with observable health and audit trails.

## Strategic pillars

| Pillar | Description |
|--------|-------------|
| **Modular product** | Features ship as modules gated by subscription plan |
| **Discord-native UX** | Commands, buttons, and panels feel native to Discord |
| **Multi-tenant SaaS** | One deployment serves many guilds with isolated settings |
| **Self-serve + admin assist** | Owners self-serve; platform admins handle billing edge cases |
| **International** | Dashboard supports multiple languages (EN/AR today) |

## Out of scope for the vision (today)

- Multi-bot clusters per customer
- White-label reseller portals
- Full plugin marketplace (planned Phase 4+)
- Mobile-native apps

## Assumption

The product remains a **single-bot, single-dashboard, single-API** monolith until scale forces service extraction. Extraction candidates (future): bot workers per shard, permission cache service, analytics pipeline.
