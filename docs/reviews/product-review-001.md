# PR-001 — World-Class SaaS Product Audit

**Review ID:** PR-001  
**Date:** 2026-07-03  
**Auditors (roles):** CTO · Product Manager · UX Director · UI Designer · SaaS Consultant · Discord Community Expert · QA Lead · Accessibility · Localization  
**Scope:** Full product — dashboard, bot-facing UX, admin, billing, permissions, docs vs reality  
**Benchmark bar:** Discord · Linear · GitHub · Notion · Vercel · Stripe · Slack  
**Verdict:** **Not ready for public launch.** Credible **closed beta** with coached operators. **Not world-class.** Significant polish backlog required before Release 1.0.

---

## Executive summary

The Discord Bot Platform has a **solid engineering foundation** (API-first, multi-tenant, EN/AR shell, modular billing, ticket read models) but **product experience is uneven** — as if multiple teams shipped features without a enforced design system or UX authority gate.

**Strengths:** Dark SaaS token foundation; i18n key parity (792 EN/AR keys); subscription change stepper (SB-003); operational overview (O-002); honest internal beta limitations doc; platform confirm dialogs (no browser `alert()` in newer flows).

**Critical gaps:** Activation does not mean first value (O-001 violation); owner never sees rejection reasons; no payment instructions for manual billing; broken `/servers` onboarding checklist; API/developer errors shown verbatim; RTL bugs on tickets; module locks without upgrade CTA; staff journeys land on wrong pages.

This document is the **official quality backlog** before Release 1.0.

---

## 1. Visual design

### What works

- Central token file (`dashboard/.../styles/tokens.css`): brand, surfaces, semantic colors, spacing scale, typography scale, motion tokens.
- Dark Discord-adjacent aesthetic is appropriate for audience.
- Shared components.css provides cards, buttons, tables, badges, toggles, stats grid.
- Inter + Noto Sans Arabic font pairing is correct for bilingual SaaS.

### Inconsistencies found

| Issue | Evidence | Impact |
|-------|----------|--------|
| **Undefined CSS variables** | `--surface-elevated`, `--border-color`, `--text-muted`, `--text-primary` used in overview/subscription/admin CSS but not in tokens | Broken/inherited styles; inconsistent elevation |
| **Page max-width chaos** | 720px–1200px per page; global `.page-narrow/medium/wide` unused | Visual rhythm breaks between Modules (720) and Overview (1200) |
| **Dual design system dead code** | `.ds-card`, `.ds-btn` aliases never used in templates | Confusion for contributors; drift risk |
| **Local badge redefinition** | Overview `.badge` overrides global pill style | Same word, different component on one page |
| **Hardcoded hex fallbacks** | `#6366f1`, `#22c55e`, `#f59e0b` in component CSS instead of tokens only | Theme changes require grep archaeology |
| **Raw px margins** | `.table-card { margin: 20px 0 }` in components.css | Breaks spacing system |
| **Duplicate modal styles** | `.confirm-overlay` in subscription, logs, admin-upgrade-requests with z-index 100 vs 1000 | Layering bugs; visual inconsistency |
| **Mixed form patterns** | `.form-field` (subscription/admin) vs bare `<label>` (settings/logs) | Forms feel like different products |
| **Overview skeleton custom** | Not using shared `loading-state` skeleton | Different loading aesthetic |
| **No elevation/shadow system** | Cards rely on border only; no consistent shadow scale | Flatter than Linear/Notion; hierarchy weak |
| **Icon inconsistency** | Emoji empty-state icons vs `app-ui-icon` SVG elsewhere | Unprofessional mix |
| **Currency always USD** | `currency:'USD'` pipes hardcoded | Wrong for non-USD beta markets |
| **13-column admin table** | Subscription Changes queue | Cramped; not responsive-card pattern |

### Typography & hierarchy

- Topbar `<h1>` shows guild name; in-page Overview also shows guild name as `<h2>` — **duplicate title hierarchy**.
- Section headings mix `<h2>` and `<h3>` without clear level rules.
- Module names from API rendered as `<h2>` inside cards — unpredictable visual weight.

### Recommendation

Adopt **one page shell**: max-width utility class per page type, one card primitive, one badge primitive, one modal primitive, one form field primitive. Ban undefined token aliases in lint rule.

---

## 2. Design system

### Current state

**Partial design system** — tokens + components.css exist, but feature pages **opt out** with local CSS (~40% compliance estimate).

### Duplicate / inconsistent patterns

