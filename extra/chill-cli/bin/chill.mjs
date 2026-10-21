#!/usr/bin/env node

import { cpSync, existsSync, mkdirSync, readFileSync, readdirSync, renameSync, rmSync, statSync, writeFileSync } from 'node:fs';
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
  const projectNames = await selectProjectNames(name, projectType, options);
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
  writeFileSync(path.join(destination, 'README.md'), projectReadme(name, projectType, projectNames));
  writeFileSync(path.join(destination, '.gitignore'), projectGitignore(projectType));

  output.log(`Created ${name}.`);
  output.log('Preparing the ChillSharp project templates...');

  if (projectType === 'api' || projectType === 'both') {
    const apiDirectory = path.join(destination, 'api');
    copyTemplate('api', apiDirectory);
    const apiProjectPath = path.join(apiDirectory, `${projectNames.api}.csproj`);
    const apiContextName = `${projectNames.api}Context`;
    renameTemplateProject(apiDirectory, [
      ['ChillSharp.Template', projectNames.api],
      ['ChillSharpTemplateContext', apiContextName]
    ]);
    const apiProject = readFileSync(apiProjectPath, 'utf8')
      .replace(/\s*<RestoreAdditionalProjectSources>.*?<\/RestoreAdditionalProjectSources>/s, '');
    writeFileSync(apiProjectPath, apiProject);
    writeFileSync(path.join(apiDirectory, 'Directory.Build.props'), `<?xml version="1.0" encoding="utf-8"?>\n<Project>\n  <PropertyGroup>\n    <ChillSharpVersion>${packageManifest.version}</ChillSharpVersion>\n  </PropertyGroup>\n</Project>\n`);
    rmSync(path.join(apiDirectory, 'NuGet.Config'), { force: true });
    writeFileSync(path.join(apiDirectory, 'README.md'), [
      `# ${projectNames.api}`,
      '',
      `This project was created from the \`ChillSharp.Template\` project. It includes the ChillSharp API, SQLite persistence, Identity authentication, Swagger, and a sample entity and query. Its database context is \`${apiContextName}\`.`,
      '',
      'Run it from the workspace root with:',
      '',
      '```bash',
      `dotnet run --project ./api/${projectNames.api}.csproj`,
      '```',
      ''
    ].join('\n'));
    await runCommand('dotnet', ['restore', path.join('api', `${projectNames.api}.csproj`)], destination);
  }

  if (projectType === 'ui' || projectType === 'both') {
    const uiDirectory = path.join(destination, 'ui');
    copyTemplate('ui', uiDirectory);
    const uiManifestPath = path.join(uiDirectory, 'package.json');
    const uiManifest = JSON.parse(readFileSync(uiManifestPath, 'utf8'));
    uiManifest.name = projectNames.ui;
    for (const dependency of ['@chill-sharp/ng-client', '@chill-sharp/ts-client', '@chill-sharp/ui-core']) {
      uiManifest.dependencies[dependency] = `^${packageManifest.version}`;
    }
    writeFileSync(uiManifestPath, `${JSON.stringify(uiManifest, null, 2)}\n`);
    renameTemplateProject(uiDirectory, [['chill-sharp-ui-template', projectNames.ui]]);
    updateUiDisplayName(uiDirectory, projectNames.display);
    rmSync(path.join(uiDirectory, 'packages'), { recursive: true, force: true });
    rmSync(path.join(uiDirectory, 'package-lock.json'), { force: true });
    writeFileSync(path.join(uiDirectory, 'README.md'), [
      `# ${projectNames.display} UI`,
      '',
      `This project was created from the \`chill-sharp-ui-template\` Angular project and renamed to \`${projectNames.ui}\`. Its ChillSharp client packages are restored from npm.`,
      '',
      '```bash',
      'npm install',
      'npm start',
      '```',
      ''
    ].join('\n'));
    await runCommand('npm', ['install'], uiDirectory);
  }

  output.log('Agent skills were added to .agents/skills.');
  output.log('Next steps:');
  output.log(`  cd ${name}`);
  if (projectType === 'api' || projectType === 'both') output.log(`  dotnet run --project ./api/${projectNames.api}.csproj`);
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

async function selectProjectNames(workspaceName, projectType, options) {
  const names = {
    api: toPascalCase(workspaceName),
    ui: toKebabCase(workspaceName),
    display: toTitleCase(workspaceName)
  };

  if (projectType === 'api' || projectType === 'both') {
    names.api = await chooseProjectName('C# project name (PascalCase)', names.api, toPascalCase, options, 'chooseApiProjectName');
  }
  if (projectType === 'ui' || projectType === 'both') {
    names.ui = await chooseProjectName('Angular project name (lowercase kebab-case)', names.ui, toKebabCase, options, 'chooseUiProjectName');
  }

  return names;
}

