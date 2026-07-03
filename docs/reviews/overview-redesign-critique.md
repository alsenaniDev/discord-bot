# PR-003 — Overview Redesign Critique (Pre-Implementation)

**Critique ID:** PR-003  
**Date:** 2026-07-03  
**Reviewer role:** Principal Product Designer (external — not PR-002 author)  
**Subject:** [PR-002 Overview Redesign Proposal](./overview-redesign-review.md)  
**Stance:** **Reject implementation as written.** Revise before engineering starts.  
**Verdict:** PR-002 is a **competent rearrangement** of O-002, not a world-class redesign. It would land at **7/10**, not 8.5/10.

---

## Executive summary

PR-002 correctly diagnoses that O-002 has **no focal point**. Its prescription, however, is to **add more sections** (9 stacked zones vs 7 today) while claiming “reduce card count.” That is contradictory.

The proposal imports **patterns by name** (Stripe alerts, Vercel hero, Linear focus) without importing **discipline** (fewer surfaces, one job per screen, nav does navigation). After implementation, a guild owner would still scroll through: status strip → up to 3 alerts → hero card → setup progress → health ring → 4 metric chips → next steps → 5 shortcuts → activity feed → activity sidebar → modules card → subscription card → resources footer.

**That is not a command center. That is a longer dashboard.**

Linear would not ship this page. Stripe would ship **half** of it. GitHub would put most of this in sub-pages.

**Recommendation:** Cut the IA to **5 zones maximum**, merge hero with setup for new users, delete shortcuts and resources, demote modules/subscription to overflow or sidebar context, and define a **veteran-user mode** that fits above the fold on a 13" laptop without scroll.

---

## The central failure of PR-002

PR-002 optimizes for **checklist completeness** (“answer every mission question”) instead of **decision completeness** (“tell me the one thing that matters now”).

| PR-002 claim | Reality |
|--------------|---------|
| “One primary action” | Hero CTA + setup CTA + next step links + shortcuts + alert CTAs + snapshot CTAs = **6+ action surfaces** |
| “Depth on demand” | Modules, subscription, resources, activity sidebar are **always visible** at page bottom |
| “10-second comprehension” | Wireframe requires **~3 viewport heights** of scrolling on desktop |
| “Target 8.5/10” | Adds complexity that world-class products **remove** |

---

## Section-by-section critique

For each section: existence, placement, merge/split, size, usefulness, read rate, and whether premium products would build it.

---

### 1. Status strip (replaces Community header)

| Question | Answer |
|----------|--------|
| **Should it exist?** | **Partially.** Guild context already lives in sidebar + topbar `h1`. A strip repeats context. |
| **Should it move?** | **Merge into topbar** for desktop; eliminate in-page strip entirely. |
| **Merge?** | Yes — topbar becomes: breadcrumbs · plan pill · bot dot · sync time. Avatar redundant with server switcher. |
| **Smaller / larger?** | Should be **zero height** as a separate row. PR-002’s 72px strip is 72px of nothing new. |
| **Disappear?** | **Yes, as a section.** Keep only **inline meta** in topbar. |
| **Actually useful?** | Plan + bot status: yes. Health score + setup % in strip: **duplicates** hero and health row. |
| **Would users read it?** | Badges: glanced, not read. “Setup 60%” next to “Health 82” is **two scores with no explanation**. |
| **Would Stripe build it?** | No. Stripe home leads with **balance + action**, not badge row. |
| **Would Linear build it?** | No. Linear header is **minimal**; project context is in sidebar. |
| **Would GitHub build it?** | Partial — repo header exists but **one line**, not 5 pills. |

**How can this be better?**  
Kill the strip. Extend topbar subtitle: `Pro · Connected · Synced 2h ago`. One line. Done.

---

### 2. Critical alerts