| Pattern | Variants found |
|---------|----------------|
| Card headers | `.card-section-header`, `.card-header-row`, bare `<h3>` |
| Progress bars | Global `.progress-bar` vs overview local reimplementation |
| Filters | `.filters-card` + grid (logs) vs toolbar inline (tickets) vs tabs (settings) |
| Empty states | `app-empty-state`, `.empty-inline`, nested empty-in-card, plain `<p class="muted">` |
| Quick actions | `.btn` vs overview `.quick-action-btn` |
| Confirm dialogs | 3 separate CSS copies |

### Recommended unified design system (DS-001)

**Foundation:** Keep `tokens.css`; add alias map (`--surface-elevated` → `--color-bg-elevated`).

**Components (ship as Angular + CSS):**

1. `PageShell` — title slot, actions slot, max-width prop  
2. `Card` — header, body, footer; variants: default, elevated, interactive  
3. `Button` — primary/secondary/ghost/danger; sizes sm/md; loading state  
4. `Badge` — status/plan/priority variants only  
5. `FormField` — label, hint, error, input/select/textarea  
6. `DataTable` — empty, loading, sticky actions column  
7. `EmptyState` — illustration slot (emoji or SVG), primary + secondary CTA  
8. `ConfirmDialog` — single z-index layer (1050)  
9. `Skeleton` — card, table row, stat  
10. `FilterBar` — consistent filter layout  

**Documentation:** Storybook or static `/design-system` page in dashboard (internal).

---

## 3. Overview dashboard audit

**Route:** `/guilds/:id/overview` (O-002)

### Layout & hierarchy

| Section | Score (1–10) | Notes |
|---------|--------------|-------|
| Community header | 6 | Good badges; duplicates topbar guild name; bot online heuristic opaque |
| Activation progress | 5 | Clear steps; **can show Activated without first value**; conflicts O-001 |
| Community health | 7 | Best new widget; factor list readable; score ring basic not premium |
| Recommendations | 6 | Priority badges good; cards dense; no dismiss/snooze (O-001) |
| Quick actions | 6 | Useful; custom buttons not DS buttons; reaction roles missing plan check |
| Recent activity | 4 | **English API strings in AR locale**; no icons per event type |
| At a glance stats | 5 | Feels tacked on; duplicates header info partially |

### Visual balance

- 2-column grid collapses well on mobile ✓  
- Activation + health side-by-side works on desktop  
- Full-width recommendation rows good  
- **Nested `app-empty-state` inside cards** creates double-padding “card in card” ugliness  
- Not at Linear/Vercel level: no subtle gradients, no micro-interactions, no density toggle  

### World-class gap (Overview)

**Linear** shows one primary action, keyboard shortcuts, instant feel. This overview shows **six competing zones** without clear visual focal point. **Vercel** dashboard leads with deployment status + one CTA. Here, owner must scan 7 sections to answer “what next?”

---

## 4. Navigation

### Sidebar

- Guild-scoped nav + Platform Admin section — logical ✓  
- **Ticket staff see Moderation + Logs** due to cross-grants — confusing for support-only role  
- Profile nav item uses **overview icon** — wrong semantics  
- Admin “Subscription Changes” and “Plans” share same icon  
- `aria-label="Main navigation"` hardcoded English  

### Topbar

- Breadcrumbs with Home crumb ✓  
- **Notifications bell is non-functional** (`notificationsOpen` unused) — trust eroder  
- Guild name as page title for all guild routes — good for context, bad when duplicated in page body  
- Discord external link useful ✓  

### Guild switching

- Server switcher dropdown uses `ds-dropdown` — good pattern  
- No “recent guilds” or keyboard shortcut  

### Settings organization

- Tab-based settings (general/welcome/logs/tickets/auto-replies) — reasonable  
- **Moderation permissions** separate route (`moderation-settings`) vs **Staff** (`staff`) — two permission UIs confuse owners  
- No settings search  

### Confusing paths

| User intent | Expected | Actual |
|-------------|----------|--------|
| Support staff opens guild | Tickets | Redirect to Moderation on guard fail |
| Owner configures tickets | Settings → Tickets tab OR `/ticket setup` | Dual path undocumented in UI |
| Owner upgrades locked module | Subscription | Modules page text only, no button |

---

## 5. User journey friction

### First login → first guild

1. OAuth — smooth  
2. `/servers` empty — hero good **but checklist always 0%** (`emptyChecklist()` hardcoded) — **P0 broken**  
3. Invite bot → Discord → `/setup` — high context switch; no in-dashboard progress persistence  
4. Return refresh — works  
5. Overview — information overload vs guided wizard (O-001 not shipped)  

