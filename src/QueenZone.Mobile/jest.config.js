/** @type {import('jest').Config} */
module.exports = {
  preset: 'jest-expo',
  // Relative glob: `<rootDir>/src/**` misses every file on Windows because
  // Jest builds a mixed-slash path that micromatch does not match.
  testMatch: ['**/src/**/*.test.tsx'],
  setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  clearMocks: true,
  restoreMocks: true,
};
