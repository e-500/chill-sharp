import { cpSync, existsSync, rmSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const packageDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const sourceDirectory = path.resolve(packageDirectory, '..', '..', '.agents', 'skills');
const destinationDirectory = path.join(packageDirectory, 'skills');

if (!existsSync(sourceDirectory)) {
  throw new Error(`Cannot find ChillSharp skills at '${sourceDirectory}'.`);
}

rmSync(destinationDirectory, { recursive: true, force: true });
cpSync(sourceDirectory, destinationDirectory, { recursive: true });
