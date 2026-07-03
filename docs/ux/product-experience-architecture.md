# PX-001 — Product Experience Architecture

**Document ID:** PX-001  
**Status:** **Mandatory authority** — supersedes conflicting UX guidance unless explicitly revised  
**Owner:** Product Experience Architecture  
**Effective:** 2026-07-03  
**Horizon:** 5+ years — principles over patterns  
**Audience:** Product, design, engineering, localization, support, leadership  

**This document is NOT:** a UI redesign · a design system (see [PP-001](../design/design-system.md)) · CSS · Angular · page wireframes (see page specs e.g. [PR-002 v2 Mission Control](../reviews/overview-redesign-v2.md))

**This document IS:** the highest-level contract for how the Discord Bot Platform **feels**, **behaves**, and **communicates** — across every dashboard page, workflow, and persona.

---

## Document hierarchy

When documents conflict, resolve in this order:

1. **PX-001** — Product Experience Architecture *(this document)*  
2. **PB-001** — Product Blueprint (scope & strategy)  
3. **UL-001** — Ubiquitous Language (terms)  
4. **Page / domain UX specs** — e.g. O-001 Activation, UX-001 Subscription, PR-002 v2 Overview  
5. **PP-001** — Design System (visual implementation)  
6. **Implementation code**  

If a page spec contradicts PX-001, **the page spec must be amended** — not PX-001 silently ignored.

---

## 1. Product philosophy

### 1.1 What we are building

The Discord Bot Platform is an **operations product** for Discord communities — not a generic admin panel, not a game, not a marketing site with a login form.

Operators use it under pressure: moderation incidents, billing deadlines, support backlogs, onboarding new staff. The product must feel like **Mission Control** — calm, honest, decisive — not like Grafana, AdminLTE, or a Bootstrap theme with charts.

### 1.2 Emotions we intentionally create

| Emotion | Meaning for the user | How the product delivers it |
|---------|----------------------|-----------------------------|
| **Trust** | “The product tells me the truth.” | Visible system state, no fake scores, failures surfaced |
| **Confidence** | “I know what to do next.” | One mission per page, one primary CTA, clear recovery |
| **Calm** | “I am not overwhelmed.” | Restraint, whitespace, progressive disclosure |
| **Focus** | “Nothing competes for attention.” | No duplicate nav, no badge forests, no hero stacking |
| **Professionalism** | “This is built for serious operators.” | Precise copy, consistent patterns, bilingual quality |
| **Speed (perceived)** | “The product respects my time.” | Skeletons, optimistic UI where safe, no blank waits |
| **Honesty** | “Bad news is not hidden.” | Stale sync, rejected billing, bot offline — stated plainly |

### 1.3 Emotions we refuse to create

| Avoid | Why |
|-------|-----|
| **Noise** | Competing widgets, alerts, and CTAs destroy comprehension |
| **Clutter** | More cards ≠ more value; operators scan, they do not read dashboards |
| **Gaming UI** | Rings, streaks, XP, confetti for configuration — undermines trust |
| **Fancy animation** | Motion without purpose reads as immature and slows comprehension |
| **Marketing inside the dashboard** | “Unlock your potential”, “Est. 2 min”, fake urgency — erodes credibility |
| **Anxiety** | Ambiguous health scores, hidden rejection reasons, silent redirects |
| **False success** | “Activated” without outcome, “Healthy” without evidence |

### 1.4 The daily-use test

Every major surface must pass:

> *If I owned this Discord server, would I open this page every day — and leave faster than I arrived, knowing what matters?*

If no → redesign the **mission**, not the padding.

---

## 2. Core UX principles

Twenty principles govern all product experience decisions. They are **non-negotiable** unless PX-001 is formally revised.

