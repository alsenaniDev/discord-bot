# PR-002 v2 — Mission Control Overview (Final Design Specification)

**Specification ID:** PR-002-v2  
**Date:** 2026-07-03  
**Status:** **Approved design authority** — implementation may begin after CTO sign-off on this document  
**Supersedes:** [PR-002 Overview Redesign](./overview-redesign-review.md) · [PR-003 Critique](./overview-redesign-critique.md) · O-002 Overview UI  
**Scope:** `/guilds/:id/overview` — Guild owner and staff personas  
**Benchmark:** Linear Home · GitHub Repository Overview · Vercel Project Dashboard · Stripe Business Overview  
**Anti-patterns:** Grafana · AdminLTE · Bootstrap admin templates · widget dashboards  

---

## Executive summary

The Guild Overview is **Mission Control** — the first page a server owner opens every day to answer: *What is the state of my community, and what should I do next?*

This specification **replaces** PR-002 and incorporates PR-003 critique. It reduces the page to **5 visual zones**, enforces an **above-the-fold contract** (1440×900, no scroll for understanding), and introduces a **dynamic Mission Card (Hero)** with exactly **one CTA** per state.

**Design target:** **9/10** for closed beta Mission Control. **10/10** requires O-003 Welcome Wizard and production analytics (out of scope for Overview UI alone).

### North star question

> *If I owned this Discord server, would this page become the first thing I open every day?*

**Answer after v2:** Yes — because it delivers **awareness, confidence, attention, next action, and operational status** in one viewport. History lives below the fold.

### What changed from PR-002 → v2

| PR-002 (rejected) | PR-002 v2 (this spec) |
|-------------------|----------------------|
| 9 stacked sections | **5 zones maximum** |
| Separate alerts + hero + setup | **One Mission Card** (adaptive) |
| Health ring + metric chips | **Community Pulse** — one compact row |
| Permanent modules/subscription cards | **Context Drawer** only |
| Shortcuts + resources footer | **Removed** |
| Scroll to understand | **1440×900 above-the-fold contract** |
| Same layout for all users | **Beginner vs Veteran modes** |

---

## Design philosophy

### Mission Control personality

| Is | Is not |
|----|--------|
| Awareness | Charts everywhere |
| Confidence | Score gamification |
| Attention | Badge Christmas trees |
| Next action | Competing CTAs |
| Operational status | Configuration dumps |

### Core principles

1. **One decision per visit** — Hero resolves the highest-priority action.  
2. **Scrolling is for history** — Pulse + Hero + start of Activity fit above the fold.  
3. **Secondary lives in the drawer** — Modules, billing, setup checklist, docs never dominate.  
4. **Page evolves with maturity** — Beginner mode → Veteran mode automatically.  
5. **No duplication** — If sidebar or topbar already shows it, Overview does not repeat it.  
6. **Calm premium** — Large whitespace, few borders, typography-led hierarchy (PP-001 DS).

---

## Final information architecture

### Five zones (maximum — not negotiable)

```
┌─────────────────────────────────────────────────────────────┐
│ ZONE 1 — Top Status Bar (extends app topbar, not a card)   │
├─────────────────────────────────────────────────────────────┤
│ ZONE 2 — Mission Card (Hero) — largest component, 1 CTA    │
├─────────────────────────────────────────────────────────────┤
│ ZONE 3 — Community Pulse — compact metric strip            │
├─────────────────────────────────────────────────────────────┤
│ ZONE 4 — Recent Activity — timeline (scroll begins here)     │
├─────────────────────────────────────────────────────────────┤
│ ZONE 5 — Context Drawer — collapsed by default              │
└─────────────────────────────────────────────────────────────┘
```

### Zone responsibilities

| Zone | Job | Always visible? |
|------|-----|-----------------|
| **1 — Top Status Bar** | Orient: plan, bot, sync — no guild name duplicate | Yes (topbar extension) |
| **2 — Mission Card** | Decide: single priority action or “all clear” | Yes |
| **3 — Community Pulse** | Scan: 5–6 equal-height operational metrics | Yes |
| **4 — Recent Activity** | Remember: what happened | Yes (header above fold; list scrolls) |
| **5 — Context Drawer** | Deep context on demand | Collapsed default; expand user-initiated |

