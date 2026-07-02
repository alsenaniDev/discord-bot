# Security

## Threat model (summary)

| Asset | Risk | Mitigation today |
|-------|------|------------------|
| JWT secret | Forgery | Strong secret, env-only storage |
| Bot API key | Unauthorized bot API calls | Shared secret header, bot not public |
| Guild data | Cross-tenant leak | GuildId filtering in services |
| OAuth tokens | Interception | HTTPS in production, one-time code exchange |
| Discord tokens | Exposure | Not stored long-term; OAuth code exchanged server-side |
| XSS on dashboard | JWT theft | **localStorage** — acceptable for beta only |

## Authentication security

See `authentication.md`.

| Control | Status |
|---------|--------|
| HTTPS in production | Required (Railway/Vercel) |
| JWT expiry | Configured in JwtTokenService |
| One-time OAuth code | 2-minute TTL, MemoryCache |
| Bot API key rotation | Manual process documented |
| Password auth | Not used (Discord OAuth only) |

## Authorization security

See `authorization.md`.

| Control | Status |
|---------|--------|
| Multi-tenant isolation | Application-layer GuildId checks |
| Owner/admin bypass | Centralized in resolver |
| 404 on access denied | Obscures resource existence |
| Bot trusts API key only | Bot must run in trusted environment |

**Gap:** No rate limiting on auth or bot endpoints.

## Data protection

| Data | Storage | Notes |
|------|---------|-------|
| Discord user IDs | PostgreSQL | Not PII under minimal scope |
| Message content in logs | LogEntry.MetadataJson | May contain user messages — retention policy undefined |
| Ticket transcripts | TicketArchiveService | Verify storage location and retention |

## CORS

Restricted to configured dashboard origin(s). Vercel wildcard optional via config flag.

## Input validation

- Guild settings validated via `GuildSettingsValidator`
- Permission keys parsed with enum/alias whitelist
- SQL injection mitigated by EF parameterized queries
- No raw user SQL

## Discord-specific

- Bot requires minimum intents documented in README
- Role hierarchy enforced in kick handler
- Native Discord permissions checked alongside platform permissions

## Secrets in repository

**Policy:** Never commit secrets. Use example files with placeholders.

If secrets were ever committed: rotate all credentials before deploy.

## Compliance readiness

| Requirement | Status |
|-------------|--------|
| Audit log for admin actions | Partial (LogEntry only) |
| Permission change audit | **Not implemented** |
| Data export/deletion (GDPR) | **Not implemented** |
| SOC 2 | Not applicable at beta stage |

## Production hardening checklist (future)

- [ ] Move JWT to httpOnly secure cookie
- [ ] Rate limiting (auth, bot evaluate, guild API)
- [ ] WAF / DDoS protection (Cloudflare)
- [ ] Secret rotation automation
- [ ] Permission change audit trail
- [ ] Security headers (HSTS, CSP) on dashboard
- [ ] Dependency vulnerability scanning in CI
- [ ] Penetration test before commercial launch

## Related docs

- `authentication.md`, `authorization.md`
- `environments.md`