| # | Principle | Rule |
|---|-----------|------|
| **P-01** | **One page → one mission** | Each route answers one primary question. Secondary work lives in tabs, drawers, or child routes. |
| **P-02** | **One primary CTA** | Maximum one dominant action per viewport. Never two heroes. |
| **P-03** | **Never surprise users** | Destructive, billing, and permission changes require explicit confirmation and outcome copy. |
| **P-04** | **Progress is always visible** | Long operations show state: queued, syncing, under review — not silent spinners. |
| **P-05** | **Waiting always explains why** | “Review usually takes 1–2 business days” beats an infinite spinner. |
| **P-06** | **Errors always explain recovery** | What failed · why (human language) · what to do next. |
| **P-07** | **No hidden system state** | Bot offline, stale sync, expired plan, rejected payment — visible, not discoverable by accident. |
| **P-08** | **No duplicate information** | If sidebar, topbar, or breadcrumb shows it, the page body does not repeat it. |
| **P-09** | **No fake metrics** | Every number links to a definition or source. No decorative KPIs. |
| **P-10** | **No fake health scores** | Aggregate scores must map to inspectable factors; otherwise use plain language (“2 items need attention”). |
| **P-11** | **No fake activation** | Activation requires **first value** (observable outcome), not configuration completeness alone. |
| **P-12** | **Truth over optimism** | Prefer “Not synced yet” over implying success. Prefer “Declined” over silent retry loops. |
| **P-13** | **Scrolling is for history** | Understanding fits above the fold; logs, lists, and archives scroll. |
| **P-14** | **Progressive disclosure** | Default scannable; details in expand, drawer, or detail page — not simultaneous cards. |
| **P-15** | **Persona-aware surfaces** | Owner, staff, and platform admin see different missions — not one UI with disabled buttons. |
| **P-16** | **Discord is the stage; dashboard is the control room** | First value often happens in Discord; dashboard configures, monitors, and explains — not replaces Discord. |
| **P-17** | **Navigation does navigation** | No shortcut grids duplicating sidebar. Muscle memory lives in one nav system. |
| **P-18** | **Bilingual parity is quality** | Arabic is not a translation afterthought — same clarity, same structure, no English API leakage. |
| **P-19** | **Accessibility is product quality** | Keyboard, screen reader, contrast, and RTL are ship criteria — not polish tickets. |
| **P-20** | **Restraint is premium** | When unsure, remove a widget. Linear ships subtraction. |

---

## 3. Product information architecture

### 3.1 Universal page hierarchy

Every dashboard page **roughly** follows this vertical story:

```
MISSION        What is this page for? What decision or work happens here?
    ↓
STATUS         What is the current state? (compact — not a card farm)
    ↓
PRIMARY WORK   The main task: form, table, mission card, or queue
    ↓
HISTORY        Recent changes, activity, audit — scrollable
    ↓
ADVANCED       Drawer, tabs, “Advanced”, docs — optional depth
```

**Never:** 20 equal cards with no narrative order.

### 3.2 Page type missions (platform map)

| Page family | Mission question | Primary work | History |
|-------------|------------------|--------------|---------|
| **Mission Control (Overview)** | What should I do right now? | Mission Card | Activity feed |
| **Lists (Tickets, Logs, Admin queues)** | What needs my attention in this queue? | Filtered table | Pagination / expand row |
| **Settings** | How is this module configured? | Form sections | Save confirmation |
| **Modules** | What is enabled for this plan? | Toggle + plan context | — |
| **Subscription** | What is my billing state? | Change / renew workflow | Change history |
| **Staff / Permissions** | Who can do what? | Role mapping form | — |
| **Profile** | How does the bot represent this server? | Profile form | — |
| **Platform admin** | What requires operator intervention? | Review queue | Audit |

### 3.3 Maximum visible zones

| Context | Max primary zones above the fold |
|---------|----------------------------------|
| Mission pages (Overview) | **5** (see PR-002 v2) |
| Work pages (Tickets, Logs) | **3** — toolbar · primary table · optional summary strip |
| Settings | **2** — tabs · active panel |
| Wizards / flows | **1** — stepper owns the viewport |

---

## 4. Above-the-fold contract

**Principle (P-13):** Users decide trust and next action **before scrolling**. Scrolling is for evidence and history.

### 4.1 Desktop (≥1024px, baseline 1440×900)

**Must be visible without scrolling:**

- Page mission (topbar title + subtitle — not repeated in body)  
- Current status (compact — status line or pulse, not badge row)  
- Primary work entry point (Mission Card, primary table filters, or form head)  
- Start of history (section header + first rows) *only on Mission Control pages*  

**May be below the fold:**

