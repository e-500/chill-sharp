import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import test from 'node:test';
import { spawnSync } from 'node:child_process';

const cliPath = path.resolve('bin/chill.mjs');

test('new creates an agent-ready project workspace', () => {
  const workingDirectory = mkdtempSync(path.join(tmpdir(), 'chill-cli-'));

  try {
    const result = spawnSync(process.execPath, [cliPath, 'new', 'my-project'], {
      cwd: workingDirectory,
      encoding: 'utf8'
    });

    assert.equal(result.status, 0, result.stderr);
    const destination = path.join(workingDirectory, 'my-project');
    assert.equal(existsSync(path.join(destination, 'AGENTS.md')), true);
    assert.equal(existsSync(path.join(destination, '.agents', 'skills', 'chillsharp_model_preparation', 'SKILL.md')), true);
    assert.equal(existsSync(path.join(destination, '.agents', 'skills', 'chillsharp-full-stack-application', 'SKILL.md')), true);
    assert.match(
      readFileSync(path.join(destination, 'AGENTS.md'), 'utf8'),
      /connected ASP\.NET Core ChillSharp API, an authenticated management UI for real data, and a user-facing frontend/
    );
  } finally {
    rmSync(workingDirectory, { recursive: true, force: true });
  }
});

test('new refuses to overwrite a project directory', () => {
  const workingDirectory = mkdtempSync(path.join(tmpdir(), 'chill-cli-'));

  try {
    const firstResult = spawnSync(process.execPath, [cliPath, 'new', 'my-project'], {
      cwd: workingDirectory,
      encoding: 'utf8'
    });
    const secondResult = spawnSync(process.execPath, [cliPath, 'new', 'my-project'], {
      cwd: workingDirectory,
      encoding: 'utf8'
    });

    assert.equal(firstResult.status, 0, firstResult.stderr);
    assert.equal(secondResult.status, 1);
    assert.match(secondResult.stderr, /already exists/);
  } finally {
    rmSync(workingDirectory, { recursive: true, force: true });
  }
});