### Activation

- Docs: first ticket/welcome message in <5 min  
- Product: configure modules + checklist weights; **Activated badge at 85% without outcome**  

### Tickets

- Create in Discord — OK  
- Dashboard list — OK with filters  
- Staff reply — 30s delay; documented in limitations, not in UI hint on first reply  
- **Staff without Discord admin cannot use Discord channel** — catastrophic for some teams; buried in docs  

### Subscription

- Stepper (SB-003) — strong  
- **No bank/payee instructions** — cannot complete manual billing self-serve  
- Reject — admin captures reason; **owner never sees it**  

### Moderation

- View-only dashboard — OK for beta  
- Staff UI offers ban/timeout permissions — **false promise**  

### Logs

- Filters good  
- Clear-all dialog good (platform pattern)  
- Empty inline vs empty-state inconsistency  

### Reaction roles

- Functional but sparse empty states  
- No link from overview recommendation if module locked  

### Permissions

- Staff page empty state OK  
- No role templates (“Support”, “Moderator”)  
- Cross-grants pollute nav  

---

## 6. UX audit

| Dimension | Finding |
|-----------|---------|
| **Click count** | Upgrade locked module: Modules → read pill → navigate manually to Subscription (3+ clicks, no deep link) |
| **Discoverability** | Notifications fake; wizard absent; `/ticket setup` only in hint text |
| **Clarity** | Health score algorithm opaque to user |
| **Feedback** | Toasts used well; guard redirects silent |
| **Success messages** | Subscription activation good; ticket reply “queued” good |
| **Error messages** | Often raw API (`InvalidOperationException`, snowflake validation) |
| **Loading** | Overview skeleton OK; no unified pattern |
| **Confirmations** | Subscription/logs/admin use platform dialogs ✓; some older flows may lack |
| **Search** | No global search; logs have text filter only |
| **Filters** | Inconsistent placement |
| **Tables** | No bulk actions; admin queue not mobile-friendly |
| **Mobile** | Sidebar off-canvas OK; wide tables scroll only |
| **Keyboard** | No shortcuts; member-select clear not keyboard accessible label |

---

## 7. Copywriting audit

### English inside Arabic

- API `log.typeLabel`, activity `item.message`, plan descriptions from DB, validation errors from API — **shown untranslated in AR UI**  
- `Invalid operation`, status enum names in auto-reply settings (`Contains`, `Exact`)  

### Developer wording exposed

| String | Location |
|--------|----------|
| “Discord snowflake” | Settings validation |
| “subscription change is 'UnderReview'” | Payment submit error |
| “Set Railway Discord__DashboardUrl” | Network error |
| “The requested resource was not found” | 404 (often permission) |

### Duplicate / stale copy

- Subscription retains `pendingApprovalNote`, `requestUpgradeHint` alongside new stepper strings  
- “Upgrade request” still in some error keys while product language is “Subscription Change”  
- Overview subtitle vs O-001 “activation” language partially aligned  

### Suggested copy principles

1. User outcome first (“We couldn’t save welcome settings”) not implementation (“snowflake”)  
2. One verb per CTA (“Submit payment reference” not “Request upgrade”)  
3. Rejection always includes human reason + next step  
4. SLA line on waiting states (“Usually within 1–2 business days”)  

---

## 8. RTL review

### Strong

- `LanguageService` sets `dir` + `lang` on `<html>`  
- `rtl.css` flips breadcrumbs, sidebar mobile slide, table text-align  
- Logical properties in globals (`border-inline-end`, `inset-inline-start`)  

### Broken / weak

| File | Issue |
|------|-------|
| `tickets.component.css` | `border-left` delivery stripes stay left in RTL |
| `ticket-transcript.component.css` | Same |
| `member-select.component.css` | Clear button `right:` fixed |
| `tickets.component.css` | `margin-left` on delivery badge |
| Mixed LTR numerals in Arabic dates | Acceptable but verify locale pipes |

### Mixed-language rendering

- Arabic UI + English API strings = **jarring reading direction mid-sentence**  
- Currency `$` prefix in AR locale  

---

## 9. Accessibility

| Criterion | Status |
|-----------|--------|
| **Contrast** | Dark theme generally OK; warning on `--color-text-muted` on elevated surfaces needs spot-check |
| **Focus** | Custom buttons in overview quick actions — verify `:focus-visible` ring |
| **Keyboard** | No skip link; modals lack documented focus trap audit |
| **ARIA** | Sidebar `aria-label` English; some dialogs have `role="dialog"` ✓ |
| **Touch targets** | `.btn-sm` in dense tables may be <44px |
| **Responsive** | Usable; tables horizontal scroll only |
| **Screen readers** | Overview skeleton phase has no `aria-live` loading announcement |
| **Reduced motion** | `animations.css` has `prefers-reduced-motion` ✓ |

