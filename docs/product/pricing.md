# Pricing

## Model

**Per-guild subscription** — each Discord server has one plan. Not per-seat or per-user pricing.

**Billing mechanism today:** Manual — owners submit upgrade requests; platform admins approve. **No Stripe/payment gateway integrated.**

## Plans (seeded defaults)

Source: `SubscriptionPlanSeeder.cs`

| Plan key | Display name | Monthly price | Modules included |
|----------|--------------|---------------|------------------|
| `free` | Free | $0.00 | welcome, logs |
| `basic` | Basic | $9.99 | welcome, logs, reaction-roles |
| `pro` | Pro | $19.99 | welcome, logs, reaction-roles, tickets, moderation |
| `premium` | Premium | $29.99 | All modules (`*`) |

Prices stored in `SubscriptionPlans.MonthlyPrice` — editable by platform admin via `/admin/plans`.

## Duration options

Upgrade requests support: **1, 3, 6, or 12 months** (`SubscriptionDurations.cs`).

## Upgrade flow

1. Owner visits `/guilds/{id}/subscription`
2. Submits upgrade request (target plan + duration)
3. Platform admin approves or rejects at `/admin/upgrade-requests`
4. On approve: `GuildSubscription` updated with expiry date

Direct plan change by owner (`PUT /api/guilds/{id}/subscription`) returns **403**.

## Module gating behavior

If guild downgrades or plan expires:

- Modules not in plan cannot be **enabled** (toggle blocked)
- Already-enabled modules — behavior on expiry should be verified per service (assumption: soft enforcement via expiry status)

## Future pricing considerations

- Stripe Checkout + webhooks
- Annual discount
- Usage-based add-ons (message volume, log retention)
- Trial period on Pro
- Regional pricing / currency codes

## Related docs

- `/docs/architecture/subscription-system.md`
- `module-list.md`
