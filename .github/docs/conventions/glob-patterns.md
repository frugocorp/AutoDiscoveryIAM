# Patrones Glob: Guía y Ejemplos

## ¿Qué es un patrón glob?
Un patrón glob es una forma de especificar conjuntos de archivos o carpetas usando caracteres comodín. Es ampliamente utilizado en archivos como `.gitignore`, `.prettierignore`, scripts y herramientas de automatización.

---

## Caracteres y patrones básicos

- `*`
  Coincide con cualquier secuencia de caracteres (excepto `/`).
  - Ejemplo: `*.md` selecciona todos los archivos con extensión `.md` en el directorio actual.

- `**`
  Coincide con cualquier número de subdirectorios (incluyendo ninguno).
  - Ejemplo: `docs/**/*.md` selecciona todos los `.md` dentro de `docs/` y todas sus subcarpetas.

- `?`
  Coincide con cualquier carácter individual.
  - Ejemplo: `file-?.js` selecciona `file-a.js`, `file-1.js`, etc.

- `[abc]`
  Coincide con cualquiera de los caracteres entre corchetes.
  - Ejemplo: `file[12].js` selecciona `file1.js` y `file2.js`.

- `[a-z]`
  Coincide con cualquier carácter dentro del rango especificado.
  - Ejemplo: `file[a-c].js` selecciona `filea.js`, `fileb.js`, `filec.js`.

- `!`
  Al inicio de la línea, invierte el patrón: incluye archivos que normalmente serían ignorados.
  - Ejemplo:
    ```ignore
    dist/
    !dist/README.md
    ```
    Ignora todo en `dist/` excepto `README.md`.

---

## Ejemplos prácticos

```ignore
# Ignora todos los archivos .log en cualquier lugar
*.log

# Ignora todas las carpetas bin y obj en cualquier parte del proyecto
**/bin/
**/obj/

# Ignora todo en la carpeta dist, excepto README.md
/dist/
!/dist/README.md

# Ignora todos los archivos .tmp dentro de la carpeta temp
/temp/*.tmp

# Incluye todos los archivos .md en src y subcarpetas, aunque src/ esté ignorado
!src/**/*.md
```

---

## Buenas prácticas
- Usa patrones glob para simplificar la gestión de archivos en herramientas de automatización y control de versiones.
- Sé específico en las excepciones usando `!` para evitar incluir archivos no deseados.
- Consulta la documentación de la herramienta para verificar compatibilidad de patrones.

---

## Recursos adicionales
- [Documentación oficial glob (npm)](https://www.npmjs.com/package/glob)
- [Guía de patrones glob de GitHub](https://docs.github.com/es/actions/using-workflows/workflow-syntax-for-github-actions#filter-pattern-cheat-sheet)
- [Explicación visual de glob](https://globster.xyz/)
