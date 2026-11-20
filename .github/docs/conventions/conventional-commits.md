# 📓 Conventional Commits `[commit-type]`

Los mensajes de commit siguen [Conventional Commits](https://www.conventionalcommits.org/es/v1.0.0/): un tipo, un scope opcional y una descripción breve. Es el estándar que leen las herramientas de changelog y versionado, y deja el historial legible sin conocer el proyecto.

Los mensajes no llevan emojis: el tipo ya identifica el cambio, y los emojis dependen de que la terminal o la herramienta los muestre.

---

## 1. Estructura

```text
<commit-type>(<scope>): <descripción>

<cuerpo opcional>
```

- `<commit-type>`: uno de los tipos de la tabla de abajo.
- `(<scope>)`: opcional; la parte del sistema que cambia (`auth`, `rbac`, `security`, `deps`, `site`...).
- `<descripción>`: en minúsculas, en imperativo o presente, sin punto final, de 72 caracteres o menos.
- Si el commit cierra el trabajo de un issue, termina con su número: `(#103)`.
- Un cambio que rompe compatibilidad agrega `!` después del tipo o scope (`feat(rbac)!: ...`) y lo explica en el cuerpo con `BREAKING CHANGE: ...`.

---

## 2. Tipos `[commit-type]`

| `[commit-type]` | `[branch-type]` habitual | Cuándo usarlo |
|-----------------|--------------------------|---------------|
| `feat` | `feature` | Funcionalidad nueva |
| `fix` | `fix`, `hotfix` | Corrección de un error, incluidas las de seguridad |
| `docs` | `docs` | Solo documentación |
| `style` | `style` | Formato, sin cambiar la lógica |
| `refactor` | `refactor` | Reestructura interna, sin cambiar el comportamiento |
| `perf` | `perf` | Mejora de rendimiento |
| `test` | `test` | Pruebas nuevas o corregidas |
| `build` | `chore` | Build, empaquetado y dependencias |
| `ci` | `ci` | Workflows y despliegue |
| `chore` | `chore` | Mantenimiento que no encaja en otro tipo |
| `revert` | cualquiera | Revierte un commit anterior |

No existe ningún otro tipo. Lo que antes eran tipos aparte se expresa con el scope: una corrección de seguridad es `fix(security): ...`, y una actualización de dependencias es `build(deps): ...`.

---

## 3. Ejemplos

```text
feat(rbac): rol administrador por RBAC y autorizacion por ruta resuelta (#103)
fix(security): regla de no escalada y endurecimiento de login (#103)
docs(site): README generados desde el sitio de documentacion (#103)
refactor(rate-limit): limitador nativo de ASP.NET Core (#103)
build(deps): actualiza EF Core a 10.0.10
ci: agrega el despliegue del sitio de documentacion
```

Con cuerpo, cada `-m` es un párrafo:

```bash
git commit -m "feat(auth): agrega login con Google (#57)" -m "Valida el id_token contra Google y vincula la cuenta por email verificado."
```

---

## 4. Relación con el tipo de rama

El `[branch-type]` describe el trabajo completo de un issue; el `[commit-type]`, cada cambio dentro de él. Una rama `feature/#57-google-login` puede llevar commits `feat`, `test` y `docs`.

---

> Referencia oficial: <https://www.conventionalcommits.org/es/v1.0.0/>
