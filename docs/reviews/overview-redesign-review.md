# PR-002 — Guild Overview Redesign Review

**Review ID:** PR-002  
**Date:** 2026-07-03  
**Status:** Design proposal — **awaiting approval before implementation**  
**Owner:** Product Design + Frontend Architecture  
**Scope:** `/guilds/:id/overview` — complete product experience redesign  
**Benchmark:** Linear · Stripe Dashboard · Vercel · GitHub · Notion · Slack Admin · Discord Developer Portal  
**Related:** [PR-001 Product Audit](./product-review-001.md) · [O-001 Activation Blueprint](../ux/first-time-user-activation.md) · [O-002 Progress](../progress/2026-07-03-O-002-dashboard-overview.md) · [PP-001 Design System](../design/design-system.md)

---

## Executive summary

The current Overview (O-002) successfully replaced a static guild summary with operational widgets — health score, activation, recommendations, quick actions, and activity. It is **functionally useful for coached beta users** but **fails the 10-second comprehension test** against world-class SaaS dashboards.

**Overall score today: 5.5 / 10** (up from 4.5 pre-O-002; PP-001 improved visual consistency but did not fix product hierarchy).

**Target after PR-002 implementation: 8.5 / 10** for closed beta; 9+ requires Welcome Wizard (O-003) and live analytics.

### Core diagnosis

The page answers *many* questions but does not **prioritize one answer**. A new owner sees seven cards, five badge types, up to three recommendation rows, up to eight quick-action tiles, and a stats grid — without a single dominant “do this now” moment comparable to Vercel’s deployment status or Stripe’s “Action required” banner.

### Redesign thesis

Transform Overview from a **widget dashboard** into a **command center**:

1. **Status first** — one glance: healthy or not, bot connected or not, billing OK or not  
2. **One primary action** — hero CTA driven by highest-priority recommendation or activation step  
3. **Evidence second** — health score + compact metrics, not duplicate badges  
4. **History third** — activity feed with typed icons and localized events  
5. **Depth on demand** — modules, subscription, and secondary actions collapsed or sidebar-sized  

**No implementation begins until this document is approved.**

---

## Phase 1 — Audit of current implementation

**Reference implementation reviewed:**  
`overview.component.html` · `overview.component.ts` · `GuildOverviewExperienceService.cs` · EN/AR i18n · topbar layout · PP-001 design system classes

**Screenshot references:** Not captured in this review. Element references use **section IDs** matching current DOM structure for QA follow-up.

---

### Issue register

