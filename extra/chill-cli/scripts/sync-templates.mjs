import { cpSync, existsSync, mkdirSync, rmSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const packageDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repositoryDirectory = path.resolve(packageDirectory, '..', '..');
const templateDirectory = path.join(packageDirectory, 'templates');

const templates = [
  {
    name: 'api',
    source: path.join(repositoryDirectory, 'ChillSharp.Template'),
    excluded: new Set(['.agents', 'bin', 'obj', 'nupkgs', 'NuGet.Config', 'upgrade-local.ps1', 'upgrade-local.sh'])
  },
  {
    name: 'ui',
    source: path.join(repositoryDirectory, 'extra', 'chill-sharp-ui-template'),
    excluded: new Set(['.agents', '.angular', 'dist', 'node_modules', 'packages', 'package-lock.json', 'upgrade-local.ps1', 'upgrade-local.sh', 'publish-to-shared-folder.ps1'])
  }
];

rmSync(templateDirectory, { recursive: true, force: true });
mkdirSync(templateDirectory, { recursive: true });

for (const template of templates) {
  if (!existsSync(template.source)) throw new Error(`Cannot find template source at '${template.source}'.`);
  cpSync(template.source, path.join(templateDirectory, template.name), {
    recursive: true,
    filter: (source) => !template.excluded.has(path.basename(source))
  });
}