**Estimated WCAG 2.1 AA compliance:** Partial — **not audit-passing** for public launch.

---

## 10. Performance perception

| Issue | Severity |
|-------|----------|
| Overview loads 3 parallel requests — acceptable | Low |
| Ticket conversation expand loads per row — feels slow | Medium |
| Resource sync “wait 5s then reload” — arbitrary | Medium |
| Full page loading spinner on navigation — acceptable | Low |
| Overview skeleton — good | Positive |
| No optimistic UI on module toggle | Medium |
| Bundle 714KB initial — budget warning | Medium |
| Layout shift when badges load in header | Low |

**Recommendations:** Shared skeleton; optimistic toggles; stale-while-revalidate on overview; progress on sync job not fixed timeout.

---

## 11. Empty states scorecard

| Page / Widget | Score /10 | Illustration | Primary CTA | Secondary | Copy quality |
|---------------|-----------|--------------|-------------|-----------|--------------|
| `/servers` no guilds | 7 | Rocket emoji | Invite bot | Refresh | Good |
| `/servers` checklist | **2** | N/A | N/A | Always 0% broken | Broken |
| Overview health empty | 5 | Emoji | None | None | Nested card |
| Overview recommendations empty | 6 | ✨ | Browse modules | None | OK |
| Overview activity empty | 6 | 📭 | View logs | None | OK |
| Tickets list | 7 | 📭 | None | None | OK |
| Logs table | 5 | Inline text | None | None | Weak |
| Moderation | 6 | Inline | None | None | OK |
| Staff | 7 | Inline | Add role | None | Good |
| Modules | 7 | 📦 | None | None | OK |
| Subscription history | N/A | Hidden when empty | — | — | — |
| Admin guilds/users | 7 | 📋 | None | None | OK |
| Reaction roles | 5 | Basic | Create | None | Weak |
| Quick actions (no perm) | 6 | 🔒 | None | None | OK |

**Target:** Every empty state ≥8/10 with illustration, outcome copy, primary CTA, secondary help link.

---

## 12. Product consistency

### Does it feel like one team?

**No.** Evidence:

- **Three form systems** (form-field, bare label, reactive forms without classes)  
- **Four empty state patterns**  
- **Five page widths** without rule  
- **Owner billing** polished (stepper, dialogs) vs **Modules** lock (text-only upsell)  
- **Admin** table-heavy vs **Owner** card-heavy without shared table/card responsive strategy  
- **Docs** describe world-class activation; **product** delivers config checklist  
- **Progress reports** mark sprints “Complete” while UX blueprints list same items “Remaining”  

### Modules vs Permissions vs Billing language

- “Modules”, “Roles & permissions”, “Moderation permissions”, “Subscription Changes” — four metaphors for entitlements  
- UL-001 vocabulary not consistently reflected in UI (Upgrade Request ghosts remain)  

---

## 13. Professionalism score

| Dimension | Score /10 | Rationale |
|-----------|-----------|-----------|
| **Visual Design** | **5.5** | Tokens exist; adoption incomplete; undefined vars; emoji/icons mix |
| **UX** | **5.0** | Core flows work; friction, silent guards, broken checklist, billing gap |
| **Navigation** | **5.5** | Structure OK; cross-grants, fake notifications, staff routing wrong |
| **Architecture** | **7.5** | API-first, domain services, read models — engineering ahead of UX |
| **Consistency** | **4.5** | Many parallel patterns; docs ≠ product |
| **Accessibility** | **5.0** | Basics present; RTL bugs; focus/screen reader gaps |
| **Localization** | **6.5** | 792 key parity; API content English-only |
| **Dashboard (Owner)** | **6.0** | O-002 uplift; not Linear-grade |
| **Admin Experience** | **5.5** | SB-004 usable; no detail drawer; mobile weak |
| **Overall Product** | **5.5** | **Closed beta: B-** · **Public launch: F** · **vs world-class: 3.5** |

---

## 14. Top 100 issues (prioritized backlog)

**Priority:** P0 = ship blocker · P1 = major · P2 = polish · P3 = nice-to-have  
**Effort:** S (<1h) · M (1–4h) · L (1–2d) · XL (3+d)