- Full lists beyond first page  
- Advanced settings panels  
- Drawer content (collapsed by default)  
- Secondary recommendations  

### 4.2 Tablet (768–1023px)

**Mindset:** Operator at a table — one hand, portrait or landscape.

- Mission + primary CTA remain above fold  
- Status compresses to 2-row pulse or summary — never horizontal badge overflow  
- Tables become horizontally scrollable **with sticky first column** — not shrunk unreadable text  
- Drawers may become bottom sheets — still collapsed by default  

### 4.3 Mobile (≤767px)

**Mindset:** Triage on the move — **not** desktop stacked.

- **Separate layout order** per page spec — never “desktop column stack”  
- One primary CTA, full width  
- Pulse as horizontal scroll snap — not grid cramming  
- Tables → card rows or dedicated mobile views where necessary  
- Mission pages: Hero → Pulse → Activity → Drawer — **max 4 sections before scroll**  

### 4.4 Why this matters

Premium SaaS products (Stripe, Vercel) treat the first viewport as **the product sentence**. Everything else is paragraph. Admin templates invert this — widgets first, meaning never.

---

## 5. CTA rules

### 5.1 CTA taxonomy

| Type | Visual (PP-001) | Use | Max per viewport |
|------|-----------------|-----|------------------|
| **Primary** | `.btn-primary` | The one action the page exists to drive | **1** |
| **Secondary** | `.btn-secondary` | Alternative safe path (Cancel, Back, Export) | 2–3, never competing size |
| **Ghost** | `.btn-ghost` | Tertiary navigation (View all, Learn more) | Unlimited but visually quiet |
| **Danger** | `.btn-danger` | Irreversible or destructive (Delete, Reject, Clear all) | **1** destructive chain per dialog |

### 5.2 Rules

1. **Maximum one primary CTA** per viewport (above-the-fold window).  
2. **Maximum one destructive CTA** per dialog — never adjacent to primary without separation.  
3. **Never two competing heroes** — alert + hero merge into one Mission Card on Mission Control.  
4. **Verb-first labels** — “Review tickets”, not “Tickets page”.  
5. **Disabled primary must explain why** — tooltip or inline hint, not silent `disabled`.  
6. **Loading primary shows in-button spinner** — do not duplicate with page-level spinner.  
7. **External actions** (Open Discord) are secondary — never primary unless mission is literally “go to Discord”.  

---

## 6. Hero rules (Mission Card pattern)

### 6.1 When a Hero exists

| Condition | Hero |
|-----------|------|
| User must take **one** action before progress continues | Yes |
| Blocker (bot offline, payment rejected, expiry) | Yes — critical/warning severity |
| Setup incomplete (Beginner mode) | Yes — instructional |
| All clear — veteran healthy state | Yes — **no CTA** (calm confirmation) |
| Page is a settings form or data table | **No hero** — form/table is primary work |

### 6.2 When a Hero disappears

- Settings sub-pages — mission is the form, not a banner  
- Admin dense tables — queue is the hero  
- Never stack hero **above** another hero (billing stepper owns its page)  

### 6.3 Allowed content

- Title (one line)  
- Body (one sentence — outcome oriented)  
- **One** primary button OR explicit no-action state  
- Optional: thin progress indicator **inside** card footer (Beginner setup only)  
- Severity border (critical / warning / info / neutral)  

### 6.4 Forbidden content

- Priority overlines (“High priority”)  
- Time estimates (“Est. 2 min”) unless measured and maintained  
- Secondary button competing with primary  
- Multiple CTAs  
- Badges duplicating pulse or topbar  
- Marketing illustrations  
- Health rings or charts  

### 6.5 Maximum height

- Desktop: **200px** including padding  
- Mobile: auto height; CTA full width; no max that pushes pulse below fold  

---

## 7. Status communication architecture

Each channel has a **single job**. Misuse creates noise.

