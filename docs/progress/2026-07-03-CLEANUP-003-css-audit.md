# CLEANUP-003 — CSS Usage Audit

**Date:** 2026-07-03  
**Task ID:** CLEANUP-003  
**Status:** Complete (report only)  
**Parent:** CODEBASE-AUDIT-001  
**Deliverables:** `docs/reviews/css-cleanup-audit.md`

---

## Summary

Performed a read-only CSS usage audit across dashboard global styles (`components.css`, `design-system.css`, `workspace-layouts.css`, `rtl.css`) and obvious feature duplication (profile preview, tickets conversation). No CSS files were modified. Findings are categorized by risk for a future CLEANUP-004+ pass.

---

## Objective

After Design System v2 workspace adoption, identify unused or duplicated CSS safely — without deleting anything yet. Produce actionable candidate lists with grep evidence and risk ratings.

---

## Method

1. Parsed selector blocks in target CSS files (~2,785 lines total).
2. Cross-referenced every candidate class against `src/**/*.html`, inline templates in `*.ts`, and `src/**/*.css`.
3. Manually verified dynamic bindings (`[class.ws-*]`, `[class.badge-*]`, `[attr.data-status]`).
4. Compared parallel implementations (profile preview vs `ws-discord-*`, tickets conversation vs design-system timeline).

---

## Key findings

| Finding | Count / size | Risk |
|---------|--------------|------|
| Unused `.ds-*` alias halves (buttons, forms, badges, tables, stats) | ~8 blocks, ~180 lines | Probably safe / Safe |
| Unused `.card-*` variants in `design-system.css` | 12 blocks, ~110 lines | Safe |
| Unused `.ws-*` utilities (divider, info-row, footer-hint, grid variants) | 6 blocks, ~40 lines | Probably safe |
| `.ws-sticky-rail` orphaned but grouped with active `.ws-aside--sticky` | 4 lines + media queries | Needs visual review (split selectors) |
| Duplicate badge/button layers (`.badge-*` + `.ds-badge-*` + `[data-status]`) | 3 layers | Needs visual review before consolidation |
| Profile preview vs `ws-discord-*` parallel embed styling | ~150 lines overlap | Needs visual review |
| Tickets conversation: global `.conversation-*` vs local `tickets-conversation-*` | ~55 lines overlap | Needs visual review |
| `.page-medium`, `.request-card`, `.metric-tile*`, `.action-tile*` | ~60 lines | Safe / Probably safe |
| Active systems confirmed **Keep** | `.btn*`, `.ds-dropdown*`, `.ws-layout/atf/workspace/toolbar`, `.confirm-dialog`, `.type-label`, `.table-card` | Keep |

---

## Files changed

| File | Action |
|------|--------|
| `docs/reviews/css-cleanup-audit.md` | **Created** — full audit report |
| `docs/progress/2026-07-03-CLEANUP-003-css-audit.md` | **Created** — this progress report |
| Dashboard CSS source | **Not modified** |

---

## Validation performed

- Static grep across full dashboard `src/` tree
- Verified CLEANUP-001/002 removals (`welcome-variables`, `metric-card`, `community-pulse` UI) left no orphan component CSS dependencies
- Confirmed `ws-atf--band`, `ws-workspace--sections`, `ws-page--compact`, `ws-grid--action-main` are **in use** (correcting initial heuristic false negatives)
- Confirmed `.ws-sticky-rail` is unused but shares rules with `.ws-aside--sticky` — flagged for split, not block delete

---

## Breaking changes

None — audit only.

---

## Risks

| Risk | Mitigation |
|------|------------|
| False negatives from dynamic class names | Manual review of `[class.*]` bindings before CLEANUP-004 |
| Deleting comma-grouped `.ds-*` aliases incorrectly | Only remove alias half; never remove canonical `.btn`/`.badge`/`.card` |
| Profile preview consolidation breaks field grid layout | Migrate incrementally; keep profile-specific field CSS |
| RTL regressions after `.ds-table` removal | Run AR locale smoke test on admin tables |

---

## Follow-up tasks (recommended order)

| ID | Task | Scope |
|----|------|-------|
| CLEANUP-004a | Strip unused `.ds-*` alias halves from `components.css` | ~80 lines |
| CLEANUP-004b | Remove unused `design-system.css` card/badge/action-tile blocks | ~200 lines |
| CLEANUP-004c | Remove orphan `.ws-*` utilities (split `.ws-sticky-rail` first) | ~40 lines |
| CLEANUP-004d | Remove `.ds-table*`, `.page-medium`, `.request-card`, unused typography | ~70 lines |
| CLEANUP-005 | Consolidate profile preview → `ws-discord-*` | ~110 lines net |
| CLEANUP-006 | Unify ticket conversation CSS | ~55 lines |

**Estimated total removable CSS:** 350–550 lines global + ~150 lines feature consolidation.

---

## Related docs

- [CSS Cleanup Audit (full report)](../reviews/css-cleanup-audit.md)
- [CODEBASE-AUDIT-001](../progress/) — parent dead-code audit
- [Design System — Workspace Layouts](../design-system/WorkspaceLayouts.md)