| ID | Severity | Category | Element / Reference | Issue | Recommendation |
|----|----------|----------|---------------------|-------|----------------|
| OV-001 | **P0** | IA | Page overall | Seven competing sections with no visual focal point; user cannot answer “what next?” in 10s | Introduce **Primary Action Hero** — single dominant CTA above the fold |
| OV-002 | **P0** | Product | Activation card + header badge | “Activated” at ≥85% config weight **without verified first value** (O-001 violation) | Gate activation badge on `firstValue` step; rename progress to “Setup progress” until then |
| OV-003 | **P0** | i18n | Activity list `.activity-message` | API emits English-only strings (`Ticket #42 opened`, `{Module} module enabled`) — breaks AR dashboard | Return `activityType` + params; render via i18n keys client-side |
| OV-004 | **P0** | IA | Missing section | Subscription status only as header badge — no expiry, renewal CTA, or change-in-progress | Add **Subscription snapshot** card when owner has billing access |
| OV-005 | **P0** | IA | Missing section | No **modules snapshot** — user cannot see which modules are active without navigating away | Add compact **Modules status** row (enabled/disabled/locked counts + link) |
| OV-006 | **P1** | Hierarchy | Topbar `h1` + `.community-header h2` | Guild name shown twice; wastes vertical space and confuses page title | Remove in-page guild name; header becomes **status strip** (avatar + badges only) |
| OV-007 | **P1** | IA | Recommendations + Quick actions | Functional overlap — both route to settings/modules/tickets; cognitive duplication | Merge into **Next steps** (ranked list) + **Shortcuts** (icon row, max 5) |
| OV-008 | **P1** | IA | Critical alerts | No banner for bot offline, sync stale, subscription expiring, payment rejected | Add **Alerts strip** (0–3 items) below header — Stripe-style |
| OV-009 | **P1** | Density | `.recommendations-card` | Full-width rows for 1–3 items; excessive vertical scroll | Max 2 visible + “View all” link; third item in drawer |
| OV-010 | **P1** | Density | `.activation-card` | Spans 2 columns but only shown pre-activation; creates layout jump | Fixed grid slot; post-activation show collapsed **“Setup complete ✓”** chip row |
| OV-011 | **P1** | CTA | `.community-header-actions` | “Sync Discord Data” is secondary, no context (last sync never shown) | Move sync to header overflow menu; show **Last synced {relative time}** in status strip |
| OV-012 | **P1** | Copy | Activation messages | Motivational but vague (“reach your first win”) — not outcome-specific | Tie message to **current step** + expected outcome in one sentence |
| OV-013 | **P1** | Visual | Health card | Plain number + text list — not scannable vs Vercel/Linear gauges | **Health ring** (score) + top 3 failing factors only; “View details” expands |
| OV-014 | **P1** | Visual | Activity feed | Plain text, no event icons, no grouping by day | Typed icons + date group headers (“Today”, “Yesterday”) |
| OV-015 | **P1** | Responsive | `.overview-grid` @900px | Desktop 2-col grid collapses to identical single column — mobile is accidental stack | Intentional mobile order (see Phase 4) |
| OV-016 | **P2** | Visual | Empty states | Emoji icons (💚 ✨ 🔒 📭) inconsistent with `app-ui-icon` elsewhere | SVG empty illustrations per widget (DS future) |
| OV-017 | **P2** | Visual | `.health-factors` | Unicode ✓/⚠ vs `app-ui-icon` in activation steps | Unified icon language |
| OV-018 | **P2** | Buttons | Recommendation CTAs | All `btn-secondary btn-sm` — no primary/danger hierarchy | Highest priority rec uses **primary** button |
| OV-019 | **P2** | Spacing | Section gaps | Uniform `gap: var(--space-4)` — no rhythm between priority tiers | **24px** between tiers, **16px** within tier (token-backed) |
| OV-020 | **P2** | Stats card | `.stats-card` | Channels/roles counts low operational value; duplicates Discord | Replace with **operational KPIs**: open tickets, staff online (future), modules enabled |
| OV-021 | **P2** | Copy | `overview.stats.title` “At a glance” | Generic; doesn’t signal purpose | “Community metrics” or merge into health row |
| OV-022 | **P2** | Copy | Unused i18n | `overview.subtitle`, `overview.statsHint`, `overview.lastSync` defined but not rendered | Use or remove in implementation |
| OV-023 | **P2** | Product | Quick actions | Reaction roles shown without plan gate in UI (backend modules check partial) | Hide or show lock state with upgrade hint |
| OV-024 | **P2** | Loading | `.overview-skeleton` | Custom skeleton not matching final grid proportions | Skeleton mirrors approved 12-col wireframe |
| OV-025 | **P2** | a11y | Skeleton phase | No `aria-live` loading announcement | `role="status"` + translated loading message |
| OV-026 | **P2** | RTL | Activity timestamps | `date:'medium'` pipe locale OK; mixed EN message + AR date awkward | Fix OV-003 first |
| OV-027 | **P3** | Motion | `.slide-up` on every card | Staggered animation on 7 cards feels sluggish | Animate hero + alerts only; rest static |
| OV-028 | **P3** | Product | Dismiss recommendations | O-001 specifies snooze/dismiss — not implemented | Snooze 7 days per recommendation ID (localStorage v1) |
| OV-029 | **P3** | Analytics | Console-only events | Cannot measure hero CTA effectiveness | Backend event sink before launch |

---

### What feels unprofessional (summary)

| Dimension | Assessment |
|-----------|------------|
| **Spacing** | Uniform gaps; no priority tiers; header card too tall |
| **Visual hierarchy** | Flat — all `h3` card titles equal weight |
| **Card order** | Activation before alerts; stats last; subscription invisible |
| **Typography** | Acceptable post-PP-001; missing `.type-*` usage on Overview |
| **Buttons** | Secondary CTAs everywhere; no hero primary |
| **Duplicated actions** | Recommendations ⊕ Quick actions ⊕ Activation CTA |
| **Empty states** | Emoji-heavy; acceptable copy but weak visual |
| **Full-width rows** | Recommendations + activity span 2 cols unnecessarily |
| **Information density** | Too sparse in health/stats; too dense in quick actions grid |
| **Icons** | Mixed SVG + unicode + emoji |
| **Mixed language** | Activity feed (P0) |
| **Alignment** | Header flex-wrap causes badge row misalignment on tablet |
| **Responsive** | Collapse-only mobile, not designed |
| **CTA placement** | Primary activation CTA buried inside conditional card |

---

## Phase 2 — Information architecture

### Design principles