| ID | P | Cat | Description | Why it matters | Solution | Effort |
|----|---|-----|-------------|----------------|----------|--------|
| PR-001-001 | P0 | UX | `/servers` onboarding checklist always empty | First-run trust destroyed | Wire `onboardingChecklist` to API status | S |
| PR-001-002 | P0 | Billing | No payment instructions (bank/payee) | Manual billing impossible self-serve | Platform billing config + UI panel (UX-001) | L |
| PR-001-003 | P0 | Billing | Owner never sees rejection reason | UX-001 #7 violated; support load | Show `adminNote` banner + history column | M |
| PR-001-004 | P0 | Activation | “Activated” at 85% without first value | False success vs O-001 | Require `firstValue` step for activation badge | M |
| PR-001-005 | P0 | UX | 404 shown as “resource not found” for access denied | Users think product broken | Map 403/404 to permission copy + CTA | M |
| PR-001-006 | P0 | i18n | API validation errors English-only in AR | Bilingual promise broken | Error code → i18n map layer | L |
| PR-001-007 | P0 | RTL | Ticket delivery `border-left` stripes | RTL layout broken | Use `border-inline-start` | S |
| PR-001-008 | P0 | CSS | Undefined `--surface-elevated`, `--border-color` | Visual bugs | Add token aliases or fix references | S |
| PR-001-009 | P1 | UX | Module locked without Subscription CTA | Revenue + clarity | “Upgrade to Pro” button with plan name | S |
| PR-001-010 | P1 | Nav | Ticket staff redirected to Moderation | Wrong landing | Guard → `/tickets` for ticket caps | S |
| PR-001-011 | P1 | Nav | Cross-grants expose Moderation/Logs to ticket-only staff | Nav clutter | Split nav flags per capability | M |
| PR-001-012 | P1 | UX | Guild access guard silent redirect | Disorienting | Toast “You don’t have access” | S |
| PR-001-013 | P1 | Copy | “Discord snowflake” in settings errors | Jargon | User-friendly validation messages | S |
| PR-001-014 | P1 | Copy | State machine errors shown raw | Confusing | Map to product copy | M |
| PR-001-015 | P1 | Tickets | Staff Discord channel access not in beta guide | Support teams fail | Update beta guide + in-app banner | S |
| PR-001-016 | P1 | Permissions | Ban/timeout flags in staff UI without commands | False promise | Hide until shipped | S |
| PR-001-017 | P1 | Admin | No subscription change detail drawer | Admin efficiency | Detail panel with timeline | L |
| PR-001-018 | P1 | Admin | No “request more info” workflow | UX-001 gap | Return to PendingPayment action | L |
| PR-001-019 | P1 | Overview | Recent activity English in AR dashboard | Localization break | Event type i18n keys | M |
| PR-001-020 | P1 | Activation | Welcome wizard not shipped (O-001) | TTFV miss | Implement W0–W6 wizard | XL |
| PR-001-021 | P1 | Billing | No renewal banners on Subscription page | Churn risk | 7/3/1-day banners (UX-001) | M |
| PR-001-022 | P1 | Billing | Waiting review lacks SLA copy | Anxiety | Add “1–2 business days” line | S |
| PR-001-023 | P2 | Nav | Notifications bell non-functional | Trust | Hide or “Coming soon” tooltip | S |
| PR-001-024 | P2 | Nav | Profile nav uses overview icon | Semantics | Use profile/user icon | S |
| PR-001-025 | P2 | Nav | Duplicate guild name in topbar + overview | Hierarchy | Remove in-page duplicate or topbar title | S |
| PR-001-026 | P2 | DS | Page max-width varies 720–1200px | Rhythm | Adopt page width utilities | M |
| PR-001-027 | P2 | DS | `.ds-*` aliases unused | Dead code | Migrate or delete | M |
| PR-001-028 | P2 | DS | Three confirm dialog CSS copies | z-index bugs | Single shared dialog component | M |
| PR-001-029 | P2 | DS | Overview `.quick-action-btn` not `.btn` | Inconsistent | Unify button component | S |
| PR-001-030 | P2 | DS | Local `.badge` in overview | Inconsistent | Use global badge | S |
| PR-001-031 | P2 | Empty | Nested empty-state in cards | Visual clutter | Empty variant without card class | S |
| PR-001-032 | P2 | Empty | Logs uses `.empty-inline` only | Weak | Full empty-state component | S |
| PR-001-033 | P2 | Loading | Overview no translated loading message | a11y/i18n | Add aria-live loading | S |
| PR-001-034 | P2 | Copy | Stale subscription i18n keys | Confusion | Remove legacy keys | S |
| PR-001-035 | P2 | Copy | `common.tryAgain` vs `common.retry` | Inconsistent | Standardize one key | S |
| PR-001-036 | P2 | RTL | member-select clear button `right:` | RTL bug | `inset-inline-end` | S |
| PR-001-037 | P2 | RTL | ticket transcript border-left | RTL bug | logical border | S |
| PR-001-038 | P2 | a11y | Sidebar aria-label English | a11y/i18n | `nav.mainNavigation` key | S |
| PR-001-039 | P2 | a11y | Settings ↑↓ buttons no aria-label | a11y | `common.moveUp/Down` | S |
| PR-001-040 | P2 | a11y | Touch targets on `.btn-sm` in tables | Mobile a11y | Min 44px hit area | M |
| PR-001-041 | P2 | Perf | Sync uses fixed 5s reload | Feels broken | Poll job status | M |
| PR-001-042 | P2 | Perf | Bundle 714KB over budget | Load time | Lazy routes / tree shake | L |
| PR-001-043 | P2 | Overview | Health algorithm opaque | Trust | Tooltip “How score works” | S |
| PR-001-044 | P2 | Overview | No recommendation dismiss/snooze | Noise (O-001) | Dismiss with localStorage | M |
| PR-001-045 | P2 | Overview | Reaction roles quick action skips plan check | Wrong action shown | Gate by `allowedByPlan` | S |
| PR-001-046 | P2 | Modules | Expired plan modules no explanation | Confusion | Banner on Modules page | M |
| PR-001-047 | P2 | Tickets | Empty state no config branch | Dead end | Branch: off vs unconfigured | M |
| PR-001-048 | P2 | Tickets | Reply delay not in UI | Expectations | First-reply hint | S |
| PR-001-049 | P2 | Settings | Dual ticket setup paths | Confusion | Wizard step linking `/ticket setup` | M |
| PR-001-050 | P2 | Settings | Auto-reply enums raw English | i18n | Translate enum labels | S |
| PR-001-051 | P2 | Staff | No role templates | Setup friction | “Support” / “Moderator” presets | M |
| PR-001-052 | P2 | Moderation | Dashboard view-only not explained | Expectations | Beta banner on page | S |
| PR-001-053 | P2 | Admin | 13-column table mobile | Unusable | Card layout on mobile | L |
| PR-001-054 | P2 | Admin | No activation funnel dashboard | Ops blind | O-001 §11 widgets | XL |
| PR-001-055 | P2 | Admin | Admin cancel request not in UI | Ops gap | Add cancel action | M |
| PR-001-056 | P2 | Analytics | Events console-only | No product learning | Backend event sink | L |
| PR-001-057 | P2 | Docs | beta-tester-guide outdated | Wrong expectations | Sync with limitations doc | M |
| PR-001-058 | P2 | Docs | R-001 predates O-002/SB-003 | Stale readiness | Refresh readiness review | M |
| PR-001-059 | P2 | Docs | Progress “Complete” vs UX “Remaining” | Team confusion | Single gap tracker | S |
| PR-001-060 | P2 | Copy | Network error mentions Railway | Customer-facing leak | Generic network message in prod | S |
| PR-001-061 | P3 | Visual | Emoji empty-state icons | Polish | SVG illustration set | L |
| PR-001-062 | P3 | Visual | No shadow/elevation scale | Flat UI | Add `--shadow-sm/md/lg` | M |
| PR-001-063 | P3 | Visual | Hardcoded hex in component CSS | Theme drift | Remove fallbacks | M |
| PR-001-064 | P3 | Visual | `.table-card margin: 20px` | Token drift | Use `--space-5` | S |
| PR-001-065 | P3 | DS | Two progress bar implementations | Duplication | Shared ProgressBar | S |
| PR-001-066 | P3 | DS | Filters: tickets toolbar vs logs card | Inconsistent | Unified FilterBar | M |
| PR-001-067 | P3 | DS | Settings tabs unique pattern | OK but document | DS guideline entry | S |
| PR-001-068 | P3 | Nav | No keyboard shortcuts | Power users | `?` shortcut palette | XL |
| PR-001-069 | P3 | Nav | No global search | Discoverability | Command palette | XL |
| PR-001-070 | P3 | Nav | Server switcher no recents | Efficiency | Last 3 guilds | M |
| PR-001-071 | P3 | UX | No bulk ticket actions | Scale | Multi-select close | L |
| PR-001-072 | P3 | UX | No optimistic module toggle | Sluggish feel | Optimistic UI | M |
| PR-001-073 | P3 | UX | Subscription no pre-submit confirm modal | Accidental submits | UX-001 modal | M |
| PR-001-074 | P3 | Billing | Currency hardcoded USD | Intl | Plan currency field | L |
| PR-001-075 | P3 | Billing | No subscription change detail for owner | Transparency | Expand history row | M |
| PR-001-076 | P3 | Tickets | Actor type English in timeline | i18n | `tickets.actorType.*` keys | S |
| PR-001-077 | P3 | Logs | Log typeLabel from API English | i18n | Map LogEventType client-side | M |
| PR-001-078 | P3 | Overview | Activity lacks event icons | Scanability | Icon per type | S |
| PR-001-079 | P3 | Overview | At a glance duplicates stats | Redundancy | Merge or remove | S |
| PR-001-080 | P3 | Profile | Page narrow 720px only | Inconsistent | page-medium | S |
| PR-001-081 | P3 | Reaction roles | Weak empty state | Polish | Full empty-state | S |
| PR-001-082 | P3 | a11y | No skip to content link | a11y | Skip link in layout | S |
| PR-001-083 | P3 | a11y | Modal focus trap audit | a11y | Manual audit + fix | M |
| PR-001-084 | P3 | Perf | Ticket row expand loads each time | Perf | Cache conversation | M |
| PR-001-085 | P3 | Perf | No service worker / offline | PWA | Future | XL |
| PR-001-086 | P3 | Copy | Plan descriptions DB English-only | AR users | Localize plan content | L |
| PR-001-087 | P3 | Copy | Module descriptions API English | AR users | i18n module metadata | L |
| PR-001-088 | P3 | RTL | `$` in currency pipe AR | Locale | `currency` locale param | S |
| PR-001-089 | P3 | RTL | profile-menu asymmetric padding | Mirror | logical padding | S |
| PR-001-090 | P3 | Admin | Same icon for plans + changes | Scanability | Distinct icons | S |
| PR-001-091 | P3 | Admin | Guild plan change no confirm | Mis-clicks | Confirm dialog | S |
| PR-001-092 | P3 | Bot | Plan gate message OK | — | Keep | — |
| PR-001-093 | P3 | Empty | Moderation inline empty weak | Polish | empty-state | S |
| PR-001-094 | P3 | Empty | Servers checklist 0% when guilds exist | Edge case | Hide checklist on hero when guilds | S |
| PR-001-095 | P3 | DS | animations unused on most pages | Lifeless | Subtle page enter | S |
| PR-001-096 | P3 | UX | Overview 7 sections cognitive load | Overwhelm | Collapse completed sections | M |
| PR-001-097 | P3 | UX | No success moments confetti (O-001) | Delight | Micro-celebrations | M |
| PR-001-098 | P3 | Docs | UX-001 Appendix A stale | Planning | Update gap tracker | S |
| PR-001-099 | P3 | Product | Two permission pages (staff + mod settings) | Confusion | Merge or explain | L |
| PR-001-100 | P3 | Product | Staff cannot see overview health | Persona gap | Staff-scoped dashboard slice | XL |

