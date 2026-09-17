const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { execFileSync } = require('node:child_process');
const { test } = require('node:test');

const root = path.resolve(__dirname, '../..');
const packages = fs.readdirSync(path.join(root, 'extra'))
  .filter(name => fs.existsSync(path.join(root, 'extra', name, 'package-lock.json')));

for (const script of ['version.ps1', 'version.sh']) {
  test(`${script} preserves third-party packages and link entries`, () => {
    const fixture = fs.mkdtempSync(path.join(os.tmpdir(), 'chillsharp-version-'));
    try {
      fs.copyFileSync(path.join(root, script), path.join(fixture, script));
      fs.copyFileSync(path.join(root, 'Directory.Build.props'), path.join(fixture, 'Directory.Build.props'));
      const originals = new Map();
      for (const name of packages) {
        const target = path.join(fixture, 'extra', name);
        fs.mkdirSync(target, { recursive: true });
        for (const file of ['package.json', 'package-lock.json']) {
          fs.copyFileSync(path.join(root, 'extra', name, file), path.join(target, file));
        }
        const lockPath = path.join(target, 'package-lock.json');
        const lock = JSON.parse(fs.readFileSync(lockPath, 'utf8'));
        // A dependency sharing the release version must also remain untouched.
        lock.packages['node_modules/version-collision'] = { version: lock.version };
        fs.writeFileSync(lockPath, JSON.stringify(lock, null, 2));
        originals.set(name, lock);
      }

      const command = script.endsWith('.ps1') ? 'pwsh' : process.platform === 'win32'
        ? path.join(process.env.ProgramFiles, 'Git', 'bin', 'bash.exe') : 'bash';
      const args = script.endsWith('.ps1')
        ? ['-NoProfile', '-File', path.join(fixture, script), '-Part', 'build']
        : [path.join(fixture, script), 'build'];
      execFileSync(command, args, { cwd: fixture, input: 'n\n', encoding: 'utf8' });

      for (const [name, before] of originals) {
        const after = JSON.parse(fs.readFileSync(path.join(fixture, 'extra', name, 'package-lock.json'), 'utf8'));
        const parts = before.version.split('.').map(Number);
        parts[2]++;
        const expected = parts.join('.');
        assert.equal(after.version, expected);
        assert.equal(after.packages[''].version, expected);
        for (const [key, entry] of Object.entries(before.packages)) {
          if (!key) continue;
          if (entry.link || !/(?:^|\/)@?chill-sharp(?:\/|-)/.test(key)) {
            assert.deepEqual(after.packages[key], entry, `${name}: ${key}`);
          } else if (entry.version) {
            assert.equal(after.packages[key].version, expected, `${name}: ${key}`);
          }
        }
      }
    } finally {
      fs.rmSync(fixture, { recursive: true, force: true });
    }
  });
}
