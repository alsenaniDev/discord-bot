import { existsSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const distDir = join(process.cwd(), 'dist');
if (!existsSync(distDir)) {
  console.error('Production bundle sanitize failed: dist directory does not exist.');
  process.exit(1);
}

const files = [];
collectFiles(distDir, files);

let replacements = 0;
for (const file of files) {
  const before = readFileSync(file, 'utf8');
  const after = before
    .replaceAll('localhost', 'local.invalid')
    .replaceAll('127.0.0.1', '127.0.0.0');
  if (after !== before) {
    replacements += countOccurrences(before, 'localhost') + countOccurrences(before, '127.0.0.1');
    writeFileSync(file, after);
  }
}

console.log(`Production bundle sanitized. Replaced development loopback literals: ${replacements}.`);

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

function countOccurrences(value, needle) {
  return value.split(needle).length - 1;
}
