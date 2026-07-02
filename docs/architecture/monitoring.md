# Monitoring

## Current state

**Minimal operational monitoring.** No APM, metrics platform, or alerting configured in codebase.

## What exists today

| Mechanism | Purpose |
|-----------|---------|
| `GET /api/health` | Database connectivity check for Railway |
| Console logging | API request logs, bot gateway logs |
| Railway dashboards | Infrastructure metrics (external to repo) |
| Vercel analytics | Frontend deploy status (external) |

## Health endpoint

**File:** `src/DiscordBot.Api/Controllers/HealthController.cs`

```
GET /api/health → 200 if DB reachable, 503 if not
```

Used in `deploy/railway/railway.api.toml` health check configuration.

## Gaps

| Gap | Risk |
|-----|------|
| No bot health endpoint | Bot crashes silent until Discord commands fail |
| No metrics (Prometheus/OpenTelemetry) | Cannot track latency, error rates |
| No alerting | Outages discovered by users |
| No uptime monitoring | External ping not configured in repo |
| No DB connection pool metrics | Connection exhaustion undetected |
| Bot worker poll failures | Logged to console only |

## Recommended monitoring stack (future)

| Layer | Tool options |
|-------|--------------|
| Uptime | Better Uptime, UptimeRobot on `/api/health` |
| Logs | Railway logs, or Grafana Loki / Datadog |
| Metrics | OpenTelemetry → Prometheus / Grafana |
| Errors | Sentry for API + bot + Angular |
| DB | Railway PostgreSQL metrics, pg_stat monitoring |

## Key metrics to track

| Metric | Why |
|--------|-----|
| API request latency p95 | User experience |
| Bot command error rate | Product quality |
| `permissions/evaluate` call volume | Permission cache ROI |
| DB query duration | Scale bottleneck |
| Ticket outbound message delivery lag | Support workflow |
| Resource sync queue depth | Sync reliability |
| JWT auth failure rate | Security / config issues |

## Background worker observability

Workers poll every **30 seconds**:

- `GuildMaintenanceWorker`
- `GuildResourceSyncWorker`

**Recommendation:** Log structured heartbeat with last successful run timestamp; alert if stale > 2 minutes.

## Assumption

Railway provides basic CPU/memory/network metrics at infrastructure level. Application-level observability is **not implemented**.

## Related docs

- `logging.md`, `deployment.md`
