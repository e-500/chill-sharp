#!/usr/bin/env node

import { cpSync, existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const packageDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const packageManifest = JSON.parse(readFileSync(path.join(packageDirectory, 'package.json'), 'utf8'));
const [command, projectName] = process.argv.slice(2);

if (command === 'new') {
  createProject(projectName);
} else if (command === '--help' || command === '-h' || command === 'help' || !command) {
  printUsage();
  process.exit(command ? 0 : 1);
} else if (command === '--version' || command === '-v') {
  console.log(packageManifest.version);
} else {
  console.error(`Unknown command: ${command}`);
  printUsage();
  process.exit(1);
}

function createProject(name) {
  if (!name) {
    console.error('A project name is required.');
    printUsage();
    process.exit(1);
  }

  if (!/^[a-z0-9][a-z0-9-]*$/.test(name)) {
    console.error('Project names must use lowercase letters, digits, and hyphens only.');
    process.exit(1);
  }

  const destination = path.resolve(process.cwd(), name);
  if (existsSync(destination)) {
    console.error(`Cannot create '${name}': '${destination}' already exists.`);
    process.exit(1);
  }

  mkdirSync(destination);
  cpSync(path.join(packageDirectory, 'skills'), path.join(destination, '.agents', 'skills'), { recursive: true });
  writeFileSync(path.join(destination, 'AGENTS.md'), projectInstructions(name));
  writeFileSync(path.join(destination, 'README.md'), projectReadme(name));
  writeFileSync(path.join(destination, '.gitignore'), '.DS_Store\nThumbs.db\n.env\n');

  console.log(`Created ${name}.`);
  console.log('Agent skills were added to .agents/skills.');
  console.log('Next steps:');
  console.log(`  cd ${name}`);
  console.log('  Open this folder in your coding agent and describe the first useful outcome.');
}

function projectInstructions(name) {
  return `# ${name}\n\nThis project is prepared for ChillSharp development with a coding agent.\n\nBefore changing ChillSharp code, inspect the relevant skill in \`.agents/skills\`. Use the project\'s existing conventions and keep implementation work focused on the requested outcome.\n\nWhen asked to build a new site or application, use \`.agents/skills/chillsharp-full-stack-application/SKILL.md\`. Unless the user explicitly asks for an API-only, UI-only, or static prototype, deliver a connected ASP.NET Core ChillSharp API, an authenticated management UI for real data, and a user-facing frontend that uses the same API. Do not replace those components with mock data, browser-only storage, or a standalone visual site.\n\nFor a new application, start by describing the users, data, and first workflow. The agent can then use the included full-stack, model, registration, permissions, i18n, menu, plugin, and MCP guidance as needed.\n`;
}

function projectReadme(name) {
  return `# ${name}\n\nA ChillSharp project workspace prepared for coding agents.\n\n## Start building\n\nOpen this folder with your coding agent and describe the first outcome you want to deliver. The agent guidance lives in [\`.agents/skills\`](.agents/skills).\n\nA request to build an application produces a connected system by default: an ASP.NET Core ChillSharp API, an authenticated management UI, and a user-facing frontend using that API. Ask for an API-only, UI-only, or static prototype only when that is genuinely the desired scope.\n\nFor example:\n\n> Build a blog. Authors sign in to manage posts and categories in an admin UI; visitors can browse published posts in a separate public frontend. Use the same ChillSharp API and database for both.\n\n## Included guidance\n\nThe workspace contains skills for full-stack application delivery, model preparation, registration, permissions, localized text, menus, client plugins, current-user preferences, documentation, and MCP.\n`;
}

function printUsage() {
  console.log('Usage: chill new <project-name>');
  console.log('');
  console.log('Commands:');
  console.log('  new <project-name>  Create an agent-ready ChillSharp workspace.');
}
