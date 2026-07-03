# UX-002 — Global Experience Unification Audit

**Audit ID:** UX-002  
**Date:** 2026-07-03  
**Type:** Product experience audit (documentation only)  
**Authority:** [PB-001](../blueprint/product-blueprint.md) · [UL-001](../blueprint/ubiquitous-language.md) · [PX-001](../ux/product-experience-architecture.md) · [PX-002](../ux/product-decision-architecture.md) · [PP-001](../design/design-system.md)  
**Prior audits:** [PR-001](./product-review-001.md) · [PR-002 v2 Mission Control](../reviews/overview-redesign-v2.md) (spec — not shipped)  
**Verdict:** **One vision on paper; multiple teams in the product.** Closed beta credible for coached operators. **Not unified enough to charge premium or compete visually with Linear/Stripe.**

---

## Executive summary

The Discord Bot Platform has **strong architectural documentation** (PB-001, PX-001, PX-002, PP-001) and **uneven implementation**. PP-001 unified tokens and page widths; PX-001 and PX-002 define world-class discipline that **most pages do not yet follow**.

**Global consistency score: 5.8 / 10**

| Dimension | Score |
|-----------|-------|
| Visual consistency (PP-001 adoption) | 6.2 |
| Mission / decision alignment (PX-002) | 4.5 |
| Trust & honesty (PX-001 §13) | 5.0 |
| Copy & i18n | 6.0 |
| Navigation & IA | 5.5 |
| Forms & tables | 5.5 |
| Dialogs & destructive UX | 5.0 |
| Empty & loading states | 5.8 |
| Accessibility & RTL | 5.5 |
| Admin experience | 6.0 |
| **Overall** | **5.8** |

**Master backlog:** 50 prioritized issues · 25 quick wins · 15 high-impact · 10 release blockers · 10-theme refactoring roadmap below.

---

## Heat map (all major areas)

| Area | Score | Priority | Mission (PX-002) | Notes |
|------|-------|----------|-------------------|-------|
| **Overview** | 5.5 | P0 | Widget dashboard — not Mission Control | O-002 shipped; PR-002 v2 / PX-002 not implemented |
| **Servers / Landing** | 6.0 | P0 | Onboarding broken checklist | Good hero; trust issue on fake progress |
| **Subscription** | 7.0 | P1 | Stepper strong; multi-CTA | Best billing UX; rejection reason hidden |
| **Tickets** | 6.5 | P1 | Table-first OK | `window.confirm`; raw enums |
| **Transcript** | 7.0 | P2 | Clear mission | Raw API fields |
| **Logs** | 6.0 | P1 | Filters good | Weak empty; API English in AR |
| **Moderation** | 5.5 | P2 | View-only OK | Inline empties; `msgs` hardcoded |
| **Moderation Settings** | 5.5 | P1 | Form OK | Mixed confirm patterns |
| **Staff** | 5.0 | P1 | Permission UX | Remove without confirm |
| **Reaction Roles** | 5.5 | P2 | Read-only sparse | No CTA empty |
| **Auto Role** (Settings tab) | 5.5 | P2 | Buried in settings | Enum copy |
| **Modules** | 6.0 | P1 | Clear toggles | No upgrade CTA when locked |
| **Guild Settings** | 5.5 | P0 | Tab sprawl | Bare labels; enum English |
| **Profile** | 6.5 | P3 | Single mission | Minor form pattern |
| **Admin Home** | 6.0 | P3 | Stats OK | — |
| **Admin Guilds** | 5.5 | P2 | Plan change no confirm | |
| **Admin Users** | 6.5 | P3 | Read-only OK | |
| **Admin Plans** | 6.0 | P2 | `form-field` good | Native confirm delete |
| **Admin Subscription Changes** | 7.5 | P2 | Best admin table UX | 13 columns mobile |
| **Login / Auth** | 4.0 | P0 | Developer copy on login | Release blocker |
| **Auth Callback** | 5.0 | P1 | TS errors not i18n | |
| **Navigation (shell)** | 5.5 | P1 | Fake notifications bell | |
| **Cross: Dialogs** | 5.0 | P0 | 3 patterns | |
| **Cross: Empty states** | 5.5 | P1 | 4 patterns | |
| **Cross: Loading** | 6.0 | P2 | Spinner-heavy | |
| **Cross: Permission errors** | 4.5 | P0 | Silent redirects | |
| **Cross: Mobile** | 5.5 | P1 | Not designed per page | |
| **Cross: RTL** | 6.0 | P2 | Foundation OK; API EN leaks | |
| **Cross: Accessibility** | 5.5 | P1 | Partial | |
| **Cross: Copy / i18n** | 5.8 | P0 | API strings in AR UI | |
| **Maintenance** | N/A | — | Not implemented | Future |

