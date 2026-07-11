import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

const distDir = join(process.cwd(), 'dist');
if (!existsSync(distDir)) {
  console.error('Production bundle assertion failed: dist directory does not exist.');
  process.exit(1);
}

const forbidden = [
  { label: 'direct production Platform API Railway domain', pattern: /discord-bot-production-b872\.up\.railway\.app/i },
  { label: 'placeholder API domain', pattern: /api\.example\.com/i },
  { label: 'placeholder Activities API domain', pattern: /activities-api\.example\.com/i },
  { label: 'private Railway hostname', pattern: /railway\.internal/i },
  { label: 'placeholder YOUR_* value', pattern: /YOUR_[A-Z0-9_]+/i },
  { label: 'placeholder CHANGE_ME value', pattern: /CHANGE_ME/i },
  { label: 'placeholder REPLACE_WITH value', pattern: /REPLACE_WITH/i },
  { label: 'localhost literal', pattern: /localhost/i },
  { label: 'loopback API config', pattern: /https?:\/\/127\.0\.0\.1(?::\d+)?/i }
];

const required = [
  { label: 'Platform API mapping path', pattern: /\/api/ },
  { label: 'Activities API mapping path', pattern: /\/activities-api/ },
  { label: 'Activity context endpoint', pattern: /\/api\/games\/activity\/context/ },
  { label: 'Activities auth exchange endpoint', pattern: /\/api\/auth\/discord\/exchange/ }
];

const files = [];
collectFiles(distDir, files);

const matches = [];
let bundle = '';
for (const file of files) {
  const content = readFileSync(file, 'utf8');
  bundle += `\n${content}`;
  for (const rule of forbidden) {
    if (rule.pattern.test(content)) {
      matches.push(`${rule.label} in ${file.replace(`${process.cwd()}/`, '')}`);
    }
  }
}

if (matches.length > 0) {
  console.error('Production bundle assertion failed. Forbidden values found:');
  for (const match of matches) console.error(`  - ${match}`);
  process.exit(1);
}

const missing = required.filter(rule => !rule.pattern.test(bundle));
if (missing.length > 0) {
  console.error('Production bundle assertion failed. Required mapping values were not found:');
  for (const rule of missing) console.error(`  - ${rule.label}`);
  process.exit(1);
}

console.log('Production bundle assertion passed.');
console.log('  effectivePlatformApiBaseUrl: /api');
console.log('  effectiveActivitiesApiBaseUrl: /activities-api');

function collectFiles(dir, output) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) {
      collectFiles(path, output);
      continue;
    }
    if (/\.(html|js|css|json|txt|map)$/.test(entry.name)) {
      output.push(path);
    }
  }
}
