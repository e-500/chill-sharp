#!/usr/bin/env node

import { cpSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { createInterface } from 'node:readline/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const packageDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const packageManifest = JSON.parse(readFileSync(path.join(packageDirectory, 'package.json'), 'utf8'));
const dependencies = {
  'ng-client': { label: 'Angular client', command: 'npm', arguments: ['install', '@chill-sharp/ng-client'] },
  'vue-client': { label: 'Vue client', command: 'npm', arguments: ['install', '@chill-sharp/vue-client'] },
  'react-client': { label: 'React client', command: 'npm', arguments: ['install', '@chill-sharp/react-client'] },
  'ts-client': { label: 'TypeScript client', command: 'npm', arguments: ['install', '@chill-sharp/ts-client'] },
  chillsharp: { label: 'ChillSharp ASP.NET Core package', command: 'dotnet', arguments: ['add', 'package', 'ChillSharp'] },
  'chillsharp-client': { label: 'ChillSharp .NET client package', command: 'dotnet', arguments: ['add', 'package', 'ChillSharp.Client'] },
  'py-client': { label: 'Python client', command: 'python', arguments: ['-m', 'pip', 'install', 'chill-sharp-py-client'] }
};

export async function run(arguments_ = process.argv.slice(2), options = {}) {
  const [command, ...commandArguments] = arguments_;
  const output = options.output ?? console;

  try {
    if (command === 'new') {
      const [projectName, ...flags] = commandArguments;
      await createProject(projectName, flags, { ...options, output });
      return 0;
    }

    if (command === 'install') {
      await installDependency(commandArguments, { ...options, output });
      return 0;
    }

    if (command === '--help' || command === '-h' || command === 'help' || !command) {
      printUsage(output);
      return command ? 0 : 1;
    }

    if (command === '--version' || command === '-v') {
      output.log(packageManifest.version);
      return 0;
    }

    output.error(`Unknown command: ${command}`);
    printUsage(output);
    return 1;
  } catch (error) {
    output.error(error instanceof Error ? error.message : String(error));
    return 1;
  }
}

async function installDependency(flags, options) {
  const { cwd = process.cwd(), output = console, runCommand = executeCommand } = options;
  const dependency = await selectDependency(flags, options);
  output.log(`Installing ${dependency.label} from its public package registry...`);
  await runCommand(dependency.command, dependency.arguments, cwd);
  output.log(`Installed ${dependency.label}.`);
}

async function selectDependency(flags, options) {
  if (flags.length > 0) {
    if (flags.length !== 1 || !flags[0].startsWith('--')) {
      throw new Error(`Use one dependency option: ${Object.keys(dependencies).map((name) => `--${name}`).join(', ')}.`);
    }

    const dependency = dependencies[flags[0].slice(2)];
    if (!dependency) {
      throw new Error(`Unknown dependency option '${flags[0]}'.`);
    }
    return dependency;
  }

  if (options.chooseDependency) {
    const selected = await options.chooseDependency();
    const dependency = dependencies[selected];
    if (!dependency) throw new Error(`Unknown dependency '${selected}'.`);
    return dependency;
  }

  const output = options.output ?? console;
  const prompt = createInterface({ input: process.stdin, output: process.stdout });
  const choices = Object.entries(dependencies);
  try {
    output.log('What would you like to install?');
    choices.forEach(([name, dependency], index) => output.log(`${index + 1}. ${dependency.label} (${name})`));
    while (true) {
      const answer = (await prompt.question('Select an option: ')).trim();
      const dependency = choices[Number(answer) - 1]?.[1];
      if (dependency) return dependency;
      output.error(`Invalid choice. Valid options: 1-${choices.length}`);
    }
  } finally {
    prompt.close();
  }
}

async function createProject(name, flags, options) {
  const { cwd = process.cwd(), output = console, runCommand = executeCommand } = options;
  if (!name) {
    throw new Error('A project name is required.\n\nUsage: chill new <project-name> [--skills|--api|--ui|--both]');
  }

  if (!/^[a-z0-9][a-z0-9-]*$/.test(name)) {
    throw new Error('Project names must use lowercase letters, digits, and hyphens only.');
  }

  const projectType = await selectProjectType(flags, options);
  const destination = path.resolve(cwd, name);
  if (existsSync(destination)) {
    throw new Error(`Cannot create '${name}': '${destination}' already exists.`);
  }

  mkdirSync(destination);
  cpSync(path.join(packageDirectory, 'skills'), path.join(destination, '.agents', 'skills'), { recursive: true });
  if (projectType === 'skills') {
    output.log(`Created ${name} with coding agent skills in .agents/skills.`);
    output.log(`Next step: cd ${name}`);
    return;
  }

  writeFileSync(path.join(destination, 'AGENTS.md'), projectInstructions(name));
  writeFileSync(path.join(destination, 'README.md'), projectReadme(name, projectType));
  writeFileSync(path.join(destination, '.gitignore'), projectGitignore(projectType));

  output.log(`Created ${name}.`);
  output.log('Downloading and restoring published ChillSharp packages...');

  if (projectType === 'api' || projectType === 'both') {
    const apiProjectName = `${toPascalCase(name)}.Api`;
    await runCommand('dotnet', [
      'new', 'webapi', '--framework', 'net8.0', '--no-https', '--use-controllers', '--name', apiProjectName, '--output', 'api'
    ], destination);
    await runCommand('dotnet', ['add', path.join('api', `${apiProjectName}.csproj`), 'package', 'ChillSharp'], destination);
  }

  if (projectType === 'ui' || projectType === 'both') {
    await runCommand('npm', [
      'exec',
      '--yes',
      '--package=@chill-sharp/create-app',
      '--',
      'create-chill-sharp-app',
      'ui'
    ], destination);
    rmSync(path.join(destination, 'ui', 'packages'), { recursive: true, force: true });
    await runCommand('npm', ['install'], path.join(destination, 'ui'));
  }

  output.log('Agent skills were added to .agents/skills.');
  output.log('Next steps:');
  output.log(`  cd ${name}`);
  if (projectType === 'api' || projectType === 'both') output.log('  dotnet run --project ./api');
  if (projectType === 'ui' || projectType === 'both') output.log('  cd ui && npm start');
}

async function selectProjectType(flags, options) {
  if (flags.length > 0) {
    if (flags.length !== 1 || !['--skills', '--api', '--ui', '--both'].includes(flags[0])) {
      throw new Error('Use one project type option: --skills, --api, --ui, or --both.');
    }

    return flags[0].slice(2);
  }

  if (options.chooseProjectType) return options.chooseProjectType();

  const output = options.output ?? console;
  const prompt = createInterface({ input: process.stdin, output: process.stdout });
  try {
    output.log('What would you like to create?');
    output.log('0. Prepare a folder with only coding agent skills');
    output.log('1. API (ASP.NET Core + ChillSharp)');
    output.log('2. UI (Angular + ChillSharp UI)');
    output.log('3. API and UI');
    while (true) {
      const answer = (await prompt.question('Select an option: ')).trim();
      if (answer === '0') return 'skills';
      if (answer === '1') return 'api';
      if (answer === '2') return 'ui';
      if (answer === '3') return 'both';
      output.error('Invalid choice. Valid options: 0, 1, 2, 3');
    }
  } finally {
    prompt.close();
  }
}

function executeCommand(command, arguments_, cwd) {
  const result = spawnSync(resolveExecutable(command), arguments_, {
    cwd,
    stdio: 'inherit',
    ...resolveCommandOptions(command)
  });
  if (result.error) throw new Error(`Unable to run ${command}: ${result.error.message}`);
  if (result.status !== 0) {
    throw new Error(`${command} ${arguments_.join(' ')} failed with exit code ${result.status}. The project directory was kept for inspection.`);
  }
}

export function resolveExecutable(command, platform = process.platform) {
  return platform === 'win32' && command === 'npm' ? 'npm.cmd' : command;
}

export function resolveCommandOptions(command, platform = process.platform) {
  // Windows command scripts (.cmd) need a shell; otherwise Node reports EINVAL.
  return platform === 'win32' && command === 'npm' ? { shell: true } : {};
}

function toPascalCase(name) {
  return name.split('-').map((part) => part.charAt(0).toUpperCase() + part.slice(1)).join('');
}

function projectInstructions(name) {
  return `# ${name}\n\nThis project is prepared for ChillSharp development with a coding agent.\n\nBefore building or changing this application, inspect the relevant skill in \`.agents/skills\`. Use the project's existing conventions and keep implementation work focused on the requested outcome.\n\nWhen asked to build a site or application, use \`.agents/skills/chillsharp-full-stack-application/SKILL.md\`. The generated ASP.NET Core API is only a starting template: a runnable application without the ChillSharp package, context, and mapped API does not fulfill this workspace's purpose. Build the requested workflow with a connected ASP.NET Core ChillSharp API and UI backed by persisted data. Use a separate public frontend when the requested users or workflows need one. Do not replace these components with mock data, browser-only storage, or a visual-only site.\n\nFor a new application, start by describing the users, data, and first workflow. The agent can then use the included full-stack, model, registration, permissions, i18n, menu, plugin, and MCP guidance as needed.\n`;
}

function projectReadme(name, projectType) {
  const hasApi = projectType === 'api' || projectType === 'both';
  const hasUi = projectType === 'ui' || projectType === 'both';
  const startCommands = [
    hasApi ? 'dotnet run --project ./api' : null,
    hasUi ? 'cd ui && npm start' : null
  ].filter(Boolean).join('\n');

  return `# ${name}\n\nA ChillSharp project workspace prepared for coding agents. The generated projects restore ChillSharp from public NuGet and npm registries; no local package archives are used.\n\n## Run\n\n\`\`\`bash\n${startCommands}\n\`\`\`\n\n## Start building\n\nOpen this folder with your coding agent and describe the first outcome you want to deliver. The agent guidance lives in [\`.agents/skills\`](.agents/skills). The generated API is a starting template; the application still needs its ChillSharp model, registration, endpoints, and authorization.\n\nA request to build an application produces a connected ASP.NET Core ChillSharp API and an authenticated UI using persisted data. Add a separate public frontend when the requested users or workflows need one. Ask for an API-only, UI-only, or static prototype only when that is genuinely the desired scope.\n\nFor example:\n\n> Build a blog. Authors sign in to manage posts and categories in an admin UI; visitors can browse published posts in a separate public frontend. Use the same ChillSharp API and database for both.\n\n## Included guidance\n\nThe workspace contains skills for full-stack application delivery, model preparation, registration, permissions, localized text, menus, client plugins, current-user preferences, documentation, and MCP.\n`;
}

function projectGitignore(projectType) {
  const entries = ['.DS_Store', 'Thumbs.db', '.env'];
  if (projectType === 'api' || projectType === 'both') entries.push('api/bin/', 'api/obj/', 'api/*.db');
  if (projectType === 'ui' || projectType === 'both') entries.push('ui/node_modules/', 'ui/dist/');
  return `${entries.join('\n')}\n`;
}

function printUsage(output = console) {
  output.log('Usage: chill new <project-name> [--skills|--api|--ui|--both]');
  output.log('       chill install [--ng-client|--vue-client|--react-client|--ts-client|--chillsharp|--chillsharp-client|--py-client]');
  output.log('');
  output.log('Commands:');
  output.log('  new <project-name>  Create a skills folder, ChillSharp API, UI, or both.');
  output.log('  install             Install a published ChillSharp client or .NET package.');
  output.log('');
  output.log('Without an option, new and install show a selection menu.');
}

if (path.resolve(process.argv[1] ?? '') === fileURLToPath(import.meta.url)) {
  process.exitCode = await run();
}
