import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false
      },
      '/activities-api': {
        target: 'https://localhost:7001',
        changeOrigin: true,
        secure: false,
        rewrite: path => path.replace(/^\/activities-api/, '')
      }
    }
  },
  build: { outDir: 'dist', sourcemap: false }
});
