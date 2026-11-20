import { describe, expect, it } from 'vitest';
import { deriveCasings } from './case-variants.js';

describe('deriveCasings', () => {
  it('desde snake_case, deriva las 4 variantes', () => {
    expect(deriveCasings('mi_mejor_proyecto')).toEqual({
      pascal: 'MiMejorProyecto',
      camel: 'miMejorProyecto',
      kebab: 'mi-mejor-proyecto',
      snake: 'mi_mejor_proyecto',
    });
  });

  it('tambien acepta kebab-case sin romperse', () => {
    expect(deriveCasings('mi-mejor-proyecto').pascal).toBe('MiMejorProyecto');
  });

  it('tambien acepta separadores mixtos ("_" y "-" combinados)', () => {
    expect(deriveCasings('tu-line_mia')).toEqual({
      pascal: 'TuLineMia',
      camel: 'tuLineMia',
      kebab: 'tu-line-mia',
      snake: 'tu_line_mia',
    });
  });

  it('tambien acepta PascalCase/camelCase de entrada (separa por limite de mayuscula)', () => {
    expect(deriveCasings('MiMejorProyecto').snake).toBe('mi_mejor_proyecto');
    expect(deriveCasings('miMejorProyecto').kebab).toBe('mi-mejor-proyecto');
  });

  it('una sola palabra funciona igual en las 4 variantes', () => {
    expect(deriveCasings('proyecto')).toEqual({
      pascal: 'Proyecto',
      camel: 'proyecto',
      kebab: 'proyecto',
      snake: 'proyecto',
    });
  });

  it('espacios/guiones/guiones bajos repetidos o en los extremos no dejan palabras vacias', () => {
    expect(deriveCasings('  mi__proyecto--nuevo  ').snake).toBe('mi_proyecto_nuevo');
  });

  it('string vacio devuelve las 4 variantes vacias', () => {
    expect(deriveCasings('')).toEqual({ pascal: '', camel: '', kebab: '', snake: '' });
  });
});
