# 🔀 Tipos de Ramas y Convenciones de Nombres

Una convención clara para nombrar ramas facilita la colaboración, el control de versiones y la automatización en proyectos de cualquier tamaño. A continuación se describen los tipos de ramas a utilizar, su propósito y ejemplos de uso.

---

## Tabla Resumen de Tipos de Ramas

| Tipo de Rama [branch-type] | Issue Type GitHub | Propósito principal                                                        | Template                                                     |
|:------------:|:----------------:|----------------------------------------------------------------------------|--------------------------------------------------------------|
| feature      | Feature          | Desarrollar nuevas funcionalidades o mejoras significativas                | [feature_request.md](../../ISSUE_TEMPLATE/feature_request.md)    |
| fix          | Bug              | Corregir bugs o errores detectados en el sistema                           | [fix_bug.md](../../ISSUE_TEMPLATE/fix_bug.md)                    |
| refactor     | Task             | Reestructurar o mejorar el código sin cambiar la lógica de negocio         | [refactor.md](../../ISSUE_TEMPLATE/refactor.md)                  |
| hotfix       | Bug              | Arreglos críticos en producción que requieren solución inmediata           | [hotfix.md](../../ISSUE_TEMPLATE/hotfix.md)                      |
| chore        | Task             | Tareas técnicas o de mantenimiento que no afectan directamente el producto | [chore.md](../../ISSUE_TEMPLATE/chore.md)                        |
| test         | Task             | Agregar, ajustar o mejorar pruebas automatizadas                           | [test.md](../../ISSUE_TEMPLATE/test.md)                          |
| docs         | Task             | Documentar o mejorar archivos `.md`, `README`, o documentación interna     | [docs.md](../../ISSUE_TEMPLATE/docs.md)                          |
| ci           | Task             | Cambios en configuración de integración/despliegue continuo                | [ci.md](../../ISSUE_TEMPLATE/ci.md)                              |
| style        | Task             | Cambios en formato, estilo o convenciones del código (sin lógica)          | [style.md](../../ISSUE_TEMPLATE/style.md)                        |
| perf         | Task             | Optimización de rendimiento                                                | [perf.md](../../ISSUE_TEMPLATE/perf.md)                          |
| release      |                  | Preparar versiones formales para producción                                | Sin Plantilla                                                |
| experiment   |                  | Probar ideas nuevas o prototipos temporales                                | Sin Plantilla                                                |
| merge        |                  | Ramas dedicadas a fusiones complejas entre ramas largas                    | Sin Plantilla                                                |

---

## Descripción de cada tipo de rama

### `feature/`
- **Propósito:** Desarrollo de nuevas funcionalidades o mejoras grandes.
- **Cuándo usar:** Cada vez que se inicia una nueva feature o user story.

### `fix/`
- **Propósito:** Corrección de errores o bugs.
- **Cuándo usar:** Cuando se resuelve un bug reportado.

### `refactor/`
- **Propósito:** Mejorar estructura, legibilidad o modularidad del código sin modificar la lógica.
- **Cuándo usar:** Limpieza, reorganización o actualización técnica.

### `hotfix/`
- **Propósito:** Solucionar problemas críticos en producción de forma urgente.
- **Cuándo usar:** Bugs que afectan usuarios finales y requieren despliegue inmediato.

### `chore/`
- **Propósito:** Tareas de mantenimiento, configuración, actualización de dependencias, etc.
- **Cuándo usar:** No afecta la lógica de negocio ni agrega funcionalidades. Se utiliza para cambios en el código que no son directamente visibles para los usuarios finales, como cambios en la configuración del proyecto o en herramientas de desarrollo.

### `test/`
- **Propósito:** Añadir o mejorar tests automatizados.
- **Cuándo usar:** Nuevos casos de prueba, refactorización de tests, cobertura.

### `docs/`
- **Propósito:** Cambios en documentación, manuales, guías, comentarios extensos.
- **Cuándo usar:** Actualización o creación de documentación.

### `ci/`
- **Propósito:** Cambios en scripts, workflows o configuración de CI/CD.
- **Cuándo usar:** Automatización, integración, despliegue.

### `style/`
- **Propósito:** Cambios de formato, estilo, convenciones de código (sin modificar lógica).
- **Cuándo usar:** Estandarización, linting, formateo.

### `perf/`
- **Propósito:** Mejoras de rendimiento y optimización.
- **Cuándo usar:** Cuando se mejora la velocidad, uso de memoria, etc.

### `release/`
- **Propósito:** Preparar versiones formales para producción.
- **Cuándo usar:** Antes de hacer un despliegue mayor o marcar un hito.

### `experiment/`
- **Propósito:** Probar ideas nuevas, prototipos o experimentos temporales.
- **Cuándo usar:** Validación rápida de hipótesis sin comprometer la rama principal.

### `merge/`
- **Propósito:** Facilitar fusiones complejas entre ramas de larga duración.
- **Cuándo usar:** Cuando se requiere una rama intermedia para resolver conflictos.

---

## Nombre de la rama `[branch-name]`

Toda rama que nace de un issue se llama:

```text
<branch-type>/#<issue-id>-<short-title>
```

- `<branch-type>`: el de la tabla de arriba, el mismo de la plantilla del issue.
- `<issue-id>`: el número que GitHub le asignó al issue. Antes de crear el issue no se conoce: el archivo local lo deja como `#` y se completa al crearlo.
- `<short-title>`: el `[short-title]` del issue, en kebab-case, sin acentos ni `ñ`, de 2 a 6 palabras.

Ejemplo: `docs/#101-docs-final-pass`.

---

## Recomendaciones

- Usa nombres descriptivos después del prefijo, separados por guiones medios.
- Mantén las ramas actualizadas y elimina las que ya no se usan.
- Documenta cualquier convención adicional que adopte tu equipo.
- Prefiere minúsculas y sin espacios.