1. **One screen, one story** — “Your community is {state}. Do {action} next.”  
2. **Conditional visibility** — hide setup widgets after true activation; never hide alerts  
3. **Progressive disclosure** — details expand; defaults scannable  
4. **No duplicate routes** — each destination appears once in primary hierarchy  
5. **10-second rule** — row 1–3 must answer all mission questions  

### Mission question mapping

| Question | Primary surface | Secondary |
|----------|-----------------|-----------|
| Is my community healthy? | Status strip + Health ring | Factor detail drawer |
| What requires attention? | Alerts strip + Hero CTA | Recommendations list |
| What happened recently? | Activity feed | Link to Logs |
| What should I do next? | Hero CTA | Next steps list |
| Subscription status? | Status strip badge + Subscription card | Subscription page |
| Modules active? | Modules snapshot row | Modules page |
| Continue onboarding? | Activation progress (pre-first-value) | Welcome wizard (O-003) |

---

### Proposed section order (final)

```
┌─────────────────────────────────────────────────────────────┐
│ 1. STATUS STRIP (compact header)                            │
├─────────────────────────────────────────────────────────────┤
│ 2. CRITICAL ALERTS (0–3 banners, conditional)               │
├─────────────────────────────────────────────────────────────┤
│ 3. PRIMARY ACTION HERO (1 CTA — highest priority)           │
├─────────────────────────────────────────────────────────────┤
│ 4. SETUP PROGRESS (conditional — hidden after first value)  │
├─────────────────────────────────────────────────────────────┤
│ 5. HEALTH + METRICS ROW                                       │
├─────────────────────────────────────────────────────────────┤
│ 6. NEXT STEPS (ranked 2–3) │ SHORTCUTS (icon row)           │
├─────────────────────────────────────────────────────────────┤
│ 7. RECENT ACTIVITY                                            │
├─────────────────────────────────────────────────────────────┤
│ 8. MODULES SNAPSHOT │ SUBSCRIPTION SNAPSHOT                  │
├─────────────────────────────────────────────────────────────┤
│ 9. RESOURCES (help links, docs, Discord setup)                │
└─────────────────────────────────────────────────────────────┘
```

### Why each section exists and its position

| # | Section | Why it exists | Why this position |
|---|---------|---------------|-------------------|
| 1 | **Status strip** | Instant context: who, plan, bot, health — no scrolling | Top — answers “who am I managing?” in 1s |
| 2 | **Critical alerts** | Stripe pattern — blockers must not be missed | Above fold — before any optional content |
| 3 | **Primary action hero** | Single decisive CTA — Linear/Vercel pattern | Highest interaction zone after alerts |
| 4 | **Setup progress** | Onboarding continuity (O-001) | Below hero — important for new users, not for veterans |
| 5 | **Health + metrics** | Evidence for “healthy?” without reading paragraphs | Mid-page — supports decision after action |
| 6 | **Next steps + shortcuts** | Secondary actions without competing with hero | Split row — ranked list vs muscle-memory shortcuts |
| 7 | **Recent activity** | Operational awareness | Below actions — history, not prescription |
| 8 | **Modules + subscription** | Entitlement visibility | Bottom pair — reference info, links to deep pages |
| 9 | **Resources** | Reduce support load; link setup docs | Footer — always available, never dominant |

### Sections removed or merged

| Current (O-002) | Disposition |
|-----------------|-------------|
| Community header card | → **Status strip** (compact) |
| Activation card | → **Setup progress** (narrower, collapsible) |
| Health card | → **Health ring** in row 5 |
| Recommendations card | → **Hero** (top 1) + **Next steps** (2–3) |
| Quick actions card | → **Shortcuts** icon row (max 5) |
| Stats card | → **Metrics** inline with health (4 KPI chips) |
| *(missing)* | → **Alerts**, **Modules**, **Subscription**, **Resources** |

---

## Phase 3 — Desktop layout (12-column grid)

**Grid:** 12 columns · gutter 24px · max width 1200px (`page-full`)  
**Breakpoints:** Desktop ≥1024px uses full grid; tablet 768–1023 uses 8-col simplification (specified in Phase 4 tablet wireframe)

### Column allocation

