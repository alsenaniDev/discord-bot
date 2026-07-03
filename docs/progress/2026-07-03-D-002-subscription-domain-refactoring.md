# D-002 — Subscription Domain Refactoring (Final Report)

**Date:** 2026-07-03  
**Task:** D-002 — Subscription Domain Refactoring  
**Type:** Domain architecture analysis — no code  
**Decision gate:** Before SB-003

---

## Summary

Completed a domain refactoring review of the Subscription billing workflow centered on **`PlanUpgradeRequest`**. The workflow aggregate is **sound**; the **name and type system are too narrow** for renewal, downgrade, scheduled changes, and admin bypass paths.

**CTO recommendation: Option C** — adopt **Subscription Change Request** as canonical business language **now**; **keep** physical entity/table `PlanUpgradeRequest` through Closed Beta; **extend** with `ChangeType` (Upgrade, Renewal first) in SB-003/0.2; **defer** full rename to Release 0.2.

**Deliverable:** [docs/domains/subscription-billing/subscription-domain-refactoring.md](../domains/subscription-billing/subscription-domain-refactoring.md)

---

## Journey Designed (Conceptual)

Owner flow reframed from upgrade-only to **plan change**:

Current Subscription → Compare plans → Choose target + duration → Review → Submit **Subscription Change Request** → Payment → Review → Active entitlement.

Alternate flows documented: rejection, cancel pending change, expiry, renewal, scheduled change (0.2).

---

## Analysis Highlights

### Current domain (`PlanUpgradeRequest`)

| | |
|--|--|
| **Strengths** | Workflow aggregate, snapshots, SB-002 state machine, Stripe-ready |
| **Weaknesses** | “Upgrade” naming, no ChangeType, no effective date, admin ops bypass request |
| **Scalability** | Renewal/downgrade/schedule need broader language + types by 0.2 |

### Business operations vs “Upgrade Request”

| Models well | Does not model |
|-------------|----------------|
| Upgrade, cancel pending change, manual approve flow | Admin direct assign, extend, cancel subscription |
| Renewal (same shape, wrong name) | Scheduled change, owner downgrade |
| Partial: reactivation, admin override on request | Automatic expiry downgrade |

---

## Canonical Term

**Subscription Change Request** — recommended for UL, UX, API descriptions, admin nav.

Deprecated primary term: **Upgrade Request** (retain as alias in migration period).

Unchanged: **Subscription** (`GuildSubscription`), **Subscription Plan**.

---

## Option Comparison

| Option | Verdict |
|--------|---------|
| **A — Keep upgrade-only** | Reject long-term; OK short-term only |
| **B — Full rename now** | Reject before beta; high churn blocks SB-003 |
| **C — Language now, extend entity, rename later** | **Adopt** |

---

## Request Types (Recommended Rollout)

| ChangeType | When |
|------------|------|
| `Upgrade` | Default today + SB-003 |
| `Renewal` | SB-003/0.2 (same flow, different label/type) |
| `Reactivation` | UX alias of Renewal |
| `Downgrade`, `PlanChange`, `Administrative` | Release 0.2+ |

---

## Effective Date Strategy

| | v1 | 0.2+ |
|--|-----|------|
| Immediate on approve | ✅ Default | ✅ |
| At renewal | ❌ | ✅ |
| Scheduled date | ❌ | ✅ |

---

## UX / Admin (Conceptual)

- Owner: **“Request plan change”** not “Request upgrade”; renewal pre-fills current paid plan.
- Admin: **Subscription Changes** queue (not Upgrade Requests); add Change type + Effective columns when enum exists.
- Scheduled change card — 0.2 only.

---

## Database & API (Recommendations only)

| | Recommendation |
|--|----------------|
| Table rename | Defer to 0.2 |
| Add `ChangeType` | SB-003 or 0.2 (additive) |
| Add `EffectiveAt` | 0.2 |
| API routes | Keep `/upgrade-requests`; add aliases later |

---

## Migration Strategy

1. **Now:** UL + UX copy → Subscription Change Request  
2. **SB-003–005:** Payment flows use new language; optional `ChangeType` column  
3. **0.2:** Effective dates, downgrade types, admin audit requests, route aliases  
4. **1.0:** Optional table rename with Stripe GA  

No data loss; no beta URL breaks.

---

## Risks

| Top risks | Mitigation |
|-----------|------------|
| User confusion (“upgrade” for renewal) | UX copy before SB-003 |
| Doc drift | UL patch + D-002 as authority |
| Rename delays beta | Option C — no rename now |
| Split admin model | 0.2 admin-as-request audit |

---

## CTO Answers (Direct)

| Question | Answer |
|----------|--------|
| Change now? | **Language yes; rename no** |
| Wait until 0.2? | **Full model features yes; terminology no** |
| DB entity + new language? | **Yes — recommended** |
| Fully rename everything? | **Not before beta stabilizes** |

---

## Open Questions

| # | Question | Proposal |
|---|----------|----------|
| OQ-1 | When exactly add `ChangeType` column? | SB-003 if trivial; else 0.2 |
| OQ-2 | Backfill Renewal for same-plan re-requests? | Infer on approve going forward only |
| OQ-3 | UL section rewrite scope? | UL-001 patch in separate doc task |
| OQ-4 | Route alias timing? | 0.2 with external API |
| OQ-5 | Admin extend → change request? | 0.2 audit epic |

---

## Recommendations

1. **Approve Option C** before SB-003 kickoff.  
2. **Update UL-001** `Upgrade Request` section → primary term Subscription Change Request.  
3. **Update UX-001** button/copy strings (plan change, renewal).  
4. **Do not rename** C# entity or table in SB-003.  
5. **Add `ChangeType`** as first additive schema change when convenient.  
6. **Plan 0.2 epic:** effective dates, downgrade, admin audit unification, optional table rename.

---

## Suggested Next Task

**SB-003 — Payment Proof Submission** (unchanged scope)

Additional D-002 follow-ups (can parallel doc-only):

- **UL-002** — Ubiquitous language patch for Subscription Change Request  
- **UX-001.1** — Copy alignment (plan change / renewal labels)

**Deferred to 0.2:** **SB-010 — Subscription Change Types & Effective Dates**

---

## Success Criteria

| Criterion | Met |
|-----------|-----|
| Current domain reviewed | ✅ |
| All customer operations mapped | ✅ |
| Canonical term recommended | ✅ |
| Three options compared with recommendation | ✅ |
| Request types + effective date strategy | ✅ |
| UX + admin conceptual design | ✅ |
| DB + API + migration strategy (no migrations) | ✅ |
| Risks + CTO recommendation | ✅ |
| No code / no migrations | ✅ |

---

## Related Documents

- [Subscription Domain Refactoring (D-002)](../domains/subscription-billing/subscription-domain-refactoring.md)
- [Manual Billing Domain Blueprint (SB-001)](../domains/subscription-billing/manual-billing-domain-blueprint.md)
- [SB-002 Progress Report](2026-07-03-SB-002-manual-billing-foundation.md)
- [UX-001 Subscription Experience](../ux/subscription-experience.md)