---

## 15. Quick wins (30+ improvements under 1 hour each)

| # | Action | Effort |
|---|--------|--------|
| QW-01 | Fix `onboardingChecklist` on `/servers` to use API data | 30m |
| QW-02 | Add token aliases `--surface-elevated` → `--color-bg-elevated` | 15m |
| QW-03 | Fix `--border-color` → `--color-border` in component CSS | 20m |
| QW-04 | Ticket delivery stripes → `border-inline-start` | 15m |
| QW-05 | member-select clear → `inset-inline-end` | 10m |
| QW-06 | Hide or disable notifications bell with tooltip | 10m |
| QW-07 | Add Modules “Upgrade plan” button linking to subscription | 20m |
| QW-08 | Show rejection `adminNote` in subscription history table | 30m |
| QW-09 | Add rejected-state banner on subscription page | 45m |
| QW-10 | Guard redirect toast on access denied | 20m |
| QW-11 | Replace snowflake validation message | 15m |
| QW-12 | Add SLA line to waiting review copy | 10m |
| QW-13 | Hide ban/timeout permission flags in staff UI | 20m |
| QW-14 | Fix profile nav icon | 10m |
| QW-15 | i18n sidebar `aria-label` | 10m |
| QW-16 | i18n member-select clear aria-label | 10m |
| QW-17 | Standardize `common.tryAgain` everywhere | 15m |
| QW-18 | Remove duplicate guild `<h2>` on overview | 10m |
| QW-19 | Overview quick actions gate reaction-roles by plan | 15m |
| QW-20 | Moderation page beta “view-only” banner | 15m |
| QW-21 | Ticket reply first-use delay hint | 15m |
| QW-22 | Translate auto-reply enum labels | 30m |
| QW-23 | Map ticket actor types to i18n in template | 20m |
| QW-24 | Production network error generic message | 15m |
| QW-25 | Fix 404 copy to mention permissions | 20m |
| QW-26 | Use `common.emptyValue` instead of em dash hacks | 15m |
| QW-27 | Align confirm dialog z-index to 1050 globally | 20m |
| QW-28 | Replace `.table-card { margin: 20px }` with token | 5m |
| QW-29 | Add “How health score works” tooltip | 30m |
| QW-30 | Update beta-tester-guide billing + ticket sections | 45m |
| QW-31 | Staff server card: add “Open tickets” for ticket staff | 25m |
| QW-32 | Redirect ticket staff to `/tickets` not `/moderation` | 20m |
| QW-33 | Remove stale subscription i18n keys | 20m |
| QW-34 | Overview loading aria-live text | 15m |
| QW-35 | Settings panel ↑↓ aria-labels | 10m |