### Permanently removed (do not implement)

- Quick Actions grid / shortcuts row  
- Resources footer  
- Large Recommendations section  
- Standalone Setup / Activation card  
- Health ring / circular gauges  
- Large Subscription card  
- Large Modules list card  
- Duplicate guild header card  
- Duplicate status badges (health + setup % in header)  
- Activity sidebar column  
- Anything that duplicates sidebar navigation  

---

## Above-the-fold contract

**Viewport:** 1440×900 CSS pixels (13" laptop baseline)  
**Chrome budget:** Topbar 64px + dashboard content padding 48px vertical = **788px usable**

### Must be visible without scrolling

| Element | Max height budget |
|---------|-------------------|
| Zone 1 — Status extension (subtitle line) | 24px |
| Zone 2 — Mission Card | 160–200px |
| Zone 3 — Community Pulse | 72px |
| Zone 4 — Activity header + first 2–3 rows | ~200px |
| **Total** | **≤ 788px** |

### Below the fold (scroll)

- Remaining activity rows (4–8 more)  
- Expanded Context Drawer content  

### Acceptance test

A guild owner in **Veteran mode** on 1440×900 must answer without scrolling:

1. Is my community healthy? → Pulse + Mission Card  
2. What requires attention? → Mission Card  
3. What happened recently? → First 2–3 activity rows  
4. What should I do next? → Mission Card CTA (or “No action required”)  
5. Subscription status? → Pulse or Mission Card (if actionable)  
6. Modules active? → Pulse “Modules” metric + Drawer  

---

## Zone 1 — Top Status Bar

### Placement

**Not a page section.** Extend existing dashboard topbar subtitle (`dashboard-layout`) when route = Overview.

### Content (single line, `.type-caption`)

```
{PlanName} · {BotStatus} · {SyncStatus}
```

| Token | Display | Example |
|-------|---------|---------|
| PlanName | Plan badge text only | `Pro` |
| BotStatus | Dot + label | `● Online` / `○ Offline` |
| SyncStatus | Relative time | `Synced 2h ago` / `Not synced` |

### Rules

- **No guild name** — topbar `h1` already shows guild from server switcher context  
- **No health score** — lives in Pulse  
- **No setup %** — lives in Mission Card (Beginner mode)  
- **No avatar** — server switcher covers identity  
- Actions: keep existing **Open in Discord** + overflow `⋮` (Sync, Copy guild ID) in topbar end — not Overview body  

### Staff persona

Same line; hide plan name if `!canManageSubscription` → show `Staff access · Bot · Sync`

---

## Zone 2 — Mission Card (Dynamic Hero)

### Purpose

The **only** primary decision surface. One title, one sentence, **one CTA** (or explicit no-action state).

### Visual spec

- Component: `.card.card-elevated` — largest card on page  
- Min height: 140px · Max: 200px  
- Padding: `--space-6`  
- Layout: title (`.type-section-title`) · body (`.type-subtitle`, one line max) · CTA row (single `.btn-primary` OR no button)  
- Optional: thin progress bar **inside** card bottom edge (Beginner mode only, 4px height)  
- **No** priority overline · **No** “Est. 2 min” · **No** secondary button (use drawer for alternates)  

### Hero state resolver (backend)

`GuildOverviewExperienceService` returns **one** `MissionState` object:

```typescript
interface MissionState {
  stateKey: string;           // enum — see matrix
  titleKey: string;           // i18n
  bodyKey: string;            // i18n
  bodyParams?: Record<string, string | number>;
  ctaKey?: string;            // i18n — omit when no action
  ctaRoute: string;           // dashboard route or external
  severity: 'neutral' | 'info' | 'warning' | 'critical';
  showProgress?: boolean;     // beginner setup bar
  progressPercent?: number;
}
```

**Precedence (first match wins):**

1. `critical` — Bot offline  
2. `critical` — Payment rejected (subscription change)  
3. `warning` — Subscription expiring ≤7 days  
4. `warning` — Sync stale >7 days (only if bot online)  
5. `warning` — Ticket backlog threshold (≥10 open OR ≥5 open >48h — configurable)  
6. `info` — Beginner: current setup phase incomplete  
7. `info` — Single top recommendation (if score ≥ threshold)  
8. `neutral` — All clear (veteran healthy state)  

**Never stack.** Never show alert banner + Mission Card for same issue.

---

## Hero State Matrix

| stateKey | Mode | Severity | Title (EN) | Body (EN) | CTA (EN) | Route |
|----------|------|----------|------------|-----------|----------|-------|
| `botOffline` | Both | critical | Bot is disconnected | Members won't receive bot features until it reconnects. | Reconnect in Discord | external Discord |
| `paymentRejected` | Owner | critical | Subscription change declined | Review the reason and update your payment reference. | View billing | subscription |
| `subscriptionExpiring` | Owner | warning | Subscription expires in {days} days | Renew to keep paid modules active. | Renew plan | subscription |
| `syncStale` | Both | warning | Server data is outdated | Sync to refresh channels and roles in the dashboard. | Sync now | sync action |
| `ticketBacklog` | Both | warning | {count} open tickets waiting | Review support requests before they pile up. | Review tickets | tickets |
| `setupPhaseConnect` | Beginner | info | Finish connecting your server | Invite the bot and run setup in Discord. | Start setup | /servers or wizard |
| `setupPhaseConfigure` | Beginner | info | Configure your first module | Enable welcome, tickets, or logs to deliver value. | Open settings | settings |
| `setupPhaseFirstWin` | Beginner | info | Get your first win | Open a ticket or send a welcome message to complete setup. | Continue setup | dynamic step route |
| `recommendation` | Both* | info | {recommendation.title} | {recommendation.description} | {recommendation.cta} | rec route |
| `allClear` | Veteran | neutral | Everything looks good | No action required. Community health: {score}. | *(no button)* | — |

\*Beginner: recommendations only if no setup phase active. Veteran: recommendations if no warning/critical states.

### Beginner Mission Card — progress bar

When `stateKey` starts with `setupPhase`:

- Show 3-phase progress inside card footer: **Connect · Configure · First win**  
- Highlight current phase (not 8 micro-steps)  
- `progressPercent` maps to overall setup weight (existing backend weights collapsed to 3 phases)  

| Phase | Includes steps |
|-------|----------------|
| Connect | addBot, linkGuild |
| Configure | enableModule, configureModule |
| First win | firstValue |

---

## Beginner mode

### Entry condition

`firstValueAchieved === false` OR `setupComplete === false` (setup ≥85% config but no first value still = Beginner until first win)

### Page behavior

| Zone | Behavior |
|------|----------|
| Mission Card | Setup-focused states take precedence over recommendations |
| Pulse | Show **subset**: Bot · Setup % · Health · Modules — hide ticket backlog emphasis until tickets enabled |
| Activity | Show last 3 events only above fold; de-emphasize empty state copy |
| Drawer default | **Setup checklist** tab active when opened |
| Hidden | Ticket backlog hero (unless tickets module enabled + configured) · Veteran “all clear” copy |

### Copy tone

Direct, instructional, single outcome per card. No motivational fluff.

---

## Veteran mode

### Entry condition

`firstValueAchieved === true`

### Page behavior

| Zone | Behavior |
|------|----------|
| Mission Card | Operational states only (bot, billing, backlog, all clear) — **no setup phases** |
| Pulse | Full 6-metric strip |
| Activity | Primary scroll content — up to 8 items visible before “View all logs” |
| Drawer default | **Modules** or **Subscription** tab only if actionable; else collapsed |
| Hidden | Setup checklist · Beginner recommendations · setup progress bar |

### “All clear” state

When no warning/critical and no high-score recommendation:

- Title: **Everything looks good**  
- Body: **No action required. Community health: {score}.**  
- **No CTA button** — absence of button is intentional (confidence, not emptiness)  
- Pulse provides operational numbers; Activity provides narrative  

This is the **daily open** experience — calm, not nagging.

---

## Zone 3 — Community Pulse

### Purpose

Replace health ring, stats card, and metric chips with **one horizontal strip** of equal-height cells.

### Visual spec

- **Not a card** — borderless row, subtle top/bottom divider (`1px solid var(--color-border)`)  
- Height: **72px** fixed  
- Layout: CSS grid, `grid-auto-flow: column`, equal columns, min-width 0  
- Each cell: label (`.type-overline`) + value (`.type-section-title` or `.type-caption` for status words)  
- **No circles, no graphs, no sparklines v1**  

### Metrics (desktop — 6 cells)

| Cell | Label (EN) | Value source | Example |
|------|------------|--------------|---------|
| Health | HEALTH | `experience.health.score` + level word | `92` / `Good` |
| Members | MEMBERS | `overview.memberCount` *(new API field if missing — or omit v1)* | `1,240` |
| Open tickets | OPEN TICKETS | `overview.openTickets` | `5` |
| Logs today | LOGS TODAY | count from API *(new)* or `—` if unavailable | `82` |
| Bot | BOT | Online/Offline | `Online` |
| Modules | MODULES | `{enabled} of {total} active` | `5 of 8 active` |

**v1 fallback:** If `memberCount` or `logsToday` unavailable, show **Warnings** (moderation case count) or **Staff** (permission roles count) — spec backend in IM-1.

### Subscription in Pulse (not separate card)

When `subscriptionExpiring` OR `free plan`:

Replace **Modules** cell OR add inline suffix on Plan in Zone 1:

- Pulse cell: **PLAN** · `Pro` · `14d left` · text link `Renew` (not button — Mission Card owns primary CTA)  

When healthy paid plan: Plan stays in Zone 1 only.

### Beginner pulse (4 cells)

`Setup` · `Health` · `Bot` · `Modules` — same strip component, fewer columns.

### Click behavior

Each cell is **optional link** to relevant page (Health → drawer Health tab; Tickets → tickets). Not required v1.

---

## Zone 4 — Recent Activity

### Purpose

**Heart of the page below the fold** — owner feels: *“I know exactly what happened.”*

### Visual spec (GitHub / Linear inspired)

- Section header: `.type-card-title` **Recent activity** + text link **View all logs →** (inline-end)  
- Timeline list: full width, no card wrapper (divider-separated rows)  
- Row height: ~56px  
- Row layout: `[icon 20px] [message flex] [relative time 80px]`  
- Max items: **8** desktop · **5** mobile  
- Day group headers: `.type-overline` — **Today**, **Yesterday**, **Earlier**  

### Row content

| Field | Spec |
|-------|------|
| Icon | `app-ui-icon` by `activityType` — ticket, module, log, moderation |
| Message | i18n from `type` + `params` — **never raw API English string** |
| Time | Relative (`2h ago`) with `<time datetime>` + tooltip absolute on hover |
| Link | Entire row clickable → ticket detail, logs filtered, or modules |

### Activity types (i18n keys)

| type | EN template |
|------|-------------|
| `TicketCreated` | Ticket #{number} opened |
| `TicketClosed` | Ticket #{number} closed |
| `TicketReply` | Reply sent on ticket #{number} |
| `ModuleEnabled` | {moduleName} enabled |
| `ModuleDisabled` | {moduleName} disabled |
| `LogEntry` | {summary} |
| `MemberWarned` | Warning issued *(future)* |
| `StaffAdded` | Staff role mapped *(future)* |

### Empty state

Single line inside timeline area — no emoji, no nested card:

**No recent activity.** Events appear when tickets open or modules change.  
Link: **View logs**

### Staff persona

Same feed; filter to tickets + logs if `!canManageModules`.

---

## Zone 5 — Context Drawer

### Purpose

All **secondary** information — collapsed by default, never competes with Mission Card.

### Visual spec

- Collapsed: single row bar, 48px height  
  - Text: **Setup · Modules · Billing · Help** as segmented text tabs OR chevron **Show details**  
- Expanded: max-height 320px, scroll inside, `--elevation-1` inset panel  
- **Default state: collapsed** on every page load (remember expand per session optional v2)  

### Drawer tabs

| Tab | Visible when | Content |
|-----|--------------|---------|
| **Setup** | Beginner mode only | 3-phase checklist + step links (collapsed 8 steps into phases) |
| **Modules** | Always | `5 of 8 active` + **Manage →** link — no full module list v1 |
| **Billing** | Owner + (`!IsPaid` OR expiring OR pending change) | Plan · status · expiry · **Manage billing →** |
| **Suggestions** | Veteran + has recommendations rank 2–3 | Max 2 links, no cards |
| **Help** | Always | Setup guide · Ticket docs · Support (3 links, `.type-caption`) |

### Rules

- **No Quick Actions** — deleted  
- **No Resources footer** — Help tab replaces it  
- Billing tab hidden when paid + healthy + no pending change  
- Setup tab hidden in Veteran mode  

---

## Desktop wireframe (1440×900)

```
TOPBAR (app chrome — 64px)
┌──────────────────────────────────────────────────────────────────────────┐
│ ☰  Home / My Server / Overview                                           │
│     My Awesome Server                                                    │
│     Pro · ● Online · Synced 2h ago              [Discord ↗]  [⋮]  [👤]  │
└──────────────────────────────────────────────────────────────────────────┘

CONTENT (788px usable — NO SCROLL for zones 1–3 + activity header)
┌──────────────────────────────────────────────────────────────────────────┐
│ ┌─ MISSION CARD (elevated) ─────────────────────────────────────────────┐ │
│ │  14 open tickets waiting                                              │ │
│ │  Review support requests before they pile up.                         │ │
│ │  [ Review tickets ]                              ← single primary btn │ │
│ └───────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
│  HEALTH    MEMBERS   OPEN TKT   LOGS TODAY   BOT      MODULES            │
│   92        1,240       14         82       Online   5 of 8 active     │
│ ─────────────────────────────────────────────────────────────────────── │
│  Recent activity                                    View all logs →      │
│  TODAY                                                                   │
│  🎫  Ticket #1042 opened                                    2h ago       │
│  📋  Tickets module enabled                                 5h ago       │
│  📝  Welcome message configured                           Yesterday     │
│  ▼ Show details — Setup · Modules · Billing · Help                       │
└──────────────────────────────────────────────────────────────────────────┘
     ↑ above-the-fold boundary ≈ row 3 of activity

── scroll ──
│  🎫  Ticket #1038 closed                                    2d ago       │
│  ... (up to 8 items)                                                     │
│  [expanded drawer content if user opened]                                │
└──────────────────────────────────────────────────────────────────────────┘
```

### All-clear veteran wireframe (above fold)

```
┌─ MISSION CARD (neutral, no button) ─────────────────────────────────────┐
│  Everything looks good                                                   │
│  No action required. Community health: 92.                               │
└──────────────────────────────────────────────────────────────────────────┘
[pulse row]
[activity starts — user opens daily for activity, not nagging]
```

---

## Tablet wireframe (768–1023px)

**Independent layout — not scaled desktop.**

- Topbar: plan + bot on one line; sync wraps  
- Mission Card: full width, 160px min height  
- Pulse: **2×3 grid** (2 rows, 3 columns) — 108px total height  
- Activity: full width  
- Drawer: collapsed bar full width; expanded = 50vh max overlay sheet from bottom (optional) OR inline expand  

```
┌─────────────────────────────┐
│ MISSION CARD                │
├─────────────────────────────┤
│ Health 92  │ Open tkt 14    │
│ Logs 82    │ Bot Online     │
│ Modules 5/8│ Members 1240   │
├─────────────────────────────┤
│ Recent activity             │
│ ...                         │
├─────────────────────────────┤
│ ▼ Show details              │
└─────────────────────────────┘
```

---

## Mobile wireframe (≤767px)

**Designed separately.** Order:

1. Mission Card (full width, CTA full-width button)  
2. Pulse (horizontal **scroll snap** strip, 4 cells visible, 80px height)  
3. Activity (5 items max)  
4. Drawer (collapsed; expand pushes content — not modal v1)  

**Removed on mobile:** topbar duplicate actions move to `⋮` menu only.

```
┌─────────────────┐
│ MISSION         │
│ [ full CTA    ] │
├─────────────────┤
│ ← Health Open … →│  (scroll snap pulse)
├─────────────────┤
│ Recent activity │
│ row             │
│ row             │
├─────────────────┤
│ ▼ Details       │
└─────────────────┘
```

**No 2-column grid. Ever.**

---

## Staff persona overview

| Zone | Staff view |
|------|------------|
| Status bar | No plan · Staff access |
| Mission Card | Ticket backlog · bot offline only — no billing/setup |
| Pulse | Open tickets · Logs today · Bot · (hide modules if no access) |
| Activity | Tickets + logs events |
| Drawer | Help tab only (+ Suggestions if ticket-related) |

Separate wireframe sign-off required before ship — same IA, filtered data.

---

## Visual language

### Alignment with PP-001 design system

Use existing tokens and components. **Do not invent Overview-specific button styles.**

| Element | Class / token |
|---------|---------------|
| Mission Card | `.card.card-elevated`, `--elevation-2` |
| Pulse | borderless, `--color-border` dividers |
| Activity | no card — list dividers |
| Drawer | `.surface-inset` when expanded |
| Typography | `.type-section-title`, `.type-subtitle`, `.type-overline`, `.type-caption` |
| Primary CTA | `.btn.btn-primary` — **max one per page** |
| Severity | Mission Card left border: `--color-error/warning/info` via `.card-status` |

### Aesthetic

- **Minimal** — 3 border lines on full page (pulse dividers + drawer)  
- **Large whitespace** — `--space-6` between Mission Card and Pulse; `--space-8` before Activity  
- **Few colors** — neutral surface + one semantic accent on Mission Card  
- **Calm** — veteran all-clear is intentionally quiet  
- **No emoji** in activity or empty states — icons only  

---

## Copy review (production strings)

### Namespace: `overview.v2.*` (new keys — do not overload O-002 keys blindly)

#### Mission Card

| Key | EN | AR direction |
|-----|-----|--------------|
| `mission.botOffline.title` | Bot is disconnected | Mirror |
| `mission.botOffline.body` | Members won't receive bot features until it reconnects. | Mirror |
| `mission.botOffline.cta` | Open Discord | Mirror |
| `mission.paymentRejected.title` | Subscription change declined | Mirror |
| `mission.paymentRejected.body` | Review the reason and resubmit your payment reference. | Mirror |
| `mission.paymentRejected.cta` | View billing | Mirror |
| `mission.subscriptionExpiring.title` | Subscription expires in {{days}} days | Mirror with plural rules |
| `mission.subscriptionExpiring.body` | Renew to keep paid modules active. | Mirror |
| `mission.subscriptionExpiring.cta` | Renew plan | Mirror |
| `mission.syncStale.title` | Server data is outdated | Mirror |
| `mission.syncStale.body` | Sync to refresh channels and roles. | Mirror |
| `mission.syncStale.cta` | Sync now | Mirror |
| `mission.ticketBacklog.title` | {{count}} open tickets waiting | Mirror |
| `mission.ticketBacklog.body` | Review support requests before they pile up. | Mirror |
| `mission.ticketBacklog.cta` | Review tickets | Mirror |
| `mission.setupConnect.title` | Finish connecting your server | Mirror |
| `mission.setupConnect.body` | Invite the bot and complete setup in Discord. | Mirror |
| `mission.setupConnect.cta` | Start setup | Mirror |
| `mission.allClear.title` | Everything looks good | Mirror |
| `mission.allClear.body` | No action required. Community health: {{score}}. | Mirror |

#### Pulse labels

| Key | EN |
|-----|-----|
| `pulse.health` | Health |
| `pulse.members` | Members |
| `pulse.openTickets` | Open tickets |
| `pulse.logsToday` | Logs today |
| `pulse.bot` | Bot |
| `pulse.modules` | Modules |
| `pulse.botOnline` | Online |
| `pulse.botOffline` | Offline |
| `pulse.modulesActive` | {{enabled}} of {{total}} active |

#### Activity

| Key | EN |
|-----|-----|
| `activity.title` | Recent activity |
| `activity.viewAll` | View all logs |
| `activity.today` | Today |
| `activity.yesterday` | Yesterday |
| `activity.earlier` | Earlier |
| `activity.empty` | No recent activity. Events appear when tickets open or modules change. |

#### Drawer

| Key | EN |
|-----|-----|
| `drawer.toggle` | Show details |
| `drawer.toggleHide` | Hide details |
| `drawer.tab.setup` | Setup |
| `drawer.tab.modules` | Modules |
| `drawer.tab.billing` | Billing |
| `drawer.tab.suggestions` | Suggestions |
| `drawer.tab.help` | Help |
| `drawer.modulesSummary` | {{enabled}} of {{total}} modules active |
| `drawer.manageModules` | Manage |
| `drawer.manageBilling` | Manage billing |

### Copy rules

- No `/setup` or snowflake in user strings  
- No “Est. X min”  
- No “High priority” labels  
- CTA = verb + object (`Review tickets`, not `Go to tickets`)  
- **EN/AR parity mandatory** before merge  

---

## Accessibility

| Requirement | Implementation |
|-------------|----------------|
| **Contrast** | Mission Card text ≥ WCAG AA on `--color-bg-card`; critical border not sole indicator |
| **Keyboard** | Tab order: Mission CTA → Pulse links (if any) → Activity rows → Drawer toggle → Drawer tabs |
| **Focus** | `:focus-visible` on all interactive rows; drawer trap focus when expanded (optional v1: no trap if short) |
| **Screen readers** | Mission Card: `role="region"` `aria-label="Mission control"`; all-clear announces no action needed |
| **Live regions** | Sync completes: `aria-live="polite"` toast only — not page reload surprise |
| **Reduced motion** | No slide-up on Mission Card; drawer expand uses height only, no animation if `prefers-reduced-motion` |
| **Activity** | Each row: `aria-label="{message}, {time}"` |
| **Pulse** | Grid `role="list"` cells `role="listitem"` |

---

## RTL review

| Element | Rule |
|---------|------|
| Topbar status line | Logical order: plan · bot · sync |
| Mission Card | CTA inline-start aligned; status border `border-inline-start` |
| Pulse grid | Same column order — numbers LTR inside cells OK |
| Activity | Icon inline-start; time inline-end |
| Drawer tabs | Horizontal tab order follows `dir` |
| Relative time | Use Angular i18n plural + locale pipes |

**Blocker:** Activity i18n structured params — same as PR-002 OV-003, now **P0** in v2.

---

## Loading, empty, and error states

### Loading

- Skeleton: Mission Card rectangle (160px) + pulse bar (72px) + 3 activity rows  
- **No** full-page spinner-only  
- `aria-busy="true"` on overview root; `aria-live="polite"` “Loading mission control…”  

### Error

- Full page: existing empty state + retry (overview load fail)  
- Partial: Mission Card shows **Unable to load recommendations** with retry inline — Pulse + Activity still render from base overview API  

### Empty

- Activity: one line + link (see Zone 4)  
- Drawer tabs: inline empty — “No suggestions right now”  
- **No** emoji empty-state cards  

---

## Competitive analysis (why this matches premium products)

| Product | Pattern adopted in v2 |
|---------|----------------------|
| **Linear** | One inbox / one focus → Mission Card |
| **GitHub** | Activity timeline as narrative → Zone 4 primary scroll |
| **Vercel** | Deployment status hero → dynamic Mission Card states |
| **Stripe** | Single action required → precedence resolver, no alert stack |
| **Slack** | Workspace awareness compact → Pulse strip |
| **Discord** | Push to Discord for bot reconnect → external CTA |

**Not adopted:** Grafana dashboards, AdminLTE widget grids, health rings, shortcut grids.

---

## Backend specification (DTO changes)

Extend `GuildOverviewExperienceDto`:

```csharp
public MissionStateDto Mission { get; init; }      // single hero
public CommunityPulseDto Pulse { get; init; }      // metric cells
public OverviewMode Mode { get; init; }            // Beginner | Veteran
public ContextDrawerDto Drawer { get; init; }      // tab visibility + content
// Activity items: replace Message string with Type + Params dictionary
// Activation: FirstValueAchieved bool, SetupComplete bool, CurrentPhase enum
```

### Activation logic fix (mandatory)

```csharp
var firstValueAchieved = /* existing logic */;
var setupComplete = progressPercent >= 85;
var isActivated = firstValueAchieved;  // NOT progressPercent >= 85 alone
Mode = firstValueAchieved ? OverviewMode.Veteran : OverviewMode.Beginner;
```

### New API fields (optional v1)

- `overview.memberCount` — from guild sync  
- `pulse.logsTodayCount` — count log entries today  
- `mission.pendingChangeRejected` — from subscription workflow  

---

## Analytics events

| Event | When |
|-------|------|
| `MissionControlViewed` | Page load — include `mode`, `missionStateKey` |
| `MissionCtaClicked` | Hero CTA — `stateKey`, `route` |
| `PulseCellClicked` | If implemented |
| `ActivityRowClicked` | `activityType`, `targetId` |
| `DrawerExpanded` | |
| `DrawerTabViewed` | `tabKey` |

---

## Implementation roadmap

| Phase | ID | Deliverable | Est. | Depends |
|-------|-----|-------------|------|---------|
| 1 | **MC-1** | Backend: `MissionState` resolver + activation fix + activity i18n shape | 4d | — |
| 2 | **MC-2** | Topbar status extension (Zone 1) | 1d | — |
| 3 | **MC-3** | Mission Card component + state matrix UI | 3d | MC-1 |
| 4 | **MC-4** | Community Pulse component | 2d | MC-1 |
| 5 | **MC-5** | Activity timeline rewrite | 3d | MC-1 |
| 6 | **MC-6** | Context Drawer | 2d | MC-1 |
| 7 | **MC-7** | Beginner / Veteran mode toggles | 2d | MC-3–6 |
| 8 | **MC-8** | Tablet + mobile layouts | 3d | MC-3–6 |
| 9 | **MC-9** | Staff persona filtering | 1d | MC-7 |
| 10 | **MC-10** | EN/AR copy + a11y QA + above-fold verification | 2d | All |
| 11 | **MC-11** | Remove O-002 overview UI dead code | 1d | MC-10 |

**Total:** ~24 dev-days (1 FTE + design review at MC-3 and MC-8)

### Definition of done

- [ ] 1440×900 above-fold test passes (Veteran + Beginner + critical states)  
- [ ] Exactly **one** primary CTA visible per state  
- [ ] 5 zones only — grep confirms no removed patterns in template  
- [ ] Activity 100% i18n — no raw API strings in UI  
- [ ] EN/AR key parity for `overview.v2.*`  
- [ ] `npm run build` + manual RTL pass  
- [ ] Staff persona smoke test  

---

## Final CTO decision

### Decision: **APPROVE for implementation**

PR-002 v2 resolves PR-003 blockers:

- IA cut to 5 zones  
- Alert + hero merged into Mission Card  
- Shortcuts, resources, health ring, bottom cards **removed**  
- Veteran / Beginner modes specified  
- Above-the-fold contract defined  
- Single implementation authority document  

### Conditions

1. **No scope creep** — anything not in 5 zones goes to drawer or another page.  
2. **Mission resolver lives in backend** — Angular does not compute hero precedence.  
3. **Comprehension test** — 5 guild owners, 10-second task, before MC-10 merge (recommended MC-8 gate).  
4. **Welcome Wizard (O-003)** remains separate — drawer Setup tab is bridge only.  

### Rejected alternatives

- PR-002 original 9-section layout — **do not implement**  
- Health ring — **deferred indefinitely**  
- Permanent modules/subscription cards — **rejected**  

---

## Document authority

This file **`overview-redesign-v2.md`** is the **single source of truth** for Overview implementation. If engineering plans conflict with this spec, **this spec wins** until formally amended via PR-002 v2.1 change request.

**Supersedes:**

- `docs/reviews/overview-redesign-review.md` (PR-002 v1)  
- PR-003 critique recommendations (incorporated)  
- O-002 overview layout (obsolete on merge)  

---

*PR-002 v2 — Mission Control Overview — Final design specification. No code in this document.*