| Section | Col span | Row | Height guidance |
|---------|----------|-----|-----------------|
| Status strip | 12 | 1 | 72px fixed |
| Critical alerts | 12 | 2 | auto (stack max 3) |
| Primary action hero | 12 | 3 | 120–160px |
| Setup progress | 8 | 4 | auto |
| Setup progress (side) | 4 | 4 | Health mini-preview when setup active |
| Health ring + factors | 5 | 5 | 200px |
| Metric chips (4) | 7 | 5 | 200px |
| Next steps list | 8 | 6 | min 240px |
| Shortcuts row | 4 | 6 | min 240px |
| Recent activity | 8 | 7 | min 320px |
| Activity sidebar | 4 | 7 | Quick stats / “Open logs” |
| Modules snapshot | 6 | 8 | 160px |
| Subscription snapshot | 6 | 8 | 160px |
| Resources | 12 | 9 | 80px |

### Desktop wireframe (ASCII)

```
┌──12──────────────────────────────────────────────────────────┐
│ [Avatar] Plan · Bot ● Health 82 · Setup 60%    [Discord ↗] [⋮]│  STATUS STRIP
├──12──────────────────────────────────────────────────────────┤
│ ⚠ Bot hasn't synced in 8 days — Sync now                     │  ALERT (optional)
├──12──────────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────────────────────────┐ │
│ │  Configure your ticket category                          │ │
│ │  Support can't start until tickets have a home.          │ │  HERO
│ │  [ Configure tickets — primary ]          High priority  │ │
│ └──────────────────────────────────────────────────────────┘ │
├──8───────────────────────────────┬──4─────────────────────────┤
│ SETUP PROGRESS ████████░░ 60%    │ Health preview 72/100    │  (pre-activation)
│ ✓ Bot  ✓ Sync  ○ Module  ○ …     │ 2 items need attention   │
├──5───────────────┬──7───────────────────────────────────────┤
│    ╭───╮         │ [Open tickets 3] [Modules 4/6] [Staff 2] │  HEALTH + METRICS
│    │82 │ Good    │ [Logs on ✓]                              │
│    ╰───╯         │                                          │
├──8───────────────────────────────┬──4─────────────────────────┤
│ NEXT STEPS                       │ SHORTCUTS                  │
│ 1. Enable logs      [Go →]       │ [⚙][🎫][📋][💳][👥]        │
│ 2. Invite staff     [Go →]       │ Settings Tickets …         │
├──8───────────────────────────────┬──4─────────────────────────┤
│ RECENT ACTIVITY                  │ Today: 5 events            │
│ Today                            │ [View all logs →]          │
│ • Ticket #12 opened    2:04p     │                            │
│ • Welcome module on    1:30p     │                            │
├──6───────────────────────────────┬──6─────────────────────────┤
│ MODULES                          │ SUBSCRIPTION               │
│ Welcome ✓  Tickets ✓  Logs ○     │ Pro plan · Active          │
│ 4 enabled · 2 locked             │ Renews Aug 12 · [Manage →] │
├──12──────────────────────────────────────────────────────────┤
│ Help: Setup guide · Ticket docs · Contact support            │  RESOURCES
└──────────────────────────────────────────────────────────────┘
```

### Layout decisions justified

| Decision | Rationale |
|----------|-----------|
| Hero full 12 cols | Vercel deployment banner pattern — one action dominates |
| Health 5 / Metrics 7 | Health is qualitative; metrics are quantitative — complementary not duplicate |
| Next steps 8 / Shortcuts 4 | Ranked list needs width; shortcuts are icon-only compact |
| Activity 8 / Sidebar 4 | Feed needs space; sidebar prevents activity card feeling endless |
| Modules ∥ Subscription | Equal entitlement weight; owner scans both in one row |
| Setup 8 + health preview 4 | Keeps activation visible without full-width waste post-O-002 |

---

## Phase 4 — Mobile layout (intentional, not stacked desktop)

**Viewport:** ≤767px · single column · sticky status strip optional (phase 2 polish)

### Mobile section order (differs from desktop)

Mobile prioritizes **action over history**:

1. Status strip (collapsed badges — max 2 visible + “+N”)  
2. Critical alerts  
3. **Primary action hero** (full bleed, sticky CTA optional)  
4. Setup progress (accordion)  
5. Health ring (horizontal compact)  
6. Metric chips (2×2 grid)  
7. Shortcuts (horizontal scroll, never wrap)  
8. Next steps (single column, max 2 visible)  
9. Recent activity (max 5 items)  
10. Modules snapshot (accordion)  
11. Subscription snapshot (accordion)  
12. Resources (collapsed link row)  

### Mobile wireframe

