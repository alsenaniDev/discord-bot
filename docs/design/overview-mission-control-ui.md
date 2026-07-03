# UI-001 — Mission Control Overview (Design Specification)

**Document ID:** UI-001  
**Date:** 2026-07-03  
**Status:** **Design authority** — implementation-ready; no code in this document  
**Route:** `/guilds/:guildId/overview`  
**Supersedes:** O-002 Overview UI (visual layout only — logic remains PX-002)  
**Implements:** [PX-001](../ux/product-experience-architecture.md) · [PX-002](../ux/product-decision-architecture.md) · [PR-002 v2](../reviews/overview-redesign-v2.md) · [PP-001](./design-system.md)  
**Informed by:** [UX-002](../reviews/global-experience-audit.md)

---

## 1. Design intent

### 1.1 What this page is

The Guild Overview is **Mission Control** — the daily command surface for Discord server operators. It answers one question:

> **What should I do right now?**

Everything else (metrics, history, billing context) supports that decision. It is **not** a dashboard of widgets.

### 1.2 Emotional target

| Feel | How the design delivers it |
|------|---------------------------|
| **Premium** | Typography-led hierarchy, generous whitespace, one elevation level |
| **Calm** | Veteran all-clear has no button; neutral surfaces dominate |
| **Professional** | No gamification, no emoji, no marketing copy |
| **Enterprise** | Honest status, inspectable metrics, audit trail below the fold |
| **Minimal** | Five zones maximum; three border lines on the full page |

### 1.3 Benchmark posture

| Product | What we borrow |
|---------|----------------|
| **Linear** | One focus surface; subtraction over addition |
| **Stripe** | Single action-required hero; billing honesty |
| **Vercel** | Status hero that adapts to deployment state |
| **GitHub** | Activity grouped by day; linked narrative rows |
| **Discord Developer Portal** | Developer-grade restraint; push to Discord when appropriate |
| **Slack** | Compact workspace awareness strip |

**Rejected:** AdminLTE grids, Grafana charts, health rings, shortcut rows, permanent billing cards.

### 1.4 Ten-second comprehension test

On **1440×900**, without scrolling, a guild owner must answer:

| Question | Answer location |
|----------|-----------------|
| Where am I? | Topbar title + subtitle (Zone 1) |
| What is happening? | Mission Card + Pulse |
| What should I do? | Mission Card CTA (or explicit “no action”) |
| What happened recently? | First 2–3 activity rows |
| Can I trust this? | Honest labels, relative sync time, no fake scores |

If any answer requires scrolling → design fails.

---

## 2. Page architecture

### 2.1 Five zones (non-negotiable)