---

## Per-area audit

### Overview (`/guilds/:id/overview`)

**Score: 5.5 / 10** · **Priority: P0**

| | |
|--|--|
| **Strengths** | PP-001 page-full width; parallel data load; nested empty states improved; analytics hooks; health + recommendations conceptually strong |
| **Weaknesses** | Seven widget zones; multiple CTAs; not PX-002 Mission Engine; fake activation at 85%; activity English in AR; duplicate guild name |
| **UX debt** | P0: Not Mission Control · P0: Multiple heroes · P1: Health score opacity · P2: Emoji icons |
| **Consistency** | Mix of action-tile, nested empty, card-header-row — partially PP-001 |
| **IA** | Partially answers “what next?” — too many answers | **Mission:** Widget model — **violates PX-002** |
| **Personas** | Owner-focused; staff see same clutter | **Mobile:** Grid collapse only |
| **World-class gap** | Linear home = one focus; this = widget grid |

---

### Servers (`/servers`)

**Score: 6.0 / 10** · **Priority: P0**

| | |
|--|--|
| **Strengths** | Onboarding hero; invite flow; i18n checklist copy; page-full |
| **Weaknesses** | `onboardingChecklist` always empty (0%); 3 CTAs per server card; Discord ID as mono |
| **UX debt** | P0: Broken checklist · P1: Multi-CTA cards · P2: ID display |
| **IA** | Where am I ✓ · What to do ✓ (invite) · Trust ✗ on progress |
| **Mission** | Should emit `CompleteSetupConnect` — not integrated with PX-002 |

---

### Subscription (`/guilds/:id/subscription`)

**Score: 7.0 / 10** · **Priority: P1**

| | |
|--|--|
| **Strengths** | UX-001 stepper; custom cancel dialog; `form-field`; beta notice; admin review polished (SB-004) |
| **Weaknesses** | Rejection `adminNote` not shown to owner; no payment instructions; multiple visible primaries; USD hardcoded |
| **UX debt** | P0: Rejection hidden · P0: Payment instructions · P1: Multi-CTA stack |
| **IA** | Strong mission for owners · Answers billing state partially |
| **Mission** | Should use PX-002 billing missions — partially manual today |
| **World-class gap** | Stripe clarity on status — close but manual billing gap hurts trust |

---

### Tickets (`/guilds/:id/tickets`)

**Score: 6.5 / 10** · **Priority: P1**

| | |
|--|--|
| **Strengths** | Wide table layout; conversation expand; reply flow; transcript link; filters |
| **Weaknesses** | `window.confirm` close; raw `actorType`; row action cluster; bare reply form |
| **UX debt** | P1: Native confirm · P1: Enum in UI · P2: Reply delay not hinted |
| **IA** | Work page — table is primary ✓ | **Mission:** Backlog mission belongs in Overview (PX-002), not here |
| **Staff** | Usable for support | **Mobile:** Wide table scroll |

---

### Ticket Transcript (`/guilds/:id/tickets/:id/transcript`)

**Score: 7.0 / 10** · **Priority: P2**

| | |
|--|--|
| **Strengths** | Archive honesty notice; clear back/refresh; medium width; good error empty |
| **Weaknesses** | Raw `metadata.source`, `actorType`; inline empty for zero entries |
| **UX debt** | P2: API i18n · P3: Empty CTA |
| **IA** | Single mission ✓ — read durable record |

---

### Logs (`/guilds/:id/logs`)

**Score: 6.0 / 10** · **Priority: P1**

