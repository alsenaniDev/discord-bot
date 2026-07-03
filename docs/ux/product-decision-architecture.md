# PX-002 — Product Decision Architecture

**Document ID:** PX-002  
**Status:** **Mandatory authority** — governs what the product shows, when, and why  
**Owner:** Product Architecture  
**Effective:** 2026-07-03  
**Supersedes:** Ad-hoc hero/recommendation logic in page specs (logic migrates here)  
**Audience:** Product, engineering, design, analytics, platform admin  

**Related:** [PB-001 Product Blueprint](../blueprint/product-blueprint.md) · [UL-001 Ubiquitous Language](../blueprint/ubiquitous-language.md) · [PX-001 Product Experience Architecture](./product-experience-architecture.md) · [PR-002 v2 Mission Control](../reviews/overview-redesign-v2.md) · [O-001 Activation](./first-time-user-activation.md) · [UX-001 Subscription](./subscription-experience.md) · [D-001 Ticket Domain](../domains/ticket-management/ticket-domain-blueprint.md)

**This document is NOT:** UI design · CSS · Angular · API contracts · database schema · executable code

**This document IS:** the **Decision Engine** specification — deterministic rules that produce exactly **one winning Mission** (or explicit calm state) for any guild context and persona.

---

## Document hierarchy

```
PB-001   Product Blueprint          — strategy & scope
PX-001   Product Experience         — how product feels (mission, trust, copy)
PX-002   Product Decision           — WHAT to show & WHY (this document)
UL-001   Ubiquitous Language        — terms
Page specs (PR-002 v2, UX-001, …)  — surface binding
PP-001   Design System              — visual rendering
Implementation                       — Mission Engine code
```

**Conflict resolution:**

| If conflict between… | Winner |
|----------------------|--------|
| Page spec hero precedence vs PX-002 | **PX-002** — amend page spec |
| PX-002 vs PX-001 (e.g. two CTAs) | **PX-001** — one primary CTA |
| PX-002 vs PB-001 (scope) | **PB-001** — mission must be in scope |
| Mission copy vs UL-001 | **UL-001** — use Subscription Change not Upgrade Request |

---

## 1. Purpose

### 1.1 Why Product Decision Architecture exists

The platform continuously evaluates **dozens of simultaneous facts**:

- Bot disconnected  
- Synchronization stale  
- Subscription expiring  
- Subscription Change rejected  
- Open ticket backlog  
- Setup incomplete  
- First value not achieved  
- Module locked by plan  
- Staff permission gaps  

Without a **single decision authority**, each page invents its own widgets, badges, heroes, and recommendations. Users see **competing missions**. Engineering embeds product judgment in scattered `if` statements. Product changes require code archaeology.

PX-002 exists so the platform never asks:

> *“What widgets should this page show?”*

It always answers:

> *“What is the single most important thing this user should do right now?”*

### 1.2 Layer definitions

| Layer | Question it answers | Owner document |
|-------|---------------------|----------------|
| **Product** | What problems do we solve? What is in scope? | PB-001 |
| **Experience (UX)** | How should it feel? What emotions? What patterns? | PX-001 |
| **Decision** | What single mission wins? When? For whom? | **PX-002** |
| **UI** | How is the mission rendered visually? | PP-001 + page specs |
| **Decision Engine** | Runtime evaluation of rules → one Mission object | Implementation of PX-002 |
| **Mission Engine** | Synonym for Decision Engine in product language — produces Mission for surfaces | Same |

**Decision Engine** is the architectural name. **Mission Engine** is the product name used in specs (PR-002 v2 Mission Card consumes its output).

---

## 2. Decision philosophy

Ten principles govern every rule in this document. Each is **deterministic** — no ML, no scoring opacity, no “smart” defaults.

### DP-01 — Truth over optimism

A mission may only appear when its **trigger condition is true** in authoritative data (database, bot heartbeat, workflow state). Never invent urgency. Never imply “Synced” when stale.

### DP-02 — Urgency over popularity

The mission that **blocks community operations** beats the mission that **improves configuration**. Bot offline beats “invite staff” recommendation.

### DP-03 — One primary action

The Decision Engine outputs **one winning Mission** with **one primary CTA** per evaluation context (Overview Mission Card, or page-local mission slot). Secondary actions live in drawer, not competing heroes.

### DP-04 — History never blocks work

