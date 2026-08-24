/**
 * Discover all .test.ts and .test.tsx files under src/.
 * Pure tests (.test.ts) run on Node's test runner. Component tests (.test.tsx)
 * run on Jest + jest-expo. A self-check writes unlisted probes of both kinds
 * so new tests do not need a package.json path list (see #870 / #833).
 */
import { globSync, rmSync, writeFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const mobileRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const srcDir = path.join(mobileRoot, 'src');
const jestBin = path.join(mobileRoot, 'node_modules', 'jest', 'bin', 'jest.js');

function toPosix(rel) {
  return path.join('src', rel).split(path.sep).join('/');
}

function discover(pattern) {
  return globSync(pattern, { cwd: srcDir }).map(toPosix).sort();
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

function runJest(files) {
  const result = spawnSync(
    process.execPath,
    [jestBin, '--ci', '--watchAll=false', '--runInBand', ...files],
    { cwd: mobileRoot, stdio: 'inherit' },
  );
  return result.status ?? 1;
}

const tsProbeRel = `src/discovery-probe-${process.pid}.test.ts`;
const tsxProbeRel = `src/discovery-probe-${process.pid}.test.tsx`;
const tsProbeAbs = path.join(mobileRoot, tsProbeRel);
const tsxProbeAbs = path.join(mobileRoot, tsxProbeRel);

let exitCode = 1;
try {
  writeFileSync(
    tsProbeAbs,
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
  writeFileSync(
    tsxProbeAbs,
    `describe('automatic component test discovery probe', () => {
  it('runs without being listed in package.json', () => {
    expect(1 + 1).toBe(2);
  });
});
`,
    'utf8',
  );

  const tsFiles = discover('**/*.test.ts');
  const tsxFiles = discover('**/*.test.tsx');

  if (!tsFiles.includes(tsProbeRel)) {
    console.error(`Discovery self-check failed: unlisted ${tsProbeRel} was not discovered.`);
  } else if (!tsxFiles.includes(tsxProbeRel)) {
    console.error(`Discovery self-check failed: unlisted ${tsxProbeRel} was not discovered.`);
  } else {
    const suiteTs = tsFiles.filter((file) => file !== tsProbeRel);
    const suiteTsx = tsxFiles.filter((file) => file !== tsxProbeRel);
    const probeTsStatus = runNodeTest([tsProbeRel]);
    const probeTsxStatus = runJest([tsxProbeRel]);
    if (probeTsStatus !== 0) {
      console.error('Discovery self-check failed: Node probe test did not pass.');
      exitCode = probeTsStatus;
    } else if (probeTsxStatus !== 0) {
      console.error('Discovery self-check failed: Jest probe test did not pass.');
      exitCode = probeTsxStatus;
    } else if (suiteTs.length === 0 && suiteTsx.length === 0) {
      console.error('No *.test.ts or *.test.tsx files found under src/.');
    } else {
      console.log(
        `Discovery self-check passed (${suiteTs.length} Node files, ${suiteTsx.length} Jest files; unlisted probes executed).`,
      );
      const nodeStatus = suiteTs.length === 0 ? 0 : runNodeTest(suiteTs);
      if (nodeStatus !== 0) {
        exitCode = nodeStatus;
      } else {
        exitCode = suiteTsx.length === 0 ? 0 : runJest(suiteTsx);
      }
    }
  }
} finally {
  rmSync(tsProbeAbs, { force: true });
  rmSync(tsxProbeAbs, { force: true });
}

process.exit(exitCode);
