/** @type {import('jest').Config} */
module.exports = {
  preset: 'jest-expo',
  testMatch: ['<rootDir>/src/**/*.test.tsx'],
  setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  clearMocks: true,
  restoreMocks: true,
};
