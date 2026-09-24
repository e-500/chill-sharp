import assert from 'node:assert/strict';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import test from 'node:test';
import { pathToFileURL } from 'node:url';

const cliModule = await import(pathToFileURL(path.resolve('bin/chill.mjs')).href);

function createOutput() {
  return { log() {}, error() {} };
}

test('uses npm.cmd when launching npm on Windows', () => {
  assert.equal(cliModule.resolveExecutable('npm', 'win32'), 'npm.cmd');
  assert.equal(cliModule.resolveExecutable('npm', 'linux'), 'npm');
  assert.equal(cliModule.resolveExecutable('dotnet', 'win32'), 'dotnet');
  assert.deepEqual(cliModule.resolveCommandOptions('npm', 'win32'), { shell: true });
  assert.deepEqual(cliModule.resolveCommandOptions('npm', 'linux'), {});
});

test('new creates an API project and restores ChillSharp from NuGet', async () => {
  const workingDirectory = mkdtempSync(path.join(tmpdir(), 'chill-cli-'));
  const commands = [];

  try {
    const status = await cliModule.run(['new', 'my-project', '--api'], {
      cwd: workingDirectory,
      output: createOutput(),
      runCommand(command, arguments_, cwd) { commands.push({ command, arguments_, cwd }); }
    });

    assert.equal(status, 0);
    const destination = path.join(workingDirectory, 'my-project');
    assert.equal(existsSync(path.join(destination, 'AGENTS.md')), true);
    assert.equal(existsSync(path.join(destination, '.agents', 'skills', 'chillsharp_model_preparation', 'SKILL.md')), true);
    assert.equal(existsSync(path.join(destination, '.agents', 'skills', 'chillsharp-full-stack-application', 'SKILL.md')), true);
    const fullStackSkill = readFileSync(
      path.join(destination, '.agents', 'skills', 'chillsharp-full-stack-application', 'SKILL.md'),
      'utf8'
    );
    assert.match(fullStackSkill, /@chill-sharp\/create-app/);
    assert.match(fullStackSkill, /pip install chill-sharp-py-client/);
    assert.deepEqual(commands.map(({ command, arguments_ }) => [command, arguments_]), [
      ['dotnet', ['new', 'webapi', '--framework', 'net8.0', '--no-https', '--use-controllers', '--name', 'MyProject.Api', '--output', 'api']],
      ['dotnet', ['add', path.join('api', 'MyProject.Api.csproj'), 'package', 'ChillSharp']]
    ]);
    assert.match(readFileSync(path.join(destination, 'README.md'), 'utf8'), /public NuGet and npm registries/);
  } finally {
    rmSync(workingDirectory, { recursive: true, force: true });
  }
});

test('new creates a UI and installs its published npm dependencies', async () => {
  const workingDirectory = mkdtempSync(path.join(tmpdir(), 'chill-cli-'));
  const commands = [];

  try {
    const status = await cliModule.run(['new', 'my-project'], {
      cwd: workingDirectory,
      output: createOutput(),
      chooseProjectType: async () => 'ui',
      runCommand(command, arguments_, cwd) {
        commands.push({ command, arguments_, cwd });
        if (command === 'npm' && arguments_[0] === 'create') mkdirSync(path.join(cwd, 'ui', 'packages'), { recursive: true });
      }
    });

    assert.equal(status, 0);
    assert.deepEqual(commands.map(({ command, arguments_ }) => [command, arguments_]), [
      ['npm', ['exec', '--yes', '--package=@chill-sharp/create-app', '--', 'create-chill-sharp-app', 'ui']],
      ['npm', ['install']]
    ]);
    assert.equal(commands[1].cwd, path.join(workingDirectory, 'my-project', 'ui'));
    assert.equal(existsSync(path.join(workingDirectory, 'my-project', 'ui', 'packages')), false);
  } finally {
    rmSync(workingDirectory, { recursive: true, force: true });
  }
});

test('new refuses to overwrite a project directory', async () => {
  const workingDirectory = mkdtempSync(path.join(tmpdir(), 'chill-cli-'));

  try {
    const options = { cwd: workingDirectory, output: createOutput(), runCommand() {} };
    assert.equal(await cliModule.run(['new', 'my-project', '--both'], options), 0);
    assert.equal(await cliModule.run(['new', 'my-project', '--both'], options), 1);
  } finally {
    rmSync(workingDirectory, { recursive: true, force: true });
  }
});

test('install uses the selected package manager and published package name', async () => {
  const workingDirectory = mkdtempSync(path.join(tmpdir(), 'chill-cli-'));
  const commands = [];

  try {
    const status = await cliModule.run(['install'], {
      cwd: workingDirectory,
      output: createOutput(),
      chooseDependency: async () => 'vue-client',
      runCommand(command, arguments_, cwd) { commands.push({ command, arguments_, cwd }); }
    });

    assert.equal(status, 0);
    assert.deepEqual(commands, [{
      command: 'npm',
      arguments_: ['install', '@chill-sharp/vue-client'],
      cwd: workingDirectory
    }]);
  } finally {
    rmSync(workingDirectory, { recursive: true, force: true });
  }
});

test('install can add the ChillSharp NuGet package without prompting', async () => {
  const workingDirectory = mkdtempSync(path.join(tmpdir(), 'chill-cli-'));
  const commands = [];

  try {
    const status = await cliModule.run(['install', '--chillsharp'], {
      cwd: workingDirectory,
      output: createOutput(),
      runCommand(command, arguments_, cwd) { commands.push({ command, arguments_, cwd }); }
    });

    assert.equal(status, 0);
    assert.deepEqual(commands, [{
      command: 'dotnet',
      arguments_: ['add', 'package', 'ChillSharp'],
      cwd: workingDirectory
    }]);
  } finally {
    rmSync(workingDirectory, { recursive: true, force: true });
  }
});