| Question | Answer |
|----------|--------|
| **Should it exist?** | **Yes — but max 1 visible**, not 3 stacked. |
| **Should it move?** | **Replace hero** when alert severity = blocker. Don't stack alert + hero. |
| **Merge?** | **Merge alert + hero** into single “Action required” panel (Stripe pattern done correctly). |
| **Smaller / larger?** | One banner, full width, **one CTA**. Not a strip of three. |
| **Disappear?** | When no blockers — **entire zone absent**, not empty card. |
| **Actually useful?** | High — if bot offline or payment rejected. Low — for “sync stale” nag. |
| **Would users read it?** | Yes for P0 blockers. **No** for day-7 sync reminder (becomes wallpaper). |
| **Would Stripe build it?** | Yes — **one** action-required module, dominant. |
| **Would Linear build it?** | Rarely — blockers appear **in context**, not dashboard billboard. |
| **Would GitHub build it?** | Yes — security alerts banner. **One at a time.** |

**How can this be better?**  
Priority queue: show **highest severity only**. “Sync stale” belongs in topbar meta, not alert banner. Session-dismiss on billing failure is **trust-destroying** — remove dismiss for payment/bot offline.

**PR-002 flaw:** Mobile carousel for alerts **hides** critical items behind swipe — unacceptable for P0.

---

### 3. Primary action hero

| Question | Answer |
|----------|--------|
| **Should it exist?** | **Yes — this is the only section PR-002 gets right.** |
| **Should it move?** | **First content block** (after optional single alert). Not below alert stack. |
| **Merge?** | **Merge with setup progress** for users where `setupComplete === false`. One card: progress + single CTA. |
| **Smaller / larger?** | **Larger** — should occupy **40% of above-the-fold** on desktop. PR-002 120–160px is too shy. |
| **Disappear?** | When no actionable state — show **“All clear”** compact state, not empty hero. |
| **Actually useful?** | Yes — if truly one action. |
| **Would users read it?** | Title + CTA: yes. “High priority · Est. 2 min”: **no** — feels like marketing fluff. |
| **Would Stripe build it?** | Yes — but copy is **transactional**, not motivational. |
| **Would Linear build it?** | Linear’s “inbox” is **the product** — hero would be the whole viewport for new teams. |
| **Would GitHub build it?** | “Create your first…” banners — yes, **one**, dismissible after done. |

**How can this be better?**  
Remove “Est. 2 min” unless measured. Remove priority overline — user doesn’t care about your internal priority score. Hero copy = **verb + outcome**: “Configure ticket category → Members can open support tickets.”

**PR-002 flaw:** Hero source toggles between recommendation and activation step — **two different mental models** in one component. Pick one resolver; document precedence.

---

### 4. Setup progress / Activation

| Question | Answer |
|----------|--------|
| **Should it exist?** | **Only for pre-first-value users.** |
| **Should it move?** | **Inside hero** (not below). Vercel onboarding is not a separate card under deployment status. |
| **Merge?** | **Merge with hero** — progress bar at bottom of hero card. |
| **Smaller / larger?** | Smaller — **step dots only**, expand on click. 8-step list is unread. |
| **Disappear?** | **Immediately** after first value — chip row “Setup complete” still adds noise. Delete it. |
| **Actually useful?** | Useful for day 1. **Noise on day 30.** |
| **Would users read it?** | Step list: **no** (users scan dots). |
| **Would Stripe build it?** | Stripe onboarding is **wizard**, not dashboard widget. |
| **Would Linear build it?** | Linear setup is **empty state of product**, not progress bar on home. |
| **Would GitHub build it?** | “Quick setup” checklist — yes, **collapsible**, max 4 items visible. |

**How can this be better?**  
Replace 8-step checklist with **3 phases** (Connect · Configure · First win) — matches O-001 philosophy. Health preview in setup row (PR-002 wireframe col 4) is **bizarre** — health before setup done is punishing, not motivating.

**PR-002 flaw:** Setup row + hero + next steps all describe onboarding — **triple redundancy**.

---

### 5. Health score + metrics row