```
┌─────────────────────┐
│ [Av] Pro · ● Bot    │
│ Health 82 · 60% setup│
├─────────────────────┤
│ ⚠ Sync overdue      │
├─────────────────────┤
│ Configure tickets   │
│ [ Primary CTA     ] │
├─────────────────────┤
│ ▼ Setup 60%         │
├─────────────────────┤
│ ╭────╮ 82 Good      │
│ │ring│              │
│ ╰────╯              │
│ [3 open][4 mod][…]  │
├─────────────────────┤
│ ← [⚙][🎫][📋][💳] → │
├─────────────────────┤
│ Next: Enable logs → │
├─────────────────────┤
│ Activity (5 max)    │
├─────────────────────┤
│ ▼ Modules           │
│ ▼ Subscription      │
├─────────────────────┤
│ Help · Docs         │
└─────────────────────┘
```

### Mobile-specific rules

- **No 2-column grid** on mobile — ever  
- **Shortcuts:** horizontal scroll with snap; min touch target 44px  
- **Hero CTA:** full-width `btn-primary`  
- **Alerts:** dismissible per session; max 1 visible at a time (carousel)  
- **Activity:** truncate to 5 items; “View all” required  
- **Sticky:** only status strip (optional v2) — hero sticky rejected (covers content)  

### Tablet wireframe (768–1023px, 8-column)

- Status + alerts: 8 cols  
- Hero: 8 cols  
- Setup: 8 cols (health preview moves below)  
- Health: 4 cols · Metrics: 4 cols (side by side)  
- Next steps: 5 cols · Shortcuts: 3 cols  
- Activity: 8 cols  
- Modules: 4 · Subscription: 4  

---

## Phase 5 — Visual language

**Baseline:** PP-001 design system (`tokens.css`, `design-system.css`) — **approved foundation**.

### Compliance assessment

| Element | Follows DS? | Gap | Proposal |
|---------|-------------|-----|----------|
| Typography | Partial | Overview uses raw `h2`/`h3` not `.type-*` | Apply `.type-card-title`, `.type-caption`, `.type-overline` |
| Spacing | Partial | Uniform gaps | Introduce `.overview-tier` spacing (32px between tiers) |
| Radius | Yes | — | Keep `--radius-lg` for cards, `--radius-md` for inset |
| Elevation | Partial | All cards equal elevation | Hero + alerts use `--elevation-2`; secondary cards `--elevation-1` |
| Icons | **No** | Emoji + unicode + SVG mix | **All UI icons via `app-ui-icon`**; emoji only in marketing empty states until SVG set |
| Empty states | Partial | `[nested]="true"` fixed PP-001 | Widget-specific SVG + single CTA |
| Progress bars | Yes | Global `.progress-bar` | Add step dots for activation (GitHub Actions style) |
| Charts | N/A | No charts today | Health ring = CSS/SVG arc (no chart library v1) |
| Badges | Yes | Too many in header (5+) | Max 3 visible in strip; overflow “+N more” |
| Buttons | Partial | No hero hierarchy | Hero: `btn-primary btn-block`; secondary list: `btn-ghost` |
| Cards | Yes | Too many full cards | Status strip = **not a card** (borderless panel) |
| Dialogs | N/A on overview | — | Sync confirm in overflow menu |

### New overview-specific tokens (proposal)

| Token | Value | Use |
|-------|-------|-----|
| `--overview-hero-min-height` | 120px | Hero card |
| `--overview-status-height` | 72px | Status strip |
| `--overview-tier-gap` | 32px | Between major sections |
| `--overview-health-ring-size` | 96px | Health visualization |

---

## Phase 6 — Card review

| Current card | Should exist? | Action | Density | Move? |
|--------------|---------------|--------|---------|-------|
| **Community header** | Yes, smaller | → Status strip (not a card) | High — inline badges | Top |
| **Activation progress** | Yes, conditional | Narrower; collapse after first value | Medium — step dots not full list | Below hero |
| **Community health** | Yes | Merge metrics; ring + top 3 factors | High | Row with metrics |
| **Recommendations** | Yes, split | Top 1 → hero; rest → next steps | Medium | Split |
| **Quick actions** | Merge | → Shortcuts only (5 max) | High | Right column / scroll row mobile |
| **Recent activity** | Yes | Add icons + day groups | Medium | Below actions |
| **At a glance stats** | No as card | → Metric chips in health row | High | Merge |
| **Critical alerts** | **New** | 0–3 banners | High | Below status |
| **Primary hero** | **New** | Single CTA | Focused | Above fold |
| **Modules snapshot** | **New** | Enabled/locked summary | High | Bottom row |
| **Subscription snapshot** | **New** | Plan/status/expiry | High | Bottom row |
| **Resources** | **New** | Text links | Low | Footer |

---

## Phase 7 — Copy review

