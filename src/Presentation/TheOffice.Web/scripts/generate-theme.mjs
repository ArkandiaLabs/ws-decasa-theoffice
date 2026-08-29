#!/usr/bin/env node
// Renderiza src/theme.css (el bloque @theme de Tailwind v4) desde el frontmatter de DESIGN.md.
// Lee DESIGN.md y no design/tokens.json porque el export DTCG de @google/design.md v0.4.0
// descarta lineHeight, y aqui cada nivel de la escala trae su interlineado.

import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parse } from 'yaml';

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..');
const SRC = join(ROOT, 'DESIGN.md');
const DEST = join(ROOT, 'src/theme.css');

// El alias y la pila de respaldo de cada familia no caben en DESIGN.md: el formato define una
// fontFamily suelta. Los alias son los del Arkandia Design System.
const FAMILIES = {
  Aleo: { alias: 'display', fallback: "Georgia, 'Times New Roman', serif" },
  Rubik: { alias: 'body', fallback: 'system-ui, -apple-system, sans-serif' },
  'Fira Code': { alias: 'mono', fallback: "ui-monospace, 'SF Mono', Menlo, monospace" },
};

function frontmatter(markdown) {
  const match = /^---\r?\n([\s\S]*?)\r?\n---\r?\n/.exec(markdown);
  if (!match) {
    throw new Error('DESIGN.md no tiene frontmatter YAML.');
  }
  return parse(match[1]);
}

const tokens = frontmatter(readFileSync(SRC, 'utf8'));
const lines = [
  '/* Generado desde DESIGN.md por scripts/generate-theme.mjs — no editar a mano. */',
  '',
  '@theme {',
  '  /* Paleta */',
];

for (const [name, value] of Object.entries(tokens.colors ?? {})) {
  lines.push(`  --color-${name}: ${String(value).toLowerCase()};`);
}

// Una variable por familia, no por nivel: el tamano y la voz se aplican por separado.
lines.push('', '  /* Tipografia (familias) */');
const emitted = new Set();
for (const level of Object.values(tokens.typography ?? {})) {
  const family = FAMILIES[level.fontFamily];
  if (!family) {
    throw new Error(`Familia sin alias ni pila de respaldo en FAMILIES: ${level.fontFamily}`);
  }
  if (!emitted.has(family.alias)) {
    emitted.add(family.alias);
    lines.push(`  --font-${family.alias}: '${level.fontFamily}', ${family.fallback};`);
  }
}

// Los modificadores `--text-x--*` hacen que una sola clase resuelva tamano, interlineado y grosor.
lines.push('', '  /* Tipografia (escala) */');
for (const [index, [name, level]] of Object.entries(tokens.typography ?? {}).entries()) {
  if (index > 0) {
    lines.push('');
  }
  lines.push(`  --text-${name}: ${level.fontSize};`);
  if (level.lineHeight !== undefined) {
    lines.push(`  --text-${name}--line-height: ${level.lineHeight};`);
  }
  if (level.fontWeight !== undefined) {
    lines.push(`  --text-${name}--font-weight: ${level.fontWeight};`);
  }
  if (level.letterSpacing !== undefined) {
    lines.push(`  --text-${name}--letter-spacing: ${level.letterSpacing};`);
  }
}

// El grupo `spacing` de DESIGN.md no se emite a proposito: sus valores ya son la escala base-4 de
// Tailwind (`p-md` y `p-4` rinden el mismo pixel) y declararlos como `--spacing-sm…xl` ensombrece
// la escala de contenedores, dejando `max-w-xl` en 40px sin que nada lo reporte.

lines.push('', '  /* Radios */');
for (const [name, value] of Object.entries(tokens.rounded ?? {})) {
  lines.push(`  --radius-${name}: ${value};`);
}

lines.push('}', '');
const css = lines.join('\n');

// En modo --check no se toca el disco: se compara, para que la compuerta no dependa de git.
if (process.argv.includes('--check')) {
  if (!existsSync(DEST) || readFileSync(DEST, 'utf8') !== css) {
    console.error(`${DEST} no coincide con DESIGN.md. Corre \`make web-tokens\` y commitealo.`);
    process.exit(1);
  }
  console.log(`${DEST} esta al dia con DESIGN.md`);
} else {
  writeFileSync(DEST, css);
  console.log(`Escrito ${DEST}`);
}