async function chooseProjectName(label, suggestion, normalize, options, callbackName) {
  if (options[callbackName]) {
    const chosen = await options[callbackName](suggestion);
    return chosen?.trim() ? normalize(chosen) : suggestion;
  }
  if (!process.stdin.isTTY) return suggestion;

  const output = options.output ?? console;
  const prompt = createInterface({ input: process.stdin, output: process.stdout });
  try {
    while (true) {
      const answer = (await prompt.question(`${label} [${suggestion}]: `)).trim();
      if (!answer) return suggestion;
      const normalized = normalize(answer);
      if (normalized) return normalized;
      output.error('Enter a name that contains at least one letter or digit.');
    }
  } finally {
    prompt.close();
  }
}

function toWords(value) {
  return value
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .match(/[A-Za-z0-9]+/g) ?? [];
}

function toPascalCase(value) {
  const words = toWords(value);
  if (words.length === 0) return '';
  const pascalCase = words.map((word) => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase()).join('');
  return /^[A-Za-z_]/.test(pascalCase) ? pascalCase : `Project${pascalCase}`;
}

function toKebabCase(value) {
  const words = toWords(value);
  if (words.length === 0) return '';
  const kebabCase = words.join('-').toLowerCase();
  return /^[a-z]/.test(kebabCase) ? kebabCase : `app-${kebabCase}`;
}

function toTitleCase(value) {
  return toWords(value).map((word) => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase()).join(' ');
}

function renameTemplateProject(directory, replacements) {
  replaceTextAndRenamePaths(directory, replacements);
}

function replaceTextAndRenamePaths(directory, replacements) {
  const textExtensions = new Set(['.cs', '.csproj', '.json', '.md', '.xml', '.config', '.yml', '.yaml', '.props', '.targets', '.txt']);
  for (const entry of readdirSync(directory)) {
    const filePath = path.join(directory, entry);
    if (statSync(filePath).isDirectory()) {
      replaceTextAndRenamePaths(filePath, replacements);
    } else if (textExtensions.has(path.extname(entry).toLowerCase())) {
      let contents = readFileSync(filePath, 'utf8');
      for (const [sourceText, targetText] of replacements) contents = contents.replaceAll(sourceText, targetText);
      writeFileSync(filePath, contents);
    }
  }

  for (const entry of readdirSync(directory)) {
    const renamedEntry = replacements.reduce((name, [sourceText, targetText]) => name.replaceAll(sourceText, targetText), entry);
    if (renamedEntry === entry) continue;
    const oldPath = path.join(directory, entry);
    const newPath = path.join(directory, renamedEntry);
    if (existsSync(newPath)) throw new Error(`Cannot rename '${oldPath}': '${newPath}' already exists.`);
    renameSync(oldPath, newPath);
  }
}

function updateUiDisplayName(directory, displayName) {
  const appConfigPath = path.join(directory, 'src', 'config', 'app-config.ts');
  const appConfig = readFileSync(appConfigPath, 'utf8').replace("appName: 'ChillSharp Client Template'", `appName: '${displayName}'`);
  writeFileSync(appConfigPath, appConfig);

  const indexPath = path.join(directory, 'src', 'index.html');
  const index = readFileSync(indexPath, 'utf8').replace('<title>ChillSharp UI Template</title>', `<title>${displayName}</title>`);
  writeFileSync(indexPath, index);
}

