/**
 * Run the mobile API consumer-contract suite against a live Testing host.
 * Requires QUEENZONE_MOBILE_CONTRACT_FIXTURE (or contracts/host.json) written
 * by the ASP.NET Testing bootstrap.
 */
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const mobileRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const contractsDir = path.join(mobileRoot, 'contracts');
const registerHooks = path.join(contractsDir, 'register-hooks.mjs');
const suite = path.join(contractsDir, 'consumer.test.ts');

const result = spawnSync(
  process.execPath,
  [
    '--experimental-strip-types',
    '--disable-warning=MODULE_TYPELESS_PACKAGE_JSON',
    '--import',
    registerHooks,
    '--test',
    '--test-reporter=spec',
    suite,
  ],
  { cwd: mobileRoot, stdio: 'inherit', env: process.env },
);

process.exit(result.status ?? 1);