| | |
|--|--|
| **Strengths** | Filter card; typed clear confirmation; member select; page-wide |
| **Weaknesses** | `.empty-inline` only; `typeLabel`/`message` API English; raw count subtitle |
| **UX debt** | P1: Weak empty · P0: AR i18n leak · P2: Filter form bare labels |
| **IA** | Mission clear · Empty fails PX-001 structure |

---

### Moderation (`/guilds/:id/moderation`)

**Score: 5.5 / 10** · **Priority: P2**

| | |
|--|--|
| **Strengths** | Dual tables documented as view-only; filters; medium width |
| **Weaknesses** | Inline empties; **`msgs` hardcoded English**; filter enum values exposed |
| **UX debt** | P2: Copy · P2: Empty states · P3: Beta banner missing |
| **IA** | Split warnings/cases — two missions on one page |

---

### Moderation Settings (`/guilds/:id/moderation/settings`)

**Score: 5.5 / 10** · **Priority: P1**

| | |
|--|--|
| **Strengths** | `form-field` on add; permission grid; narrow width |
| **Weaknesses** | `window.confirm` delete; ban/ktimeout flags false promise (PR-001); empty list no CTA |
| **UX debt** | P1: Confirm inconsistency · P1: False capability flags · P2: Empty |

---

### Staff (`/guilds/:id/staff`)

**Score: 5.0 / 10** · **Priority: P1**

| | |
|--|--|
| **Strengths** | Clear add form; `form-field`; permission grid labels i18n |
| **Weaknesses** | **Remove with no confirm**; overlaps moderation-settings metaphor |
| **UX debt** | P0: Destructive no confirm · P1: Dual permission UIs (PR-001) |
| **IA** | Mission OK · Trust ✗ on delete |

---

### Reaction Roles (`/guilds/:id/reaction-roles`)

**Score: 5.5 / 10** · **Priority: P2**

| | |
|--|--|
| **Strengths** | List + deactivate; page-wide |
| **Weaknesses** | Inline empty; deactivate no confirm; Discord content untranslated |
| **UX debt** | P2: Empty + CTA · P2: Confirm · P3: Create flow only in Discord |

---

### Auto Role (Settings → Auto role tab)

**Score: 5.5 / 10** · **Priority: P2**

| | |
|--|--|
| **Strengths** | Part of unified settings save |
| **Weaknesses** | Same bare-label pattern as settings; buried in tabs |
| **UX debt** | P2: Form consistency · P3: No standalone mission |

---

### Modules (`/guilds/:id/modules`)

**Score: 6.0 / 10** · **Priority: P1**

| | |
|--|--|
| **Strengths** | Toggle UX; plan lock messaging; narrow width; module cards |
| **Weaknesses** | No upgrade CTA when locked; API module names/descriptions English |
| **UX debt** | P1: Upgrade CTA · P2: i18n metadata · P2: No empty if list empty |

---

### Guild Settings (`/guilds/:id/settings`)

**Score: 5.5 / 10** · **Priority: P0**

| | |
|--|--|
| **Strengths** | Tab organization; single save; sync CTA; reactive forms |
| **Weaknesses** | Bare `<label>` not `form-field`; auto-reply enums English; `window.confirm` delete; ↑↓ not i18n |
| **UX debt** | P0: Form pattern split · P1: Enum copy · P1: Tab = many missions |
| **IA** | Answers configure · Overwhelms beginners |

---

### Profile (`/guilds/:id/profile`)

**Score: 6.5 / 10** · **Priority: P3**

| | |
|--|--|
| **Strengths** | Single save mission; narrow width; relatively clean |
| **Weaknesses** | Bare labels vs `form-field` elsewhere |
| **UX debt** | P3: Form consistency |

---

### Platform Admin — Home

**Score: 6.0 / 10** · **Priority: P3**

Stats grid; minimal empty guidance when counts zero.

---

### Platform Admin — Guilds

**Score: 5.5 / 10** · **Priority: P2**

Plan change in-table without confirm; Discord IDs exposed.

---

### Platform Admin — Users

**Score: 6.5 / 10** · **Priority: P3**

Read-only; good empties.

---

### Platform Admin — Plans

**Score: 6.0 / 10** · **Priority: P2**