function executeCommand(command, arguments_, cwd) {
  const executable = process.platform === 'win32' && command === 'npm'
    ? process.env.ComSpec ?? 'cmd.exe'
    : resolveExecutable(command);
  const childArguments = process.platform === 'win32' && command === 'npm'
    ? ["/d", "/c", npmCommandLine(arguments_)]
    : arguments_;
  const result = spawnSync(executable, childArguments, {
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
  return {};
}

function npmCommandLine(arguments_) {
  // npm arguments are package names and flags; reject shell syntax before passing
  // the command to cmd.exe, which resolves the Windows npm.cmd launcher.
  if (arguments_.some((argument) => !/^[A-Za-z0-9@._/-]+$/.test(argument))) {
    throw new Error('npm arguments contain unsupported shell characters on Windows.');
  }
  return [resolveExecutable('npm', 'win32'), ...arguments_].join(' ');
}

function copyTemplate(templateName, destination) {
  const packagedTemplate = path.join(packageDirectory, 'templates', templateName);
  const sourceTemplate = templateName === 'api'
    ? path.resolve(packageDirectory, '..', '..', 'ChillSharp.Template')
    : path.resolve(packageDirectory, '..', '..', 'extra', 'chill-sharp-ui-template');
  const templateDirectory = existsSync(packagedTemplate) ? packagedTemplate : sourceTemplate;
  if (!existsSync(templateDirectory)) throw new Error(`Cannot find the ${templateName} template at '${templateDirectory}'.`);

  const excluded = templateName === 'api'
    ? new Set(['.agents', 'bin', 'obj', 'nupkgs', 'NuGet.Config', 'upgrade-local.ps1', 'upgrade-local.sh'])
    : new Set(['.agents', '.angular', 'dist', 'node_modules', 'packages', 'package-lock.json', 'upgrade-local.ps1', 'upgrade-local.sh', 'publish-to-shared-folder.ps1']);
  cpSync(templateDirectory, destination, {
    recursive: true,
    filter: (source) => !excluded.has(path.basename(source))
  });
}

function projectInstructions(name) {
  return `# ${name}\n\nThis project is prepared for ChillSharp development with a coding agent.\n\nBefore building or changing this application, inspect the relevant skill in \`.agents/skills\`. Use the project's existing conventions and keep implementation work focused on the requested outcome.\n\nWhen asked to build a site or application, use \`.agents/skills/chillsharp-full-stack-application/SKILL.md\`. The generated ASP.NET Core project already uses ChillSharp with a configured context and mapped API. Extend that foundation. Build the requested workflow with a connected ASP.NET Core ChillSharp API and UI backed by persisted data. Use a separate public frontend when the requested users or workflows need one. Do not replace these components with mock data, browser-only storage, or a visual-only site.\n\nFor a new application, start by describing the users, data, and first workflow. The agent can then use the included full-stack, model, registration, permissions, i18n, menu, plugin, and MCP guidance as needed.\n`;
}

function projectReadme(name, projectType, projectNames) {
  const hasApi = projectType === 'api' || projectType === 'both';
  const hasUi = projectType === 'ui' || projectType === 'both';
  const startCommands = [
    hasApi ? `dotnet run --project ./api/${projectNames.api}.csproj` : null,
    hasUi ? 'cd ui && npm start' : null
  ].filter(Boolean).join('\n');

  return `# ${name}\n\nA ChillSharp project workspace prepared for coding agents. The generated projects restore ChillSharp from public NuGet and npm registries; no local package archives are used.\n\n## Run\n\n\`\`\`bash\n${startCommands}\n\`\`\`\n\n## Start building\n\nOpen this folder with your coding agent and describe the first outcome you want to deliver. The agent guidance lives in [\`.agents/skills\`](.agents/skills). The API already includes a configured ChillSharp context and mapped endpoints; extend its sample model and workflow for your application.\n\nA request to build an application produces a connected ASP.NET Core ChillSharp API and an authenticated UI using persisted data. Add a separate public frontend when the requested users or workflows need one. Ask for an API-only, UI-only, or static prototype only when that is genuinely the desired scope.\n\nFor example:\n\n> Build a blog. Authors sign in to manage posts and categories in an admin UI; visitors can browse published posts in a separate public frontend. Use the same ChillSharp API and database for both.\n\n## Included guidance\n\nThe workspace contains skills for full-stack application delivery, model preparation, registration, permissions, localized text, menus, client plugins, current-user preferences, documentation, and MCP.\n`;
}

function projectGitignore(projectType) {
  const entries = ['.DS_Store', 'Thumbs.db', '.env'];
  if (projectType === 'api' || projectType === 'both') entries.push('api/bin/', 'api/obj/', 'api/*.db');
  if (projectType === 'ui' || projectType === 'both') entries.push('ui/node_modules/', 'ui/dist/', 'ui/.angular/');
  return `${entries.join('\n')}\n`;
}

function printUsage(output = console) {
  output.log('Usage: chill new <project-name> [--skills|--api|--ui|--both]');
  output.log('       chill install [--ng-client|--vue-client|--react-client|--ts-client|--chillsharp|--chillsharp-client|--py-client]');
  output.log('');
  output.log('Commands:');
  output.log('  new <project-name>  Create a skills folder, ChillSharp.Template API, UI, or both.');
  output.log('  install             Install a published ChillSharp client or .NET package.');
  output.log('');
  output.log('Without an option, new and install show a selection menu.');
}

if (path.resolve(process.argv[1] ?? '') === fileURLToPath(import.meta.url)) {
  process.exitCode = await run();
}