| Channel | Purpose | Lifetime | Examples |
|---------|---------|----------|----------|
| **Topbar subtitle** | Persistent orientation | Always on route | `Pro · Online · Synced 2h ago` |
| **Banner** | Blocker or urgent platform state | Until resolved or session dismiss (non-critical only) | Bot offline, maintenance |
| **Mission Card** | **One** prioritized action or all-clear | Per page load / state change | Renew plan, Review tickets |
| **Toast** | Feedback on user action | 3–5s auto dismiss | Saved, Sync started, Copy failed |
| **Dialog** | Confirm irreversible or collect required input | Until user decides | Delete logs, Reject subscription change |
| **Inline status** | Field or row level | Persistent in context | Validation error, delivery failed on ticket row |
| **Badge** | Compact enum on entity | Persistent on row/card | Open, Closed, Pending review |
| **Activity event** | Historical fact | Immutable timeline | Ticket #42 opened |

### 7.1 Decision matrix

| Situation | Channel |
|-----------|---------|
| Save settings success | Toast |
| Save settings validation fail | Inline + focus first error |
| Clear all logs | Dialog → Toast on success |
| Subscription change rejected | Mission Card or billing page banner — **not dismissible** |
| Bot offline | Mission Card (critical) — not toast only |
| Sync queued 30s | Toast + inline on affected dropdowns |
| Plan expires in 5 days | Mission Card (owner) — not badge only |
| Staff lacks permission | Redirect + toast — not blank 404 |
| Background job still running | Inline progress or polling status — not silent |

---

## 8. Empty state architecture

### 8.1 Philosophy

Empty is a **state**, not a failure. Every empty surface teaches the next step.

**Never:** blank white content area · table with only headers · silent “no data”

### 8.2 Required structure

Every empty state contains:

1. **Illustration** — SVG icon (PP-001); emoji only as interim beta  
2. **Explanation** — one sentence, outcome-focused  
3. **Primary CTA** — start the workflow that fills this surface  
4. **Optional secondary CTA** — docs or alternative path  

### 8.3 Context rules

| Context | Empty behavior |
|---------|----------------|
| Full page (no guilds) | Hero empty — invite bot |
| Table (no tickets) | Inline empty in table region — configure or wait for Discord |
| Widget inside Mission page | Nested compact empty — no card-in-card padding |
| Search no results | “No matches” + clear filters ghost CTA |

### 8.4 Forbidden

- Empty with no CTA when user can act  
- Duplicate empty on same page (one “caught up” message per mission)  

---

## 9. Success moments

Celebration is **proportionate** — professional, not gamified.

| Moment | When | How it should feel | Channel |
|--------|------|--------------------|---------|
| **First ticket opened** | First value for tickets module | Quiet pride — “Support is live” | Toast + optional Mission Card transition |
| **First welcome delivered** | Welcome module first value | Relief — setup worked | Toast |
| **First automation** | Auto-reply or role panel live | Confidence | Inline confirmation on settings save |
| **Subscription activated** | Paid plan approved | Trust — receipt-like clarity | Billing page status + email (future) |
| **First warning prevented** | Moderation log recorded | Professional — no confetti | Activity event |
| **Setup complete** | All phases + first value | Calm graduation to Veteran mode | Mission Card → all-clear pattern |

**Rules:**

- No confetti v1  
- No sound  
- Copy acknowledges **outcome**, not configuration (“Your first ticket is open”, not “Step 5 complete”)  
- Success toasts ≤ 1 per user action  

---

## 10. Error philosophy

### 10.1 Severity levels

| Level | Definition | User can continue? | UI behavior |
|-------|------------|-------------------|-------------|
| **Blocking** | Cannot proceed safely | No | Mission Card / full page / dialog |
| **Recoverable** | Action failed, retry possible | Yes | Toast + inline retry |
| **Informational** | Expected edge case | Yes | Inline hint |
| **Validation** | User input invalid | Yes | Field-level inline, focus first error |
| **Offline** | Network unreachable | Partial | Banner + cached read where possible |
| **Rate limited** | Too many requests | Yes | Toast with wait time |

### 10.2 Error copy structure

Every user-visible error:

```
[What happened] — [Why in plain language] — [What to do next]
```

**Never expose:** exception types, stack traces, snowflake validation jargon, state machine enum names.

**Never map 403 to** “Resource not found” — use permission copy + path to request access.

### 10.3 Developer vs user errors

API returns codes; **product maps codes to i18n**. English API strings never render raw in UI — especially in Arabic locale.

---

## 11. Notification architecture

### 11.1 What deserves each channel

