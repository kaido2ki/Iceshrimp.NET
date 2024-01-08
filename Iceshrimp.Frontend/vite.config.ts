import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import eslint from 'vite-plugin-eslint';

export default defineConfig({
	plugins: [
		vue(),
		eslint({
			cache: true,
			failOnError: true,
			failOnWarning: false,
		}),
	],
	build: {
		emptyOutDir: true,
		manifest: true,
		rollupOptions: {
			input: 'src/main.ts',
		},
	},
})
