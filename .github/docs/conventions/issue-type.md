# 🚧 Tipo de issue `[issue-type]`

📍 **Dónde vive:** organización `frugocorp` → *Issue types*. Se asigna en el issue, en el campo **Type** de la barra lateral.

El catálogo lo define la organización, no el repositorio: los mismos tres tipos sirven para todos los repos de `frugocorp`.

---

## Catálogo

| Valor | Cuándo usarlo | Ejemplo |
|-------|---------------|---------|
| `Feature` | Funcionalidad nueva o mejora visible para quien usa la librería. | "Agregar login con Google". |
| `Bug` | Algo no funciona como debería: error, falla de seguridad, resultado incorrecto. | "El refresh token no se revoca al cambiar la contraseña". |
| `Task` | Trabajo interno que no cambia lo que ve quien usa la librería: mantenimiento, refactor, pruebas, CI, documentación, estilo, rendimiento. | "Actualizar dependencias de NuGet". |

No existe ningún otro tipo. `Spike`, `Enhancement`, `Epic` o cualquier otro valor no se pueden asignar en GitHub.

---

## Tipo según la plantilla

Cada plantilla de `.github/ISSUE_TEMPLATE/` fija su tipo, y la plantilla se elige por el tipo de rama (ver [`branch-type.md`](branch-type.md)):

| Plantilla | `[branch-type]` | `[issue-type]` |
|-----------|-----------------|----------------|
| `feature_request.md` | `feature` | `Feature` |
| `fix_bug.md` | `fix` | `Bug` |
| `hotfix.md` | `hotfix` | `Bug` |
| `chore.md` | `chore` | `Task` |
| `refactor.md` | `refactor` | `Task` |
| `test.md` | `test` | `Task` |
| `docs.md` | `docs` | `Task` |
| `ci.md` | `ci` | `Task` |
| `style.md` | `style` | `Task` |
| `perf.md` | `perf` | `Task` |

---

## Campos que GitHub muestra según el tipo

Al elegir el tipo, GitHub muestra en el issue los campos de organización fijados a ese tipo:

| `[issue-type]` | Campos que muestra |
|----------------|--------------------|
| `Feature` | Priority, Effort, Start date, Target date |
| `Bug` | Priority, Effort |
| `Task` | Priority, Effort |

Las plantillas solo piden Priority y Effort (ver [`issue-priority.md`](issue-priority.md) e [`issue-effort.md`](issue-effort.md)). Las fechas no se usan en los archivos de issue: si hacen falta, se asignan directamente en GitHub (ver [`glossary.md`](../../glossary.md)).