| Channel | Deserves | Does not deserve |
|---------|----------|------------------|
| **Banner** | Ongoing blocker affecting work session | Save success |
| **Toast** | Result of user action | Persistent billing state |
| **Modal** | Irreversible confirm, required admin note | Informational tips |
| **Activity event** | Auditable fact others might review | Transient UI feedback |
| **Email** | Billing receipt, security, async review complete *(future)* | Routine saves |
| **Discord DM** | Critical bot delivery failure to owner *(future, careful)* | Marketing |
| **Nothing (in-app)** | Routine background sync success | — |

### 11.2 Notification budget

- **Max 1 blocking surface** (Mission Card or banner — merged, not both)  
- **Max 3 toasts** stacked  
- **No notification bell** until real inbox exists — hide rather than fake  

---

## 12. Loading philosophy

| Pattern | When | Never |
|---------|------|-------|
| **Skeleton** | Initial page load >300ms | Short button actions |
| **Spinner** | In-button / inline row expand | Full page except auth callback |
| **Optimistic UI** | Toggle module, benign saves | Billing submission, reject approve |
| **Progress bar** | Multi-step known progress | Indeterminate >30s without message |
| **Polling** | Ticket reply delivery, async review | Blind 5s reload without status API |
| **Long-running jobs** | Resource sync, transcript export | Without cancel or ETA hint |

**Never:** empty white page while loading · layout shift when data arrives · skeleton that does not match final layout.

---

## 13. Trust architecture

Trust is the **foundation emotion**. Without it, no feature matters.

### 13.1 Truth commitments

| Never say… | Unless… |
|------------|---------|
| **Healthy** | Inspectable factors support the label; score maps to defined rules users can open |
| **Activated** / **Community is live** | `firstValueAchieved` — observable outcome recorded |
| **Synced** | `resourcesSyncedAt` within defined freshness window; else “Synced {time}” or “Outdated” |
| **Online** | Bot heartbeat or defined heuristic documented; else “Last seen” |
| **Under review** | Workflow state matches backend; show SLA copy |
| **Success** | Operation confirmed by API — not optimistic alone |

### 13.2 Failure honesty

- Rejection reasons **visible to the actor** (owner sees admin note on declined subscription change)  
- Bot permission failures **explained in product language** — not “Missing Access” only  
- Partial API failure → “Some data unavailable” — not silent omission  
- Beta limitations **linked from affected surfaces** — not buried in docs  

### 13.3 Timestamp freshness

Every time-based claim shows **relative + absolute on hover**:

- Synced 2h ago *(Jul 3, 2026, 3:14 AM)*  

Stale thresholds must be **consistent** across Overview, Settings, and dropdown empty states.

### 13.4 Confidence without gamification

Prefer:

- “2 items need attention” + link  
Over:  
- “Health: 82/100” ring with opaque algorithm  

Scores are allowed when **decomposable** and **stable** — not when they change daily from heuristic tweaks.

---

## 14. Copywriting architecture

### 14.1 Product voice

| We are | We are not |
|--------|------------|
| Direct | Verbose |
| Verbs first | Nouns stacked |
| Operator-focused | Developer-focused |
| Calm | Hype |
| Honest | Optimistic filler |

### 14.2 Language rules

1. **Use verbs in CTAs** — “Review tickets”, “Renew plan”, “Enable logs”  
2. **Avoid developer words** — snowflake, endpoint, workflow state, resource, payload  
3. **Avoid moderation jargon** unless audience is mod-only — “Issue warning”, not “Execute moderation workflow”  
4. **One idea per sentence** — body copy max one sentence in heroes  
5. **Consistent terms** — UL-001 wins: Subscription Change not Upgrade Request in UI  
6. **EN + AR parity** — same keys, same meaning; no English fragments in AR UI  
7. **Numbers localized** — dates, plurals, currency via i18n pipes  

### 14.3 Forbidden copy patterns

- “Oops!”  
- “Something went wrong” without recovery  
- “Est. X min” without measurement  
- “High priority” labels visible to users  
- Railway / infrastructure vendor names in user errors  

---

## 15. Accessibility (global rules)

Accessibility is **P-19** — ship blocker for public release.

