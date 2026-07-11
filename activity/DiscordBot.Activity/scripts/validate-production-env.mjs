import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const files = ['.env', '.env.local', '.env.production', '.env.production.local'];
const values = { ...process.env };

for (const file of files) {
  const path = resolve(process.cwd(), file);
  if (!existsSync(path)) continue;
  for (const line of readFileSync(path, 'utf8').split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#') || !trimmed.includes('=')) continue;
    const index = trimmed.indexOf('=');
    const key = trimmed.slice(0, index).trim();
    const value = trimmed.slice(index + 1).trim().replace(/^['"]|['"]$/g, '');
    values[key] ??= value;
  }
}

const missing = [];
if (!values.VITE_DISCORD_CLIENT_ID?.trim()) missing.push('VITE_DISCORD_CLIENT_ID');
if (!values.VITE_ACTIVITIES_API_BASE_URL?.trim()) missing.push('VITE_ACTIVITIES_API_BASE_URL');
if (!values.VITE_API_BASE_URL?.trim() && !values.VITE_PLATFORM_API_BASE_URL?.trim()) {
  missing.push('VITE_API_BASE_URL or VITE_PLATFORM_API_BASE_URL');
}

if (missing.length > 0) {
  console.error('Missing required production Activity build variables:');
  for (const key of missing) console.error(`  - ${key}`);
  console.error('VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS may be empty; empty means all guilds use the legacy Roulette runtime.');
  process.exit(1);
}

console.log('Activity production environment variables validated.');