| Question | Answer |
|----------|--------|
| **Should it exist?** | **Debated.** Rule-based 0–100 “health” is **product fiction** unless user trusts formula. |
| **Should it move?** | If kept: **right column**, secondary. Not row 5 after hero. |
| **Merge?** | **Merge metrics into health** as 3 inline stats — not ring + 4 chips (7 numbers on one row). |
| **Smaller / larger?** | **Much smaller** — text row: `Health: Good · 3 open tickets · 4/6 modules`. No ring. |
| **Disappear?** | For **veteran users**, hide entirely or show only **failing factors**. |
| **Actually useful?** | Failing factors: yes. Aggregate score: **marginal** — feels like upsell gamification. |
| **Would users read it?** | Factor list: **no** unless ≤3 failures shown inline in hero/subtitle. |
| **Would Stripe build it?** | **No health ring.** Stripe shows **metrics that affect money**. |
| **Would Linear build it?** | **No score.** Linear shows **counts** (issues, cycles). |
| **Would GitHub build it?** | Insights yes — but **repo-specific**, not abstract wellness. |

**How can this be better?**  
Kill the ring v1. Show **“2 items need attention”** as link — expands drawer. Ring is **UI debt** for unclear algorithm (PR-001 already flagged opacity).

**PR-002 flaw:** Health score appears in **status strip, setup preview, health row** — tripled again.

---

### 6. Recommendations → Next steps

| Question | Answer |
|----------|--------|
| **Should it exist?** | **Only if hero doesn’t consume top recommendation.** |
| **Should it move?** | **Delete as separate section.** Hero + “See 2 more suggestions” link. |
| **Merge?** | **Merge into hero footer** — not standalone 8-col card. |
| **Smaller / larger?** | Smaller — **0–2 links**, not numbered list with ghost buttons. |
| **Disappear?** | When caught up — **gone**, not empty state with sparkle emoji. |
| **Actually useful?** | Item 2–3 recommendations: low click rate industry-wide. |
| **Would users read it?** | **10–15%** will scroll here. |
| **Would Stripe build it?** | No separate list — **onboarding checklist OR dashboard alert**. |
| **Would Linear build it?** | **No** — triage is the product. |
| **Would GitHub build it?** | Suggested tasks — **sidebar**, not main feed. |

**How can this be better?**  
One hero. Optional drawer: “Other suggestions (2).” Never a permanent widget.

---

### 7. Quick actions → Shortcuts

| Question | Answer |
|----------|--------|
| **Should it exist?** | **No on Overview.** This is **sidebar nav duplicated**. |
| **Should it move?** | **Delete.** Navigation belongs in nav. |
| **Merge?** | N/A |
| **Smaller / larger?** | N/A — **remove** |
| **Disappear?** | **Yes.** |
| **Actually useful?** | Power users already use sidebar. Mobile: **bottom nav** better than icon scroll row. |
| **Would users read it?** | Icons without labels on mobile: **mis-tap city**. |
| **Would Stripe build it?** | No — left nav. |
| **Would Linear build it?** | **Cmd+K** — not icon strip. |
| **Would GitHub build it?** | No — tab bar. |

**How can this be better?**  
Delete shortcuts entirely. PR-002 kept them to avoid political fight with O-002 — **cowardice**, not design.

---

### 8. Subscription snapshot

| Question | Answer |
|----------|--------|
| **Should it exist?** | **Only when action required** (expiring, rejected, pending change). |
| **Should it move?** | **Into alert/hero**, not bottom card. |
| **Merge?** | Merge with billing alert. |
| **Smaller / larger?** | Alert-sized, not 6-col card. |
| **Disappear?** | When subscription healthy — **no card**. Plan name in topbar suffices. |
| **Actually useful?** | High at renewal; **zero** day-to-day. |
| **Would Stripe build it?** | Billing on home **only when issue** or **MRR dashboard** (different product). |
| **Would Linear build it?** | Plan in settings — **not overview**. |
| **Would GitHub build it?** | Billing in settings — **not repo home**. |

**How can this be better?**  
Conditional alert only. PR-002 adds permanent bottom card — **bottom of page = never seen**.

---

### 9. Modules snapshot

| Question | Answer |
|----------|--------|
| **Should it exist?** | **Weak yes** — but not as card. |
| **Should it move?** | **Modules page** is 1 click away. Overview line: “4 modules active” in topbar or hero meta. |
| **Merge?** | Merge with metric chips or delete. |
| **Smaller / larger?** | One line, not 6-col card listing Welcome/Tickets/Logs. |
| **Disappear?** | For users with stable config — yes. |
| **Actually useful?** | Moderate for new owners. |
| **Would Stripe build it?** | Product list in **Products nav**, not home. |
| **Would Linear build it?** | **No** — teams don’t list “features enabled” on home. |
| **Would GitHub build it?** | Actions/workflows — **dedicated tab**. |