---

## 16. World-class gap analysis

### vs Linear

| Linear | This product |
|--------|--------------|
| One primary action per view | Overview shows 6+ competing sections |
| Instant keyboard navigation | No shortcuts |
| Unified issue detail drawer | Admin subscription table only |
| Opinionated density | Mixed table/card without system |

### vs GitHub

| GitHub | This product |
|--------|--------------|
| Clear empty repos with next steps | Broken servers checklist |
| Consistent settings sidebar | Settings tabs + separate mod/staff routes |
| Activity feed with icons | Plain text activity |

### vs Discord

| Discord | This product |
|---------|--------------|
| Native context for actions | Constant Discord ↔ dashboard switching without guided loop |
| Role permissions intuitive | Cross-grants + two permission UIs |
| Real-time feel | 30s ticket reply polling |

### vs Stripe

| Stripe | This product |
|--------|--------------|
| Billing self-serve with test cards | Manual billing without payee details |
| Clear invoice/status timeline | Rejection reason hidden from owner |
| Progressive disclosure | Upsell timing inconsistent |

### vs Notion

| Notion | This product |
|--------|--------------|
| Empty pages invite creation | Many weak inline empties |
| Consistent block UI | 4 form patterns |
| Gentle onboarding | No wizard |

### vs Vercel

