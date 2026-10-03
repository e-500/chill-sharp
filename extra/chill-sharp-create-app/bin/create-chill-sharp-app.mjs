#!/usr/bin/env node

import { copyFileSync, cpSync, existsSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const packageDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const templateDirectory = path.join(packageDirectory, 'template');
const packageManifest = JSON.parse(readFileSync(path.join(packageDirectory, 'package.json'), 'utf8'));
const [projectName] = process.argv.slice(2).filter((argument) => !argument.startsWith('-'));

if (!projectName || process.argv.includes('--help') || process.argv.includes('-h')) {
  printUsage();
  process.exit(projectName ? 0 : 1);
}

if (!/^[a-z0-9][a-z0-9-]*$/.test(projectName)) {
  console.error('Project names must use lowercase letters, digits, and hyphens only.');
  process.exit(1);
}

const destination = path.resolve(process.cwd(), projectName);
if (existsSync(destination)) {
  console.error(`Cannot create '${projectName}': '${destination}' already exists.`);
  process.exit(1);
}

cpSync(templateDirectory, destination, { recursive: true });

const generatedManifestPath = path.join(destination, 'package.json');
const generatedManifest = JSON.parse(readFileSync(generatedManifestPath, 'utf8'));
generatedManifest.name = projectName;
generatedManifest.private = true;
generatedManifest.description = 'Angular application powered by ChillSharp UI.';
generatedManifest.dependencies = {
  ...generatedManifest.dependencies,
  '@chill-sharp/ng-client': `^${packageManifest.version}`,
  '@chill-sharp/ts-client': `^${packageManifest.version}`,
  '@chill-sharp/ui-core': `^${packageManifest.version}`
};
writeFileSync(generatedManifestPath, `${JSON.stringify(generatedManifest, null, 2)}\n`);

rmSync(path.join(destination, 'package-lock.json'), { force: true });
rmSync(path.join(destination, 'upgrade.ps1'), { force: true });
rmSync(path.join(destination, 'upgrade.sh'), { force: true });
rmSync(path.join(destination, 'publish-to-shared-folder.ps1'), { force: true });
copyFileSync(path.join(destination, 'gitignore'), path.join(destination, '.gitignore'));
rmSync(path.join(destination, 'gitignore'), { force: true });
writeFileSync(path.join(destination, 'README.md'), `# ${projectName}\n\nAngular application shell powered by ChillSharp UI.\n\n## Get started\n\n\`\`\`bash\nnpm install\nnpm start\n\`\`\`\n\nThe application uses published ChillSharp packages from npm. Update the shared UI explicitly with:\n\n\`\`\`bash\nnpm install @chill-sharp/ui-core@latest\n\`\`\`\n\nConfigure the API and client URLs in \`public/env.js\` and \`public/runtime-config.js\`.\n`);
writeFileSync(path.join(destination, 'HOW_TO_BUILD.md'), `# Build ${projectName}\n\n\`\`\`bash\nnpm install\nnpm run build\n\`\`\`\n\nThe ChillSharp dependencies are restored from npm; no local package archives are required.\n`);

console.log(`Created ${projectName}.`);
console.log('Next steps:');
console.log(`  cd ${projectName}`);
console.log('  npm install');
console.log('  npm start');

function printUsage() {
  console.log('Usage: npx --yes @chill-sharp/create-app <project-name>');
}
