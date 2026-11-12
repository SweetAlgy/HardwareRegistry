// Keeps browser requests same-origin, so the API needs no CORS setup.
module.exports = {
  '/api/**': {
    target: process.env.HARDWARE_REGISTRY_API_URL || 'http://localhost:5080',
    changeOrigin: false,
  },
  '/hubs/**': {
    target: process.env.HARDWARE_REGISTRY_API_URL || 'http://localhost:5080',
    changeOrigin: false,
    ws: true,
  },
};