`form-field` editor; native confirm delete; no empty when zero plans.

---

### Platform Admin — Subscription Changes

**Score: 7.5 / 10** · **Priority: P2**

Best admin UX: filters, dialogs, banners; mobile table crush.

---

### Login / Authentication

**Score: 4.0 / 10** · **Priority: P0** · **Release blocker**

| | |
|--|--|
| **Strengths** | Discord OAuth button; centered layout |
| **Weaknesses** | **API URL + Railway env instructions on login page**; TS error strings not i18n |
| **UX debt** | P0: Developer copy · P0: Trust destruction on first impression |

---

### Auth Callback

**Score: 5.0 / 10** · **Priority: P1**

Hardcoded English errors in component TS.

---

### Cross-cutting: Navigation

**Score: 5.5 / 10** · **Priority: P1**

Sidebar logical; breadcrumbs OK; **fake notifications**; profile wrong icon; staff cross-grants pollute nav; `aria-label` English on nav.

---

### Cross-cutting: Permission errors (403/404)

**Score: 4.5 / 10** · **Priority: P0**

`GuildAccessGuard` silent redirect; 404 mapped to “resource not found”; no permission empty state.

---

### Cross-cutting: Dialogs

**Score: 5.0 / 10** · **Priority: P0**

Three patterns: custom `confirm-overlay` (good), `window.confirm` (5+ flows), no confirm (staff remove, admin plan change, reaction deactivate).

---

### Cross-cutting: Empty states

**Score: 5.5 / 10** · **Priority: P1**

`app-empty-state` vs `.empty-inline` vs muted `<p>` vs none — PX-001 requires illustration + explanation + CTA consistently.

---

### Cross-cutting: Loading

**Score: 6.0 / 10** · **Priority: P2**

Overview skeleton only; elsewhere spinner panel; no stale-while-revalidate.

---

### Cross-cutting: Forms

**Score: 5.5 / 10** · **Priority: P1**

Split: `form-field` (subscription, staff, admin queues) vs bare label (settings, profile, logs, moderation, tickets).

---

### Cross-cutting: Tables

**Score: 6.0 / 10** · **Priority: P2**

Shared `.data-table`; admin 13-col; mobile horizontal scroll only; no responsive cards.

---

### Cross-cutting: Mobile

**Score: 5.5 / 10** · **Priority: P1**

PP-001 widths help; most pages not mobile-designed (PX-001 §16.3).

---

### Cross-cutting: RTL

**Score: 6.0 / 10** · **Priority: P2**

Shell RTL OK post PP-001 tickets fix; **English API content in AR UI** breaks mixed rendering.

---

### Cross-cutting: Accessibility

**Score: 5.5 / 10** · **Priority: P1**

Focus on some controls; English aria-labels; dense tables; touch targets on `.btn-sm`.

---

### Cross-cutting: Copywriting

**Score: 5.8 / 10** · **Priority: P0**

792 EN/AR keys good shell; API + enums + login dev copy violate PX-001 §14.

---

### Cross-cutting: Notifications

**Score: 4.0 / 10** · **Priority: P1**

Toasts used well; **bell is fake**; no mission analytics pipeline.

---

### Maintenance mode

**Not implemented** — document as gap for enterprise trajectory (PB-001).

---

## Cross-page consistency audit

| Pattern | Consistent? | Evidence |
|---------|-------------|----------|
| **Dialogs** | **No** | Custom overlay vs `window.confirm` vs none |
| **Badges** | Mostly | PP-001 variants; overview had local overrides (fixed PP-001) |
| **CTA hierarchy** | **No** | Multiple primaries on overview, subscription, servers cards |
| **Spacing** | Partial | PP-001 tokens; local margins remain on some pages |
| **Page widths** | **Yes** | PP-001 page-narrow/medium/wide/full adopted |
| **Empty states** | **No** | 4 patterns |
| **Tone** | Partial | Subscription good; login developer tone |
| **Loading** | Partial | loading-state vs skeleton vs inline |
| **Permission messages** | **No** | Silent guards |
| **Cards** | Mostly | PP-001 `.card` |
| **Button placement** | Partial | Save bottom on settings; scattered elsewhere |
| **Page titles** | **Yes** | Topbar + breadcrumbs |
| **Breadcrumbs** | **Yes** | Shared component |
| **Section headers** | **No** | h2/h3/card-header mix |
| **Forms** | **No** | form-field vs bare label |
| **Tables** | Mostly | data-table wrap |
| **Icons** | Partial | ui-icon + emoji empty states |
| **Copy** | **No** | i18n shell vs API leakage |