```
┌─────────────────────────────────────────────────────────────────┐
│ ZONE 1 — Status extension (topbar subtitle, not a page card)     │
├─────────────────────────────────────────────────────────────────┤
│ ZONE 2 — Mission Card (hero — one CTA max)                       │
├─────────────────────────────────────────────────────────────────┤
│ ZONE 3 — Community Pulse (borderless metric strip)               │
├─────────────────────────────────────────────────────────────────┤
│ ZONE 4 — Recent Activity (timeline — scroll begins here)         │
├─────────────────────────────────────────────────────────────────┤
│ ZONE 5 — Context Drawer (collapsed by default)                   │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Permanently removed

Do not implement on Overview:

- Quick Actions / shortcut grid  
- Resources footer  
- Health ring or circular gauges  
- Permanent Subscription card  
- Permanent Modules list card  
- Duplicate guild header card  
- Setup / Activation card separate from Mission Card  
- Recommendations section (rank 2–3 → Drawer Suggestions tab)  
- Alert banner stacked above Mission Card for same issue  
- Fake notification bell (shell issue — hide globally per UX-002)

### 2.3 Page shell

| Property | Value |
|----------|-------|
| Container | `.page-content.page-full` (max 1200px centered) |
| Horizontal padding | `--space-6` (24px) |
| Vertical stack gap | `--space-6` between Mission Card ↔ Pulse; `--space-8` before Activity |
| Background | `--color-bg-app` — no full-page card wrapper |

---

## 3. Spacing system

All spacing uses PP-001 tokens. Overview-specific rhythm:

| Region | Token | px | Rule |
|--------|-------|-----|------|
| Page top inset (below topbar) | `--space-6` | 24px | First content element |
| Mission Card internal padding | `--space-6` | 24px | All sides |
| Mission Card → Pulse | `--space-6` | 24px | No divider between |
| Pulse row height | fixed | 72px | Includes internal padding `--space-3` vertical |
| Pulse → Activity header | `--space-8` | 32px | Visual section break |
| Activity row height | fixed | 56px | Padding `--space-3` vertical |
| Activity row gap (divider) | 1px | `--color-border` | Full width |
| Day group header margin | `--space-4` top, `--space-2` bottom | | `.type-overline` |
| Drawer collapsed bar | `--space-4` padding | 48px total height |
| Drawer expanded max height | 320px | scroll inside | `--elevation-1` inset |
| Mission Card min / max height | 140px / 200px | | Includes optional progress footer |

**Rule:** No raw pixel margins in Overview CSS. No card inside card.

---

## 4. Typography hierarchy

| Element | Class | Size token | Weight | Color | Max lines |
|---------|-------|------------|--------|-------|-----------|
| Topbar guild name | `.type-page-title` (shell) | `--text-2xl` | 700 | `--color-text` | 1 |
| Zone 1 status line | `.type-caption` | `--text-xs` | 400 | `--color-text-muted` | 1 |
| Mission title | `.type-section-title` | `--text-xl` | 600 | `--color-text` | 1 |
| Mission body | `.type-subtitle` | `--text-base` | 500 | `--color-text-secondary` | 1 |
| Mission CTA | `.btn.btn-primary` | `--text-sm` | 600 | on brand | — |
| Pulse label | `.type-overline` | `--text-xs` | 700 | `--color-text-muted` | 1 |
| Pulse value (numeric) | `.type-section-title` | `--text-xl` | 600 | `--color-text` | 1 |
| Pulse value (status word) | `.type-caption` | `--text-xs` | 500 | semantic token | 1 |
| Activity section title | `.type-card-title` | `--text-lg` | 600 | `--color-text` | 1 |
| Activity “View all” link | `.btn-ghost` or text link | `--text-sm` | 500 | `--color-brand` | 1 |
| Day group label | `.type-overline` | `--text-xs` | 700 | `--color-text-muted` | 1 |
| Activity message | `.type-body` | `--text-base` | 400 | `--color-text` | 2 max |
| Activity time | `.type-caption` | `--text-xs` | 400 | `--color-text-muted` | 1 |
| Drawer toggle | `.type-body` | `--text-base` | 500 | `--color-text-secondary` | 1 |
| Drawer tab | `.type-label` | `--text-sm` | 500 | active: `--color-text` | 1 |
| Drawer body | `.type-caption` | `--text-xs` | 400 | `--color-text-muted` | — |

**Arabic:** `--font-ar` applies globally via `html[dir='rtl']`. Pulse overlines may stay uppercase in EN; AR uses sentence case per localization guide — keys must not hardcode uppercase in AR strings.

---

## 5. Color & severity

Mission Card uses **one semantic accent**: `border-inline-start` 4px on `.card.card-elevated.card-status`.

| Severity | Border class | Token | Icon tint | Use |
|----------|--------------|-------|-----------|-----|
| **critical** | `.is-danger` *(add to PP-001)* | `--color-danger` | `--color-text-danger` | Bot offline, payment rejected, expired |
| **warning** | `.is-warning` | `--color-warning` | `--color-text-warning` | Sync stale, expiring, ticket backlog |
| **info** | `.is-info` | `--color-info` | `--color-text-info` | Setup phases, recommendations |
| **neutral** | *(no status class)* | `--color-border` | `--color-text-muted` | Everything operational |

**Pulse cells:** Default text only. Semantic color on value when state is bad:

| Cell | Colored when |
|------|--------------|
| Bot | Offline → `--color-text-warning` |
| Health | Score &lt; 60 → `--color-text-warning`; &lt; 40 → `--color-text-danger` |
| Open tickets | ≥ elevated threshold → `--color-text-warning`; ≥ critical → `--color-text-danger` |
| Sync *(if shown in pulse v2)* | Stale → `--color-text-warning` |

**No background fills** on Pulse cells — typography and subtle color only.

---

## 6. Motion

Philosophy: **motion confirms state change; it never decorates.**

| Interaction | Animation | Duration | Easing | Reduced motion |
|-------------|-----------|----------|--------|----------------|
| Mission Card content swap | Opacity crossfade | 150ms | `--ease-out` | Instant swap |
| Mission CTA loading | `.btn.is-loading` spinner | — | — | Same |
| Pulse value update | Opacity 120ms | 120ms | `--ease-out` | Instant |
| Activity row enter (poll) | None v1 | — | — | — |
| Drawer expand/collapse | `max-height` + opacity | 200ms | `--ease-out` | Instant height toggle |
| Drawer tab switch | None | — | — | — |
| Activity row hover | Background `--color-bg-elevated` | 100ms | — | No hover required for a11y |

**Forbidden:** Slide-up heroes, confetti, parallax, animated health rings, bouncing badges.

---

## 7. Zone 1 — Top status extension

### 7.1 Placement

Extends existing dashboard topbar when `route === overview`. **Not duplicated in page body.**

### 7.2 Layout

Single line beneath guild name (`.type-caption`):

```
{PlanBadge} · {BotDot}{BotLabel} · {SyncLabel}
```

| Segment | Owner | Staff (no billing) |
|---------|-------|---------------------|
| Plan | `Pro` badge `.badge-plan` | Omitted |
| Access | — | `Staff access` muted text |
| Bot | `● Online` / `○ Offline` | Same |
| Sync | `Synced 2h ago` / `Not synced` / `Syncing…` | Same |

### 7.3 Rules

- No guild name repeat in body  
- No setup % in topbar (Beginner → Mission Card progress)  
- No health score in topbar (→ Pulse)  
- Bot dot: 6px circle, `--color-success` or `--color-warning`  
- Sync tooltip on hover: absolute timestamp (ISO in `<time>`)  
- Topbar actions unchanged: Open Discord (secondary), overflow ⋮ (Sync, Copy ID)

### 7.4 States

| State | Display |
|-------|---------|
| Loading | `··· · ··· · ···` skeleton captions in topbar |
| Error | `Unable to load status` — page-level error handles full fail |
| Sync in progress | `Syncing…` with subtle pulse dot (respect reduced motion) |

---

## 8. Zone 2 — Mission Card

### 8.1 Component anatomy

```
┌─ border-inline-start 4px (severity) ─────────────────────────────┐
│  [optional icon 20px]  Title (.type-section-title)                │
│                        Body (.type-subtitle, one line)            │
│                        [ Primary CTA ]  ← max one                 │
│  ─── optional footer: 3-phase progress (Beginner only) ───      │
└───────────────────────────────────────────────────────────────────┘
```

| Property | Spec |
|----------|------|
| Classes | `.card.card-elevated.card-status.{severity}` |
| Width | 100% of page content |
| Min height | 140px |
| Max height | 200px |
| Padding | `--space-6` |
| Icon | Optional 20px `app-ui-icon` inline-start of title row — not emoji |
| CTA placement | Below body, `margin-top: --space-4`, inline-start aligned |
| Secondary actions | **Forbidden** in card — use Context Drawer |
| Dismiss control | Only for dismissible missions (see matrix) — ghost icon button top-end |

### 8.2 Layout grid (internal)

```
[ icon? ] [ title block (flex 1) ] [ dismiss? ]
          [ body ]
          [ cta row ]
          [ progress footer? ]
