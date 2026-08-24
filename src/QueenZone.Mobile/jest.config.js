/** @type {import('jest').Config} */
module.exports = {
  preset: 'jest-expo',
  // Relative glob: `<rootDir>/src/**` misses every file on Windows because
  // Jest builds a mixed-slash path that micromatch does not match.
  testMatch: ['**/src/**/*.test.tsx'],
  setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  clearMocks: true,
  restoreMocks: true,
  // Coverage floors live in scripts/mobile-coverage-floors.json (enforced by
  // scripts/Test-MobileCoverageGate.mjs). Do not put thresholds here — this
  // runner is only one of two suites (#871 Option A).
  collectCoverageFrom: [
    '**/src/**/*.{ts,tsx}',
    '!**/src/**/*.test.{ts,tsx}',
    '!**/src/**/*.d.ts',
    '!**/src/test/**',
  ],
  coverageDirectory: 'coverage/jest',
  coverageReporters: ['json', 'json-summary', 'lcov', 'text-summary', 'cobertura'],
};