**How can this be better?**  
Link in hero when module setup incomplete. Otherwise omit.

---

### 10. Recent activity

| Question | Answer |
|----------|--------|
| **Should it exist?** | **Yes** — best O-002 widget, worth keeping. |
| **Should it move?** | **Second major block** for veteran users (after hero/all-clear). |
| **Merge?** | **Delete activity sidebar** (“Today: 5 events”) — pure duplication. |
| **Smaller / larger?** | 5 items max, compact rows — **not** 8-col + 4-col sidebar. |
| **Disappear?** | Never — but compact empty state. |
| **Actually useful?** | High for operators. |
| **Would users read it?** | Top 3 items: yes. Rest: click through to Logs. |
| **Would Stripe build it?** | **Event timeline** — yes, dense, no sidebar. |
| **Would Linear build it?** | Activity is **the app** — richer than this feed. |
| **Would GitHub build it?** | Activity feed — yes, **full width**, linked entities. |

**How can this be better?**  
Each row links to ticket/log. Remove sidebar. Group by day — good. Add actor avatars if available.

---

### 11. Resources footer

| Question | Answer |
|----------|--------|
| **Should it exist?** | **No.** |
| **Should it move?** | Help menu in profile dropdown or `?` icon. |
| **Merge?** | N/A |
| **Disappear?** | **Yes — delete.** |
| **Actually useful?** | **<2% click rate** on dashboard footers (industry norm). |
| **Would users read it?** | No. |
| **Would Stripe build it?** | Docs link in nav footer globally — **not per-page widget**. |
| **Would Linear build it?** | Help in **?** menu. |
| **Would GitHub build it?** | **No** on repo overview. |

**How can this be better?**  
Delete. PR-002 added this to tick “support reduction” box — **cargo cult**.

---

## Visual hierarchy critique

### Is there one obvious thing to do?

**After PR-002: Still no.**

Competing focal points in order:
1. Red alert banner  
2. Hero primary button  
3. Setup progress CTA (same route as hero sometimes)  
4. Next step ghost buttons  
5. Shortcut icons  
6. Alert inline links  
7. Snapshot “Manage →” links  

Linear’s principle: **one cursor, one target**. PR-002 has **seven**.

### Forced thinking moments

| Moment | Why user pauses |
|--------|-----------------|
| Strip badges | “What's the difference between Health 82 and Setup 60%?” |
| Alert + Hero | “Do I fix sync or configure tickets first?” |
| Hero + Setup | “Are these the same task?” |
| Health ring | “What does 82 mean? Is 70 bad?” |
| Next steps vs Hero | “Why isn’t the top item in the hero?” |
| Shortcuts vs Sidebar | “Why two ways to reach Settings?” |
| Modules bottom | “Did I already see this in metrics?” |

---

## Information density critique

| Assessment | Detail |
|------------|--------|
| **Too empty?** | Hero + strip + generous padding = **large areas, low bytes** |
| **Too crowded?** | Yes — **9 sections** with redundant metrics |
| **Too repetitive?** | **Severe** — health/setup/plan/bot appear 2–3× each |

**PR-002 traded O-002’s “flat chaos” for “tiered chaos.”**

---

## Spacing, typography, CTA, icons, color, badges

| Dimension | PR-002 plan | Critique |
|-----------|-------------|----------|
| **Spacing** | 32px tiers + 16px inner | Good intent; **9 tiers × 32px = 288px gutters alone** — page feels long |
| **Typography** | `.type-*` classes | Correct direction; wireframe still shows many equal `h3` section titles |
| **CTA hierarchy** | Hero primary, rest ghost | Undermined by alerts, setup, snapshots all having CTAs |
| **Icons** | `app-ui-icon` mandate | Good; shortcuts row still icon-only without labels on desktop |
| **Colors** | Semantic badges | Risk of **Christmas tree** — plan + bot + health + priority + status pills |
| **Badges** | Max 3 + overflow | Wireframe violates own rule (shows 4+) |
| **Loading** | 9-row skeleton | **Skeleton length trains user to expect long page** — self-fulfilling |
| **Empty states** | Per-widget nested | Too many “you’re caught up” messages — **empty page of empty states** |
| **Accessibility** | Solid checklist | Alert carousel on mobile **breaks** `role="alert"` persistence |
| **RTL** | Logical properties | Fine; **priority is i18n activity** — PR-002 correctly flags, must be P0 |
| **Mobile** | 12-section scroll | **Worse than desktop** for 10-second goal — fails mission |
| **Tablet** | 8-col variant | Third layout to maintain — **high QA cost** for marginal traffic |