Activity timelines, logs, and audit feeds **never** take precedence over actionable missions. History surfaces in Zone 4 (Overview) — it does not win the Mission resolver.

### DP-05 — Critical first

**Critical** and **Blocking** missions always beat **Recommendation** and **Information**. See §4 priority order.

### DP-06 — Human attention is limited

If two missions feel equally important, **the resolver must pick one** using documented tie-breakers — never show both. Users do not choose between heroes.

### DP-07 — Never show two competing missions

Banner + hero + setup card + recommendation row for the same underlying issue is **forbidden**. One condition → one mission ID.

### DP-08 — No fake urgency

“Renew soon” appears only inside defined day thresholds. “Ticket backlog” only above defined counts or age rules. No rotating promotional missions.

### DP-09 — Persona truth

A mission shown to a user must be **actionable by that persona** given Permission Roles and subscription. Impossible missions are **filtered out**, not shown disabled.

### DP-10 — Rules decide; AI recommends only

Future AI may **suggest** new mission candidates or copy improvements. AI **never** selects the winning mission. See §12.

---

## 3. Mission object

The **Mission** is the canonical output of the Decision Engine. All dashboard surfaces that need “what next?” consume a Mission (or explicit `EverythingOperational` calm mission).

### 3.1 Field specification

| Field | Type (conceptual) | Required | Description |
|-------|-------------------|----------|-------------|
| **MissionId** | Stable string enum | Yes | Permanent identifier — never reused for different meaning. Analytics and i18n keys anchor here. |
| **MissionType** | Category enum | Yes | `Blocker` · `Setup` · `Billing` · `Operations` · `Growth` · `Calm` |
| **Priority** | Ordered enum | Yes | See §4 — determines resolver rank |
| **Severity** | Presentation enum | Yes | `critical` · `warning` · `info` · `neutral` — maps to Mission Card border (PX-001 §6) |
| **Persona** | Audience set | Yes | Which personas may receive this mission: Owner, Staff, PlatformAdmin, etc. |
| **Blocking** | Boolean | Yes | If true, guild operations are materially impaired until resolved |
| **TitleKey** | i18n key | Yes | Short headline — one line |
| **DescriptionKey** | i18n key | Yes | Single sentence body — outcome oriented |
| **DescriptionParams** | Key-value map | No | Interpolation: `{days}`, `{count}`, `{planName}`, `{reason}` |
| **CtaKey** | i18n key | No | Omit when calm state — `EverythingOperational` has no button |
| **CtaRoute** | Route or action | No | Dashboard path, or symbolic action: `Sync`, `OpenDiscord` |
| **SecondaryCtaKey** | i18n key | No | Rare — drawer-only links, never second hero button |
| **SecondaryCtaRoute** | Route | No | Paired with SecondaryCtaKey |
| **RequiredPermissions** | Capability flags | No | Mission suppressed if user lacks all listed capabilities |
| **RequiredModules** | Module keys | No | Mission suppressed if module not enabled **and** allowed by plan |
| **SubscriptionRequirements** | Rule | No | e.g. `OwnerOnly`, `PaidPlan`, `AnyPlan` |
| **Dismissible** | Boolean | Yes | Whether user may snooze/dismiss |
| **DismissPolicy** | Enum | If dismissible | `Snooze7Days` · `SessionOnly` · `Never` |
| **ExpiresAt** | Timestamp | No | Mission auto-invalid after time — rare; used for time-bound promos (avoid v1) |
| **AnalyticsKey** | String | Yes | Stable event property — usually equals MissionId |
| **Source** | Enum | Yes | Which evaluator emitted candidate: `PlatformHealth`, `Billing`, `Tickets`, `Activation`, `Recommendations` |
| **Reason** | Internal string | Yes | Human-readable for logs — not shown to user — explains why rule fired |
| **SupportingData** | Structured bag | No | Inspectable facts for drawer/debug: factor keys, counts, timestamps |

### 3.2 Calm mission variant

When no higher-priority mission wins, emit **`EverythingOperational`** (or persona-specific calm variant):

- `Blocking`: false  
- `Severity`: neutral  
- `CtaKey`: null  
- Body includes **honest** health summary if available — not fake praise  

---

## 4. Mission priority levels

Priority is **total order** — lower number wins.

