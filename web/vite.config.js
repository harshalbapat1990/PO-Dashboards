import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig(() => {
  return {
    plugins: [vue()],
    server: {
      host: 'localhost',
      proxy: {
        '/api': {
          target: 'http://localhost:7071',
          changeOrigin: false,
        },
      },
    },
  }
})
