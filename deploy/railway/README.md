# Railway deployment files

Deploy the Discord Bot Platform to [Railway](https://railway.app).

**Full guide:** [`docs/step-23-railway-deployment.md`](../../docs/step-23-railway-deployment.md)

## Quick start

1. Create Railway project → add **PostgreSQL**
2. Add **API** service → Dockerfile `deploy/railway/Dockerfile.api` → generate domain
3. Add **Activities API** service → Dockerfile `deploy/railway/Dockerfile.activities-api` → generate domain
4. Add **Bot** service → Dockerfile `deploy/railway/Dockerfile.bot` → disable public networking
5. Add private **Lavalink** service → Dockerfile `deploy/railway/Dockerfile.lavalink` → disable public networking
6. Set variables from `railway.env.example`
7. Run migrations from an SDK migration job:
   - `./deploy/railway/migrate-platform.sh`
   - `./deploy/railway/migrate-activities.sh`
8. Deploy the dashboard on Vercel (`dashboard/DiscordBot.Dashboard/vercel.json`)

## Health check

```
GET /health
GET /health/ready
```

## Services

| Dockerfile | Railway public URL |
|------------|-------------------|
| `Dockerfile.api` | Yes |
| `Dockerfile.activities-api` | Yes |
| `Dockerfile.bot` | No (worker) |
| `Dockerfile.lavalink` | No (private service) |
| `Dockerfile.dashboard` | Yes (optional) |

For the Music production rollout and troubleshooting checklist, see
[`docs/music-production-deployment.md`](../../docs/music-production-deployment.md).