| Rank | Priority level | Meaning | Typical MissionType | Blocking default |
|------|----------------|---------|---------------------|------------------|
| **1** | **Critical** | Platform or billing failure — community or payment at risk | Blocker, Billing | true |
| **2** | **Blocking** | Operations impaired — must fix to use product as intended | Blocker, Operations | true |
| **3** | **ActionRequired** | Time-bound or SLA risk — action needed soon | Billing, Operations | false |
| **4** | **Important** | Setup or configuration gap preventing first value | Setup | false |
| **5** | **Recommendation** | Improvement with measurable benefit — not blocking | Growth | false |
| **6** | **Information** | Awareness only — may appear in drawer, not Mission Card | Growth | false |
| **7** | **History** | Never wins Mission resolver — activity feed only | — | — |

**Rule:** Only priorities **1–5** may win Overview Mission Card. **Information** appears in Context Drawer Suggestions tab. **History** never competes.

---

## 5. Mission resolver

The **Mission Resolver** is the deterministic pipeline that selects one winning Mission.

### 5.1 Pipeline (architecture)

```
INPUTS
  Guild context (settings, sync timestamps, modules)
  Subscription state (plan, expiry, active Subscription Change)
  Bot connectivity / heartbeat
  Ticket aggregates (open count, age)
  Activation state (first value, setup phases)
  Permission context (viewing user's capabilities)
  Persona (Owner | Staff | PlatformAdmin | …)
  Optional: page scope (Overview | Subscription | Tickets | …)
        ↓
EVALUATE RULES
  Each rule emits zero or one Mission candidate
  Candidates tagged with Priority, MissionId, Source
        ↓
FILTER
  Remove candidates user cannot act on (permissions, modules, persona)
  Remove dismissed/snoozed missions (see §8)
        ↓
SORT BY PRIORITY
  Critical → Blocking → ActionRequired → Important → Recommendation
        ↓
CONFLICT RESOLUTION (§6)
  Tie-break within same priority
        ↓
WINNER
  Single Mission object — or EverythingOperational
        ↓
OUTPUT SURFACES
  Overview → Mission Card (Zone 2)
  Subscription page → page mission slot (billing missions only)
  Tickets page → page mission slot (ticket backlog missions)
  Context Drawer → rank 2–3 Recommendation missions (Information priority)
```

### 5.2 Evaluation frequency

| Trigger | Recalculate |
|---------|-------------|
| User opens Overview | Full evaluation |
| User completes mission CTA destination | On next navigation / poll |
| Webhook/job: subscription approved | Push invalidation — next load |
| Sync completes | Refresh guild context — recalculate |
| Polling interval (ticket delivery) | Does not change mission unless backlog rules crossed |
| User dismisses/snoozes | Immediate re-run — next candidate wins |

**Default:** Full evaluation on **every Overview load** and **every guild context refresh**. Page-local scopes may evaluate subset rules only.

### 5.3 Page scope

| Page | Resolver scope |
|------|----------------|
| **Overview** | Full platform mission — global winner |
| **Subscription** | Billing missions only; if global Critical exists, show inline banner pointing to Overview mission — do not second hero |
| **Tickets** | Ticket operations missions; global Critical still wins in topbar alert chip |
| **Settings / Modules** | No mission hero — form is primary work (PX-001 §6) |

---

## 6. Conflict resolution

When multiple **candidates** survive filtering, apply in order:

### 6.1 Step 1 — Highest priority wins

`Critical` beats `ActionRequired` regardless of source.

### 6.2 Step 2 — Same priority tie-breakers

| Order | Tie-breaker rule |
|-------|------------------|
| 1 | Higher **Blocking** flag (true beats false) |
| 2 | Lower **MissionId lexicographic rank** within frozen precedence table (§6.3) |
| 3 | More recent **trigger timestamp** (e.g. rejection just now vs expiring in 7 days) |

### 6.3 Frozen precedence table (same priority)

Within **Critical**:

1. `BotOffline`  
2. `PaymentRejected`  
3. `SubscriptionExpired`  
4. `GuildSuspended` *(future)*  

Within **Blocking**:

1. `SynchronizationStale` *(only if bot online)*  
2. `BotMissingPermissions` *(future)*  
3. `TicketBacklogCritical`  

Within **ActionRequired**:

1. `SubscriptionExpiringSoon`  
2. `SubscriptionChangePendingReview` *(owner wait state — informational calm copy, not blocker)*  
3. `TicketBacklogElevated`  

