import { defineConfig } from 'vite';

// One entry for each `type: "bundle"` extension in umbraco-package.json. An entry only exports `manifests`; the
// backoffice's bundle loader reads the exported values.
//
// `preserveEntrySignatures: 'strict'` stops Rollup renaming the entry's exports, which would leave the backoffice
// with an empty module. A relative `base` makes chunks load from wherever the bundle is served, which is
// /App_Plugins/Heliograph/dist/ and not the site root. Both were learned in Wayfinder.Umbraco.
export default defineConfig({
  base: './',
  build: {
    outDir: '../Heliograph.Umbraco/wwwroot/dist',
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      input: {
        'heliograph-correspondence-manifests': 'src/backoffice/correspondence/manifests.ts',
      },
      output: {
        format: 'es',
        entryFileNames: '[name].js',
        chunkFileNames: '[name]-[hash].js',
      },
      // Umbraco's own code is there at run time and is not bundled.
      external: [/^@umbraco-cms\/backoffice/],
      preserveEntrySignatures: 'strict',
    },
  },
});