---

## Brutal competitive comparison — why they feel premium

| Product | Why it feels premium | What PR-002 misses |
|---------|---------------------|-------------------|
| **Linear** | **Restraint** — empty space is intentional; home is triage, not widgets | Adds widgets; doesn’t remove |
| **Stripe** | **Money-critical only** on home; everything else is deep nav | Puts modules, resources, shortcuts on home |
| **Vercel** | **One deployment status** — binary mental model (building / ready / error) | Replaces with 8-step setup + health ring + 4 metrics |
| **GitHub** | **Feed + code** — activity is real objects with links | Activity is text lines; sidebar wastes space |
| **Discord** | **Contextual** — you’re always *in* a channel | Overview fights Discord — should push user **to Discord** for first value |
| **Slack** | **Workspace status** minimal; apps live elsewhere | Tries to be app directory on overview |

**Premium ≠ more patterns. Premium = fewer decisions.**

---

## Re-scored areas (PR-002 as proposed — not current O-002)

| Area | PR-002 self-score | **Critique score** | Gap |
|------|-------------------|-------------------|-----|
| Visual Design | 8 | **6.5** | Still badge-heavy; ring is ornament |
| Navigation | — | **5** | Shortcuts duplicate sidebar |
| Hierarchy | 9 | **6** | Multiple primaries |
| Discoverability | — | **7** | Better alerts; worse scroll depth |
| Activation | — | **7** | Truth fix good; triple onboarding UI bad |
| Trust | — | **6** | Fake “health science”; dismissible billing alerts |
| Density | — | **5** | Repetitive metrics |
| Readability | 9 | **7** | Too many section titles |
| Accessibility | 8 | **7** | Alert carousel regression |
| Mobile | 8 | **5.5** | 12 sections = not premium |
| RTL | 9 | **8** | Fine if OV-003 ships |
| **Overall polish** | **8.5** | **6.5–7** | Overstated by ~1.5 points |

**If built exactly as PR-002:** expect **7/10**, not 8.5.

---

## Top 20 improvements: 8.5 → 10/10

Realistic, no fantasy. These revise PR-002 before implementation.

