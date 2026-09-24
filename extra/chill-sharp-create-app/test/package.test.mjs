import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';

const packageDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const executablePath = path.join(packageDirectory, 'bin', 'create-chill-sharp-app.mjs');

test('declares a publishable app-generator executable', () => {
  const manifest = JSON.parse(readFileSync(path.join(packageDirectory, 'package.json'), 'utf8'));

  assert.equal(manifest.bin['create-chill-sharp-app'], './bin/create-chill-sharp-app.mjs');
  assert.ok(manifest.files.includes('bin'));
  assert.ok(existsSync(executablePath));
  assert.match(readFileSync(executablePath, 'utf8'), /^#!\/usr\/bin\/env node/);
});
