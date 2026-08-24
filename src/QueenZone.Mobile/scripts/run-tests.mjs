/**
 * Discover and run every src glob *.test.ts and *.test.tsx file.
 * A self-check writes an unlisted probe file and fails if discovery misses it,
 * so new tests do not need a package.json path list (see #870).
 */
import { globSync, rmSync, writeFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const mobileRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const srcDir = path.join(mobileRoot, 'src');

function discoverTestFiles() {
  const files = [];
  for (const pattern of ['**/*.test.ts', '**/*.test.tsx']) {
    for (const rel of globSync(pattern, { cwd: srcDir })) {
      files.push(path.join('src', rel).split(path.sep).join('/'));
    }
  }
  files.sort();
  return files;
}

function runNodeTest(files) {
  const result = spawnSync(
    process.execPath,
    [
      '--experimental-strip-types',
      '--disable-warning=MODULE_TYPELESS_PACKAGE_JSON',
      '--test',
      ...files,
    ],
    { cwd: mobileRoot, stdio: 'inherit' },
  );
  return result.status ?? 1;
}

const probeRel = `src/discovery-probe-${process.pid}.test.ts`;
const probeAbs = path.join(mobileRoot, probeRel);

let exitCode = 1;
try {
  writeFileSync(
    probeAbs,
    `import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

describe('automatic test discovery probe', () => {
  it('runs without being listed in package.json', () => {
    assert.equal(1 + 1, 2);
  });
});
`,
    'utf8',
  );

  const discovered = discoverTestFiles();
  if (!discovered.includes(probeRel)) {
    console.error(
      `Discovery self-check failed: unlisted ${probeRel} was not discovered.`,
    );
  } else {
    const suiteFiles = discovered.filter((file) => file !== probeRel);
    const probeStatus = runNodeTest([probeRel]);
    if (probeStatus !== 0) {
      console.error('Discovery self-check failed: probe test did not pass.');
      exitCode = probeStatus;
    } else if (suiteFiles.length === 0) {
      console.error('No *.test.ts or *.test.tsx files found under src/.');
    } else {
      console.log(
        `Discovery self-check passed (${suiteFiles.length} suite files; unlisted probe executed).`,
      );
      exitCode = runNodeTest(suiteFiles);
    }
  }
} finally {
  rmSync(probeAbs, { force: true });
}

process.exit(exitCode);