### Issues found

| Issue | Examples | Fix |
|-------|----------|-----|
| English in Arabic | Activity API messages | Structured i18n (OV-003) |
| Developer wording | `/setup`, `/ticket setup` in recommendations | “Run setup in Discord” → “Connect your server in Discord” |
| Weak CTAs | “Open modules”, “Go to servers” | Verb-first: “Enable modules”, “Sync server data” |
| Duplicate wording | “Recommended next steps” + “Quick actions” | “Next steps” + “Shortcuts” |
| Long labels | “Configure your first module” | “Finish module setup” |
| Unused strings | `overview.subtitle`, `statsHint`, `lastSync` | Integrate or delete |
| Misleading | “Activated” at 85% | “Setup complete” vs “Community live” (after first value) |

### Rewritten copy (EN — implementation keys proposed)

#### Status strip

| Key | Current | Proposed |
|-----|---------|----------|
| `overview.status.plan` | *(badge only)* | `{planName}` |
| `overview.status.botOnline` | Bot online | Connected |
| `overview.status.botOffline` | Bot offline | Not connected |
| `overview.status.lastSync` | *(unused)* | Synced {time} |
| `overview.status.neverSynced` | Never synced | Not synced yet |

#### Hero (dynamic — template)

```
Title: {recommendation.title}  — or —  {activation.step.title}
Body: One sentence outcome, not instruction list.
CTA: {recommendation.cta} — primary verb
Meta: {priority} · Est. 2 min
```

Example:
- **Title:** Set up support tickets  
- **Body:** Pick a category so members can open tickets in Discord.  
- **CTA:** Configure tickets  
- **Meta:** Required · ~3 min  

#### Setup progress

| Key | Current | Proposed |
|-----|---------|----------|
| `overview.setup.title` | Activation progress | Setup progress |
| `overview.setup.complete` | Activated | Setup complete |
| `overview.setup.live` | *(new)* | Community is live |
| `overview.setup.messages.start` | Let's connect your Discord community… | Connect your server to get started. |
| `overview.setup.messages.almostThere` | One step away… | One step left: {stepName}. |

#### Alerts (new)

| Key | Proposed EN |
|-----|-------------|
| `overview.alerts.botOffline` | The bot is not connected. Check that it’s online in Discord. |
| `overview.alerts.syncStale` | Server data is {days} days old. Sync to refresh channels and roles. |
| `overview.alerts.subscriptionExpiring` | Your {plan} plan expires in {days} days. |
| `overview.alerts.paymentRejected` | Your subscription change was declined. View the reason and resubmit. |

#### Activity (structured)

| Type | EN template |
|------|-------------|
| `TicketCreated` | Ticket #{number} opened |
| `ModuleEnabled` | {moduleName} turned on |
| `LogEntry` | {summary} *(sanitized, short)* |

Arabic: mirror keys in `ar.json` — no English fragments in AR UI.

---

## Phase 8 — Competitive review

### Linear

| What they do better | Adaptation for us |
|--------------------|-------------------|
| One primary inbox / focus | **Hero CTA** = single “inbox zero” equivalent for setup |
| Keyboard-first | Defer shortcuts palette; Overview hero focus on load |
| Dense but calm typography | Reduce card count; increase information per card |

### Stripe Dashboard

| What they do better | Adaptation |
|--------------------|------------|
| “Action required” banners impossible to miss | **Alerts strip** with severity colors |
| Clear payment status on home | **Subscription snapshot** with expiry + CTA |
| Test mode / live mode clarity | **Plan badge** + status in strip |

### Vercel

| What they do better | Adaptation |
|--------------------|------------|
| Deployment status = hero | **Setup/health hero** with one CTA |
| Project selector ≈ our guild switcher | Already in sidebar — don’t duplicate in body |
| Skeleton matches layout exactly | Redesign skeleton to match 12-col |

### GitHub

| What they do better | Adaptation |
|--------------------|------------|
| Activity feed with icons + refs | Typed activity icons + links to ticket/log |
| Repo insights at a glance | **Metric chips** not raw channel/role counts |
| Setup wizards for new repos | O-003 Welcome Wizard links from hero |

### Discord Developer Portal

| What they do better | Adaptation |
|--------------------|------------|
| Bot status prominently shown | Status strip bot indicator + last sync |
| Clear “getting started” checklist | Setup progress with Discord-native language |
| App → server connection clarity | “Connected to {guild}” in strip not duplicate name |

### Notion (reference)

