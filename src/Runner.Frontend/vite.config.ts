import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/auth': 'http://localhost:8080',
      '/assignments': 'http://localhost:8080',
      '/submissions': 'http://localhost:8080',
      '/webhooks': 'http://localhost:8080',
    },
  },
})