```

### 8.3 Beginner progress footer

When `MissionId` ∈ `CompleteSetup*`:

- Height: 4px track + labels row  
- Three phases: **Connect · Configure · First win**  
- Current phase: `--color-brand` underline on label  
- Completed phases: `--color-text-muted` with check icon  
- `progressPercent` maps to overall weight — not 8 micro-steps  

---

## 9. Mission Card — complete state matrix

Each row maps **MissionId** (PX-002) → UI presentation. Backend resolver emits one winner.

**Legend — Dismiss:** N = Never · S7 = Snooze 7 days · — = N/A (calm)

| MissionId | Severity | Icon (`app-ui-icon`) | Border | Title (EN) | Description (EN) | CTA (EN) | Route / action | Dismiss |
|-----------|----------|----------------------|--------|------------|------------------|----------|----------------|---------|
| **BotOffline** | critical | `bot-offline` | danger | Bot is disconnected | Members won't receive bot features until it reconnects. | Open Discord | External Discord invite/deep link | N |
| **BotMissingPermissions** | critical | `shield-alert` | danger | Bot is missing permissions | The bot can't post or manage channels until permissions are fixed. | Fix permissions | `/guilds/:id/settings` | N |
| **SubscriptionExpired** | critical | `credit-card-off` | danger | Subscription expired | Paid modules are locked until you renew. | Renew plan | `/guilds/:id/subscription` | N |
| **PaymentRejected** | critical | `credit-card-x` | danger | Subscription change declined | {{reason}} — update your payment reference and resubmit. | View billing | `/guilds/:id/subscription` | N |
| **GuildSuspended** *(future)* | critical | `ban` | danger | Server access suspended | Contact support to restore access. | Contact support | Help link | N |
| **SynchronizationStale** | warning | `refresh-cw` | warning | Server data is outdated | Sync to refresh channels and roles in the dashboard. | Sync now | `Sync` action (topbar/API) | S7 |
| **TicketBacklogCritical** | warning | `ticket-alert` | warning | {{count}} open tickets need attention | At least one ticket has waited too long. | Review tickets | `/guilds/:id/tickets` | N |
| **TicketBacklogElevated** | warning | `ticket` | warning | {{count}} open tickets waiting | Review support requests before they pile up. | Review tickets | `/guilds/:id/tickets` | S7 |
| **SubscriptionExpiringSoon** | warning | `clock` | warning | Subscription expires in {{days}} days | Renew to keep paid modules active. | Renew plan | `/guilds/:id/subscription` | S7 |
| **SubscriptionChangePendingPayment** | info | `wallet` | info | Payment reference required | Submit your payment details to continue the subscription change. | Complete payment | `/guilds/:id/subscription` | N |
| **CompleteSetupConnect** | info | `link` | info | Finish connecting your server | Invite the bot and link this server to your account. | Start setup | `/servers` | N |
| **CompleteSetupConfigure** | info | `settings` | info | Configure your first module | Enable welcome, tickets, or logs to deliver value. | Open settings | `/guilds/:id/settings` | N |
| **CompleteSetupFirstValue** | info | `sparkle` | info | Get your first win | Complete one outcome in Discord — open a ticket or send a welcome. | Continue setup | Dynamic step route | N |
| **SynchronizationNever** | info | `cloud-off` | info | Server not synced yet | Sync once to load channels and roles for configuration. | Go to servers | `/servers` | N |
| **SubscriptionChangePendingReview** | info | `hourglass` | info | Subscription change under review | Review usually takes 1–2 business days. No action needed now. | View status | `/guilds/:id/subscription` | N |
| **InviteStaff** | info | `users` | info | Add staff to help manage | Map permission roles so moderators can work without owner access. | Add staff roles | `/guilds/:id/staff` | S7 |
| **EnableModule** | info | `puzzle` | info | Enable your first module | Turn on tickets, welcome, or logs to start serving members. | Browse modules | `/guilds/:id/modules` | S7 |
| **CreateWelcome** | info | `message-circle` | info | Set up welcome messages | New members won't get a greeting until welcome is configured. | Configure welcome | `/guilds/:id/settings` | S7 |
| **CreateTicketPanel** | info | `layout-panel` | info | Create a ticket panel | Tickets are enabled but members can't open one yet. | Set up tickets | `/guilds/:id/settings` | S7 |
| **CreateReactionPanel** | info | `smile-plus` | info | Create a reaction role panel | Let members self-assign roles from a panel in Discord. | Set up reaction roles | `/guilds/:id/reaction-roles` | S7 |
| **PaymentRequired** | info | `arrow-up-circle` | info | Upgrade to unlock modules | This module requires a paid plan. | View plans | `/guilds/:id/subscription` | S7 |
| **ReviewLogs** | info | `scroll-text` | info | Logs are quiet | No log events in 7 days — confirm logging is configured. | Check logs | `/guilds/:id/logs` | S7 |
| **PendingReports** *(planned)* | warning | `flag` | warning | Moderation cases awaiting review | Review reported cases before they escalate. | Review moderation | `/guilds/:id/moderation` | S7 |
| **Recommendation** *(generic)* | info | dynamic | info | {{title}} | {{description}} | {{cta}} | {{route}} | S7 |
| **EverythingOperational** | neutral | `check-circle` | none | Everything looks good | No action required. Community health: {{score}}. | *(none)* | — | — |

### 9.1 Dismiss UX (snoozeable missions only)

- Control: `.icon-btn` ghost, `×` or `clock-snooze`, top-inline-end of card  
- Action: Snooze 7 days per PX-002 §8  
- Toast: “Reminder snoozed for 7 days”  
- **Never** snooze Category A missions (critical blockers)

### 9.2 Payment rejected — reason interpolation

`{{reason}}` = admin note from subscription workflow, truncated to 120 chars. If empty: “Your payment reference could not be verified.”

### 9.3 Staff persona filtering

Staff never see: PaymentRejected, SubscriptionExpiringSoon, CompleteSetup*, InviteStaff, EnableModule, PaymentRequired.

Staff calm variant title: **No urgent actions for your role**

---

## 10. Zone 3 — Community Pulse

### 10.1 Visual spec

**Not a card.** Borderless horizontal strip.

```
────────────────────────────────────────────────────────────
  HEALTH      MEMBERS     OPEN TKT    LOGS TODAY    BOT     MODULES
   92          1,240         14          82        Online   5 of 8