---

## Top 50 UX issues (prioritized)

| ID | P | Area | Issue |
|----|---|------|-------|
| UX-002-001 | P0 | Auth | Login page shows API URL + Railway deploy instructions |
| UX-002-002 | P0 | Errors | `api-error.util.ts` English-only; infra text to users |
| UX-002-003 | P0 | Overview | Not Mission Control — PX-002 not implemented |
| UX-002-004 | P0 | Overview | Multiple competing CTAs / widgets |
| UX-002-005 | P0 | Activation | Fake “Activated” at 85% without first value (PX-001 P-11) |
| UX-002-006 | P0 | i18n | Activity/log messages English in AR dashboard |
| UX-002-007 | P0 | Guards | Silent redirect on permission denial |
| UX-002-008 | P0 | Subscription | Owner never sees rejection reason (UX-001) |
| UX-002-009 | P0 | Subscription | No manual payment instructions |
| UX-002-010 | P0 | Servers | Onboarding checklist always 0% |
| UX-002-011 | P1 | Dialogs | `window.confirm` on tickets, settings, admin plans, mod settings |
| UX-002-012 | P1 | Staff | Remove role without confirmation |
| UX-002-013 | P1 | Nav | Fake notifications bell |
| UX-002-014 | P1 | Modules | Locked modules — no upgrade CTA |
| UX-002-015 | P1 | Settings | Bare labels vs form-field split |
| UX-002-016 | P1 | Settings | Auto-reply enums shown in English |
| UX-002-017 | P1 | Logs | Weak empty state (inline only) |
| UX-002-018 | P1 | Overview | Duplicate guild name topbar + body |
| UX-002-019 | P1 | Permissions | Two UIs: staff + moderation-settings |
| UX-002-020 | P1 | Moderation | False ban/timeout capability flags |
| UX-002-021 | P1 | Mobile | Pages not mobile-designed — stack only |
| UX-002-022 | P1 | Accessibility | English aria-labels on nav/breadcrumbs |
| UX-002-023 | P1 | Tickets | Raw actorType in conversation |
| UX-002-024 | P1 | Errors | 404 shown as resource not found not permission |
| UX-002-025 | P2 | Admin | Guild plan change without confirm |
| UX-002-026 | P2 | Reaction roles | Deactivate without confirm |
| UX-002-027 | P2 | Moderation | Hardcoded `msgs` |
| UX-002-028 | P2 | Transcript | Raw metadata.source |
| UX-002-029 | P2 | Overview | Health score without explain drawer |
| UX-002-030 | P2 | Subscription | Multiple primary buttons visible |
| UX-002-031 | P2 | Servers | Three CTAs per server card |
| UX-002-032 | P2 | Loading | No skeleton except overview |
| UX-002-033 | P2 | Empty | Emoji icons vs ui-icon mix |
| UX-002-034 | P2 | Admin queue | 13-column table mobile |
| UX-002-035 | P2 | Profile | form-field inconsistency |
| UX-002-036 | P2 | Tickets | Close ticket native confirm |
| UX-002-037 | P2 | Settings | Panel ↑↓ buttons not i18n |
| UX-002-038 | P2 | Currency | USD hardcoded in subscription/admin |
| UX-002-039 | P2 | Nav | Profile nav wrong icon |
| UX-002-040 | P2 | Staff nav | Cross-grants show moderation/logs to ticket staff |
| UX-002-041 | P2 | Reaction roles | Empty inline no CTA |
| UX-002-042 | P2 | Moderation | View-only not explained in UI |
| UX-002-043 | P2 | Callback | Auth errors not i18n |
| UX-002-044 | P2 | Overview | Sync 5s arbitrary reload |
| UX-002-045 | P3 | Admin plans | No empty when zero plans |
| UX-002-046 | P3 | Bundle | 717KB initial JS budget |
| UX-002-047 | P3 | Analytics | Console-only events |
| UX-002-048 | P3 | Maintenance | No maintenance mode UI |
| UX-002-049 | P3 | Success | No proportionate success moments (PX-001 §9) |
| UX-002-050 | P3 | Docs | beta-tester-guide drift from product |

