# Railway deployment files

Deploy the Discord Bot Platform to [Railway](https://railway.app).

**Full guide:** [`docs/step-23-railway-deployment.md`](../../docs/step-23-railway-deployment.md)

## Quick start

1. Create Railway project → add **PostgreSQL**
2. Add **API** service → Dockerfile `deploy/railway/Dockerfile.api` → generate domain
3. Add **Bot** service → Dockerfile `deploy/railway/Dockerfile.bot` → disable public networking
4. Set variables from `railway.env.example`
5. Run migrations: `railway run --service api ./deploy/railway/migrate.sh`
6. Deploy dashboard on Railway (`Dockerfile.dashboard`) or Vercel (`dashboard/DiscordBot.Dashboard/vercel.json`)

## Health check

```
GET /api/health
```

## Services

| Dockerfile | Railway public URL |
|------------|-------------------|
| `Dockerfile.api` | Yes |
| `Dockerfile.bot` | No (worker) |
| `Dockerfile.dashboard` | Yes (optional) |