Within **Important** (Beginner mode only for setup missions):

1. `CompleteSetupConnect`  
2. `CompleteSetupConfigure`  
3. `CompleteSetupFirstValue`  

Within **Recommendation**:

1. Score from recommendation engine — **deterministic integer rank** documented per mission in catalog  
2. If tie: `InviteStaff` > `EnableLogs` > `CreateReactionPanel` *(example — catalog is source of truth)*  

### 6.4 Worked conflict examples

| Simultaneous conditions | Winner | Why |
|-------------------------|--------|-----|
| Bot offline + Subscription expiring 5d | **BotOffline** | Critical > ActionRequired |
| Payment rejected + Ticket backlog 20 | **PaymentRejected** | Critical rank 2 vs Blocking backlog |
| Sync stale 8d + Setup incomplete | **SynchronizationStale** | Blocking > Important (if bot online) |
| Setup incomplete + Recommendation invite staff | **CompleteSetup*** phase | Important > Recommendation; Beginner mode |
| Ticket backlog 8 + Everything else healthy | **TicketBacklogElevated** | ActionRequired — if above threshold |
| All healthy + Recommendation invite staff | **InviteStaff** | Top recommendation |
| All healthy + no recommendations | **EverythingOperational** | Calm mission |

---

## 7. Mission lifetime

| Phase | Behavior |
|-------|----------|
| **Created** | Rule trigger true → candidate enters pool |
| **Displayed** | Winner rendered on surface; emit `MissionShown` |
| **Acted** | User clicks CTA → emit `MissionCompleted`; navigate |
| **Resolved** | Underlying condition false on next evaluation → mission disappears **without** dismiss |
| **Dismissed** | User snooze/dismiss → hidden per §8 until policy expires |
| **Expired** | `ExpiresAt` passed — candidate removed |
| **Superseded** | Higher priority mission wins on re-evaluation — lower mission not shown |

### 7.1 Refresh rules

- **Auto-disappear:** When trigger condition clears (bot online, sync fresh, ticket count below threshold)  
- **Never auto-disappear until acted:** `PaymentRejected` — remains until owner views billing and submits new change or admin resolves  
- **Recalculate on:** Overview load, guild sync, subscription workflow transition, module toggle, ticket open/close crossing threshold  

---

## 8. Dismiss rules

| Category | Missions | Dismissible | Policy |
|----------|----------|-------------|--------|
| **A — Never dismiss** | BotOffline, PaymentRejected, SubscriptionExpired, GuildSuspended | No | Must act or condition clears |
| **B — Never dismiss (owner wait)** | SubscriptionChangePendingReview | No | Wait for platform admin |
| **C — Snooze 7 days** | Recommendation missions, SyncStale (warning only), InviteStaff, EnableLogs | Yes | `Snooze7Days` per MissionId + guildId + userId |
| **D — Session only** | Informational tips *(future)* | Yes | Reappears next session |
| **E — Auto-clear** | EverythingOperational | N/A | Not dismissible — replaced when new mission wins |

**Rules:**

- Dismiss **never** deletes underlying problem — only hides lower-priority missions  
- Snooze **cannot** hide Category A missions  
- Payment rejection **must** remain visible on Subscription page even if user snoozed a recommendation on Overview  

---

## 9. Personas

| Persona | Definition | Mission sources | Suppressed missions |
|---------|------------|-----------------|---------------------|
| **Guild Owner** | Discord guild owner with full guild authority | All owner-relevant missions | None owner-specific |
| **Guild Staff Member** | User with Permission Role capabilities | Operations: tickets, logs, moderation view | Billing, subscription, setup connect, module purchase |
| **Support Agent** | Staff with ticket capabilities only | TicketBacklog*, BotOffline (if affects replies) | Billing, modules setup, staff invite |
| **Platform Administrator** | Platform admin persona | PlatformAdmin queue missions *(separate resolver scope on /admin)* | Guild-scoped owner missions |
| **Future Enterprise** | Org-level admin across guilds *(planned)* | Aggregated critical/blocking across portfolio | Per-guild setup missions |

### 9.1 Overview persona matrix