────────────────────────────────────────────────────────────
```

| Property | Value |
|----------|-------|
| Height | 72px fixed |
| Top/bottom border | `1px solid var(--color-border)` |
| Layout | CSS grid, equal columns, `min-width: 0` |
| Cell alignment | Label top, value bottom, center-aligned text |
| Hover | Optional: subtle `--color-bg-elevated` if cell is link |
| Click | Optional v1: Health → drawer; Tickets → tickets route |

### 10.2 Desktop columns (Veteran — 6 cells)

| # | Label key | Value | Source | Empty | Loading | Error |
|---|-----------|-------|--------|-------|---------|-------|
| 1 | `pulse.health` | Score + level word | `experience.health.score` | `—` | skeleton bar | `—` |
| 2 | `pulse.members` | Formatted count | `overview.memberCount` | `—` | skeleton | hide cell |
| 3 | `pulse.openTickets` | Count | `overview.openTickets` | `0` | skeleton | `—` |
| 4 | `pulse.logsToday` | Count | `pulse.logsTodayCount` | `0` | skeleton | `—` |
| 5 | `pulse.bot` | Online/Offline | bot heartbeat | — | skeleton | Offline assumed |
| 6 | `pulse.modules` | `{{enabled}} of {{total}} active` | module summary | `0 of 8` | skeleton | `—` |

### 10.3 Beginner columns (4 cells)

`Setup` · `Health` · `Bot` · `Modules`

| Setup cell | Value |
|------------|-------|
| Label | `SETUP` |
| Value | `{{phase}}` e.g. `Configure` or `{{percent}}%` |

Hide ticket emphasis until tickets module configured.

### 10.4 Subscription-aware pulse variant

When `SubscriptionExpiringSoon` active **and** Mission Card is about sync/tickets:

Replace **Modules** cell with **Plan** cell:

| Label | Value |
|-------|-------|
| `PLAN` | `Pro` + `14d left` (caption, warning color) |

Primary renew CTA stays on Mission Card only. Plan cell may include ghost text link “Renew” — **not** `.btn-primary`.

### 10.5 Mobile pulse

Horizontal scroll snap container, 80px height, 4 cells visible (~140px each). No grid cramming.

---

## 11. Zone 4 — Activity Timeline

### 11.1 Visual spec (GitHub-inspired)

```
Recent activity                          View all logs →
────────────────────────────────────────────────────────
TODAY
  [icon]  Ticket #1042 opened                         2h ago
  [icon]  Tickets module enabled                      5h ago
