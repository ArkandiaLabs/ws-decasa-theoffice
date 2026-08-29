#!/usr/bin/env node
// Falla si una plantilla usa una clase del sistema de diseno que Tailwind no genero.
// Tailwind emite toda clase que reconoce en el codigo fuente, asi que "usada en la plantilla pero
// ausente del CSS compilado" solo puede significar una cosa: no la reconocio. Ahi viven los fallos
// silenciosos -- `text-muted` cayendo en el namespace de tamanos, `max-w-xl` contra `--spacing-xl`.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');
const DIST = join(ROOT, 'dist');
const SRC = join(ROOT, 'src/app');

// Los prefijos que consumen tokens. Lo demas de Tailwind no lo cubre el sistema de diseno.
const PREFIXES = 'bg|text|border|divide|font|rounded|shadow|outline|ring|fill|stroke';
const USED = new RegExp(
  String.raw`\b((?:hover:|focus:|focus-visible:|active:|disabled:|sm:|md:|lg:|xl:)?(?:${PREFIXES})-[a-z][a-z0-9-]*)\b`,
  'g',
);

function walk(dir, match, found = []) {
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) {
      walk(full, match, found);
    } else if (match(entry)) {
      found.push(full);
    }
  }
  return found;
}

const bundles = walk(DIST, (name) => /^styles-.*\.css$/.test(name));
if (bundles.length === 0) {
  console.error('No hay CSS compilado en dist/. Corre `pnpm build` antes de esta verificacion.');
  process.exit(1);
}

// Los nombres de clase reales, des-escapando lo que Tailwind escapa (`.hover\:bg-x` -> `hover:bg-x`).
const generated = new Set();
for (const bundle of bundles) {
  for (const [, raw] of readFileSync(bundle, 'utf8').matchAll(/\.((?:\\.|[A-Za-z0-9_-])+)/g)) {
    generated.add(raw.replace(/\\(.)/g, '$1'));
  }
}

// La otra forma de fallo: la clase resuelve, pero al token equivocado. No es detectable en general,
// pero el caso conocido si: declarar `--spacing-*` ensombrece la escala de contenedores y deja
// `max-w-xl` valiendo el espaciado. Por eso el generador nunca lo emite; esto vigila que siga asi.
const theme = readFileSync(join(ROOT, 'src/theme.css'), 'utf8');
const shadowing = [...theme.matchAll(/^\s*(--spacing-[a-z0-9-]+):/gm)].map(([, name]) => name);
if (shadowing.length > 0) {
  console.error(
    `src/theme.css declara ${shadowing.join(', ')}.\n` +
      'El namespace `--spacing-*` ensombrece la escala de contenedores de Tailwind: `max-w-xl`\n' +
      'pasaria a valer el espaciado en vez de 36rem, sin que nada mas lo reporte. El grupo `spacing`\n' +
      'de DESIGN.md no se emite a proposito -- ver scripts/generate-theme.mjs.',
  );
  process.exit(1);
}

const orphans = new Map();
for (const file of walk(SRC, (name) => /\.(html|ts)$/.test(name) && !name.endsWith('.spec.ts'))) {
  for (const [, cls] of readFileSync(file, 'utf8').matchAll(USED)) {
    if (!generated.has(cls)) {
      orphans.set(cls, (orphans.get(cls) ?? new Set()).add(file.slice(ROOT.length + 1)));
    }
  }
}

if (orphans.size > 0) {
  console.error('Clases del sistema de diseno que Tailwind no genero:\n');
  for (const [cls, files] of orphans) {
    console.error(`  ${cls}`);
    for (const file of files) console.error(`      ${file}`);
  }
  console.error(
    '\nNo es un error de escritura necesariamente: el token puede existir con otro nombre de clase.' +
      '\n`--color-text-muted` genera `text-text-muted`, y `text-muted` a secas cae en el namespace de' +
      '\ntamanos de fuente. Revisa src/theme.css.',
  );
  process.exit(1);
}

console.log(
  `Las clases del sistema resuelven: ${generated.size} selectores generados, 0 huerfanas`,
);