| Mission | Owner | Staff | Support |
|---------|-------|-------|---------|
| BotOffline | ✓ | ✓ | ✓ |
| PaymentRejected | ✓ | — | — |
| SubscriptionExpiringSoon | ✓ | — | — |
| CompleteSetup* | ✓ | — | — |
| TicketBacklog* | ✓ | ✓ | ✓ |
| InviteStaff | ✓ | — | — |
| EverythingOperational | ✓ | ✓ (staff variant copy) | ✓ |

Staff variant calm copy: **“No urgent actions for your role.”**

---

## 10. Permission integration

Mission emission pipeline **filters before sort**:

### 10.1 Module before permission (PB-001)

If mission requires Module `tickets`:

1. Module enabled for guild  
2. Module allowed by Subscription Plan  
3. User has ticket capability  

If (1) or (2) fails → mission **suppressed**, not disabled UI.

### 10.2 Capability mapping (examples)

| Mission | Required capabilities |
|---------|----------------------|
| CompleteSetupConfigure | `canManageSettings` |
| TicketBacklogElevated | `canAccessTickets` |
| InviteStaff | `canManageStaff` or owner |
| PaymentRejected | `canManageSubscription` + Owner |
| EnableLogs | `canManageSettings` + logs module |

### 10.3 Guild ownership

Billing missions require **Guild Owner** identity — not delegable to Permission Role in v1.

### 10.4 Platform admin

Admin missions (`PendingSubscriptionChangesReview`, etc.) use **separate catalog** — never mixed into guild Overview resolver.

---

## 11. Mission catalog

Permanent **MissionId** registry. Adding a mission requires PX-002 amendment + analytics registration.

**Legend:** P = Priority level · B = Blocking · D = Dismiss policy · Exp = expiry

### 11.1 Platform health

| MissionId | Purpose | P | B | CTA route | D | Exp |
|-----------|---------|---|---|-----------|---|-----|
| **BotOffline** | Bot not connected — features unavailable in Discord | Critical | yes | OpenDiscord | Never | — |
| **BotMissingPermissions** *(planned)* | Bot lacks channel permissions | Blocking | yes | Settings / docs | Never | — |
| **SynchronizationStale** | Resource cache older than freshness SLA | Blocking | no* | Sync | Snooze7Days | — |
| **SynchronizationNever** | Guild never synced after invite | Important | no | /servers | Never | — |

*Blocking = false but Priority Blocking tier when bot online; impairs configuration accuracy.

### 11.2 Billing & subscription (UL-001: Subscription Change)

| MissionId | Purpose | P | B | CTA route | D | Exp |
|-----------|---------|---|---|-----------|---|-----|
| **SubscriptionExpired** | Paid plan expired — modules locked | Critical | yes | subscription | Never | — |
| **PaymentRejected** | Subscription Change rejected — adminNote exists | Critical | yes | subscription | Never | — |
| **SubscriptionExpiringSoon** | Paid plan expires within 7 days | ActionRequired | no | subscription | Snooze7Days | — |
| **SubscriptionChangePendingReview** | Owner waiting admin review | Information | no | subscription | Never | — |
| **SubscriptionChangePendingPayment** | Owner must submit payment reference | Important | no | subscription | Never | — |
| **PaymentRequired** | Free plan — upgrade needed for module | Recommendation | no | subscription | Snooze7Days | — |
| **RenewSoon** | Alias precedence below SubscriptionExpiringSoon — **do not emit both** | — | — | — | — | — |

### 11.3 Activation & setup (O-001)

| MissionId | Purpose | P | B | CTA route | D | Exp |
|-----------|---------|---|---|-----------|---|-----|
| **CompleteSetupConnect** | Phase A: bot invite + link guild | Important | no | /servers | Never | — |
| **CompleteSetupConfigure** | Phase B: enable + configure module | Important | no | settings / modules | Never | — |
| **CompleteSetupFirstValue** | Phase B: achieve first value event | Important | no | dynamic | Never | — |
| **CreateWelcome** | Welcome module enabled but not configured | Recommendation | no | settings | Snooze7Days | — |
| **CreateTicketPanel** | Tickets enabled — category/panel missing | Recommendation | no | settings | Snooze7Days | — |
| **EnableModule** | No module enabled yet | Recommendation | no | modules | Snooze7Days | — |

**Beginner mode:** Only one setup mission shown — current phase per O-001 three-phase model.

### 11.4 Operations