| What they do better | Adaptation |
|--------------------|------------|
| Empty pages invite creation | Hero CTA on empty activity/modules |
| Block hierarchy | Tier spacing between overview sections |

### Slack Admin

| What they do better | Adaptation |
|--------------------|------------|
| Workspace health indicators | Health ring + alerts |
| Quick admin actions | Shortcuts row (5 max) |

**Principle:** Adapt **patterns**, not pixels. We are a Discord-adjacent ops tool — dark theme, guild context, module entitlements stay.

---

## Phase 9 — Final blueprint

### UX problems (prioritized)

| Priority | Problem |
|----------|---------|
| P0 | No single primary action; 10-second test fails |
| P0 | Activity English in AR |
| P0 | False “Activated” state |
| P0 | Subscription/modules invisible on overview |
| P1 | Duplicate guild name + redundant widgets |
| P1 | No critical alerts |
| P1 | Mobile layout accidental |
| P2 | Weak visual hierarchy and icon consistency |
| P2 | Stats card low value |

---

### Card specifications (implementation reference)

#### 1. Status strip

- **Not a card** — transparent panel, bottom border only  
- Content: avatar 48px · plan badge · bot dot · health score compact · setup % if <100%  
- Actions: Discord external · overflow menu (Sync, Copy guild ID)  
- Badges max 3 + overflow  

#### 2. Alerts strip

- Component: `.banner-warning`, `.banner-danger`, `.banner-info`  
- Max 3 stacked desktop; 1 carousel mobile  
- Each: icon + message + inline CTA link  
- Dismiss: session-only (except payment rejected)  

#### 3. Primary action hero

- Component: `.card.card-elevated` with `--elevation-2`  
- Content: overline (priority) · title · one-line body · primary CTA · optional secondary “Learn more”  
- Data: top recommendation OR current activation step if no recommendations  
- Analytics: `OverviewHeroCtaClicked`  

#### 4. Setup progress

- Visible when `!firstValueAchieved`  
- Progress bar + step dots (8 steps max visible as dots, expand for list)  
- Collapses to chip row “Setup complete ✓” when done  

#### 5. Health + metrics row

- Health: SVG ring 0–100, color by level, label below  
- Factors: top 3 failing only + “View all factors” expand  
- Metrics: 4 chips — Open tickets · Modules enabled · Staff roles · Logs enabled  

#### 6. Next steps + shortcuts

- Next steps: ordered list max 3, ghost buttons  
- Shortcuts: 5 icon buttons with tooltips, permission-filtered  

#### 7. Recent activity

- Day group headers  
- Row: icon · message (i18n) · relative time · optional link  
- Max 8 desktop / 5 mobile  

#### 8. Modules snapshot

- Rows: module name · status pill (On/Off/Locked)  
- Footer: “{n} enabled · {m} locked” + Manage modules →  

#### 9. Subscription snapshot

- Plan name · status pill · expiry date  
- CTA: Manage billing → or Renew (when ≤7 days)  
- Show pending change badge if active workflow  

#### 10. Resources

- Inline links: Setup guide · Module docs · Support  
- `.type-caption` styling  

---

### Accessibility

| Requirement | Implementation |
|-------------|----------------|
| Focus order | DOM order = visual order on all breakpoints |
| Hero CTA | First focusable after alerts |
| Health ring | `aria-label="Community health score {n} out of 100, {level}"` |
| Alerts | `role="alert"` for critical; dismiss button labeled |
| Activity | List semantics `<ul>`/`<li>`; times in `<time datetime>` |
| Shortcuts | `aria-label` per icon from i18n |
| Loading | `aria-live="polite"` on overview shell |
| Color | Status never color-only — icon + text |

---

### RTL review

| Element | Rule |
|---------|------|
| Status strip | Flex row uses logical properties; avatar inline-start |
| Hero CTA | Full-width button OK |
| Health ring | No directional bias |
| Activity list | Icons inline-start; timestamps inline-end |
| Shortcuts scroll | Scroll snap inline-start |
| Alerts | Border accent `border-inline-start` |
| Metric chips | Grid auto-fill OK |

All new copy **must** exist in `ar.json` before ship. Activity **must** use i18n keys (OV-003).

---

### Empty states

| Widget | Score today | Target | Copy | CTA |
|--------|-------------|--------|------|-----|
| Hero | N/A | — | Always has CTA when data loads | — |
| Setup | N/A | — | Hidden when complete | — |
| Health factors | 6/10 | 9/10 | “Complete setup to see health breakdown” | Open setup |
| Next steps | 7/10 | 9/10 | “You're caught up” | Browse modules |
| Shortcuts | 6/10 | 8/10 | “No actions available for your role” | — |
| Activity | 6/10 | 9/10 | “Nothing recent yet” | View logs |
| Modules | N/A new | 9/10 | “No modules enabled yet” | Enable modules |
| Subscription | N/A new | 9/10 | Free plan messaging | View plans |