---

## Top 25 quick wins (<1 hour each)

| # | Action |
|---|--------|
| QW-01 | Remove developer copy from login template |
| QW-02 | Generic network error in prod for status 0 |
| QW-03 | i18n auth callback error strings |
| QW-04 | Guard redirect toast “You don’t have access” |
| QW-05 | Map 403/404 to permission copy |
| QW-06 | Hide or disable notifications bell + tooltip |
| QW-07 | Modules “Upgrade plan” link when locked |
| QW-08 | Show rejection adminNote on subscription page |
| QW-09 | Fix servers onboardingChecklist wiring |
| QW-10 | Replace `msgs` with i18n key |
| QW-11 | i18n sidebar aria-label |
| QW-12 | i18n breadcrumbs aria-label |
| QW-13 | Fix profile nav icon |
| QW-14 | Moderation beta view-only banner |
| QW-15 | Ticket reply first-use delay hint |
| QW-16 | Remove duplicate overview guild h2 |
| QW-17 | Standardize `common.tryAgain` |
| QW-18 | Hide ban/timeout flags until shipped |
| QW-19 | Staff remove confirm dialog (reuse overlay) |
| QW-20 | Replace ticket close `window.confirm` with overlay |
| QW-21 | Settings auto-reply enum label keys |
| QW-22 | Logs empty → app-empty-state |
| QW-23 | Ticket actorType i18n keys |
| QW-24 | Reaction roles deactivate confirm |
| QW-25 | Admin guild plan change confirm |

---

## Top 15 high-impact improvements

| # | Impact | Initiative |
|---|--------|------------|
| HI-01 | Trust + conversion | Manual billing payment instructions + rejection UX |
| HI-02 | Daily use | Implement PR-002 v2 Mission Control + PX-002 Mission Engine |
| HI-03 | Bilingual quality | Structured i18n for activity, logs, API errors |
| HI-04 | First impression | Login + error layer rewrite (no dev copy) |
| HI-05 | Permission clarity | Access denied page + guard toasts |
| HI-06 | Destructive trust | Unified ConfirmDialog component (all flows) |
| HI-07 | Onboarding | Fix checklist + O-001 first-value activation |
| HI-08 | Settings UX | Migrate settings to form-field + tab mission clarity |
| HI-09 | Staff journey | Persona-filtered nav + staff overview slice |
| HI-10 | Tickets | Platform confirm + delivery status copy |
| HI-11 | Modules revenue | Upgrade CTA + plan name on lock |
| HI-12 | Empty states | PX-001 compliance pass all tables |
| HI-13 | Mobile | Mission Control mobile layout (PR-002 v2) |
| HI-14 | Admin | Responsive card layout for subscription changes queue |
| HI-15 | Analytics | MissionShown / Completed pipeline |

---

## Top 10 release blockers

| # | Blocker |
|---|---------|
| RB-01 | Login developer copy visible to customers |
| RB-02 | API error infra text shown to end users |
| RB-03 | Permission denial silent / misleading 404 |
| RB-04 | Subscription rejection reason hidden from owner |
| RB-05 | No payment instructions for manual billing |
| RB-06 | AR dashboard shows English API strings (activity/logs) |
| RB-07 | Fake activation / onboarding checklist broken |
| RB-08 | Fake notifications bell (trust) |
| RB-09 | Destructive actions without confirm (staff remove) |
| RB-10 | Overview not implementing PX-002 single mission (public launch) |

*Closed beta with coached users may waive RB-03, RB-08, RB-10 with documented limitations.*

---

## Refactoring roadmap (by theme)

### Theme 1 — Navigation & shell

**Pages:** All · **Priority:** P1  
Unify topbar status (PR-002 v2 Zone 1); fix nav icons; persona-filtered sidebar; remove fake notifications; access denied route.

### Theme 2 — Forms

