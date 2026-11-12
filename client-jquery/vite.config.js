import { playwright } from '@vitest/browser-playwright';
import { defineConfig } from 'vite';

export default defineConfig({
  server: {
    port: 4300,
    strictPort: true,
    // Keeps browser requests same-origin, so the API needs no CORS setup.
    proxy: {
      '/api': {
        target: process.env.HARDWARE_REGISTRY_API_URL || 'http://localhost:5080',
        changeOrigin: false,
      },
      '/hubs': {
        target: process.env.HARDWARE_REGISTRY_API_URL || 'http://localhost:5080',
        changeOrigin: false,
        ws: true,
      },
    },
  },
  // Pre-bundled up front: discovering them mid-run makes Vite reload the page, which can load
  // a second copy of jQuery without the DevExtreme widgets registered on it.
  optimizeDeps: {
    include: [
      '@microsoft/signalr',
      'devextreme/data/custom_store',
      'devextreme/integration/jquery',
      'devextreme/localization',
      'devextreme/ui/button',
      'devextreme/ui/data_grid',
      'devextreme/ui/dialog',
      'devextreme/ui/form',
      'devextreme/ui/notify',
      'devextreme/ui/number_box',
      'devextreme/ui/popup',
      'devextreme/ui/select_box',
      'devextreme/ui/text_box',
      'jquery',
    ],
  },
  test: {
    include: ['src/**/*.spec.js'],
    setupFiles: ['src/test-setup.js'],
    // DevExtreme widgets need a real layout engine: under jsdom its dialogs recurse forever.
    browser: {
      enabled: true,
      provider: playwright(),
      headless: true,
      instances: [{ browser: 'chromium' }],
    },
  },
});