---

### Loading states

- Skeleton mirrors final 12-col layout (9 rows)  
- Status strip skeleton: avatar circle + 3 pill bars  
- Hero skeleton: full-width rectangle 140px  
- No spinner-only full page  
- Stale-while-revalidate on revisit (future): show cached overview + refresh indicator  

---

### Error states

| Error | UX |
|-------|-----|
| Overview load fail | Full-page empty state (existing) + retry |
| Partial experience fail | Show overview base + inline banner “Some insights unavailable” |
| Sync fail | Toast (existing) + alert strip |
| 403 | Redirect with toast — not overview error |

---

### Responsive rules

| Breakpoint | Columns | Notes |
|------------|---------|-------|
| ≥1024 | 12 | Full wireframe |
| 768–1023 | 8 | Tablet wireframe |
| ≤767 | 1 | Mobile wireframe order |
| ≤480 | 1 | Reduce metric chips to 2×2; smaller health ring (72px) |

---

### Constraints (implementation)

1. **No new API routes required for v1** — extend `experience` DTO only  
2. **Business logic stays in** `GuildOverviewExperienceService`  
3. **Use PP-001 design system** — no new button/card primitives without DS update  
4. **No Welcome Wizard in PR-002** — link placeholder for O-003  
5. **Analytics events** must be spec’d before merge  
6. **EN + AR parity** mandatory for all new keys  
7. **Staff users** see reduced overview (no billing, filtered shortcuts) — separate persona spec in implementation  

---

### Implementation roadmap

| Phase | Sprint | Deliverable | Effort |
|-------|--------|-------------|--------|
| **IM-1** | PR-002-A | Status strip + remove duplicate title + alerts component | 3d |
| **IM-2** | PR-002-B | Hero CTA + backend top recommendation selection | 2d |
| **IM-3** | PR-002-C | Setup progress redesign + activation truth fix (first value) | 3d |
| **IM-4** | PR-002-D | Health ring + metric chips + merge stats | 3d |
| **IM-5** | PR-002-E | Next steps + shortcuts split | 2d |
| **IM-6** | PR-002-F | Activity i18n + icons + grouping (API + UI) | 3d |
| **IM-7** | PR-002-G | Modules + subscription snapshots | 2d |
| **IM-8** | PR-002-H | Mobile/tablet responsive + skeleton update | 3d |
| **IM-9** | PR-002-I | Copy pass EN/AR + a11y audit + QA | 2d |

**Total estimate:** ~23 dev-days (1 engineer + design review checkpoints)

### Approval checklist

- [ ] Product approves IA and section order  
- [ ] Design approves wireframes and visual language  
- [ ] Engineering approves DTO changes and roadmap  
- [ ] Localization approves copy keys  
- [ ] CTO approves scope boundary (no Welcome Wizard in PR-002)  

---

## Appendix A — Backend DTO changes (spec only)

Extend `GuildOverviewExperienceDto`:

```typescript
// Proposed fields (illustrative — not implemented)
alerts: OverviewAlert[]           // id, severity, messageKey, params, route, ctaKey
hero: OverviewHeroAction          // source: recommendation | activation, titleKey, bodyKey, route, ctaKey
modulesSnapshot: ModuleStatus[]   // key, name, status: enabled|disabled|locked
activity: { type, params, occurredAt }[]  // replace raw Message string
activation.firstValueAchieved: boolean
activation.displayMode: 'full' | 'compact' | 'hidden'
```

Fix activation:

```csharp
// isActivated should require firstValueAchieved, not progressPercent >= 85
var isActivated = firstValueAchieved;
var setupComplete = progressPercent >= 85; // separate flag for setup badge
```

---

## Appendix B — Analytics events (spec only)

| Event | Properties |
|-------|------------|
| `OverviewViewed` | guildId, persona, setupPercent, healthScore |
| `OverviewAlertClicked` | alertId |
| `OverviewHeroCtaClicked` | source, recommendationId or stepKey |
| `OverviewShortcutClicked` | actionId |
| `OverviewActivityItemClicked` | activityType, targetRoute |
| `OverviewModuleSnapshotClicked` | — |
| `OverviewSubscriptionSnapshotClicked` | — |

---

*PR-002 — Design proposal only. No code modified. Awaiting approval.*