| Area | Rule |
|------|------|
| **Keyboard** | All flows completable without mouse; logical tab order matches visual order |
| **Focus** | `:focus-visible` on all interactive elements; no focus trap except modals |
| **Screen readers** | Landmarks, labels on icon buttons, `aria-live` for async status |
| **Contrast** | WCAG 2.1 AA minimum for text and controls |
| **Touch targets** | 44×44px minimum on mobile |
| **Reduced motion** | Respect `prefers-reduced-motion` — no essential info in animation only |
| **RTL** | Logical properties; mirrored layout; bilingual strings never mixed in one sentence |
| **LTR** | Numbers and codes may stay LTR inside AR — document in i18n guidelines |

---

## 16. Responsive philosophy

Responsive is **not** “breakpoints on desktop layout.” It is **three mindsets**.

### 16.1 Desktop mindset

Operator at desk — density allowed, keyboard likely, multiple columns for **secondary** work only. Mission stays single-column narrative.

### 16.2 Tablet mindset

Review and approve — tables scroll, CTAs full width, reduced parallel columns.

### 16.3 Mobile mindset

Triage — **fewer sections, bigger targets, horizontal snap for metrics**, no cramming 6 badges into one row.

### 16.4 Progressive disclosure

Default **scannable**; depth via drawer, expand row, detail route — never simultaneous exposition of every module on one page.

---

## 17. SaaS benchmarks

Study premium products for **discipline**, not pixels.

### 17.1 What we adopt

| Product | Pattern we adopt |
|---------|-------------------|
| **Linear** | One focus · restraint · subtract widgets |
| **GitHub** | Activity as narrative timeline · linked entities |
| **Stripe** | Single action-required surface · billing honesty |
| **Vercel** | Mission status hero · skeleton matches layout |
| **Discord** | Push to Discord for bot-native actions when appropriate |
| **Notion** | Empty states invite creation |
| **Slack** | Compact workspace status · apps in nav not on home |

### 17.2 What we deliberately reject

| Anti-pattern | Source | Why rejected |
|--------------|--------|--------------|
| Widget grid dashboards | AdminLTE, Grafana | No single mission |
| Health rings / gamified scores | Generic SaaS templates | Trust (P-10, P-11) |
| Shortcut grid duplicating nav | Legacy admin UIs | P-17 |
| Footer doc links on every page | Corporate portals | Low use; Help in drawer/global |
| Fake notification bell | Placeholder UIs | Trust |
| Chart-heavy home | Analytics products | We are ops, not BI |
| Stacked alert banners | Poor Stripe clones | One blocking surface |

---

## 18. Product consistency rules

Every page must answer **five questions**:

| Question | Where answered |
|----------|----------------|
| **Where am I?** | Topbar title, breadcrumbs, sidebar active state |
| **What is happening?** | Status line, pulse, or table state |
| **What should I do?** | Primary CTA or explicit “no action” |
| **Can I trust this?** | Fresh timestamps, honest labels, visible failures |
| **What changed?** | Activity, toast, or inline diff — not silent mutation |

If any question has no answer on the page → **page fails PX-001 review**.

---

## 19. UX debt rules

### 19.1 Severity

| Level | Definition | Release impact |
|-------|------------|----------------|
| **P0** | Trust break, wrong permission, bilingual break, blocker loop | **Blocks release** |
| **P1** | Mission unclear, competing CTAs, missing empty/loading, mobile broken | Blocks feature launch |
| **P2** | Copy drift, minor duplication, non-critical a11y | Sprint backlog |
| **P3** | Polish, illustration upgrade | Nice to have |

### 19.2 Tracking

- UX debt logged in `docs/project-management/backlog.md` with `UX-` prefix  
- Product audit issues (PR-001) and critiques (PR-003) feed backlog  
- **No “design debt” silently shipped** — if knowingly deferred, document in release notes  

### 19.3 When design debt blocks releases

- Any open **P0** UX item affecting the release surface  
- Missing EN/AR parity on new strings  
- Missing empty + error + loading on new pages  
- PX-001 checklist failure on PR review  

---

## 20. Design review checklist

**Every dashboard PR** must answer **Yes** or **N/A with reason**. Thirty questions:

### Mission & hierarchy