| # | Improvement | Why it moves the needle | Effort |
|---|-------------|------------------------|--------|
| **1** | **Collapse IA to 5 zones:** Topbar meta · Action panel (alert OR hero) · Pulse row · Activity · (optional) Drawer | Removes scroll fatigue; matches Linear restraint | Design 2d |
| **2** | **Merge alert + hero** into single “Action required” component with one CTA | Eliminates dual-primary problem | Design 1d · Dev 2d |
| **3** | **Merge setup into hero** for incomplete users; delete standalone setup row | Removes triple onboarding | Dev 2d |
| **4** | **Delete shortcuts section** entirely | Stops nav duplication | Dev 0.5d |
| **5** | **Delete resources footer**; move to global help menu | Removes dead weight | Dev 0.5d |
| **6** | **Subscription + modules: alert-only surfaces** — no bottom cards | Bottom cards are never read | Dev 1d |
| **7** | **Replace health ring with “N issues” text + expandable drawer** | Trust + clarity; less UI build | Dev 2d |
| **8** | **Veteran mode:** first-value achieved → hide setup, health score, next steps; show hero “All clear” + activity | 80% of visits are not day-1 | Dev 3d |
| **9** | **Staff persona wireframe** — not one line in constraints | Half users may be staff | Design 1d |
| **10** | **Activity rows link to entities** (ticket #, log entry) | GitHub pattern; increases usefulness | Dev 2d |
| **11** | **Remove “Est. 2 min” and “High priority” meta** from hero | Reduces marketing smell | Copy 0.5d |
| **12** | **Single metric pulse row:** `3 open tickets · 4/6 modules · Logs on` — one line, not 4 chips + ring | Stripe density | Dev 1d |
| **13** | **Above-the-fold contract:** 1440×900 — hero + pulse + 3 activity rows **without scroll** | Enforces 10-second rule | Design + QA 1d |
| **14** | **No dismiss on billing/bot alerts** | Trust | Dev 0.5d |
| **15** | **Mobile: max 5 sections**, combine hero+setup, drop snapshots | Mobile premium = short | Dev 2d |
| **16** | **Replace 8 setup steps with 3 phases** in UI | O-001 aligned; scannable | Dev + copy 2d |
| **17** | **Hero precedence rules documented:** blocker > renewal > setup step > recommendation > all-clear | Stops engineering guesswork | Design 0.5d |
| **18** | **Delete activity sidebar column** | Removes duplication | Dev 0.5d |
| **19** | **Empty overview for “all clear” veterans:** activity-only layout | Linear empty calm | Dev 2d |
| **20** | **Prototype test with 5 users before IM-1** — measure 10-second comprehension | PR-002 has zero validation protocol | Research 3d |

**Combined impact:** Could reach **9/10** for beta. **10/10** requires O-003 wizard + real analytics + Discord-native first value loop — outside overview alone.

---

## Recommended revised IA (for PR-002 v2)

```
TOPBAR (extended meta — not a section)
  Guild name (existing) · Plan · Bot · Last sync

ACTION PANEL (one card)
  IF blocker → alert message + single CTA
  ELSE IF setup incomplete → progress (3 phases) + single CTA  
  ELSE IF recommendation → hero
  ELSE → “All clear” compact state

PULSE ROW (one line, optional expand)
  Health summary · open tickets · modules count · logs status

ACTIVITY (full width)
  Last 5 events · link to Logs

[Drawer: other suggestions · health factors · module list]
```

**Section count: 3** (+ topbar). Not 9.

---

## What PR-002 got right (do not throw away)

1. **Activation truth fix** — `firstValueAchieved` vs fake “Activated” — ship this regardless.  
2. **Activity i18n structured events** — P0, non-negotiable.  
3. **Single hero concept** — correct direction, wrong execution (undermined by neighbors).  
4. **Persona-aware data** — mentioned but under-specified; must expand.  
5. **Removing duplicate guild name** — yes, via topbar not strip.  
6. **Backend-owned hero/alerts resolver** — good architecture; simplify outputs.

---

## Implementation gate (Principal Designer sign-off)

**Do not start IM-1** until:

- [ ] IA reduced to ≤5 visible zones  
- [ ] Wireframes for **veteran**, **new owner**, **staff** personas  
- [ ] Above-the-fold contract defined with pixel height  
- [ ] Shortcuts + resources removed from scope  
- [ ] Alert/hero merge approved  
- [ ] Health ring deferred or replaced with text  
- [ ] 5-user comprehension test scheduled  

---

## Appendix — PR-002 issues PR-003 adds

| ID | Issue |
|----|-------|
| CR-001 | 9 sections contradict “command center” thesis |
| CR-002 | Target 8.5/10 is inflated; realistic 7/10 |
| CR-003 | Shortcuts duplicate sidebar — delete |
| CR-004 | Resources footer — delete |
| CR-005 | Bottom modules/subscription cards — wrong placement |
| CR-006 | Health ring — ornament without trust |
| CR-007 | Mobile alert carousel hides P0 |
| CR-008 | Activity sidebar duplicates feed |
| CR-009 | Setup row below hero — redundant |
| CR-010 | No veteran-user wireframe |
| CR-011 | No comprehension test before build |
| CR-012 | “Est. 2 min” undermines credibility |
| CR-013 | 23 dev-days for revised 7/10 ROI — scope too large |
| CR-014 | Staff persona unspecified |
| CR-015 | Health metric tripled across UI |

---

*PR-003 — Independent design critique. No code modified. PR-002 requires revision before implementation.*
