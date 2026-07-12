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

const invalid = [];
const urlKeys = ['VITE_API_BASE_URL', 'VITE_PLATFORM_API_BASE_URL', 'VITE_ACTIVITIES_API_BASE_URL'];
const explicitDirectPlatformOverride = values.VITE_ALLOW_DIRECT_PLATFORM_API_BASE_URL === 'true';
const effectivePlatformApiBaseUrl = values.VITE_PLATFORM_API_BASE_URL?.trim() || values.VITE_API_BASE_URL?.trim() || '/api';
const effectiveActivitiesApiBaseUrl = values.VITE_ACTIVITIES_API_BASE_URL?.trim() || '';
const placeholderFragments = [
  'example.com',
  'YOUR_',
  'CHANGE_ME',
  'REPLACE_WITH',
  'YOUR_PLATFORM_API_DOMAIN',
  'YOUR_ACTIVITIES_API_DOMAIN'
];

for (const key of urlKeys) {
  const value = values[key]?.trim();
  if (!value) continue;
  for (const fragment of placeholderFragments) {
    if (value.toLowerCase().includes(fragment.toLowerCase())) {
      invalid.push(`${key} contains placeholder value "${fragment}".`);
      break;
    }
  }
  if (/localhost|127\.0\.0\.1/i.test(value)) {
    invalid.push(`${key} must not point to localhost for a production Activity build.`);
  }
  if (/railway\.internal/i.test(value)) {
    invalid.push(`${key} must not point to a private railway.internal hostname. Discord clients cannot reach it.`);
  }
  if (/^http:\/\//i.test(value)) {
    invalid.push(`${key} must use HTTPS or a same-origin Discord URL Mapping path.`);
  }
  if (!/^https:\/\//i.test(value) && !value.startsWith('/') && value !== '') {
    invalid.push(`${key} must be an HTTPS URL or a same-origin mapping path such as /activities-api.`);
  }
}

if (values.VITE_ENVIRONMENT?.trim() === 'production' && !explicitDirectPlatformOverride && effectivePlatformApiBaseUrl !== '/api') {
  invalid.push('effectivePlatformApiBaseUrl must be /api for Discord URL Mapping production builds. Delete VITE_API_BASE_URL and VITE_PLATFORM_API_BASE_URL, or set VITE_ALLOW_DIRECT_PLATFORM_API_BASE_URL=true intentionally.');
}

if (values.VITE_ENVIRONMENT?.trim() === 'production' && effectiveActivitiesApiBaseUrl !== '/activities-api') {
  invalid.push('effectiveActivitiesApiBaseUrl must be /activities-api for Discord URL Mapping production builds.');
}

if (values.VITE_LOCAL_BROWSER_MODE?.trim().toLowerCase() === 'true') {
  invalid.push('VITE_LOCAL_BROWSER_MODE must not be enabled for production Activity builds.');
}

if (missing.length > 0) {
  console.error('Missing required production Activity build variables:');
  for (const key of missing) console.error(`  - ${key}`);
  console.error('VITE_API_BASE_URL and VITE_PLATFORM_API_BASE_URL may be empty when Discord URL Mapping /api -> YOUR_API_DOMAIN/api is configured.');
  console.error('VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS may be empty; empty means all guilds use the legacy Roulette runtime.');
  process.exit(1);
}

if (invalid.length > 0) {
  console.error('Invalid production Activity build variables:');
  for (const message of invalid) console.error(`  - ${message}`);
  console.error('Recommended Discord Activity URL Mappings:');
  console.error('  - /api            -> YOUR_PLATFORM_API_DOMAIN/api');
  console.error('  - /activities-api -> YOUR_ACTIVITIES_API_DOMAIN');
  console.error('Then build with VITE_API_BASE_URL empty or same-origin, and VITE_ACTIVITIES_API_BASE_URL=/activities-api.');
  process.exit(1);
}

const publicSummary = {
  discordClientId: values.VITE_DISCORD_CLIENT_ID?.trim(),
  effectivePlatformApiBaseUrl,
  effectiveActivitiesApiBaseUrl,
  pilotGuildCount: (values.VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS ?? '').split(',').map(x => x.trim()).filter(Boolean).length,
  environment: values.VITE_ENVIRONMENT?.trim() || values.NODE_ENV || 'production'
};

console.log('Activity production environment variables validated.');
console.log('Safe Activity build summary:');
console.log(`  Discord Client ID: ${publicSummary.discordClientId}`);
console.log(`  Platform API base URL: ${publicSummary.effectivePlatformApiBaseUrl}`);
console.log(`  Activities API base URL: ${publicSummary.effectiveActivitiesApiBaseUrl}`);
console.log(`  Pilot guild count: ${publicSummary.pilotGuildCount}`);
console.log(`  Environment: ${publicSummary.environment}`);