| Vercel | This product |
|--------|--------------|
| Dashboard = status + one CTA | Overview requires scanning |
| Skeleton loading everywhere | Mixed loading patterns |
| Flawless responsive tables | Horizontal scroll admin queue |

### vs Slack

| Slack | This product |
|--------|--------------|
| Notification center real | Fake bell |
| Channel context preserved | Guild switch OK but staff routing wrong |

**Summary:** Engineering architecture is **mid-stage SaaS**; product polish is **early beta**. World-class gap is **~18–24 months of focused UX/design sprints** at current velocity — or **~6–9 months** with dedicated design system + UX gate on every PR.

---

## Recommended sprint sequence (post-audit)

1. **PR-002 Quick Wins** — P0/P1 items QW-01–QW-35 (1 sprint)  
2. **PR-003 Design System** — tokens, shared components, page widths (2 sprints)  
3. **PR-004 Billing Trust** — payment instructions, rejection UX, renewal banners (1 sprint)  
4. **PR-005 Activation Truth** — wizard, first-value detection, fix activation math (2 sprints)  
5. **PR-006 Staff Journeys** — nav grants, ticket landing, staff overview slice (1 sprint)  
6. **PR-007 Admin Polish** — detail drawer, mobile queue, funnel metrics (2 sprints)  
7. **PR-008 Accessibility & RTL** — audit remediation (1 sprint)  
8. **PR-009 Launch Readiness** — refresh R-001, beta guide, copy pass (1 sprint)  

---

## Appendix: Documents reviewed

- PB-001 Product Blueprint  
- O-001 First-Time User Activation  
- UX-001 Subscription Experience  
- R-001 Release 0.1 Readiness  
- beta-known-limitations.md  
- O-002, SB-003, SB-004 progress reports  
- Dashboard source: layout, overview, subscription, tickets, logs, settings, admin, i18n, tokens  

---

*PR-001 — Audit only. No code modified. This document is the official quality backlog before Release 1.0.*
