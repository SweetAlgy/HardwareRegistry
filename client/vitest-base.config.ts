import { defineConfig } from 'vitest/config';

// Tests run in real Chromium (see "browsers" in angular.json): under jsdom DevExtreme dialogs recurse forever.
export default defineConfig({});