| MissionId | Purpose | P | B | CTA route | D | Exp |
|-----------|---------|---|---|-----------|---|-----|
| **TicketBacklogCritical** | Open tickets ≥ critical threshold OR any open > SLA age | Blocking | yes | tickets | Never | — |
| **TicketBacklogElevated** | Open tickets ≥ warning threshold | ActionRequired | no | tickets | Snooze7Days | — |
| **ReviewLogs** | Logs module on — no activity in 7d *(optional)* | Recommendation | no | logs | Snooze7Days | — |
| **PendingReports** *(planned)* | Moderation cases awaiting review | ActionRequired | no | moderation | Snooze7Days | — |

**Thresholds (configurable constants — document in implementation):**

- Critical: ≥10 open OR ≥1 open >72h  
- Elevated: ≥5 open OR ≥3 open >48h  

### 11.5 Growth & calm

| MissionId | Purpose | P | B | CTA route | D | Exp |
|-----------|---------|---|---|-----------|---|-----|
| **InviteStaff** | Activation complete — no Permission Roles | Recommendation | no | staff | Snooze7Days | — |
| **CreateReactionPanel** | Plan allows — no active reaction roles | Recommendation | no | reaction-roles | Snooze7Days | — |
| **GuildHealthy** | Deprecated — use **EverythingOperational** | — | — | — | — | — |
| **EverythingOperational** | No higher mission — calm state | Recommendation* | no | none | — | — |

*Priority Recommendation tier but **no CTA** — special calm presentation.

### 11.6 Platform admin catalog (separate resolver)

| MissionId | Purpose | P | Surface |
|-----------|---------|---|---------|
| **AdminSubscriptionChangesReview** | Queue has UnderReview items | ActionRequired | /admin/upgrade-requests |
| **AdminGuildsAttention** | Guilds in error state | Blocking | /admin |

Guild Overview resolver **does not** emit admin missions.

---

## 12. Future AI

| Rule | Detail |
|------|--------|
| **AI does not decide** | Winning mission always from PX-002 rules |
| **AI may recommend** | Suggest new Recommendation candidates with score — human approves catalog addition |
| **AI may draft copy** | i18n suggestions reviewed by localization — not auto-shipped |
| **AI may rank within Recommendation** | Only if rank function is **documented, versioned, and overrideable** by frozen table — default v1: **no AI rank** |
| **Audit** | Any AI suggestion logged — never silently alters MissionId precedence |

Architecture enforcement: Mission Resolver accepts only **registered MissionId** from catalog §11. Unknown IDs rejected at build time in implementation reviews.

---

## 13. Analytics

Every mission lifecycle event emits analytics (implementation binds to AnalyticsKey).

| Event | When | Required properties |
|-------|------|---------------------|
| **MissionShown** | Winner rendered | guildId, userId, persona, MissionId, Priority, Source, pageScope |
| **MissionCompleted** | Primary CTA clicked | + ctaRoute, timeOnScreenMs |
| **MissionDismissed** | User snooze/dismiss | + dismissPolicy, snoozeUntil |
| **MissionIgnored** | Shown but user navigates away without action within session | + timeOnScreenMs |
| **MissionExpired** | ExpiresAt or condition cleared without click | + resolutionReason |
| **MissionSuperseded** | Replaced by higher priority on refresh | + previousMissionId, newMissionId |

**Product metrics derived:**

- Mission completion rate by MissionId  
- Time-to-first-value (CompleteSetupFirstValue completed)  
- Snooze rate — indicator of nuisance missions  
- Ignored rate on Recommendations — catalog pruning signal  

---

## 14. Governance

### 14.1 Authority

PX-002 is **mandatory** for any feature that surfaces “what should I do next?”

### 14.2 MissionId permanence

- MissionId strings are **immutable** once shipped  
- Never repurpose `PaymentRejected` for a different trigger  
- Deprecate by marking **inactive** in catalog — analytics retain historical MissionId  

### 14.3 Change process

| Change type | Requirement |
|-------------|-------------|
| **Add mission** | PX-002 amendment · catalog row · analytics key · EN/AR keys · persona matrix |
| **Change priority** | PX-002 version bump · migration note · replay analytics impact |
| **Change threshold** | Document constant · QA scenarios updated |
| **Change tie-break order** | Version bump · explicit CTO sign-off |

### 14.4 Implementation gate

Engineers implement **Mission Engine** — they do not invent:

- New MissionId without catalog entry  
- New precedence without §6 update  
- Page-local hero logic outside resolver output  

PR checklist adds PX-002 questions:

1. Which MissionId(s) does this feature emit?  
2. Priority and dismiss category?  
3. Persona matrix updated?  
4. Conflict scenarios documented?  

---

## 15. Examples

### 15.1 Scenario — Bot offline, subscription healthy, tickets OK

```
Persona: Guild Owner
Inputs:
  botOnline = false
  subscription = Active Pro
  openTickets = 2
  firstValueAchieved = true

Candidates:
  BotOffline (Critical)
  EverythingOperational (blocked — condition false)

Winner: BotOffline
Mission Card:
  Title: Bot is disconnected
  Body: Members won't receive bot features until it reconnects.
  CTA: Open Discord
```

### 15.2 Scenario — Everything healthy, staff not invited

```
Persona: Guild Owner
Inputs:
  all blockers clear
  firstValueAchieved = true
  permissionRoleCount = 0
  recommendation rank: InviteStaff

Candidates:
  InviteStaff (Recommendation)

Winner: InviteStaff
  CTA: Add staff roles
```

### 15.3 Scenario — Beginner setup phase B

```
Persona: Guild Owner
Mode: Beginner
Inputs:
  firstValueAchieved = false
  modulesEnabled = true
  moduleConfigured = false

Candidates:
  CompleteSetupConfigure (Important)

Winner: CompleteSetupConfigure
  Progress bar: Configure phase active
  CTA: Open settings
```

### 15.4 Scenario — Payment rejected + ticket backlog

```
Persona: Guild Owner
Inputs:
  subscriptionChange.status = Rejected
  openTickets = 15

Candidates:
  PaymentRejected (Critical)
  TicketBacklogCritical (Blocking)

Winner: PaymentRejected
Reason: Critical tier + precedence table §6.3
Drawer: TicketBacklog still visible in Pulse + Suggestions — not Mission Card
```

### 15.5 Scenario — Staff support agent, ticket backlog

```
Persona: Support Agent
Inputs:
  openTickets = 12
  botOnline = true

Candidates:
  TicketBacklogElevated (ActionRequired)

Winner: TicketBacklogElevated
  CTA: Review tickets

Suppressed: PaymentRejected (no billing capability)
```

### 15.6 Scenario — Veteran all clear

```
Persona: Guild Owner
Inputs:
  all rules pass
  no recommendation score ≥ threshold

Winner: EverythingOperational
  Title: Everything looks good
  Body: No action required. Community health: 92.
  CTA: none
```

### 15.7 Scenario — Subscription expiring + sync stale

```
Persona: Guild Owner
Inputs:
  expiresInDays = 5
  syncAgeDays = 8
  botOnline = true

Candidates:
  SubscriptionExpiringSoon (ActionRequired)
  SynchronizationStale (Blocking)

Winner: SynchronizationStale
Reason: Blocking tier > ActionRequired
Note: Expiring subscription shown in Pulse plan cell — Mission Card owns sync CTA
```

---

## Appendix A — Mapping to PR-002 v2 surfaces

| PX-002 output | PR-002 v2 surface |
|---------------|-------------------|
| Winning Mission | Zone 2 Mission Card |
| Pulse metrics | Zone 3 Community Pulse |
| Rank 2–3 Recommendation missions | Zone 5 Drawer → Suggestions tab |
| Activity events | Zone 4 — never missions |
| Topbar status | Zone 1 — orientation only, not a mission |

PR-002 v2 `stateKey` maps 1:1 to **MissionId** — rename implementation to MissionId for consistency.

---

## Appendix B — Mapping to existing services

| Today | PX-002 target |
|-------|---------------|
| `GuildOverviewExperienceService.BuildRecommendations` | Recommendation **candidate** emitter only |
| `BuildActivationProgress` | Setup **candidate** emitter — phase missions |
| Hero precedence in PR-002 v2 doc | **§6 Conflict resolution** |
| `isActivated = progressPercent >= 85` | **Wrong** — use `firstValueAchieved` for Veteran mode; setup missions for Beginner |

---

## Appendix C — Version history

| Version | Date | Change |
|---------|------|--------|
| 1.0 | 2026-07-03 | Initial PX-002 — Mission catalog, resolver, governance |

---

*PX-002 — Product Decision Architecture — Mandatory authority. No code.*