1. Does this page have **one clear mission**?  
2. Does layout follow Mission → Status → Primary Work → History → Advanced?  
3. Are there **≤5 primary zones** (Mission pages) or ≤3 (work pages)?  
4. Is anything duplicated from sidebar or topbar?  

### CTAs & hero

5. Is there **at most one primary CTA** above the fold?  
6. Is there **at most one destructive** action per dialog?  
7. If a Hero exists, does it follow Hero Rules (§6)?  
8. Are there zero competing heroes?  

### Truth & trust

9. Is all status language **truthful** (§13)?  
10. Are timestamps shown with freshness rules?  
11. Are failures visible with recovery paths?  
12. No fake activation / health / synced claims?  

### Copy & i18n

13. Verb-first CTAs?  
14. No developer jargon in user strings?  
15. EN and AR keys added with parity?  
16. No raw API English in UI?  

### States

17. Empty state: illustration + explanation + primary CTA?  
18. Loading: skeleton or inline — not white page?  
19. Error: severity handled per §10?  
20. Success: proportionate feedback per §9?  

### Notifications

21. Correct channel per §7 matrix (not toast for blockers)?  
22. No fake notification UI?  

### Accessibility & responsive

23. Keyboard completable?  
24. Focus visible?  
25. Screen reader labels on icon-only controls?  
26. RTL logical layout verified?  
27. Mobile layout **designed**, not stacked desktop?  
28. Touch targets ≥44px on mobile?  

### System fit

29. PP-001 tokens/components used — no one-off visual language?  
30. Does this conflict with PX-001? If yes, **stop** — amend spec or PX-001 first.  

---

## 21. Governance

### 21.1 Mandatory authority

PX-001 is **mandatory** for all dashboard experience work. Engineers, designers, and PMs cite it in PR descriptions.

### 21.2 Conflict resolution

If another UX document conflicts with PX-001:

- **PX-001 wins** until explicitly revised via PX-001 amendment (version bump + changelog)  
- Page specs (O-*, UX-*, PR-*) implement PX-001 — they do not override principles  
- PP-001 implements visual language — it does not override mission or trust rules  

### 21.3 Amendment process

1. Propose change with rationale and affected principles  
2. Product + design review  
3. Update PX-001 version and `docs/project-management/changelog.md`  
4. Audit affected page specs for alignment  

### 21.4 Related documents

| Document | Relationship |
|----------|--------------|
| [Product Blueprint (PB-001)](../blueprint/product-blueprint.md) | Strategy — PX-001 implements experience half |
| [Ubiquitous Language (UL-001)](../blueprint/ubiquitous-language.md) | Terms — copy must match |
| [Design System (PP-001)](../design/design-system.md) | Visual layer below PX-001 |
| [First-Time Activation (O-001)](./first-time-user-activation.md) | Domain spec — must obey P-11 |
| [Subscription Experience (UX-001)](./subscription-experience.md) | Domain spec — must obey trust chapter |
| [Mission Control Overview (PR-002 v2)](../reviews/overview-redesign-v2.md) | Reference implementation of PX-001 on Overview |

---

## 22. Platform evolution (5-year relevance)

Principles in PX-001 are **stable**; implementations change.

| Future capability | PX-001 still applies via |
|-------------------|-------------------------|
| New modules | One page mission · first value activation |
| Automation builder | Progressive disclosure · no widget grid home |
| Multi-guild operators | Persona + mission per context — no combined dashboard of everything |
| Real-time analytics | Activity + honest metrics — not chart home |
| Mobile app | Mobile mindset §16.3 — triage first |

When new patterns emerge, ask: **Does it increase trust, focus, and honesty — or noise?**

If noise → reject, regardless of competitor feature parity.

---

## Appendix — Quick reference card

```
ONE mission · ONE primary CTA · TRUTH over optimism
Mission → Status → Work → History → Advanced
Scrolling = history · Above fold = understanding
Hero: one action or calm all-clear · Never two heroes
Empty: explain + CTA · Error: recover · Loading: no white void
Trust: Activated = first value · Synced = fresh · Failures visible
Copy: verbs · no jargon · EN/AR parity
Mobile: designed · not stacked desktop
PX-001 wins conflicts
```

---

*PX-001 — Product Experience Architecture — Mandatory authority. No code. Version 1.0.*