YESTERDAY
  [icon]  Welcome message configured                  1d ago
```

| Property | Value |
|----------|-------|
| Wrapper | No card — full width list |
| Section header | flex row: title + ghost link inline-end |
| Row height | 56px |
| Row layout | `[icon 20px] [message flex 1] [time 80px fixed]` |
| Divider | `border-bottom: 1px solid var(--color-border)` |
| Max items | 8 desktop · 5 tablet · 5 mobile |
| Group headers | Today / Yesterday / Earlier — `.type-overline`, sticky optional v2 |

### 11.2 Row interaction

- Entire row clickable → detail route  
- Hover: `--color-bg-elevated` background  
- Focus: `:focus-visible` ring on row  
- `aria-label`: `"{{message}}, {{time}}"`

### 11.3 Activity types & icons

| type | Icon | EN message template |
|------|------|---------------------|
| `TicketCreated` | `ticket` | Ticket #{{number}} opened |
| `TicketClosed` | `ticket-check` | Ticket #{{number}} closed |
| `TicketReply` | `message-square` | Reply sent on ticket #{{number}} |
| `ModuleEnabled` | `toggle-right` | {{moduleName}} enabled |
| `ModuleDisabled` | `toggle-left` | {{moduleName}} disabled |
| `LogEntry` | `scroll-text` | {{summary}} |
| `MemberWarned` | `alert-triangle` | Warning issued to member |
| `StaffAdded` | `user-plus` | Staff role mapped for {{name}} |
| `SubscriptionChange` | `credit-card` | Subscription change {{status}} |

**Critical:** Message from i18n keys + params — **never raw API English string** (UX-002 P0).

### 11.4 Staff filtering

Show tickets + logs only if `!canManageModules`. Hide module events.

---

## 12. Zone 5 — Context Drawer

### 12.1 Purpose

Secondary information on demand. **Never competes with Mission Card.**

### 12.2 Collapsed state (default every page load)

```
┌──────────────────────────────────────────────────────────────┐
│  ▼  Show details     Setup · Modules · Billing · Help        │
└──────────────────────────────────────────────────────────────┘
```

| Property | Value |
|----------|-------|
| Height | 48px |
| Background | `--color-bg-panel` subtle |
| Toggle | “Show details” / “Hide details” — `.btn-ghost` |
| Tab hints | Muted inline labels — not active tabs until expanded |

### 12.3 Expanded state

| Property | Value |
|----------|-------|
| Max height | 320px, `overflow-y: auto` |
| Surface | `.surface-inset` full width |
| Tab bar | Horizontal tabs, `.type-label`, underline on active |
| Tab panel padding | `--space-4` |

### 12.4 Tab content spec

| Tab | Visible when | Content |
|-----|--------------|---------|
| **Setup** | Beginner mode | 3-phase checklist with links; each phase expandable to steps |
| **Modules** | Always | One line: `5 of 8 modules active` + ghost `Manage →` |
| **Billing** | Owner + (free / expiring / pending change) | Plan name, status badge, expiry date, `Manage billing →` |
| **Suggestions** | Veteran + rank 2–3 recommendations | Max 2 text links, no cards |
| **Help** | Always | 3 links: Setup guide · Ticket docs · Support |

**Hidden tabs** are removed from DOM — not disabled gray tabs.

### 12.5 When should users open the drawer?

| User intent | Tab |
|-------------|-----|
| “Where am I in setup?” | Setup (Beginner) |
| “What's enabled?” | Modules |
| “What's my plan status?” (when Mission isn't billing) | Billing |
| “What else could I improve?” | Suggestions |
| “Where's documentation?” | Help |

Drawer is **never auto-expanded** on page load v1.

---

## 13. Desktop wireframe — 1440×900

**Chrome:** Topbar 64px + page padding 48px vertical = **788px usable**

### 13.1 Veteran — ticket backlog mission

```
┌─ TOPBAR 64px ──────────────────────────────────────────────────────────────────────────────┐
│ ☰   Home / Aurora Community / Overview                                                       │
│     Aurora Community                                                                         │
│     Pro · ● Online · Synced 2h ago                              [ Open Discord ]  [⋮]  [👤] │
└──────────────────────────────────────────────────────────────────────────────────────────────┘
│ ← 24px page padding top
│
│ ┌─ MISSION CARD 160px ─ card-elevated card-status is-warning ─────────────────────────────┐
│ │ ▌ 14 open tickets waiting                                                                │
│ │   Review support requests before they pile up.                                           │
│ │   [ Review tickets ]                                                                     │
│ └──────────────────────────────────────────────────────────────────────────────────────────┘
│ ← 24px gap
│ ┌─ PULSE 72px ─ border top+bottom ─────────────────────────────────────────────────────────┐
│ │  HEALTH    MEMBERS   OPEN TKT   LOGS TODAY   BOT       MODULES                           │
│ │   92         1,240      14          82       Online    5 of 8 active                     │
│ └──────────────────────────────────────────────────────────────────────────────────────────┘
│ ← 32px gap
│  Recent activity                                                    View all logs →
│  TODAY
│  🎫  Ticket #1042 opened                                                          2h ago
│  ─────────────────────────────────────────────────────────────────────────────────────────
│  📋  Tickets module enabled                                                       5h ago
│  ─────────────────────────────────────────────────────────────────────────────────────────
│  📝  Welcome message configured                                               Yesterday
│  ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ above-the-fold boundary (~788px) ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─
│ ▼ Show details          Setup · Modules · Billing · Help
│
│  (scroll)
│  🎫  Ticket #1038 closed                                                          2d ago
│  ...
└──────────────────────────────────────────────────────────────────────────────────────────────┘
```

*Note: Wireframe uses emoji for readability only — implementation uses `app-ui-icon`.*

### 13.2 Veteran — everything operational

```
│ ┌─ MISSION CARD 140px ─ neutral, no border accent ────────────────────────────────────────┐
│ │ ✓  Everything looks good                                                                 │
│ │    No action required. Community health: 92.                                             │
│ │    (no button — intentional calm)                                                        │
│ └──────────────────────────────────────────────────────────────────────────────────────────┘
│ [ pulse row — all neutral colors ]
│ [ activity — user opens daily for narrative, not nagging ]
```

### 13.3 Beginner — setup configure

```
│ ┌─ MISSION CARD 180px ─ is-info ──────────────────────────────────────────────────────────┐
│ │ ▌ Configure your first module                                                            │
│ │   Enable welcome, tickets, or logs to deliver value.                                     │
│ │   [ Open settings ]                                                                      │
│ │   Connect ──●── Configure ─── First win    (phase progress footer)                       │
│ └──────────────────────────────────────────────────────────────────────────────────────────┘
│ [ pulse: 4 columns — Setup | Health | Bot | Modules ]
```

### 13.4 Critical — bot offline

```
│ ┌─ MISSION CARD ─ is-danger ──────────────────────────────────────────────────────────────┐
│ │ ▌ Bot is disconnected                                                                    │
│ │   Members won't receive bot features until it reconnects.                                │
│ │   [ Open Discord ]                                                                       │
│ └──────────────────────────────────────────────────────────────────────────────────────────┘
│ [ pulse: Bot cell shows Offline in warning color; Health may show — ]
```

---

## 14. Tablet wireframe — 768×1024

**Independent layout — not scaled desktop.**

```
┌───────────────────────────────┐
│ TOPBAR (plan+bot line wraps)  │
├───────────────────────────────┤
│ MISSION CARD full width       │
│ [ CTA full width optional ]   │
├───────────────────────────────┤
│ PULSE 2×3 grid  (~108px)      │
│ ┌──────────┬──────────┐       │
│ │ Health 92│ Open 14  │       │
│ ├──────────┼──────────┤       │
│ │ Logs 82  │ Bot On   │       │
│ ├──────────┼──────────┤       │
│ │ Mod 5/8  │ Mem 1240 │       │
│ └──────────┴──────────┘       │
├───────────────────────────────┤
│ Recent activity               │
│ TODAY                         │
│ row                           │
│ row                           │
├───────────────────────────────┤
│ ▼ Show details                │
└───────────────────────────────┘
```

| Breakpoint | 768px – 1023px |
|------------|----------------|
| Mission CTA | May use `.btn-block` on narrow tablet |
| Drawer expand | Inline expand OR bottom sheet 50vh (team choice at MC-8) |
| Activity sticky header | Optional |

---

## 15. Mobile wireframe — 375×812

**Separate design order — never 2-column grid.**

```
┌─────────────────┐
│ TOPBAR compact  │
│ (⋮ has Discord) │
├─────────────────┤
│ MISSION CARD    │
│ Title           │
│ Body            │
│ [ CTA block ]   │
├─────────────────┤
│ ← scroll snap → │
│ Health│Open│Bot│
├─────────────────┤
│ Recent activity │
│ row × 5 max     │
├─────────────────┤
│ ▼ Show details  │
└─────────────────┘
```

| Rule | Value |
|------|-------|
| Section order | Mission → Pulse → Activity → Drawer |
| Pulse | Horizontal scroll snap, 140px cells, 80px height |
| Touch targets | CTA min 44px height |
| Topbar | Open Discord moved to overflow menu |
| Max sections before scroll | 4 |

---

## 16. Widget states — Empty / Loading / Error

### 16.1 Page-level loading

Skeleton layout matching final geometry:

| Zone | Skeleton |
|------|----------|
| Zone 1 | Three caption bars in topbar |
| Zone 2 | Rectangle 160px, `--radius-lg`, shimmer |
| Zone 3 | Row of 6 equal bars, 72px |
| Zone 4 | Header + 3 row bars, 56px each |
| Zone 5 | Single bar 48px |

Root: `aria-busy="true"`. Live region: “Loading mission control…”

**No full-page spinner-only.**

### 16.2 Page-level error

When overview API fails entirely:

- Use `<app-empty-state>` full width  
- Title: “Unable to load overview”  
- Description: “Check your connection and try again.”  
- CTA: “Retry” (primary)

### 16.3 Mission Card states

| State | UI |
|-------|-----|
| **Loading** | Skeleton inside card shell with severity border neutral |
| **Error** | Inline in card: “Unable to load mission” + ghost “Retry” — Pulse + Activity still render from partial data |
| **Empty** | N/A — resolver always emits a mission including `EverythingOperational` |

### 16.4 Pulse states

| State | UI |
|-------|-----|
| **Loading** | Shimmer in each cell |
| **Error** | Show `—` per cell; optional caption “Some metrics unavailable” below strip (once, not per cell) |
| **Empty** | Zero values display as `0` — not empty state component |

### 16.5 Activity states

| State | UI |
|-------|-----|
| **Loading** | 3 skeleton rows |
| **Error** | Single line: “Unable to load activity” + ghost Retry |
| **Empty** | Inline (no nested card): “No recent activity. Events appear when tickets open or modules change.” + ghost link “View logs” |

### 16.6 Drawer states

| State | UI |
|-------|-----|
| **Loading** | Tabs visible; panel shows 2 skeleton lines |
| **Error** | “Unable to load details” in panel |
| **Empty Suggestions** | “No suggestions right now.” |
| **Empty Setup** | Should not occur in Beginner — if no steps: link to `/servers` |

---

## 17. Accessibility

| Requirement | Spec |
|-------------|------|
| **Landmarks** | Main content `role="main"`; Mission Card `role="region" aria-label="Mission control"` |
| **Tab order** | Mission CTA → dismiss (if any) → Pulse links → Activity rows → Drawer toggle → Drawer tabs |
| **Focus visible** | PP-001 `:focus-visible` on all interactives |
| **Screen reader — all clear** | Announce: “Everything looks good. No action required.” |
| **Screen reader — critical** | `aria-live="assertive"` on Mission Card when severity changes to critical |
| **Activity rows** | `<a>` or `button` with descriptive `aria-label` |
| **Pulse** | `role="list"` / `role="listitem"`; values not color-only |
| **Drawer** | `aria-expanded` on toggle; tablist pattern for tabs |
| **Reduced motion** | All §6 rules |
| **Contrast** | WCAG 2.1 AA — body on `--color-bg-card`, captions on muted still ≥ 4.5:1 |
| **Touch** | 44×44px min on mobile CTAs and drawer toggle |

---

## 18. RTL review (Arabic)

| Element | Rule |
|---------|------|
| Mission border | `border-inline-start` — mirrors correctly |
| Mission layout | Icon inline-start; dismiss inline-end |
| CTA | Inline-start aligned (right side in RTL) |
| Pulse grid | Same column order; numbers may stay LTR inside cells |
| Activity | Icon inline-start; time inline-end |
| Drawer tabs | Order follows `dir`; chevron mirrors |
| Relative time | `@angular/localize` plural rules |
| **Blocker** | All activity messages via i18n keys — no English API leakage |

**QA gate:** Manual RTL pass before merge (MC-10).

---

## 19. Copy namespace

All new strings under `overview.v2.*` — see PR-002 v2 for key list. UI-001 adds:

| Key | EN |
|-----|-----|
| `overview.v2.loading` | Loading mission control… |
| `overview.v2.error.title` | Unable to load overview |
| `overview.v2.error.retry` | Retry |
| `overview.v2.mission.error` | Unable to load mission |
| `overview.v2.activity.error` | Unable to load activity |
| `overview.v2.dismiss.snoozed` | Reminder snoozed for 7 days |
| `overview.v2.staff.calm.title` | No urgent actions for your role |
| `overview.v2.staff.calm.body` | Check recent activity below for ticket updates. |

EN/AR parity mandatory before ship.

---

## 20. Analytics (design bindings)

| Event | Trigger |
|-------|---------|
| `MissionControlViewed` | Page load — `mode`, `missionId`, `severity` |
| `MissionCtaClicked` | Primary CTA — `missionId`, `route` |
| `MissionDismissed` | Snooze — `missionId`, `policy` |
| `PulseCellClicked` | If links enabled |
| `ActivityRowClicked` | `activityType`, `targetId` |
| `DrawerExpanded` | Toggle open |
| `DrawerTabViewed` | `tabKey` |

---

## 21. Backend contract (design dependency)

UI consumes single DTO — resolver lives server-side (PX-002). See PR-002 v2 § Backend specification.

**Activation fix (mandatory):**

```
Mode = firstValueAchieved ? Veteran : Beginner
isActivated = firstValueAchieved   // NOT progressPercent >= 85
```

---

## 22. Definition of done (design acceptance)

- [ ] 1440×900 above-fold test passes — Veteran, Beginner, critical, all-clear  
- [ ] Exactly one `.btn-primary` visible per viewport state  
- [ ] Five zones only — no removed patterns  
- [ ] Every MissionId in §9 has EN + AR keys  
- [ ] Activity 100% i18n structured params  
- [ ] All widgets have loading + error; activity has empty  
- [ ] RTL manual pass  
- [ ] 5 guild owners — 10-second comprehension test  
- [ ] Staff persona variant reviewed  

---

## 23. Top 20 implementation tasks (priority order)

| # | ID | Task | Est. | Depends |
|---|-----|------|------|---------|
| 1 | **UI-001-01** | Mission Engine backend: PX-002 resolver, `Mission` DTO, activation fix (`firstValueAchieved`) | 4d | — |
| 2 | **UI-001-02** | Activity API: replace raw `Message` with `Type` + `Params` for i18n | 2d | — |
| 3 | **UI-001-03** | Add `.card-status.is-danger` to PP-001 for critical missions | 0.5d | — |
| 4 | **UI-001-04** | Topbar Zone 1 status extension component (plan · bot · sync) | 1d | 01 |
| 5 | **UI-001-05** | Mission Card component — full state matrix §9, dismiss/snooze | 3d | 01, 03 |
| 6 | **UI-001-06** | Beginner 3-phase progress footer inside Mission Card | 1d | 05 |
| 7 | **UI-001-07** | Community Pulse component — Veteran 6-col + Beginner 4-col | 2d | 01 |
| 8 | **UI-001-08** | Pulse API fields: `memberCount`, `logsTodayCount` (or documented fallbacks) | 1d | 01 |
| 9 | **UI-001-09** | Activity Timeline component — grouped rows, icons, i18n messages | 3d | 02 |
| 10 | **UI-001-10** | Context Drawer — collapsed default, tab visibility rules | 2d | 01 |
| 11 | **UI-001-11** | Overview page assembly — replace O-002; wire 5 zones; remove dead widgets | 2d | 04–10 |
| 12 | **UI-001-12** | Loading skeletons matching §16 geometry | 1d | 11 |
| 13 | **UI-001-13** | Error / partial failure states per §16 | 1d | 11 |
| 14 | **UI-001-14** | Beginner / Veteran mode switching (UI visibility rules) | 1d | 05, 07, 10 |
| 15 | **UI-001-15** | Staff persona filtering (missions, pulse, activity, drawer) | 1d | 14 |
| 16 | **UI-001-16** | Tablet layout — Pulse 2×3 grid, optional block CTA | 2d | 11 |
| 17 | **UI-001-17** | Mobile layout — section order, scroll snap pulse, 44px targets | 2d | 11 |
| 18 | **UI-001-18** | EN/AR `overview.v2.*` keys — full parity | 2d | 05, 09 |
| 19 | **UI-001-19** | Accessibility QA — keyboard, focus, aria-live, reduced motion | 1d | 11 |
| 20 | **UI-001-20** | Delete O-002 overview code; analytics events; above-fold verification | 1d | 11–19 |

**Total estimate:** ~28 dev-days (aligns with PR-002 v2 MC-1..MC-11)

---

## 24. Document authority

| Document | Relationship |
|----------|--------------|
| **UI-001** (this file) | Visual + interaction design authority for Overview implementation |
| **PR-002 v2** | Structural IA authority — UI-001 extends with pixel-level spec |
| **PX-002** | Mission logic — UI-001 does not override resolver |
| **PX-001** | Experience principles — UI-001 must pass checklist §20 |
| **PP-001** | Tokens/components — UI-001 uses existing classes |

If engineering conflicts with UI-001 visual spec, **UI-001 wins** until UI-001.1 amendment.

---

*UI-001 — Mission Control Overview Design Specification. No code. Product design deliverable for implementation sprint.*