**Pages:** Settings, Profile, Logs, Moderation, Tickets, Admin plans  
Migrate to `form-field`; validation inline; enum labels i18n; consistent save placement.

### Theme 3 — Tables

**Pages:** Tickets, Logs, Moderation, Admin *  
Sticky actions; mobile card pattern for admin queue; consistent empty in table region.

### Theme 4 — Dialogs

**Pages:** Tickets, Settings, Staff, Mod settings, Admin plans, Subscription, Logs  
Single ConfirmDialog; ban `window.confirm`; confirm all destructive actions.

### Theme 5 — Cards & layout

**Pages:** Overview (Mission Control), Servers, Subscription  
Implement 5-zone Overview; reduce server card CTAs; subscription single primary per state.

### Theme 6 — Typography & headers

**Pages:** All  
Adopt `.type-*` classes; eliminate duplicate titles; section header rules from PP-001.

### Theme 7 — Mission (PX-002)

**Pages:** Overview primary; Subscription, Tickets secondary missions  
Build Mission Engine backend; wire Mission Card; drawer for rank 2–3; Beginner/Veteran modes.

### Theme 8 — Loading & empty

**Pages:** All  
Skeleton matching layout; empty state audit; remove emoji → icon path.

### Theme 9 — Accessibility & RTL

**Pages:** All  
Aria i18n; focus order on modals; touch targets; eliminate English in AR API surfaces.

### Theme 10 — Copy & trust

**Pages:** Auth, errors, Subscription, Overview, Guards  
api-error i18n map; UL-001 consistency; trust chapter PX-001 §13 compliance pass.

---

## World-class comparison (gaps)

| Product | We adopt | We lack |
|---------|----------|---------|
| **Stripe** | Billing stepper direction; action-required singularity | Payment clarity; test mode simplicity; no dev login copy |
| **Linear** | Restraint principle in docs | One focus per page in implementation |
| **GitHub** | Activity timeline direction | Linked entities; clean permission errors |
| **Vercel** | Hero mission spec | Shipped hero; skeleton match |
| **Discord** | Native push for bot reconnect | Seamless Discord ↔ dashboard loop |
| **Notion** | Empty invites action | Weak table empties |
| **Slack** | Compact status direction | Real notification model |

---

## Final verdict

### Can this product compete visually with Linear?

**Not today.** PP-001 improved tokens and widths, but widget Overview, inconsistent empties, emoji icons, and dense admin tables read **admin template**, not Linear-level restraint. **After Mission Control + empty/dialog unification: plausible for niche ops SaaS, not full Linear parity.**

### Can this product compete with Stripe?

**Not on billing trust yet.** Subscription flow direction is good, but missing payment instructions, hidden rejections, and developer login copy violate Stripe’s honesty bar. **Fix trust blockers first.**

### Would experienced Discord admins trust it?

**Conditionally.** Coaches who know beta limitations — yes. Self-serve admins hitting login API text, silent 403, or English logs in Arabic — **no.**

### Would someone pay after seeing the UI?

**Unlikely self-serve today.** Polished subscription admin review (7.5) helps operators; first impression login (4.0) and Overview clutter hurt. **Manual billing requires trust copy Stripe users expect.**

### Five biggest reasons to hesitate

1. **Login page looks like a developer tool**, not a product — instant credibility loss.  
2. **Permission and error handling feel opaque** — silent redirects and raw API messages.  
3. **Overview does not answer “one thing now”** — widget fatigue vs Mission Control promise.  
4. **Billing loop incomplete** — pay without instructions; reject without visible reason.  
5. **Bilingual promise breaks in practice** — Arabic UI with English activity/logs/errors.

---

## Governance

This audit is the **master UX backlog** for upcoming sprints. Prioritize:

1. **Release blockers (RB-*)**  
2. **Theme 7 Mission Engine + PR-002 v2**  
3. **Theme 10 Copy & trust**  
4. **Theme 4 Dialogs**  
5. **Theme 9 Accessibility & i18n**

All future dashboard work must cite **PX-001 checklist** + **PX-002 mission catalog** compliance.

---

*UX-002 — Global Experience Unification Audit. Documentation only. No code modified.*
