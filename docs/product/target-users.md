# Target Users

## Primary personas

### 1. Guild Owner (Primary)

**Who:** Discord server owner or head admin  
**Goals:** Set up bot quickly, control who accesses dashboard, choose appropriate plan  
**Technical level:** Low to medium — may not know API/database concepts  
**Key flows:** Invite bot, onboarding checklist, settings, staff permissions, subscription upgrade request

### 2. Guild Staff (Secondary)

**Who:** Moderators, support agents with assigned Discord roles  
**Goals:** Handle tickets, view moderation cases, read logs  
**Technical level:** Low — uses dashboard occasionally  
**Key flows:** Login, access tickets/moderation/logs based on permissions  
**Limitation today:** Permissions tied to Discord roles only — no individual user grants

### 3. Platform Admin (Internal)

**Who:** Product operator / business owner  
**Goals:** Approve upgrades, manage plans, oversee all guilds  
**Technical level:** Medium  
**Key flows:** Admin panel, upgrade approval, plan CRUD, subscription extend/cancel

### 4. Beta Tester (Transient)

**Who:** Early adopter validating closed beta  
**Goals:** Test flows, report bugs  
**Reference:** `docs/beta-tester-guide.md`

## Non-target users (today)

| User | Why not targeted |
|------|------------------|
| Discord bot developers | No public plugin SDK yet |
| Enterprise IT with SSO | No SSO integration |
| Non-Discord communities | Platform is Discord-only |

## Geographic / language

- Dashboard: **English and Arabic**
- Bot responses: English (assumption — not fully i18n on bot side)
- Admin currency display: USD implied in seeded prices

## Related docs

- `pricing.md`, `module-list.md`
- `/docs/architecture/product-overview.md`
