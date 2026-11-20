import { describe, expect, it } from 'vitest';
import { applySiteVariables, findConflicts, groupPlaceholders, scanPlaceholders } from './site-variables.js';

describe('scanPlaceholders', () => {
  it('encuentra una clave simple', () => {
    expect(scanPlaceholders('ssh -p 22 (--USER_NEW--)@(--HOST_IP--)')).toEqual([
      { raw: '(--USER_NEW--)', base: 'USER_NEW', casing: null },
      { raw: '(--HOST_IP--)', base: 'HOST_IP', casing: null },
    ]);
  });

  it('encuentra una clave con sufijo de casing', () => {
    expect(scanPlaceholders('npm install (--PROYECTO:pascal--)')).toEqual([{ raw: '(--PROYECTO:pascal--)', base: 'PROYECTO', casing: 'pascal' }]);
  });

  it('ignora una ocurrencia escapada con barra invertida', () => {
    expect(scanPlaceholders('la sintaxis es (-\\-CLAVE--), reemplaza (--REAL--)')).toEqual([{ raw: '(--REAL--)', base: 'REAL', casing: null }]);
  });

  it('no matchea texto que no cumple el charset estricto de la clave', () => {
    expect(scanPlaceholders('esto no es una clave (-- no vale --)')).toEqual([]);
  });

  it('sustituye dentro de un bloque de codigo igual que en prosa (es texto plano, sin distincion)', () => {
    const markdown = ['`````code-block', '```bash', 'sudo ufw allow (--SSH_NEW_PORT--)/tcp', '```', '`````'].join('\n');
    expect(scanPlaceholders(markdown)).toEqual([{ raw: '(--SSH_NEW_PORT--)', base: 'SSH_NEW_PORT', casing: null }]);
  });
});

describe('groupPlaceholders', () => {
  it('agrupa por nombre base, alfabetico', () => {
    const occurrences = scanPlaceholders('(--HOST_IP--) (--USER_NEW--) (--HOST_IP--)');
    expect(groupPlaceholders(occurrences)).toEqual([
      { base: 'HOST_IP', casing: false },
      { base: 'USER_NEW', casing: false },
    ]);
  });

  it('marca un grupo como "de casing" si ALGUNA ocurrencia trae sufijo', () => {
    const occurrences = scanPlaceholders('(--PROYECTO--) (--PROYECTO:kebab--)');
    expect(groupPlaceholders(occurrences)).toEqual([{ base: 'PROYECTO', casing: true }]);
  });
});

describe('applySiteVariables', () => {
  it('sustituye una clave simple con el valor guardado', () => {
    // 203.0.113.0/24 (TEST-NET-3, RFC 5737) -- rango reservado para documentacion, nunca una IP real.
    const values = new Map([['HOST_IP', '203.0.113.10']]);
    expect(applySiteVariables('ssh -p 22 user@(--HOST_IP--)', values)).toBe('ssh -p 22 user@203.0.113.10');
  });

  it('deja la clave CRUDA si todavia no hay valor guardado', () => {
    expect(applySiteVariables('ssh -p 22 user@(--HOST_IP--)', new Map())).toBe('ssh -p 22 user@(--HOST_IP--)');
  });

  it('deriva los 4 casings de un grupo desde un solo valor guardado', () => {
    const values = new Map([['PROYECTO', 'mi_super_app']]);
    const markdown = 'npm install (--PROYECTO:pascal--)\ncd (--PROYECTO:kebab--)';
    expect(applySiteVariables(markdown, values)).toBe('npm install MiSuperApp\ncd mi-super-app');
  });

  it('restaura una ocurrencia escapada a texto literal, sin sustituirla', () => {
    expect(applySiteVariables('la sintaxis es (-\\-CLAVE--)', new Map([['CLAVE', 'x']]))).toBe('la sintaxis es (--CLAVE--)');
  });
});

describe('findConflicts', () => {
  it('detecta una clave usada suelta en un archivo Y como grupo de casing en otro', () => {
    const byFile = new Map([
      ['a.md', scanPlaceholders('(--PROYECTO--)')],
      ['b.md', scanPlaceholders('(--PROYECTO:kebab--)')],
    ]);
    expect(findConflicts(byFile)).toEqual([{ base: 'PROYECTO', plainFiles: ['a.md'], casingFiles: ['b.md'] }]);
  });

  it('no reporta nada si una clave es consistente en todo el sitio', () => {
    const byFile = new Map([
      ['a.md', scanPlaceholders('(--HOST_IP--)')],
      ['b.md', scanPlaceholders('(--HOST_IP--)')],
    ]);
    expect(findConflicts(byFile)).toEqual([]);
  });

  it('no reporta nada si un grupo de casing aparece en varios archivos, siempre con sufijo', () => {
    const byFile = new Map([
      ['a.md', scanPlaceholders('(--PROYECTO:pascal--)')],
      ['b.md', scanPlaceholders('(--PROYECTO:kebab--)')],
    ]);
    expect(findConflicts(byFile)).toEqual([]);
  });
});
